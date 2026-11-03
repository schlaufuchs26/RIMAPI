// The proof against a running game. It is skipped unless RIMAPI_CONTRACT_URL
// names one, because a unit run must not need a game; the run that verifies a
// change points it at a debug slot:
//
//   RIMAPI_CONTRACT_URL=http://localhost:8767 bun test test/contract.test.ts

import { describe, expect, test } from "bun:test";
import { RimApiClient, RimApiRefusalError } from "../src/index";

const baseUrl = process.env.RIMAPI_CONTRACT_URL;
const client = baseUrl === undefined ? undefined : new RimApiClient({ baseUrl });

describe("the generated client against the game", () => {
  test.skipIf(client === undefined)("reads a handful of routes", async () => {
    const version = await client!.getVersion();
    expect(version.success).toBe(true);
    expect(version.data?.mod_version).toBe("1.10.0");

    const state = await client!.getGameState();
    expect(state.success).toBe(true);
    expect(state.data?.has_game).toBe(true);
    expect(typeof state.data?.game_tick).toBe("number");

    const colonists = await client!.getColonists();
    expect(colonists.success).toBe(true);
    expect(Array.isArray(colonists.data)).toBe(true);

    const maps = await client!.mapControllerGetGameState();
    expect(maps.success).toBe(true);
    expect(Array.isArray(maps.data)).toBe(true);

    const resources = await client!.getResourcesSummary({ map_id: 0 });
    expect(resources.success).toBe(true);
    expect(resources.data).toBeDefined();
  });

  test.skipIf(client === undefined)("writes the game speed and reads the effect back", async () => {
    await client!.setGameSpeed({ speed: 1 });
    const running = await client!.getGameState();
    expect(running.data?.is_paused).toBe(false);
    expect(running.data?.time_speed).toBeDefined();

    await client!.setGameSpeed({ speed: 0 });
    const paused = await client!.getGameState();
    expect(paused.data?.is_paused).toBe(true);
  });

  test.skipIf(client === undefined)("surfaces the game's own refusal sentence", async () => {
    const error = await client!
      .getMapThings({ map_id: 999 })
      .catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(RimApiRefusalError);
    expect((error as RimApiRefusalError).errors).toContain("Map not found: 999");
  });
});
