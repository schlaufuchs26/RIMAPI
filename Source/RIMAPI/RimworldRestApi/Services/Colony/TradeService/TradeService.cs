
using System;
using System.Collections.Generic;
using HarmonyLib;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;


namespace RIMAPI.Services
{
    class TradeService : ITradeService
    {
        public ApiResult<List<TraderKindDto>> GetAllTraderDefs()
        {
            try
            {
                var results = new List<TraderKindDto>();

                foreach (var def in DefDatabase<TraderKindDef>.AllDefsListForReading)
                {
                    var dto = new TraderKindDto
                    {
                        DefName = def.defName,
                        Label = def.label ?? def.defName,
                        Orbital = def.orbital,
                        Visitor = !def.orbital,
                        Commonality = def.commonality,
                        // Initialize lists only as needed below to save allocation
                        Items = new List<StockRuleDto>(),
                        Categories = new List<StockRuleDto>(),
                        Tags = new List<StockRuleDto>(),
                        Special = new List<StockRuleDto>()
                    };

                    foreach (var gen in def.stockGenerators)
                    {
                        var traverse = Traverse.Create(gen);

                        // 1. FIX COUNT: If range is effectively zero, make it null
                        string countRange = null;
                        if (gen.countRange.max > 0)
                        {
                            countRange = $"{gen.countRange.min}~{gen.countRange.max}";
                        }

                        string priceMode = gen.price == PriceType.Normal ? null : gen.price.ToString();

                        // Most generators allow buying, but strict "Buy" generators usually imply 
                        // they ONLY buy this and don't sell it.
                        // However, for the API, "Buys=true" is the important part.
                        bool buys = true;

                        StockRuleDto CreateRule(string name) => new StockRuleDto
                        {
                            Name = name,
                            Count = countRange, // Will be null if 0~0
                            Price = priceMode,
                            Buys = buys
                        };

                        // --- STANDARD GENERATORS ---
                        if (gen is StockGenerator_SingleDef)
                        {
                            var thingDef = traverse.Field("thingDef").GetValue<ThingDef>();
                            if (thingDef != null) dto.Items.Add(CreateRule(thingDef.label));
                        }
                        else if (gen is StockGenerator_MultiDef)
                        {
                            var thingDefs = traverse.Field("thingDefs").GetValue<List<ThingDef>>();
                            if (thingDefs != null)
                                foreach (var t in thingDefs) dto.Items.Add(CreateRule(t.label));
                        }
                        else if (gen is StockGenerator_Category)
                        {
                            var catDef = traverse.Field("categoryDef").GetValue<ThingCategoryDef>();
                            if (catDef != null) dto.Categories.Add(CreateRule(catDef.label));
                        }
                        else if (gen is StockGenerator_Tag)
                        {
                            var tag = traverse.Field("tradeTag").GetValue<string>();
                            if (!string.IsNullOrEmpty(tag)) dto.Tags.Add(CreateRule(tag));
                        }

                        // --- SPECIAL CASES (Enhanced Reflection) ---
                        else
                        {
                            string specialName;

                            if (gen is StockGenerator_Animals)
                            {
                                specialName = "Animals";
                            }
                            else if (gen is StockGenerator_BuySlaves)
                            {
                                specialName = "Slaves";
                            }
                            else if (gen is StockGenerator_Techprints)
                            {
                                specialName = "Techprints";
                            }
                            else if (gen is StockGenerator_ReinforcedBarrels)
                            {
                                specialName = "ReinforcedBarrels";
                            }
                            // FIX: Extract the tag from 'BuyTradeTag'
                            else if (gen.GetType().Name == "StockGenerator_BuyTradeTag")
                            {
                                var tag = traverse.Field("tag").GetValue<string>();
                                specialName = !string.IsNullOrEmpty(tag) ? $"Buys: {tag}" : "Buys: Tag";
                                countRange = null; // Explicitly ensure count is hidden for buy-only
                            }
                            // FIX: Extract the tag from 'MarketValue'
                            else if (gen.GetType().Name == "StockGenerator_MarketValue")
                            {
                                // MarketValue generators typically generate items of a specific tag 
                                // that fall within a value range.
                                var tag = traverse.Field("tradeTag").GetValue<string>();
                                specialName = !string.IsNullOrEmpty(tag) ? $"Random: {tag}" : "Random: Value";
                            }
                            else
                            {
                                specialName = gen.GetType().Name.Replace("StockGenerator_", "");
                            }

                            // Re-create rule to ensure updated specialName/countRange is used
                            var rule = CreateRule(specialName);
                            rule.Count = countRange; // Update count in case we forced it to null above
                            dto.Special.Add(rule);
                        }
                    }

                    // Cleanup: Set empty lists to null so they aren't serialized
                    if (dto.Items.Count == 0) dto.Items = null;
                    if (dto.Categories.Count == 0) dto.Categories = null;
                    if (dto.Tags.Count == 0) dto.Tags = null;
                    if (dto.Special.Count == 0) dto.Special = null;

                    results.Add(dto);
                }

                return ApiResult<List<TraderKindDto>>.Ok(results);
            }
            catch (Exception ex)
            {
                return ApiResult<List<TraderKindDto>>.Fail(ex.Message);
            }
        }

