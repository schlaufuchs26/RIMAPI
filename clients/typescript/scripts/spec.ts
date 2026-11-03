// Shared spec handling for the client generator: canonical JSON so a snapshot
// diff names a route instead of a key order, and the two ways to obtain the
// spec (the pinned file, or a live game).

import { readFile } from "node:fs/promises";

export type JsonValue =
  | string
  | number
  | boolean
  | null
  | JsonValue[]
  | { [key: string]: JsonValue };

export interface Schema {
  $ref?: string;
  type?: string | string[];
  format?: string;
  enum?: JsonValue[];
  anyOf?: Schema[];
  oneOf?: Schema[];
  allOf?: Schema[];
  items?: Schema;
  properties?: Record<string, Schema>;
  required?: string[];
  additionalProperties?: boolean | Schema;
  description?: string;
  nullable?: boolean;
}

export interface Parameter {
  name: string;
  in: "path" | "query" | "header" | "cookie";
  required?: boolean;
  description?: string;
  schema?: Schema;
}

export interface RequestBody {
  required?: boolean;
  content?: Record<string, { schema?: Schema }>;
}

export interface Response {
  description?: string;
  content?: Record<string, { schema?: Schema }>;
}

export interface Operation {
  operationId: string;
  summary?: string;
  description?: string;
  tags?: string[];
  parameters?: Parameter[];
  requestBody?: RequestBody;
  responses?: Record<string, Response>;
}

export interface PathItem {
  get?: Operation;
  post?: Operation;
  put?: Operation;
  patch?: Operation;
  delete?: Operation;
  parameters?: Parameter[];
}

export interface Spec {
  openapi: string;
  info: { title: string; version: string; description?: string };
  servers?: Array<{ url: string }>;
  paths: Record<string, PathItem>;
  components?: { schemas?: Record<string, Schema> };
}

export const METHODS = ["get", "post", "put", "patch", "delete"] as const;
export type Method = (typeof METHODS)[number];

// Canonical form: object keys sorted at every depth, arrays keep their order.
// Two runs over the same spec therefore serialise to the same bytes, and a
// changed spec shows up as a route-level diff rather than as reordered keys.
export function canonicalize(value: JsonValue): JsonValue {
  if (Array.isArray(value)) return value.map(canonicalize);
  if (value !== null && typeof value === "object") {
    const out: { [key: string]: JsonValue } = {};
    for (const [key, item] of Object.entries(value).sort(([a], [b]) =>
      a < b ? -1 : a > b ? 1 : 0,
    )) {
      out[key] = canonicalize(item);
    }
    return out;
  }
  return value;
}

export function canonicalJson(value: JsonValue): string {
  return `${JSON.stringify(canonicalize(value), null, 2)}\n`;
}

export async function readSpecFile(path: string): Promise<Spec> {
  const text = await readFile(path, "utf8");
  // The file is our own snapshot, written from this parser's output; a cast at
  // this boundary is the one place where the shape cannot be checked by TS.
  return JSON.parse(text) as Spec;
}

export function specUrl(baseUrl: string): string {
  return new URL("/api/v1/docs?format=openapi-json", baseUrl).toString();
}

export async function fetchSpec(baseUrl: string): Promise<Spec> {
  const url = specUrl(baseUrl);
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`GET ${url} answered HTTP ${response.status}`);
  }
  return JSON.parse(await response.text()) as Spec;
}

export function refName(ref: string): string {
  const prefix = "#/components/schemas/";
  if (!ref.startsWith(prefix)) {
    throw new Error(`unsupported $ref: ${ref}`);
  }
  return ref.slice(prefix.length);
}
