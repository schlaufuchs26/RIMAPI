import { describe, expect, test } from "bun:test";
import { createHash } from "node:crypto";
import { readFile } from "node:fs/promises";
import { resolve } from "node:path";
import {
  METHODS,
  canonicalJson,
  canonicalize,
  fetchSpec,
  readSpecFile,
  type JsonValue,
  type Method,
  type Spec,
} from "../scripts/spec";

const ROOT = resolve(import.meta.dir, "..");
const SNAPSHOT = resolve(ROOT, "spec/rimapi-openapi.json");

interface Pin {
  taken: string;
  source: string;
  openapi: string;
  version: string;
  paths: number;
  operations: number;
  sha256: string;
}

const snapshot = await readSpecFile(SNAPSHOT);
const pin = JSON.parse(await readFile(resolve(ROOT, "spec/pin.json"), "utf8")) as Pin;

// A configured game that does not answer is a failure, not a skip: the run
// asked for a comparison and got none. Without RIMAPI_URL the test stays quiet,
// because a unit run must not need a game (engineering practices: unit tests
// never dial a live service).
const url = process.env.RIMAPI_URL;
let live: Spec | undefined;
let liveError: string | undefined;
if (url) {
  try {
    live = await fetchSpec(url);
  } catch (error) {
    liveError = error instanceof Error ? error.message : String(error);
  }
}

function operationKey(method: Method, path: string): string {
  return `${method.toUpperCase()} ${path}`;
}

function routeDiff(left: Spec, right: Spec): string[] {
  const diff: string[] = [];
  const paths = new Set([...Object.keys(left.paths), ...Object.keys(right.paths)]);
  for (const path of [...paths].sort()) {
    for (const method of METHODS) {
      const a = left.paths[path]?.[method];
      const b = right.paths[path]?.[method];
      const key = operationKey(method, path);
      if (!a && b) diff.push(`+ ${key}`);
      else if (a && !b) diff.push(`- ${key}`);
      else if (a && b) {
        const leftText = JSON.stringify(canonicalize(a as unknown as JsonValue));
        const rightText = JSON.stringify(canonicalize(b as unknown as JsonValue));
        if (leftText !== rightText) diff.push(`~ ${key}`);
      }
    }
  }
  return diff;
}

describe("the pinned snapshot", () => {
  test("is canonical and matches its pin", async () => {
    const text = await readFile(SNAPSHOT, "utf8");
    expect(text).toBe(canonicalJson(JSON.parse(text) as JsonValue));
    expect(createHash("sha256").update(text).digest("hex")).toBe(pin.sha256);
    expect(pin.paths).toBe(Object.keys(snapshot.paths).length);
  });
});

describe("the live game", () => {
  test.skipIf(!url)("answers the configured URL", () => {
    expect(liveError).toBeUndefined();
    expect(live).toBeDefined();
  });

  test.skipIf(!url || live === undefined)(
    "agrees with the snapshot; a disagreement names the routes",
    () => {
      const diff = routeDiff(snapshot, live!);
      if (diff.length > 0) {
        throw new Error(
          `${diff.length} route(s) differ from spec/rimapi-openapi.json ` +
            `(pinned ${pin.taken} from ${pin.source}):\n${diff.join("\n")}\n` +
            "refresh with: bun run snapshot:rimworld-spec --url <game>",
        );
      }
      expect(diff).toEqual([]);
    },
  );
});
