import { describe, expect, test } from "bun:test";
import { readFile } from "node:fs/promises";
import { resolve } from "node:path";
import { generate, type Generated } from "../scripts/generate";
import { METHODS, readSpecFile } from "../scripts/spec";

const ROOT = resolve(import.meta.dir, "..");
const SPEC_PATH = resolve(ROOT, "spec/rimapi-openapi.json");

const spec = await readSpecFile(SPEC_PATH);
const first = generate(spec);
const second = generate(spec);

const operations = Object.values(spec.paths).reduce(
  (count, item) => count + METHODS.filter((method) => item[method] !== undefined).length,
  0,
);

function camelCase(value: string): string {
  const words = value
    .replace(/[^A-Za-z0-9]+/g, " ")
    .trim()
    .split(/\s+/)
    .filter((word) => word.length > 0);
  const [head, ...rest] = words;
  return (
    head!.charAt(0).toLowerCase() +
    head!.slice(1) +
    rest.map((word) => word.charAt(0).toUpperCase() + word.slice(1)).join("")
  );
}

async function checkedIn(path: string): Promise<string> {
  return readFile(resolve(ROOT, path), "utf8");
}

describe("determinism", () => {
  test("two runs over one snapshot produce identical bytes", () => {
    expect(second).toEqual(first);
  });

  test("the checked-in files are what the generator writes now", async () => {
    const pairs: Array<[string, string]> = [
      ["src/generated/types.ts", first.types],
      ["src/generated/client.ts", first.client],
      ["spec/thin.json", first.thin],
    ];
    for (const [path, expected] of pairs) {
      expect({ path, text: await checkedIn(path) }).toEqual({ path, text: expected });
    }
  });

  test("the snapshot is canonical, so a refresh diffs per route", async () => {
    const text = await checkedIn("spec/rimapi-openapi.json");
    expect(text).toBe(`${JSON.stringify(JSON.parse(text), null, 2)}\n`);
  });
});

describe("coverage", () => {
  test("every operation gets a method", () => {
    const methods = first.client
      .split("\n")
      .filter((line) => /^ {2}[A-Za-z_$][\w$]*\(.*\): Promise<ApiResult<.+>> \{$/.test(line));
    expect(methods).toHaveLength(operations);
    expect(operations).toBe(299);
  });

  test("every operationId is reachable under its camelCase name", () => {
    for (const item of Object.values(spec.paths)) {
      for (const method of METHODS) {
        const operation = item[method];
        if (!operation) continue;
        const name = camelCase(operation.operationId);
        expect(first.client).toContain(`  ${name}(`);
      }
    }
  });

  test("no route falls back to an untyped payload", () => {
    const thin = JSON.parse(first.thin) as {
      counts: { operations: number; typedRoutes: number; untypedRoutes: number; freeFormFields: number };
      routes: unknown[];
      fields: Array<{ schema: string; property: string; reason: string }>;
    };
    expect(thin.counts).toEqual({
      operations,
      typedRoutes: operations,
      untypedRoutes: 0,
      freeFormFields: 0,
    });
    expect(thin.routes).toHaveLength(0);
    expect(thin.fields).toEqual([]);
  });

  test("the public surface has no any", () => {
    for (const text of [first.client, first.types]) {
      expect(text).not.toMatch(/:\s*any\b/);
      expect(text).not.toMatch(/<any>/);
      expect(text).not.toMatch(/\bas any\b/);
      expect(text).not.toMatch(/\bany\[\]/);
    }
  });

  test("the response payload is the schema the route declares", () => {
    expect(first.client).toContain(
      "  getVersion(): Promise<ApiResult<Types.VersionDto>> {",
    );
    expect(first.client).toContain(
      "  getGameState(): Promise<ApiResult<Types.GameStateDto>> {",
    );
    expect(first.client).toContain(
      "  setGameSpeed(params: SetGameSpeedParams): Promise<ApiResult<null>> {",
    );
  });
});

describe("types", () => {
  test("a body is typed by its component, not by a hand-written table", () => {
    expect(first.client).toContain("body: Types.CreateBillRequest");
    expect(first.types).toContain("export interface CreateBillRequest {");
  });

  test("a nullable property is a union with null", () => {
    expect(first.types).toMatch(/repeat_count\?: number \| null;/);
  });

  test("a map property is a Record, not an index signature of any", () => {
    expect(first.types).toContain("Record<string, number>");
  });
});
