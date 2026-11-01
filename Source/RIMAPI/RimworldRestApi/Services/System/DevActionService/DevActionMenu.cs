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
        /// Lists the entries of the game's dev-action menu, the tools a player
        /// finds under the debug dialog's Actions tab, so a check can look up
        /// what exists before it needs it (ticket #3799).
        ///
        /// The walk stops as soon as it holds as many matches as the answer
        /// lists (ticket #3855): the whole menu costs about 7 s and holds more
        /// than the 20,000-entry cap, so a filter that only walked to the cap
        /// could miss an entry the game does have.
        /// </summary>
        private DevActionResultDto ListDebugActions(DevActionRequest request)
        {
            string filter = DevActionRules.ActionPath(request.Name);
            int limit = DevActionRules.ListLimit(request.Count);
            DebugActionWalk walk = WalkDebugActions(
                path => DevActionRules.MatchesFilter(path, filter),
                limit
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
            result.Detail = ListingDetail(filter, walk);
            return result;
        }

        /// <summary>
        /// What the listing walked and what it left out, so a caller can tell a
        /// menu with three entries from one whose walk stopped at three matches.
        /// </summary>
        private static string ListingDetail(string filter, DebugActionWalk walk)
        {
            string filterNote = filter.Length == 0 ? "" : ", " + walk.Nodes.Count + " matching \"" + filter + "\"";
            if (walk.Complete)
            {
                return "the dev action menu holds "
                    + walk.Walked
                    + " entries"
                    + filterNote
                    + "; this answer lists "
                    + walk.Nodes.Count;
            }
            if (walk.Capped)
            {
                return "the dev action menu holds at least "
                    + walk.Walked
                    + " entries (the walk stopped at the "
                    + DevActionRules.MaxNodes
                    + "-entry cap)"
                    + filterNote
                    + "; this answer lists "
                    + walk.Nodes.Count;
            }
            if (filter.Length == 0)
            {
                return "the dev action menu has more than the "
                    + walk.Nodes.Count
                    + " entries this answer lists; pass `name` to search the rest";
            }
            return "the walk stopped after "
                + walk.Walked
                + " entries, once it had "
                + walk.Nodes.Count
                + " matching \""
                + filter
                + "\"; more entries may match, so narrow the filter for the rest";
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
            // ambiguous, so the walk stops there instead of building the whole
            // menu for a refusal that names five (ticket #3855).
            DebugActionWalk walk = WalkDebugActions(
                path => DevActionRules.MatchesPath(path, wanted),
                AmbiguousAt
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
                            : " (the walk stopped after " + walk.Walked + " of the menu's entries)")
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
        /// One walk of the dev-action menu (ticket #3855): the entries that
        /// matched, the first few entries it met, and whether it reached the
        /// menu's end. The whole menu costs about 7 s, so a caller that has what
        /// it asked for stops the walk instead of building the rest.
        /// </summary>
        private sealed class DebugActionWalk
        {
            public readonly List<DebugActionNode> Nodes = new List<DebugActionNode>();
            public readonly List<DebugActionNode> Samples = new List<DebugActionNode>();

            /// <summary>Entries the walk visited; a lower bound when Complete is false.</summary>
            public int Walked;

            /// <summary>True when the walk reached the end of the menu.</summary>
            public bool Complete = true;

            /// <summary>True when the node cap, not a full set of matches, stopped the walk.</summary>
            public bool Capped;
        }

        /// <summary>
        /// The dev-action menu as the game builds it for the debug dialog: one
        /// node per attribute-declared action, three levels deep so a leaf like
        /// Actions/Spawning/WildMan is reachable by path. The walk ends as soon
        /// as it holds `want` matching entries.
        /// </summary>
        private static DebugActionWalk WalkDebugActions(Func<string, bool> match, int want)
        {
            DebugActionNode root = new DebugActionNode("root");
            new DebugTabMenu_Actions(DebugTabMenuDefOf.Actions, null, root).InitActions(root);
            DebugActionWalk walk = new DebugActionWalk();
            if (CollectNodes(root, 0, walk, match, want))
            {
                walk.Complete = false;
            }
            return walk;
        }

        /// <summary>
        /// Visits one node and its sub-tree; true when the walk stopped early,
        /// either with the matches it wanted or at the node cap.
        /// </summary>
        private static bool CollectNodes(
            DebugActionNode node,
            int depth,
            DebugActionWalk walk,
            Func<string, bool> match,
            int want
        )
        {
            const int maxDepth = 4;
            int maxNodes = DevActionRules.MaxNodes;
            if (depth < maxDepth && walk.Walked < maxNodes)
            {
                node.TrySetupChildren();
            }
            if (node.parent != null)
            {
                walk.Walked++;
                if (walk.Samples.Count < SamplePaths)
                {
                    walk.Samples.Add(node);
                }
                if (match(node.Path))
                {
                    walk.Nodes.Add(node);
                    if (walk.Nodes.Count >= want) return true;
                }
            }
            if (depth >= maxDepth || walk.Walked >= maxNodes)
            {
                walk.Capped = walk.Walked >= maxNodes;
                return walk.Capped;
            }
            foreach (DebugActionNode child in node.children)
            {
                if (CollectNodes(child, depth + 1, walk, match, want)) return true;
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
