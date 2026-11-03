// Hand-written half of the client: the envelope, the HTTP call, the refusals,
// the retry, the read-only guard and the one-agent-at-a-time claim. The
// generated half (src/generated/) only names routes and types.

import { randomUUID } from "node:crypto";
import { mkdir, open, readFile, stat, unlink } from "node:fs/promises";
import { join } from "node:path";

/** Any JSON value; the payload type of a route whose data schema is free-form. */
export type Json =
  | string
  | number
  | boolean
  | null
  | Json[]
  | { [key: string]: Json };

/**
 * The envelope every RIMAPI route answers with. `success: false` is a normal
 * HTTP 200 carrying the game's own refusal sentence in `errors`; the client
 * turns it into a RimApiRefusalError instead of handing back empty data.
 */
export interface ApiResult<T = Json> {
  success: boolean;
  data?: T;
  errors?: string[];
  warnings?: string[];
  timestamp: string;
  tick: number;
}

export type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export type QueryValue =
  | string
  | number
  | boolean
  | null
  | undefined
  | ReadonlyArray<string | number | boolean>;

export interface RetryOptions {
  /** Total attempts, including the first one. Default 3. */
  attempts?: number;
  /** Delay before the second attempt. Default 200 ms. */
  baseDelayMs?: number;
  /** Multiplier per further attempt. Default 2. */
  factor?: number;
  /** Cap for one delay. Default 2000 ms. */
  maxDelayMs?: number;
}

export interface RimApiOptions {
  /** The mod's listen address. Default http://localhost:8765. */
  baseUrl?: string;
  /** Sent as `Authorization: Bearer <token>`; the stock mod ignores it. */
  token?: string;
  headers?: Record<string, string>;
  /** Refuse every non-GET call before it reaches the game. */
  readOnly?: boolean;
  /** Per-attempt budget. Default 30000 ms. */
  timeoutMs?: number;
  retry?: RetryOptions;
  /** Test seam: the fetch implementation. */
  fetch?: typeof fetch;
  /** Test seam: the delay between attempts. */
  sleep?: (ms: number) => Promise<void>;
}

export class RimApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "RimApiError";
  }
}

/** A refusal: the mod answered success:false and named the reason itself. */
export class RimApiRefusalError<T = Json> extends RimApiError {
  readonly method: HttpMethod;
  readonly path: string;
  readonly status: number;
  readonly errors: string[];
  readonly warnings: string[];
  readonly tick: number;
  readonly envelope: ApiResult<T>;

  constructor(method: HttpMethod, path: string, status: number, envelope: ApiResult<T>) {
    const errors = envelope.errors ?? [];
    const reason = errors.length > 0 ? errors.join("; ") : "the game refused the call";
    super(`RIMAPI refused ${method} ${path}: ${reason}`);
    this.name = "RimApiRefusalError";
    this.method = method;
    this.path = path;
    this.status = status;
    this.errors = errors;
    this.warnings = envelope.warnings ?? [];
    this.tick = envelope.tick;
    this.envelope = envelope;
  }
}

/** A non-2xx answer that was not the API envelope (or a broken one). */
export class RimApiHttpError extends RimApiError {
  readonly method: HttpMethod;
  readonly path: string;
  readonly status: number;
  readonly body: string;

  constructor(method: HttpMethod, path: string, status: number, body: string) {
    const cut = body.length > 300 ? `${body.slice(0, 300)}…` : body;
    super(`RIMAPI answered HTTP ${status} to ${method} ${path}: ${cut}`);
    this.name = "RimApiHttpError";
    this.method = method;
    this.path = path;
    this.status = status;
    this.body = body;
  }
}

/** The game did not answer within the attempt budget; retried by default. */
export class RimApiTimeoutError extends RimApiError {
  constructor(method: HttpMethod, path: string, timeoutMs: number) {
    super(`RIMAPI did not answer ${method} ${path} within ${timeoutMs} ms`);
    this.name = "RimApiTimeoutError";
  }
}

/** The connection failed; retried by default (the game may still be loading). */
export class RimApiNetworkError extends RimApiError {
  readonly reason: unknown;

  constructor(method: HttpMethod, path: string, cause: unknown) {
    super(`RIMAPI is unreachable for ${method} ${path}: ${describe(cause)}`);
    this.name = "RimApiNetworkError";
    this.reason = cause;
  }
}

/** A write was attempted on a read-only client; no request was sent. */
export class RimApiReadOnlyError extends RimApiError {
  readonly method: HttpMethod;
  readonly path: string;

