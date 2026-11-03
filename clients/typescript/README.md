# rimapi-client

Type-safe TypeScript client for [RIMAPI](https://github.com/schlaufuchs26/RIMAPI),
generated from the OpenAPI 3.1 document the mod serves itself. One method per
operation, request and response types from the document's schemas, no
hand-kept route table.

The document carries 298 operations on 293 paths and 580 schemas. Every one of
them names the type it answers with (`ApiResultOfPawnDto`,
`ApiResultNoData`, …), so the generated method's return type is
`ApiResult<PawnDto>` rather than an untyped envelope. One schema property is
still free-form (`CacheStatusDto.statistics`, an object the game fills
loosely); it is typed `Json` and listed in `spec/thin.json` instead of being
guessed.

## Use

```ts
import { RimApiClient, RimApiRefusalError, withClaim } from "rimapi-client";

const game = new RimApiClient({ baseUrl: "http://localhost:8765" });

const version = await game.getVersion();        // ApiResult<VersionDto>
console.log(version.data?.rim_world_version);

await game.setGameSpeed({ speed: 1 });           // POST, query param typed
const colonists = await game.getColonists();     // ApiResult<PawnDto[]>
```

A refusal is not an empty answer: the mod reports `success: false` with its own
sentence, and the client throws it.

```ts
try {
  await game.getMapThings({ map_id: 999 });
} catch (error) {
  if (error instanceof RimApiRefusalError) {
    console.log(error.errors);   // ["Map not found: 999"]
  }
}
```

`readOnly: true` refuses every non-GET call before it reaches the game, and
`withClaim` keeps two agents off one game:

```ts
const readOnly = new RimApiClient({ readOnly: true });
await readOnly.setGameSpeed({ speed: 1 });  // throws RimApiReadOnlyError

await withClaim({ dir: "/tmp/rimapi-leases", holder: "my-agent" }, async () => {
  await game.setGameSpeed({ speed: 1 });
});
```

Retries cover a game that is loading or briefly not answering: network errors,
timeouts, and HTTP 429/5xx without a refusal envelope are retried with
exponential backoff (3 attempts, 200 ms base by default). A refusal is never
retried.

## Regenerate

```sh
bun install
bun run generate:rimworld-client          # spec/rimapi-openapi.json → src/generated/
bun run generate:rimworld-client --check  # fail when the tree is stale
```

`bun run generate:rimworld-client --url http://localhost:8767` reads a live
game instead of the snapshot. Do not edit `src/generated/*` by hand; the next
run overwrites it.

The output is deterministic: routes sorted by path and method, schema
properties by name, no timestamp and no machine name in the header. Running
the generator twice, on this machine or another, produces the same bytes, and
`test/generate.test.ts` fails when the checked-in files are not that output.

## Refresh the snapshot

The snapshot is pinned so the client can be built without a running game:

```sh
bun run snapshot:rimworld-spec --url http://localhost:8767
bun run generate:rimworld-client
```

`spec/pin.json` records when and where the snapshot was taken and its sha256.

When the live document and the snapshot disagree, `test/snapshot.test.ts`
fails and names the routes that differ (`+`, `-`, `~` per method and path).
Then:

1. Refresh the snapshot from a game that runs the build you mean to support.
2. Regenerate the client.
3. Read the diff: an added or changed route is a real API change, so check it
   against the mod's `[ResponseBody(typeof(T))]` declaration rather than
   accepting the generated type blindly.
4. Commit the snapshot, the pin and the generated files together.

A route whose document entry is too thin to type is written as `Json` and
listed in `spec/thin.json` (with its reasons) instead of being invented. Today
that list has no route; it holds one free-form field.

## Test

```sh
bun test                                   # unit + generator + determinism
bun run typecheck                          # tsc over generated code and call sites

RIMAPI_URL=http://localhost:8767 bun test test/snapshot.test.ts
RIMAPI_CONTRACT_URL=http://localhost:8767 bun test test/contract.test.ts
```

The live checks skip when their variable is unset, so a unit run never needs a
game. `test/contract.test.ts` reads version, game state, colonists, maps and
the resources summary, sets the game speed and reads the effect back, and
provokes a refusal to prove the error carries the game's sentence.
`test/typecheck.ts` holds call sites that must not compile (a string where a
number belongs, a missing path parameter, an unknown route); `tsc` fails if one
of those errors disappears.

## Layout

```
spec/rimapi-openapi.json   pinned document (canonical JSON)
spec/pin.json              where and when it was taken, sha256
spec/thin.json             routes/fields the document cannot type (generated)
scripts/spec.ts            canonical JSON, file and URL loading
scripts/generate.ts        the generator
scripts/snapshot.ts        fetch the live document into the snapshot
src/runtime.ts             envelope, HTTP call, errors, retry, read-only, claim
src/generated/types.ts     the document's schemas (generated)
src/generated/client.ts    one method per operation (generated)
```
