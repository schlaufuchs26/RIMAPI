import { afterEach, describe, expect, test } from "bun:test";
import { mkdtemp, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import {
  RimApiBase,
  RimApiClaimBusyError,
  RimApiHttpError,
  RimApiNetworkError,
  RimApiReadOnlyError,
  RimApiRefusalError,
  RimApiTimeoutError,
  withClaim,
  type ApiResult,
  type Json,
  type QueryValue,
} from "../src/runtime";

interface Call {
  url: string;
  init: RequestInit;
}

function fakeFetch(
  handler: (call: Call) => Response | Promise<Response>,
): { fetch: typeof fetch; calls: Call[] } {
  const calls: Call[] = [];
  const impl = (async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const call = { url: String(input), init: init ?? {} };
    calls.push(call);
    return handler(call);
  }) as typeof fetch;
  return { fetch: impl, calls };
}

function envelope<T>(data: T, extra: Partial<ApiResult<T>> = {}): string {
  return JSON.stringify({
    success: true,
    data,
    errors: [],
    warnings: [],
    timestamp: "2026-11-03T00:00:00Z",
    tick: 7,
    ...extra,
  });
}

class TestClient extends RimApiBase {
  get(path: string, query?: Record<string, QueryValue>): Promise<ApiResult<Json>> {
    return this.call<Json>("GET", path, { query });
  }

  post(path: string, body?: unknown): Promise<ApiResult<Json>> {
    return this.call<Json>("POST", path, { body });
  }
}

describe("the request", () => {
  test("joins the base URL, encodes query values, and skips empty ones", async () => {
    const fake = fakeFetch(() => new Response(envelope({ ok: true })));
    const client = new TestClient({ baseUrl: "http://localhost:9999/", fetch: fake.fetch });

    await client.get("/api/v1/map/zones", {
      map_id: 0,
      name: "a b",
      flag: true,
      missing: undefined,
      nothing: null,
    });

    expect(fake.calls).toHaveLength(1);
    expect(fake.calls[0]!.url).toBe("http://localhost:9999/api/v1/map/zones?map_id=0&name=a+b&flag=true");
  });

  test("sends the bearer token, its own headers and a JSON body", async () => {
    const fake = fakeFetch(() => new Response(envelope(null)));
    const client = new TestClient({
      baseUrl: "http://localhost:9999",
      token: "secret",
      headers: { "X-Test": "1" },
      fetch: fake.fetch,
    });

    await client.post("/api/v1/bill", { recipe: "MealSimple" });

    const init = fake.calls[0]!.init;
    expect(init.method).toBe("POST");
    expect(init.headers).toMatchObject({
      Accept: "application/json",
      "Content-Type": "application/json",
      Authorization: "Bearer secret",
      "X-Test": "1",
    });
    expect(init.body).toBe(JSON.stringify({ recipe: "MealSimple" }));
  });

  test("returns the envelope with its payload", async () => {
    const fake = fakeFetch(() => new Response(envelope({ name: "Fuchsbau" })));
    const client = new TestClient({ fetch: fake.fetch });

    const answer = await client.get("/api/v1/version");

    expect(answer.success).toBe(true);
    expect(answer.data).toEqual({ name: "Fuchsbau" });
    expect(answer.tick).toBe(7);
  });
});

describe("refusals", () => {
  test("success:false becomes a refusal error carrying the game's sentence", async () => {
    const fake = fakeFetch(
      () =>
        new Response(
          JSON.stringify({
            success: false,
            errors: ["Map not found: 7"],
            warnings: ["nothing was read"],
            timestamp: "2026-11-03T00:00:00Z",
            tick: 12,
          }),
        ),
    );
    const client = new TestClient({ fetch: fake.fetch });

    const error = await client.get("/api/v1/map/things?map_id=7").catch((e: unknown) => e);

    expect(error).toBeInstanceOf(RimApiRefusalError);
    const refusal = error as RimApiRefusalError;
    expect(refusal.errors).toEqual(["Map not found: 7"]);
    expect(refusal.warnings).toEqual(["nothing was read"]);
    expect(refusal.tick).toBe(12);
    expect(refusal.message).toContain("Map not found: 7");
  });

  test("a 500 carrying a refusal envelope is not retried", async () => {
    const fake = fakeFetch(
      () =>
        new Response(
          JSON.stringify({
            success: false,
            errors: ["the game is not loaded"],
            timestamp: "2026-11-03T00:00:00Z",
            tick: 0,
          }),
          { status: 500 },
        ),
    );
    const client = new TestClient({ fetch: fake.fetch, sleep: async () => undefined });

    await expect(client.get("/api/v1/game/state")).rejects.toBeInstanceOf(RimApiRefusalError);
    expect(fake.calls).toHaveLength(1);
  });

  test("a non-envelope answer is an HTTP error", async () => {
    const fake = fakeFetch(() => new Response("not json", { status: 404 }));
    const client = new TestClient({ fetch: fake.fetch });

    const error = await client.get("/api/v1/nope").catch((e: unknown) => e);

    expect(error).toBeInstanceOf(RimApiHttpError);
    expect((error as RimApiHttpError).status).toBe(404);
  });
});

