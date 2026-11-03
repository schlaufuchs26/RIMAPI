// Compiled by `bun run typecheck`, not run as a test: these call sites are the
// proof that a wrong parameter fails the build. A @ts-expect-error line turns
// red when the error it expects stops happening, so a loosened type cannot
// pass unnoticed.

import { RimApiClient, type ApiResult } from "../src/index";

const client = new RimApiClient({ baseUrl: "http://localhost:8765", token: "optional" });

async function positive(): Promise<void> {
  const version: ApiResult<{ rim_world_version?: string }> = await client.getVersion();
  version.data?.rim_world_version?.toUpperCase();

  await client.getGameState();
  await client.setGameSpeed({ speed: 1 });
  await client.changeZoom({ zoom: 2 });
  await client.getResourcesSummary({ map_id: 0 });
  await client.getExtensionDocumentation({ extensionId: "my-extension" });
  await client.createBill({ building_id: 1 }, { recipe_def_name: "Make_Steel" });
  await client.getColonists();
}

async function negative(): Promise<void> {
  // @ts-expect-error a query parameter is a number, not a string
  await client.changeZoom({ zoom: "2" });

  // @ts-expect-error the path parameter is required
  await client.getExtensionDocumentation({});

  // @ts-expect-error getVersion takes no argument
  await client.getVersion(1);

  // @ts-expect-error the body field is a string
  await client.createBill({ building_id: 1 }, { recipe_def_name: 42 });

  // @ts-expect-error there is no such operation
  await client.noSuchRoute();

  // @ts-expect-error the second argument of a one-object route is not accepted
  await client.getResourcesSummary({ map_id: 0 }, { extra: true });
}

void positive;
void negative;
