using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    /// <summary>
    /// The dev-action menu half of `dev` (ticket #3799): `debug_actions` lists
    /// the entries of the game's own Actions tab, `debug_action` runs one by
    /// path. Split from DevActionService.cs when the filtered walk grew it past
    /// the service line cap (ticket #3855).
    /// </summary>
    public partial class DevActionService
    {
        // ---- the game's own dev-action menu (ticket #3799) ------------------

        /// <summary>
        /// The whole menu, walked once and kept for the game's lifetime
        /// (ticket #3946). The walk costs about 8 s for 60,456 entries, so a
        /// filtered listing that matched little used to pay it again on the
        /// next call; now only the first call does.
        /// </summary>
        private static readonly MenuCache<ActionMenu> CachedMenu = new MenuCache<ActionMenu>();

        /// <summary>
        /// Drops the cached menu (ticket #3946). The game builds the menu from
        /// its loaded defs, so another colony (a load, a new game, a return to
        /// the main menu) may hold a different menu; the game-state hooks call
        /// this.
        /// </summary>
        internal static void ForgetActionMenu()
        {
            CachedMenu.Invalidate();
        }

        /// <summary>
        /// Lists the entries of the game's dev-action menu, the tools a player
        /// finds under the debug dialog's Actions tab, so a check can look up
        /// what exists before it needs it (ticket #3799).
        ///
        /// The listing collects as many matches as the answer lists, but reads
        /// the whole cached menu (ticket #3946), so it can say exactly how many
        /// entries the menu holds and how many match the filter. `offset` skips
        /// the matches an earlier answer already printed, so a caller pages
        /// through the menu instead of guessing substrings (ticket #3947).
        /// </summary>
        private DevActionResultDto ListDebugActions(DevActionRequest request)
        {
            string filter = DevActionRules.ActionPath(request.Name);
            int limit = DevActionRules.ListLimit(request.Count);
            int skip = DevActionRules.ListOffset(request.Offset);
            DebugActionWalk walk = WalkDebugActions(
                path => DevActionRules.MatchesFilter(path, filter),
                limit,
                skip
            );
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Actions = new DevActionsStateDto
                {
                    Walked = walk.Walked,
                    Shown = walk.Nodes.Count,
                    Complete = walk.Complete,
                    Nodes = walk.Nodes.Select(DescribeNode).ToList(),
                },
            };
            result.Detail = ListingDetail(filter, walk, skip);
            return result;
        }

        /// <summary>
        /// What the menu holds, how much of it matches, and which part of that
        /// this answer lists, so a caller can tell a menu with three entries
        /// from one whose answer lists three of many, and knows the `offset`
        /// that reaches the next page (ticket #3947).
        /// </summary>
        private static string ListingDetail(string filter, DebugActionWalk walk, int skip)
        {
            string menu = walk.Capped
                ? "the dev action menu is past the "
                    + DevActionRules.MaxNodes
                    + "-entry cap, so the walk searched "
                    + walk.Walked
                    + " of its entries"
                : "the dev action menu holds " + walk.Walked + " entries";
            if (filter.Length > 0)
            {
                menu += ", " + walk.Matches + " matching \"" + filter + "\"";
            }
            string listed = ListingRange(walk.Nodes.Count, skip);
            int next = skip + walk.Nodes.Count;
            if (next >= walk.Matches)
            {
                return menu + "; " + listed;
            }
            string tail = filter.Length == 0
                ? "; pass `offset=" + next + "` for the rest, or `name` to search"
                : "; pass `offset=" + next + "` for the rest, or narrow the filter";
            return menu + "; " + listed + tail;
        }

        /// <summary>
        /// The part of the matches one answer holds: its count from the start,
        /// or the range it covers when the call skipped earlier ones.
        /// </summary>
        private static string ListingRange(int shown, int skip)
        {
            if (shown == 0)
            {
                return skip == 0
                    ? "this answer lists none"
                    : "nothing matches past the first " + skip;
            }
            if (skip == 0)
            {
                return "this answer lists " + shown;
            }
            return "this answer lists matches " + (skip + 1) + " to " + (skip + shown);
        }

        /// <summary>
        /// Runs one entry of the dev-action menu that does its work at once. An
        /// entry that holds sub-entries, and a tool that acts on the clicked
        /// cell, are refused with the entries that would work instead: a call has
        /// no mouse, and a menu it cannot walk is not an action.
        /// </summary>
        private DevActionResultDto RunDebugAction(DevActionRequest request)
        {
            string wanted = DevActionRules.ActionPath(request.Name);
            // Six matches are enough: one is a hit and two already make the call
            // ambiguous, so the answer names five instead of listing the menu
            // (ticket #3855). The menu itself is already cached (#3946).
            DebugActionWalk walk = WalkDebugActions(
                path => DevActionRules.MatchesPath(path, wanted),
                AmbiguousAt,
                0
            );
            List<DebugActionNode> nodes = walk.Nodes;
            List<DebugActionNode> hits = nodes;
            if (hits.Count == 0)
            {
                throw new DevRefusal(
                    "\""
                        + wanted
                        + "\" is not a dev action this game has; the `debug_actions` op lists them"
                        + (walk.Samples.Count == 0
                            ? ""
                            : ", e.g. " + string.Join(", ", walk.Samples.Select(n => n.Path)))
                        + (walk.Complete
                            ? ""
                            : " (the menu is past the "
                                + DevActionRules.MaxNodes
                                + "-entry cap, so the walk searched "
                                + walk.Walked
                                + " of its entries)")
                );
            }
            if (hits.Count > 1)
            {
                throw new DevRefusal(
                    "\""
                        + wanted
                        + "\" names "
                        + hits.Count
                        + " entries; write the whole path: "
                        + string.Join(", ", hits.Take(5).Select(n => n.Path))
                );
            }
            DebugActionNode node = hits[0];
            node.TrySetupChildren();
            if (node.children.Any())
            {
                throw new DevRefusal(
                    node.Path
                        + " is a menu with "
                        + node.children.Count
                        + " entries; name one of them: "
                        + string.Join(", ", node.children.Take(5).Select(n => n.Path))
                );
            }
            if (node.actionType != DebugActionType.Action || node.action == null)
            {
                throw new DevRefusal(
                    node.Path
                        + " is a "
                        + node.actionType
                        + " entry: a tool acts on the cell clicked in the game, and a call has no mouse"
                );
            }
            int windows = Find.WindowStack.Windows.Count;
            // Two stamps before the entry and one after (ticket #3922): a
            // counter that already moved between the first two is the game
            // ticking, not the entry, so the comparison leaves it out.
            DevActionRules.ColonyStamp quiet = Stamp();
            DevActionRules.ColonyStamp before = Stamp();
            node.action();
            List<Window> opened = Find.WindowStack.Windows.Skip(windows).ToList();
            if (opened.Count > 0)
            {
                // A dialog waits for a click that never comes, so it is closed
                // again and the call says why it did nothing.
                string names = string.Join(", ", opened.Select(w => w.GetType().Name));
                foreach (Window window in opened)
                {
                    Find.WindowStack.TryRemove(window, false);
                }
                throw new DevRefusal(
                    node.Path
                        + " opens the game's own window ("
                        + names
                        + "), which a call cannot drive; nothing was applied"
                );
            }
            List<string> moved = DevActionRules.Difference(quiet, before, Stamp());
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Actions = new DevActionsStateDto
                {
                    Walked = walk.Walked,
                    Shown = 1,
                    Complete = walk.Complete,
                    Changed = moved.Count > 0,
                    Differences = moved,
                    Nodes = new List<DevActionNodeDto> { DescribeNode(node) },
                },
            };
            result.Detail =
                moved.Count > 0
                    ? DevActionRules.ChangedDetail(node.Path, moved)
                    : DevActionRules.UnchangedDetail(node.Path, before);
            return result;
        }

        /// <summary>Matches one `debug_action` call needs before the walk stops: two make it ambiguous.</summary>
        private const int AmbiguousAt = 6;

        /// <summary>Paths a refusal names as examples of what the menu has.</summary>
        private const int SamplePaths = 3;

        /// <summary>
        /// One read of the cached dev-action menu (tickets #3855, #3946): the
        /// entries whose path matched, the first few entries it met, and what
        /// the menu holds. The listing collects at most `want` matches, but the
        /// menu is already in memory, so the counts are exact.
        /// </summary>
        private sealed class DebugActionWalk
        {
            public readonly List<DebugActionNode> Nodes = new List<DebugActionNode>();
            public readonly List<DebugActionNode> Samples = new List<DebugActionNode>();

            /// <summary>Entries the menu holds, i.e. the cached walk's length.</summary>
            public int Walked;

            /// <summary>Entries of the whole menu whose path matched the filter.</summary>
            public int Matches;

            /// <summary>True when the cached walk reached the end of the menu.</summary>
            public bool Complete = true;

            /// <summary>True when the node cap, not the menu's end, stopped the build.</summary>
            public bool Capped;
        }

        /// <summary>
        /// One read of the cached menu: the entries whose path matched, the
        /// first few entries, and what the menu holds. Only the first call
        /// walks the game's menu (ticket #3946); every later one filters the
        /// list it left behind. `skip` drops the matches an earlier page
        /// printed, so a caller reaches the next page without a filter
        /// (ticket #3947).
        /// </summary>
        private static DebugActionWalk WalkDebugActions(Func<string, bool> match, int want, int skip)
        {
            ActionMenu menu = CachedMenu.Get(BuildActionMenu);
            DebugActionWalk walk = new DebugActionWalk
            {
                Walked = menu.Nodes.Count,
                Complete = !menu.Capped,
                Capped = menu.Capped,
            };
            int seen = 0;
            foreach (DebugActionNode node in menu.Nodes)
            {
                if (walk.Samples.Count < SamplePaths)
                {
                    walk.Samples.Add(node);
                }
                if (!match(node.Path)) continue;
                walk.Matches++;
                if (seen++ < skip) continue;
                if (walk.Nodes.Count < want)
                {
                    walk.Nodes.Add(node);
                }
            }
            return walk;
        }

        /// <summary>
        /// The dev-action menu as the game builds it for the debug dialog: one
        /// node per attribute-declared action, three levels deep so a leaf like
        /// Actions/Spawning/WildMan is reachable by path. This is the expensive
        /// half (ticket #3946): CachedMenu keeps the result for the game's
        /// lifetime.
        /// </summary>
        private static ActionMenu BuildActionMenu()
        {
            DebugActionNode root = new DebugActionNode("root");
            new DebugTabMenu_Actions(DebugTabMenuDefOf.Actions, null, root).InitActions(root);
            ActionMenu menu = new ActionMenu();
            if (CollectNodes(root, 0, menu.Nodes))
            {
                menu.Capped = true;
            }
            return menu;
        }

        /// <summary>The built menu: its entries in walk order, and whether the cap stopped the walk.</summary>
        private sealed class ActionMenu
        {
            public readonly List<DebugActionNode> Nodes = new List<DebugActionNode>();
            public bool Capped;
        }

        /// <summary>
        /// Adds one node and its sub-tree to the list; true when the node cap
        /// stopped the walk.
        /// </summary>
        private static bool CollectNodes(
            DebugActionNode node,
            int depth,
            List<DebugActionNode> nodes
        )
        {
            const int maxDepth = 4;
            if (depth < maxDepth && nodes.Count < DevActionRules.MaxNodes)
            {
                node.TrySetupChildren();
            }
            if (node.parent != null)
            {
                nodes.Add(node);
            }
            if (depth >= maxDepth || nodes.Count >= DevActionRules.MaxNodes)
            {
                return nodes.Count >= DevActionRules.MaxNodes;
            }
            foreach (DebugActionNode child in node.children)
            {
                if (CollectNodes(child, depth + 1, nodes)) return true;
            }
            return false;
        }

        private static DevActionNodeDto DescribeNode(DebugActionNode node)
        {
            string kind;
            if (node.children.Any())
            {
                kind = "menu";
            }
            else if (node.actionType == DebugActionType.Action)
            {
                kind = "action";
            }
            else
            {
                kind = "tool";
            }
            return new DevActionNodeDto
            {
                Path = node.Path,
                Category = node.category,
                Kind = kind,
            };
        }
    }
}
