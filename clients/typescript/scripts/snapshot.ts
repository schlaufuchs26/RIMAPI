// Pin the mod's OpenAPI document as spec/rimapi-openapi.json.
//
//   bun scripts/snapshot.ts [--url http://localhost:8765] [--out spec/rimapi-openapi.json]
//
// The document is stored canonically (sorted keys, two-space indent, trailing
// newline), so a refreshed snapshot diffs per route and a re-run on an
// unchanged game writes identical bytes. spec/pin.json records where and when
// it was taken plus its sha256, which the live check names when the two drift
// apart. Read it from the debug slot that runs the build you want the client
// for; the playing colony is the default only because that is the port a
// person has open.

import { createHash } from "node:crypto";
import { mkdir, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { canonicalJson, fetchSpec, specUrl } from "./spec";

const ROOT = resolve(import.meta.dir, "..");

function argument(name: string, fallback: string): string {
  const index = process.argv.indexOf(name);
  if (index < 0) return fallback;
  const value = process.argv[index + 1];
  if (value === undefined) throw new Error(`${name} needs a value`);
  return value;
}

const url = argument("--url", process.env.RIMAPI_URL ?? "http://localhost:8765");
const out = resolve(ROOT, argument("--out", "spec/rimapi-openapi.json"));
const pinPath = resolve(ROOT, argument("--pin", "spec/pin.json"));

const spec = await fetchSpec(url);
const operations = Object.values(spec.paths).reduce((count, item) => {
  return (
    count +
    (["get", "post", "put", "patch", "delete"] as const).filter(
      (method) => item[method] !== undefined,
    ).length
  );
}, 0);

const text = canonicalJson(spec as never);
const sha256 = createHash("sha256").update(text).digest("hex");

const pin = {
  taken: new Date().toISOString().slice(0, 10),
  source: specUrl(url),
  openapi: spec.openapi,
  version: spec.info.version,
  paths: Object.keys(spec.paths).length,
  operations,
  sha256,
};

await mkdir(dirname(out), { recursive: true });
await writeFile(out, text);
await writeFile(pinPath, canonicalJson(pin as never));
console.log(
  `${pin.paths} paths, ${operations} operations → ${out} (${text.length} bytes, sha256 ${sha256.slice(0, 12)})`,
);