        // GetTraders reads the traders who are on the map right now: a caravan
        // visitor (a pawn with a trader kind) and an orbital trade ship. The
        // trader kind defs above say what a kind could stock; this says what
        // the one in front of the colony actually holds (ticket #3170).
        public ApiResult<List<TraderDto>> GetTraders(int mapId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null)
                {
                    return ApiResult<List<TraderDto>>.Fail($"Map {mapId} not found.");
                }

                Pawn negotiator = FindNegotiator(map);
                List<TraderDto> traders = new List<TraderDto>();

                // Orbital ships first: their id is negative, counted from -1 in
                // the order the passing-ship manager lists them, because a
                // TradeShip carries no id of its own.
                int shipIndex = 0;
                foreach (PassingShip ship in map.passingShipManager.passingShips)
                {
                    TradeShip tradeShip = ship as TradeShip;
                    if (tradeShip == null) continue;
                    shipIndex++;
                    traders.Add(TraderOf(tradeShip, -shipIndex, tradeShip.CanTradeNow, negotiator));
                }

                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned) continue;
                    if (pawn.TraderKind == null || pawn.trader == null) continue;
                    if (pawn.Faction != null && pawn.Faction == Faction.OfPlayer) continue;
                    traders.Add(TraderOf(pawn, pawn.thingIDNumber, pawn.CanTradeNow, negotiator));
                }

                traders.Sort((a, b) => a.Id.CompareTo(b.Id));
                return ApiResult<List<TraderDto>>.Ok(traders);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error reading traders on map {mapId}: {ex}");
                return ApiResult<List<TraderDto>>.Fail(ex.Message);
            }
        }

        // Trade buys or sells one def through the game's own TradeDeal, the
        // object the trade window fills: the deal holds a Tradeable per def
        // (the trader's stock and the colony's goods), the currency tradeable
        // moves the silver, and TryExecute resolves it. Nothing is invented:
        // the game's own prices, the game's own transfer, the game's own
        // placement of what was bought (a caravan drops it beside the trader,
        // a ship sends it down in a drop pod).
        public ApiResult<TradeResultDto> Trade(TradeRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return ApiResult<TradeResultDto>.Fail("no request body");
                }
                Map map = MapHelper.GetMapByID(request.MapId);
                if (map == null)
                {
                    return ApiResult<TradeResultDto>.Fail($"Map {request.MapId} not found.");
                }
                string op = (request.Op ?? "").Trim().ToLowerInvariant();
                if (op != "buy" && op != "sell")
                {
                    return ApiResult<TradeResultDto>.Fail(
                        $"op must be buy or sell, not \"{request.Op}\"");
                }
                if (string.IsNullOrEmpty(request.DefName))
                {
                    return ApiResult<TradeResultDto>.Fail(
                        "def_name is required: the defName of the thing to trade (the `trade` read lists it)");
                }
                if (request.Count <= 0)
                {
                    return ApiResult<TradeResultDto>.Fail(
                        $"count must be above 0, not {request.Count}");
                }
                if (TradeSession.Active)
                {
                    return ApiResult<TradeResultDto>.Fail(
                        "a trade is already open in the game, so this call would fight the open window; close it first");
                }
                ITrader trader = FindTrader(map, request.TraderId);
                if (trader == null)
                {
                    return ApiResult<TradeResultDto>.Fail(request.TraderId == 0
                        ? "no trader on the map can trade now; the `trade` read lists every trader with its id"
                        : $"no trader with id {request.TraderId}; the `trade` read lists every trader with its id");
                }
                if (!trader.CanTradeNow)
                {
                    return ApiResult<TradeResultDto>.Fail(
                        $"{trader.TraderName} cannot trade now (not willing, downed, hostile or not on the map)");
                }
                Pawn negotiator = FindNegotiator(map);
                if (negotiator == null)
                {
                    return ApiResult<TradeResultDto>.Fail(
                        "no colonist can trade right now (all downed, or none able to talk)");
                }

                TradeSession.SetupWith(trader, negotiator, false);
                try
                {
                    TradeDeal deal = TradeSession.deal;
                    bool buying = op == "buy";
                    List<Tradeable> matches = MatchingTradeables(deal, request.DefName, buying);
                    if (matches.Count == 0)
                    {
                        // A sale that finds nothing is the case Evan met on
                        // 2026-10-31: the goods exist, the game trades only the
                        // stored part, and the refusal said nothing about the
                        // rest (ticket #3494).
                        return ApiResult<TradeResultDto>.Fail(buying
                            ? $"{trader.TraderName} sells no {request.DefName}"
                            : WithHolding(
                                $"the colony has no {request.DefName} to sell",
                                TradeHolding.OutsideTrade(map, trader, FindThingDef(request.DefName), 0)));
                    }

                    // Positive countToTransfer moves goods toward the colony
                    // (a purchase), negative away from it (a sale); the game's
                    // own limits say how much each tradeable can move.
                    int sign = buying ? 1 : -1;
                    int remaining = request.Count;
                    foreach (Tradeable tradeable in matches)
                    {
                        if (remaining <= 0) break;
                        int available = buying
                            ? tradeable.GetMaximumToTransfer()
                            : -tradeable.GetMinimumToTransfer();
                        if (available <= 0) continue;
                        int take = Math.Min(remaining, available);
                        tradeable.ForceTo(sign * take);
                        remaining -= take;
                    }
                    int moved = request.Count - remaining;
                    if (moved == 0)
                    {
                        return ApiResult<TradeResultDto>.Fail(buying
                            ? $"{trader.TraderName} has none of {request.DefName} left"
                            : $"the colony has none of {request.DefName} that {trader.TraderName} would buy");
                    }

                    deal.UpdateCurrencyCount();
                    Tradeable silver = deal.CurrencyTradeable;
                    if (silver == null)
                    {
                        return ApiResult<TradeResultDto>.Fail(
                            $"{trader.TraderName} holds no silver, so nothing can change hands");
                    }
                    // The game caps a payment it cannot cover instead of
                    // refusing, which would hand goods over for less silver
                    // than the deal says; refuse here instead, with the numbers.
                    if (buying && silver.CountPostDealFor(Transactor.Colony) < 0)
                    {
                        return ApiResult<TradeResultDto>.Fail(WithHolding(
                            $"the colony has {silver.CountHeldBy(Transactor.Colony)} silver and this purchase of {moved} {request.DefName} costs {-silver.CountToTransfer}",
                            TradeHolding.OutsideTrade(
                                map,
                                trader,
                                ThingDefOf.Silver,
                                silver.CountHeldBy(Transactor.Colony))));
                    }
                    if (!buying && silver.CountPostDealFor(Transactor.Trader) < 0)
                    {
                        return ApiResult<TradeResultDto>.Fail(
                            $"{trader.TraderName} has {silver.CountHeldBy(Transactor.Trader)} silver and this sale of {moved} {request.DefName} is worth {silver.CountToTransfer}");
                    }
                    int paid = -silver.CountToTransfer;

                    bool actuallyTraded;
                    if (!deal.TryExecute(out actuallyTraded))
                    {
                        return ApiResult<TradeResultDto>.Fail(
                            "the game refused the deal (the colony cannot afford it or the trader cannot pay)");
                    }
                    if (!actuallyTraded)
                    {
                        return ApiResult<TradeResultDto>.Fail(
                            "the game accepted the call but nothing changed hands");
                    }

                    Tradeable after = TradeSession.deal == null ? null : TradeSession.deal.CurrencyTradeable;
                    TradeResultDto result = new TradeResultDto
                    {
                        Traded = true,
                        Op = op,
                        DefName = request.DefName,
                        Label = matches[0].Label,
                        Count = moved,
                        Silver = paid,
                        ColonySilver = after != null
                            ? after.CountHeldBy(Transactor.Colony)
                            : map.resourceCounter.GetCount(ThingDefOf.Silver),
                        TraderSilver = after != null ? after.CountHeldBy(Transactor.Trader) : 0,
                    };
                    if (moved < request.Count)
                    {
                        result.Note = $"only {moved} of the {request.Count} asked for were available";
                    }
                    return ApiResult<TradeResultDto>.Ok(result);
                }
                finally
                {
                    TradeSession.Close();
                }
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error trading on map {(request == null ? 0 : request.MapId)}: {ex}");
                return ApiResult<TradeResultDto>.Fail(ex.Message);
            }
        }

        // TraderOf turns one live trader into the read's shape. Silver is what
        // the trader holds, so a sale can tell whether it can pay at all.
        private static TraderDto TraderOf(ITrader trader, int id, bool canTradeNow, Pawn negotiator)
        {
            TraderDto dto = new TraderDto
            {
                Id = id,
                Name = trader.TraderName,
                Kind = trader.TraderKind == null ? null : trader.TraderKind.defName,
                Label = trader.TraderKind == null ? null : trader.TraderKind.label,
                Faction = trader.Faction == null ? null : trader.Faction.Name,
                Orbital = trader is TradeShip,
                CanTradeNow = canTradeNow,
                Silver = 0,
                NegotiatorId = negotiator == null ? 0 : negotiator.thingIDNumber,
                Negotiator = negotiator == null ? null : negotiator.LabelShort,
                Stock = new List<TraderStockDto>(),
            };

            // One stock line per def and material: a caravan carries the same
            // def in several stacks, and a caller buys by defName.
            Dictionary<string, TraderStockDto> lines = new Dictionary<string, TraderStockDto>();
            List<string> order = new List<string>();
            foreach (Thing thing in trader.Goods)
            {
                if (thing == null || thing.def == null) continue;
                if (thing.def == ThingDefOf.Silver)
                {
                    dto.Silver += thing.stackCount;
                    continue;
                }
                string stuff = thing.Stuff == null ? "" : thing.Stuff.defName;
                string key = thing.def.defName + "/" + stuff;
                TraderStockDto line;
                if (!lines.TryGetValue(key, out line))
                {
                    line = new TraderStockDto
                    {
                        DefName = thing.def.defName,
                        Label = thing.def.label,
                        Count = 0,
                        MarketValue = thing.MarketValue,
                    };
                    lines[key] = line;
                    order.Add(key);
                }
                line.Count += thing.stackCount;
            }
            order.Sort((a, b) => string.Compare(lines[a].Label, lines[b].Label, StringComparison.OrdinalIgnoreCase));
            foreach (string key in order)
            {
                dto.Stock.Add(lines[key]);
            }
            return dto;
        }

        // FindTrader takes the id the read prints: a pawn's thing id, or a
        // negative index for an orbital ship. Zero means the only trader who
        // can trade now.
        private static ITrader FindTrader(Map map, int traderId)
        {
            if (traderId > 0)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn != null && pawn.thingIDNumber == traderId && pawn.trader != null)
                    {
                        return pawn;
                    }
                }
                return null;
            }
            List<ITrader> shiptraders = new List<ITrader>();
            foreach (PassingShip ship in map.passingShipManager.passingShips)
            {
                TradeShip tradeShip = ship as TradeShip;
                if (tradeShip != null) shiptraders.Add(tradeShip);
            }
            if (traderId < 0)
            {
                int index = -traderId - 1;
                return index < shiptraders.Count ? shiptraders[index] : null;
            }
            List<ITrader> candidates = new List<ITrader>();
            foreach (ITrader trader in shiptraders)
            {
                if (trader.CanTradeNow) candidates.Add(trader);
            }
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.trader == null) continue;
                if (pawn.CanTradeNow) candidates.Add(pawn);
            }
            return candidates.Count == 1 ? candidates[0] : null;
        }

        // FindNegotiator picks the colonist who would stand at the trade
        // window: spawned, not downed, and able to talk.
        private static Pawn FindNegotiator(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn == null || pawn.Dead || pawn.Downed) continue;
                if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking)) continue;
                return pawn;
            }
            return null;
        }

        // WithHolding appends what lies outside the trade's reach to a refusal,
        // so a run reads the cause and the fix in the same answer (ticket
        // #3494). An empty holding leaves the refusal as it was.
        private static string WithHolding(string refusal, string holding)
        {
            return string.IsNullOrEmpty(holding) ? refusal : refusal + "; " + holding;
        }

        // FindThingDef resolves the defName a request names, falling back to the
        // def's label the way MatchingTradeables matches. Null means the request
        // names no thing at all, and the refusal then carries no holding text.
        private static ThingDef FindThingDef(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return null;
            }
            ThingDef byName = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (byName != null)
            {
                return byName;
            }
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def != null && string.Equals(def.label, defName, StringComparison.OrdinalIgnoreCase))
                {
                    return def;
                }
            }
            return null;
        }

        // MatchingTradeables finds the deal's tradeables for one defName (or
        // label), on the side the trade moves from: the trader's stock for a
        // purchase, the colony's goods for a sale.
        private static List<Tradeable> MatchingTradeables(TradeDeal deal, string defName, bool buying)
        {
            List<Tradeable> matches = new List<Tradeable>();
            foreach (Tradeable tradeable in deal.AllTradeables)
            {
                if (tradeable.IsCurrency || tradeable.ThingDef == null) continue;
                if (!string.Equals(tradeable.ThingDef.defName, defName, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(tradeable.ThingDef.label, defName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (buying && tradeable.thingsTrader.Count == 0) continue;
                if (!buying && tradeable.thingsColony.Count == 0) continue;
                matches.Add(tradeable);
            }
            return matches;
        }
    }
}