  constructor(method: HttpMethod, path: string) {
    super(`read-only client refuses ${method} ${path}`);
    this.name = "RimApiReadOnlyError";
    this.method = method;
    this.path = path;
  }
}

interface CallOptions {
  query?: Record<string, QueryValue>;
  body?: unknown;
}

const DEFAULT_BASE_URL = "http://localhost:8765";
const DEFAULT_TIMEOUT_MS = 30_000;
const DEFAULT_RETRY: Required<RetryOptions> = {
  attempts: 3,
  baseDelayMs: 200,
  factor: 2,
  maxDelayMs: 2_000,
};

/** The retryable statuses: a game that is loading, not a refusal. */
const RETRYABLE_STATUS = new Set([429, 500, 502, 503, 504]);

export class RimApiBase {
  readonly baseUrl: string;
  readonly readOnly: boolean;

  private readonly token: string | undefined;
  private readonly headers: Record<string, string>;
  private readonly timeoutMs: number;
  private readonly retry: Required<RetryOptions>;
  private readonly fetchImpl: typeof fetch;
  private readonly sleep: (ms: number) => Promise<void>;

  constructor(options: RimApiOptions = {}) {
    this.baseUrl = (options.baseUrl ?? DEFAULT_BASE_URL).replace(/\/+$/, "");
    this.readOnly = options.readOnly ?? false;
    this.token = options.token;
    this.headers = options.headers ?? {};
    this.timeoutMs = options.timeoutMs ?? DEFAULT_TIMEOUT_MS;
    this.retry = { ...DEFAULT_RETRY, ...options.retry };
    this.fetchImpl = options.fetch ?? fetch;
    this.sleep =
      options.sleep ?? ((ms: number) => new Promise((resolve) => setTimeout(resolve, ms)));
  }

  protected async call<T = Json>(
    method: HttpMethod,
    path: string,
    options: CallOptions = {},
  ): Promise<ApiResult<T>> {
    if (this.readOnly && method !== "GET") {
      throw new RimApiReadOnlyError(method, path);
    }

    const url = this.buildUrl(path, options.query);
    const init = this.buildInit(method, options.body);

    let attempt = 0;
    for (;;) {
      try {
        return await this.attempt<T>(method, path, url, init);
      } catch (error) {
        if (attempt >= this.retry.attempts - 1 || !isRetryable(error)) throw error;
        const delay = Math.min(
          this.retry.baseDelayMs * this.retry.factor ** attempt,
          this.retry.maxDelayMs,
        );
        await this.sleep(delay);
        attempt += 1;
      }
    }
  }

  private buildUrl(path: string, query: Record<string, QueryValue> | undefined): string {
    const url = new URL(`${this.baseUrl}${path}`);
    for (const [key, value] of Object.entries(query ?? {})) {
      if (value === undefined || value === null) continue;
      if (Array.isArray(value)) {
        for (const item of value) url.searchParams.append(key, String(item));
      } else {
        url.searchParams.set(key, String(value));
      }
    }
    return url.toString();
  }

  private buildInit(method: HttpMethod, body: unknown): RequestInit {
    const headers: Record<string, string> = {
      Accept: "application/json",
      ...this.headers,
    };
    if (this.token) headers.Authorization = `Bearer ${this.token}`;

    let payload: string | undefined;
    if (body !== undefined) {
      headers["Content-Type"] = "application/json";
      payload = JSON.stringify(body);
    }
    return { method, headers, body: payload };
  }

  private async attempt<T>(
    method: HttpMethod,
    path: string,
    url: string,
    init: RequestInit,
  ): Promise<ApiResult<T>> {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), this.timeoutMs);

    try {
      let response: Response;
      try {
        response = await this.fetchImpl(url, { ...init, signal: controller.signal });
      } catch (error) {
        if (controller.signal.aborted) {
          throw new RimApiTimeoutError(method, path, this.timeoutMs);
        }
        throw new RimApiNetworkError(method, path, error);
      }

      const text = await response.text();
      const parsed = parseJson(text);

      if (isEnvelope(parsed)) {
        if (!parsed.success) {
          throw new RimApiRefusalError(method, path, response.status, parsed as ApiResult<T>);
        }
        if (!response.ok) {
          throw new RimApiHttpError(method, path, response.status, text);
        }
        return parsed as ApiResult<T>;
      }

      if (!response.ok) {
        throw new RimApiHttpError(method, path, response.status, text);
      }
      throw new RimApiHttpError(
        method,
        path,
        response.status,
        `answer is not the API envelope: ${text}`,
      );
    } finally {
      clearTimeout(timer);
    }
  }
}

