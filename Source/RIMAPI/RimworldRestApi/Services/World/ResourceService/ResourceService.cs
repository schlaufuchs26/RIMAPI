using System;
using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    public class ResourceService : IResourceService
    {
        public ResourceService() { }

        public ApiResult<Dictionary<string, List<ThingDto>>> GetAllStoredResources(int mapId)
        {
            Map map = MapHelper.GetMapByID(mapId);
            var items = ResourcesHelper.GetItemsFromStorageLocations(map);
            LogApi.Info("Items: " + items.Count);
            var result = ResourcesHelper.GetStoredItemsByCategory(items);
            return ApiResult<Dictionary<string, List<ThingDto>>>.Ok(result);
        }

        public ApiResult<List<ThingDto>> GetAllStoredResourcesByCategory(
            int mapId,
            string categoryDef
        )
        {
            Map map = MapHelper.GetMapByID(mapId);
            var items = ResourcesHelper.GetItemsFromStorageLocations(map);
            var result = ResourcesHelper.GetStoredItemsListByCategory(items, categoryDef);
            return ApiResult<List<ThingDto>>.Ok(result);
        }

        public ApiResult<ResourcesSummaryDto> GetResourcesSummary(int mapId)
        {
            Map map = MapHelper.GetMapByID(mapId);
            var result = ResourcesHelper.GenerateResourcesSummary(map);
            return ApiResult<ResourcesSummaryDto>.Ok(result);
        }

        public ApiResult<StoragesSummaryDto> GetStoragesSummary(int mapId)
        {
            Map map = MapHelper.GetMapByID(mapId);
            var result = ResourcesHelper.StoragesSummary(map);
            return ApiResult<StoragesSummaryDto>.Ok(result);
        }

        public ApiResult SpawnItem(SpawnItemRequestDto request)
        {
            Map map = Find.CurrentMap;
            IntVec3 cell = new IntVec3(request.X, 0, request.Z);

            // 1. Safety Checks
            if (map == null || !cell.InBounds(map)) return ApiResult.Fail("Safety Checks");

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(request.DefName);
            if (def == null) return ApiResult.Fail("def == null");

            // 2. STOP if trying to spawn a Pawn (Human/Animal) with this method
            if (def.category == ThingCategory.Pawn)
            {
                Log.Error("Rest API: Do not use SpawnItem for Pawns. Use a Pawn generation method.");
                return ApiResult.Fail("Rest API: Do not use SpawnItem for Pawns. Use a Pawn generation method");
            }

            // 3. Handle Stuff (Material)
            ThingDef stuffDef = null;
            if (def.MadeFromStuff)
            {
                if (!string.IsNullOrEmpty(request.StuffDefName))
                {
                    stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(request.StuffDefName);
                }

                // Fallback: If no stuff provided, pick the default cheap one (usually Wood/Steel)
                if (stuffDef == null)
                {
                    stuffDef = GenStuff.DefaultStuffFor(def);
                }
            }

            // 4. Create the Thing
            Thing thing = ThingMaker.MakeThing(def, stuffDef);
            thing.stackCount = UnityEngine.Mathf.Min(request.Amount, def.stackLimit);

            // 5. Apply Quality (Optional)
            // Only applies if the item HAS a quality component (like weapons/art/apparel)
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null && !string.IsNullOrEmpty(request.Quality))
            {
                if (Enum.TryParse(request.Quality, true, out QualityCategory qc))
                {
                    compQuality.SetQuality(qc, ArtGenerationContext.Outsider);
                }
            }

            // 6. Spawn
            GenSpawn.Spawn(thing, cell, map, WipeMode.Vanish);

            // (Add loop for remaining stackCount if > stackLimit, as shown previously)
            return ApiResult.Ok();
        }
    }
}
