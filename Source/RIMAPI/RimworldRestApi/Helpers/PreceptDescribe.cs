using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Models;
using RimWorld;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// What a precept buys and costs, read from the game's own def (fox
    /// ticket #3927). A precept's label says nothing about its effect:
    /// "exalted" can mean a mood bonus for a situation the colony does not
    /// have. This walks the def the game uses; stat factors, stat offsets,
    /// the thought its comps grant, what it refuses; so a founding can name
    /// the effect it chose instead of guessing from the label.
    ///
    /// Every line is a restatement of def data, never an interpretation: a
    /// factor is the number in the XML, a mood range is the stages of the
    /// thought the comp points at.
    /// </summary>
    public static class PreceptDescribe
    {
        /// <summary>
        /// Details is the ideo's precept set with what each one buys and
        /// costs, for the ideo read a founding reports.
        /// </summary>
        public static List<PreceptDetailDto> Details(Ideo ideo)
        {
            if (ideo == null)
            {
                return new List<PreceptDetailDto>();
            }
            return ideo.PreceptsListForReading.Select(p => new PreceptDetailDto
            {
                Def = p.def.defName,
                Label = p.TipLabel,
                Issue = p.def.issue != null ? p.def.issue.LabelCap.ToString() : "",
                Impact = p.def.impact.ToString(),
                Description = p.def.description,
                RequiredMemes = RequiredMemes(p.def),
                Effects = Effects(p.def),
                Ritual = Ritual(p.def),
            }).ToList();
        }

        /// <summary>Effects lists the mechanical effect of one precept, one line per effect.</summary>
        public static List<string> Effects(PreceptDef def)
        {
            var lines = new List<string>();
            if (def == null)
            {
                return lines;
            }
            foreach (StatModifier mod in def.statFactors ?? Enumerable.Empty<StatModifier>())
            {
                if (mod?.stat == null)
                {
                    continue;
                }
                lines.Add(IdeoChoiceRules.StatFactor(mod.stat.LabelCap.ToString(), mod.value));
            }
            foreach (StatModifier mod in def.statOffsets ?? Enumerable.Empty<StatModifier>())
            {
                if (mod?.stat == null)
                {
                    continue;
                }
                lines.Add(IdeoChoiceRules.StatOffset(mod.stat.LabelCap.ToString(), mod.value));
            }
            foreach (PreceptComp comp in def.comps ?? Enumerable.Empty<PreceptComp>())
            {
                switch (comp)
                {
                    case PreceptComp_Thought thoughtComp:
                        AddThought(lines, thoughtComp.thought, thoughtComp.thoughtStageDescriptions);
                        break;
                    case PreceptComp_UnwillingToDo unwilling:
                        lines.Add("refuses " + (unwilling.eventDef != null ? unwilling.eventDef.LabelCap.ToString() : "a task"));
                        break;
                    case PreceptComp_DevelopmentPoints points:
                        string what = points.eventDef != null ? points.eventDef.LabelCap.ToString() : "the event";
                        lines.Add($"+{points.points} development points ({what})");
                        break;
                }
            }
            return lines;
        }

        private static void AddThought(List<string> lines, ThoughtDef thought, List<string> stageDescriptions)
        {
            if (thought == null)
            {
                return;
            }
            List<float> moods = thought.stages != null
                ? thought.stages.Select(s => s.baseMoodEffect).ToList()
                : new List<float>();
            string range = IdeoChoiceRules.MoodRange(moods);
            string label = thought.LabelCap.ToString();
            if (!string.IsNullOrEmpty(range))
            {
                lines.Add($"{range} ({label})");
            }
            // A stage description list is the game's own wording for the
            // situation that pays or costs, which is what a reader needs to
            // judge whether three pawns can satisfy it.
            if (stageDescriptions != null && stageDescriptions.Count > 0)
            {
                lines.Add($"situations: {string.Join(" / ", stageDescriptions)}");
            }
        }

        /// <summary>
        /// RequiredMemes names the memes a precept demands, by defName, so a
        /// caller can compare them with the meme it chose.
        /// </summary>
        public static List<string> RequiredMemes(PreceptDef def)
        {
            return (def?.requiredMemes ?? new List<MemeDef>())
                .Where(m => m != null)
                .Select(m => m.defName)
                .ToList();
        }

        /// <summary>
        /// ConflictingMemes names the memes that cannot stand beside it.
        /// </summary>
        public static List<string> ConflictingMemes(PreceptDef def)
        {
            return (def?.conflictingMemes ?? new List<MemeDef>())
                .Where(m => m != null)
                .Select(m => m.defName)
                .ToList();
        }

        /// <summary>
        /// Ritual names the ritual the precept brings, with the roles it hands
        /// out and whether each is required. A precept whose ritual needs a
        /// role nobody can fill is the trap the ticket names, so the fact is
        /// printed instead of left to a later failure.
        /// </summary>
        public static string Ritual(PreceptDef def)
        {
            RitualPatternDef pattern = def?.ritualPatternBase;
            RitualBehaviorDef behavior = pattern?.ritualBehavior;
            if (behavior == null)
            {
                return "";
            }
            List<RitualRole> roles = behavior.roles ?? new List<RitualRole>();
            List<string> required = roles
                .Where(r => r != null && r.required)
                .Select(r => r.Label.ToString())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();
            string name = behavior.LabelCap.ToString();
            if (required.Count == 0)
            {
                return $"ritual {name}";
            }
            return $"ritual {name} (roles: {string.Join(", ", required)})";
        }

        /// <summary>
        /// TipLabel is the "Issue: label" the ideoligion window shows, which
        /// is also the name a caller may pass. Built from the def so it does
        /// not need a Precept instance.
        /// </summary>
        public static string TipLabel(PreceptDef def)
        {
            if (def == null)
            {
                return "";
            }
            string issue = def.issue != null ? def.issue.LabelCap.ToString() : "";
            return string.IsNullOrEmpty(issue) ? def.LabelCap.ToString() : issue + ": " + def.LabelCap.ToString();
        }
    }
}