function parseJson(text: string): unknown {
  if (text.length === 0) return undefined;
  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}

function isEnvelope(value: unknown): value is ApiResult<Json> {
  return (
    typeof value === "object" &&
    value !== null &&
    typeof (value as { success?: unknown }).success === "boolean"
  );
}

function isRetryable(error: unknown): boolean {
  if (error instanceof RimApiTimeoutError || error instanceof RimApiNetworkError) return true;
  if (error instanceof RimApiHttpError) return RETRYABLE_STATUS.has(error.status);
  return false;
}

function describe(value: unknown): string {
  if (value instanceof Error) return value.message;
  return String(value);
}

export interface ClaimOptions {
  /** Directory that holds the lease file; created when missing. */
  dir: string;
  /** Lease name, default "rimapi". */
  name?: string;
  /** Who holds it, default `pid:<pid>`; name the agent. */
  holder?: string;
  /** How long a lease stays valid without release. Default 120000 ms. */
  ttlMs?: number;
  /** Test seam: the clock. */
  now?: () => number;
}

export class RimApiClaimBusyError extends RimApiError {
  readonly path: string;
  readonly holder: string;
  readonly expiresAt: number;

  constructor(path: string, holder: string, expiresAt: number) {
    super(`RIMAPI is claimed by ${holder} until ${new Date(expiresAt).toISOString()}`);
    this.name = "RimApiClaimBusyError";
    this.path = path;
    this.holder = holder;
    this.expiresAt = expiresAt;
  }
}

interface ClaimRecord {
  holder: string;
  token: string;
  pid: number;
  expiresAt: number;
}

/**
 * Run `work` while holding a lease file, so two agents do not drive one game
 * at the same time. A second caller is refused with the holder's name and the
 * lease's end; an expired lease (a crashed holder) is taken over. The lease is
 * released on the way out, whether the work succeeded or threw.
 */
export async function withClaim<T>(
  options: ClaimOptions,
  work: () => Promise<T>,
): Promise<T> {
  const name = options.name ?? "rimapi";
  const path = join(options.dir, `${name}.claim`);
  const now = options.now ?? Date.now;
  const ttlMs = options.ttlMs ?? 120_000;
  const holder = options.holder ?? `pid:${process.pid}`;
  const token = randomUUID();

  await mkdir(options.dir, { recursive: true });

  for (let attempt = 0; attempt < 2; attempt += 1) {
    let handle: Awaited<ReturnType<typeof open>>;
    try {
      handle = await open(path, "wx");
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code !== "EEXIST") throw error;
      const current = await readClaim(path);
      if (current && current.expiresAt > now()) {
        throw new RimApiClaimBusyError(path, current.holder, current.expiresAt);
      }
      if (!current) {
        // The holder created the file but has not written it yet. Its age is
        // the only evidence; a fresh one is busy, an old one was abandoned.
        const age = await claimAge(path, now());
        if (age !== undefined && age < ttlMs) {
          throw new RimApiClaimBusyError(path, "an unreadable claim", now() + (ttlMs - age));
        }
      }
      await unlink(path).catch(() => undefined);
      continue;
    }

    try {
      const record: ClaimRecord = { holder, token, pid: process.pid, expiresAt: now() + ttlMs };
      await handle.writeFile(JSON.stringify(record));
    } finally {
      await handle.close();
    }

    try {
      return await work();
    } finally {
      const current = await readClaim(path);
      if (current?.token === token) await unlink(path).catch(() => undefined);
    }
  }

  throw new RimApiError(`claim ${path}: another process took it twice`);
}

async function readClaim(path: string): Promise<ClaimRecord | undefined> {
  try {
    const parsed = JSON.parse(await readFile(path, "utf8")) as Partial<ClaimRecord>;
    if (
      typeof parsed.holder === "string" &&
      typeof parsed.token === "string" &&
      typeof parsed.expiresAt === "number"
    ) {
      return parsed as ClaimRecord;
    }
    return undefined;
  } catch {
    return undefined;
  }
}

async function claimAge(path: string, now: number): Promise<number | undefined> {
  try {
    const info = await stat(path);
    return now - info.mtimeMs;
  } catch {
    return undefined;
  }
}
