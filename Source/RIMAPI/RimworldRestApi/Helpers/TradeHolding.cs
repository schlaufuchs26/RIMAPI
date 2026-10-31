using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    // TradeHolding measures the colony's side of a trade the way a refusal has
    // to explain it (fox ticket #3494): colony 13 held 847 silver in two
    // stacks, an orbital supplier was in range, and every purchase answered
    // "the colony has 0 silver".
    //
    // The cause is the game's own rule, and it differs per trader:
    //   * an orbital ship trades only what lies on the cells of a powered
    //     orbital trade beacon (TradeUtility.AllLaunchableThingsForTrade);
    //   * a caravan visitor trades what lies in the home area or in storage
    //     (Pawn_TraderTracker.ColonyThingsWillingToBuy).
    // So the refusal names the stacks that fall outside, where they lie, and
    // which of the two rules left them out.
    public static class TradeHolding
    {
        // OutsideTrade describes the colony's goods of one def when the deal
        // counted fewer of them than the map holds. Empty when the counted part
        // is all there is.
        public static string OutsideTrade(Map map, ITrader trader, ThingDef def, int counted)
        {
            if (map == null || def == null)
            {
                return "";
            }
            TradeHoldingText holding = new TradeHoldingText
            {
                Label = def.label ?? def.defName,
                DefName = def.defName,
                Counted = counted,
                Places = new List<string>(),
                Rule = RuleFor(map, trader),
            };

            List<IntVec3> nearest = new List<IntVec3>();
            // Spawned stacks: the ones a stockpile or a shelf holds, and the
            // ones lying on the ground.
            foreach (Thing thing in map.listerThings.ThingsOfDef(def))
            {
                if (thing == null || thing.def != def || thing.stackCount <= 0)
                {
                    continue;
                }
                holding.Total += thing.stackCount;
                holding.Stacks++;
                nearest.Add(thing.PositionHeld);
                holding.Places.Add(
                    $"#{thing.thingIDNumber} x{thing.stackCount} "
                        + (PlaceOf(map, thing) ?? "on the ground at (" + thing.Position.x + "," + thing.Position.z + ")")
                );
            }

            // Stacks inside a container: a grave, a pod, a bookcase. They are
            // not spawned, so the map's thing list never names them, and no
            // trade counts them.
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                IThingHolder holder = building as IThingHolder;
                if (holder == null || building is ISlotGroupParent)
                {
                    continue;
                }
                int held = HeldCount(holder, def);
                if (held <= 0)
                {
                    continue;
                }
                holding.Total += held;
                holding.Stacks++;
                holding.Places.Add(
                    $"x{held} inside {building.def.defName} #{building.thingIDNumber} ({building.LabelShort})"
                );
            }

            // The beacon clause is only useful with a distance: a run that
            // reads "the nearest beacon is 34 cells away" can build one there.
            if (holding.HasOutside && trader is TradeShip)
            {
                holding.Rule = OrbitalRule(map, nearest);
            }
            return holding.Sentence();
        }

        // RuleFor is the game rule that decides which of the colony's goods the
        // deal sees, in the trader's own terms.
        private static string RuleFor(Map map, ITrader trader)
        {
            if (trader is TradeShip)
            {
                return OrbitalRule(map, new List<IntVec3>());
            }
            return "a visiting caravan trades only goods in the home area or in a stockpile, so haul it there first";
        }

        // OrbitalRule names the beacon the goods would have to lie on: the
        // nearest powered one and its distance, or why no beacon can carry them.
        private static string OrbitalRule(Map map, List<IntVec3> places)
        {
            string rule = "an orbital trade counts only goods on the cells of a powered orbital trade beacon";
            List<Building_OrbitalTradeBeacon> beacons =
                new List<Building_OrbitalTradeBeacon>(Building_OrbitalTradeBeacon.AllPowered(map));
            if (beacons.Count == 0)
            {
                bool any = false;
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building is Building_OrbitalTradeBeacon)
                    {
                        any = true;
                        break;
                    }
                }
                return any
                    ? rule + ", and the beacon on the map has no power"
                    : rule + ", and none stands on the map";
            }
            if (places.Count == 0)
            {
                return rule;
            }
            float closest = -1f;
            foreach (IntVec3 place in places)
            {
                foreach (Building_OrbitalTradeBeacon beacon in beacons)
                {
                    float distance = place.DistanceTo(beacon.Position);
                    if (closest < 0f || distance < closest)
                    {
                        closest = distance;
                    }
                }
            }
            return rule + $"; the nearest one is {closest:F0} cells from the closest of them";
        }

        // PlaceOf names the storage a spawned stack lies in, or null when it
        // lies on the ground. "Stockpile" is the zone; any other slot group is a
        // storage building (a shelf).
        private static string PlaceOf(Map map, Thing thing)
        {
            if (!thing.Spawned || thing.Position == IntVec3.Invalid)
            {
                return null;
            }
            SlotGroup group = map.haulDestinationManager.SlotGroupAt(thing.Position);
            if (group == null)
            {
                return null;
            }
            string where = group.parent is Zone ? "in a stockpile" : "in storage";
            return where + " at (" + thing.Position.x + "," + thing.Position.z + ")";
        }

        // HeldCount sums one def's stacks in a container.
        private static int HeldCount(IThingHolder holder, ThingDef def)
        {
            ThingOwner things = holder.GetDirectlyHeldThings();
            if (things == null)
            {
                return 0;
            }
            int count = 0;
            foreach (Thing thing in things)
            {
                if (thing != null && thing.def == def)
                {
                    count += thing.stackCount;
                }
            }
            return count;
        }
    }
}