describe("retry", () => {
  test("retries a 503 without an envelope and returns the later answer", async () => {
    let calls = 0;
    const fake = fakeFetch(() => {
      calls += 1;
      if (calls < 3) return new Response("starting up", { status: 503 });
      return new Response(envelope({ ok: true }));
    });
    const delays: number[] = [];
    const client = new TestClient({
      fetch: fake.fetch,
      retry: { attempts: 3, baseDelayMs: 10, factor: 2 },
      sleep: async (ms) => {
        delays.push(ms);
      },
    });

    const answer = await client.get("/api/v1/version");

    expect(answer.data).toEqual({ ok: true });
    expect(fake.calls).toHaveLength(3);
    expect(delays).toEqual([10, 20]);
  });

  test("retries a network error and gives up after the attempt budget", async () => {
    const fake = fakeFetch(() => {
      throw new Error("connect ECONNREFUSED");
    });
    const client = new TestClient({
      fetch: fake.fetch,
      retry: { attempts: 2, baseDelayMs: 1 },
      sleep: async () => undefined,
    });

    await expect(client.get("/api/v1/version")).rejects.toBeInstanceOf(RimApiNetworkError);
    expect(fake.calls).toHaveLength(2);
  });

  test("a timeout is retried and reported once the budget is spent", async () => {
    const fake = fakeFetch(
      (call) =>
        new Promise<Response>((_resolve, reject) => {
          call.init.signal?.addEventListener("abort", () => reject(new Error("aborted")));
        }),
    );
    const client = new TestClient({
      fetch: fake.fetch,
      timeoutMs: 5,
      retry: { attempts: 2, baseDelayMs: 1 },
      sleep: async () => undefined,
    });

    await expect(client.get("/api/v1/version")).rejects.toBeInstanceOf(RimApiTimeoutError);
    expect(fake.calls).toHaveLength(2);
  });
});

describe("read-only", () => {
  test("refuses a write without sending it", async () => {
    const fake = fakeFetch(() => new Response(envelope(null)));
    const client = new TestClient({ fetch: fake.fetch, readOnly: true });

    await expect(client.post("/api/v1/game/speed?speed=1")).rejects.toBeInstanceOf(
      RimApiReadOnlyError,
    );
    expect(fake.calls).toHaveLength(0);
  });

  test("still allows reads", async () => {
    const fake = fakeFetch(() => new Response(envelope({ ok: true })));
    const client = new TestClient({ fetch: fake.fetch, readOnly: true });

    await expect(client.get("/api/v1/version")).resolves.toMatchObject({ success: true });
  });
});

describe("withClaim", () => {
  const dirs: string[] = [];

  afterEach(async () => {
    for (const dir of dirs.splice(0)) await rm(dir, { recursive: true, force: true });
  });

  async function claimDir(): Promise<string> {
    const dir = await mkdtemp(join(tmpdir(), "rimapi-claim-"));
    dirs.push(dir);
    return dir;
  }

  test("holds the lease, refuses a second holder, and releases on the way out", async () => {
    const dir = await claimDir();
    const events: string[] = [];

    await withClaim({ dir, holder: "agent-a" }, async () => {
      events.push("a holds");
      const second = await withClaim({ dir, holder: "agent-b" }, async () => "unreachable").catch(
        (error: unknown) => error,
      );
      expect(second).toBeInstanceOf(RimApiClaimBusyError);
      expect((second as RimApiClaimBusyError).holder).toBe("agent-a");
    });

    await expect(readFile(join(dir, "rimapi.claim"), "utf8")).rejects.toThrow();
    await withClaim({ dir, holder: "agent-b" }, async () => {
      events.push("b holds after release");
    });
    expect(events).toEqual(["a holds", "b holds after release"]);
  });

  test("takes over a lease whose holder crashed", async () => {
    const dir = await claimDir();
    const now = Date.now();
    await writeFile(
      join(dir, "rimapi.claim"),
      JSON.stringify({ holder: "dead", token: "old", pid: 1, expiresAt: now - 1 }),
    );

    const result = await withClaim({ dir, holder: "agent-b", now: () => now }, async () => "ran");

    expect(result).toBe("ran");
  });

  test("keeps a lease that is still live even with a fake clock", async () => {
    const dir = await claimDir();
    const now = 1_000_000;
    await writeFile(
      join(dir, "rimapi.claim"),
      JSON.stringify({ holder: "live", token: "t", pid: 1, expiresAt: now + 5_000 }),
    );

    const error = await withClaim({ dir, holder: "agent-b", now: () => now }, async () => "ran").catch(
      (e: unknown) => e,
    );

    expect(error).toBeInstanceOf(RimApiClaimBusyError);
    expect((error as RimApiClaimBusyError).holder).toBe("live");
  });

  test("releases the lease when the work throws", async () => {
    const dir = await claimDir();

    await expect(
      withClaim({ dir, holder: "agent-a" }, async () => {
        throw new Error("boom");
      }),
    ).rejects.toThrow("boom");

    const result = await withClaim({ dir, holder: "agent-b" }, async () => "next");
    expect(result).toBe("next");
  });
});
