using System;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    // The ordered attack and the gate it has to pass (fox tickets #3737 and
    // #4160), split off CombatService when the file crossed the size gate: which
    // of the game's two attack jobs an order queues, and why an attack that
    // cannot land says so instead of leaving the run to guess.
    public partial class CombatService
    {
        private ApiResult<OrderResultDto> Attack(Pawn pawn, OrderRequestDto request, OrderResultDto result)
        {
            Pawn target = PawnHelper.FindPawnById(request.TargetId);
            if (target == null)
            {
                return ApiResult<OrderResultDto>.Fail($"no pawn with id {request.TargetId} to attack");
            }
            if (target.Dead || !target.Spawned)
            {
                return ApiResult<OrderResultDto>.Fail($"{target.LabelShort} is dead or gone");
            }
            // A colonist in a mental state is still a colonist (ticket #3433):
            // the game's IsColonistPlayerControlled is false for him, and a
            // berserk colonist is hostile to everyone, so this test must not
            // fall through to the hostility check below.
            if (target.IsColonist)
            {
                MentalStateDef state = target.MentalStateDef;
                string mental = state == null
                    ? ""
                    : $" (in a mental state: {CombatRefusal.MentalStateName(state.label, state.defName)})";
                return ApiResult<OrderResultDto>.Fail(
                    $"{target.LabelShort} is a colonist{mental}; the tool does not order friendly fire");
            }
            // The same rule the fight read counts with (fox #3630): a wild man
            // who is beating a colonist is a hostile, so `attack` must take the
            // id the read just printed. Asking HostileTo alone refused him with
            // "not hostile, so the colony has no fight with him" while he was
            // hurting Veniamin.
            if (!Hostility.IsHostileToColony(target))
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"{target.LabelShort} is not hostile, so the colony has no fight with him");
            }
            // The same reachability check as a move (ticket #3175): a raider
            // behind a sealed wall or across water takes the attack job and
            // then drops it, so the pawn stands still and the caller learns
            // nothing. A ranged pawn needs a path to its firing position too.
            Danger maxDanger;
            string dangerError;
            if (!TryParseDanger(request.MaxDanger, out maxDanger, out dangerError))
            {
                return ApiResult<OrderResultDto>.Fail(dangerError);
            }
            string destText = $"{target.LabelShort} (id {target.thingIDNumber})";
            string refusal = ReachRefusal(pawn, target, PathEndMode.Touch, maxDanger, destText);
            if (refusal != null) return ApiResult<OrderResultDto>.Fail(refusal);
            // The walk is read to touching distance, because that is where the
            // melee job paths to (ticket #4176): the default OnCell asked for a
            // path onto the target's own cell, which is one cell longer than
            // the walk the game takes.
            WalkReport walk = InspectWalk(pawn, target, PathEndMode.Touch);
            if (walk.Worst > maxDanger)
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"{pawn.LabelShort} cannot reach {destText} from {pawn.Position.x},{pawn.Position.z} at " +
                    $"max_danger={DangerText(maxDanger)}: the walk the game would take runs through " +
                    $"{walk.HazardText()}, so the pawn would take damage on the way; " +
                    "pass max_danger=deadly to order it anyway");
            }
            // Which of the game's two attack jobs the order queues (fox #4160):
            // a projectile weapon cannot be fired at an adjacent target, so a
            // gunner one cell from a manhunter queued the ranged job and stood
            // beside it for ten minutes doing nothing. The game's own float menu
            // offers "Melee attack" exactly when the melee verb can reach, so
            // the order takes the same choice instead of always shooting.
            Verb ranged = RangedVerb(pawn);
            Verb melee = MeleeVerb(pawn, target);
            AttackBlockRules.Facts rangedFacts = FactsOf(pawn, target, ranged ?? melee, ranged, melee);
            bool swing = AttackBlockRules.Choose(rangedFacts) == AttackBlockRules.AttackJob.Melee;
            JobDef def = DefDatabase<JobDef>.GetNamedSilentFail(swing ? AttackMeleeDef : AttackStaticDef);
            if (def == null)
            {
                return ApiResult<OrderResultDto>.Fail("the game has no attack job for this pawn");
            }
            Job job = JobMaker.MakeJob(def, target);
            if (swing)
            {
                // What the game's own melee order sets: playerForced so the job
                // is not treated as self-chosen, and a downed target is finished.
                job.playerForced = true;
                Pawn targetPawn = target as Pawn;
                if (targetPawn != null) job.killIncappedTarget = targetPawn.Downed;
            }
            else
            {
                // The verb is named, so the shot uses the weapon the check read,
                // and the job ends when the target comes too close to shoot
                // instead of holding the pawn in place forever (Verb.OrderForceTarget).
                job.verbToUse = ranged;
                job.endIfCantShootInMelee = true;
            }
            if (!pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                return ApiResult<OrderResultDto>.Fail($"{pawn.LabelShort} refused the attack order");
            }
            // The answer names the target and the job it queued, so a run does
            // not read "attack - (id 0)" and wonder what went out (fox #4160).
            result.Job = job.def.defName;
            result.TargetId = target.thingIDNumber;
            result.Target = target.LabelShort;
            result.PathHazards = walk.HazardText();
            // How long the approach is, so a run knows when to step the clock
            // (ticket #4176): the live check of #4163 read "Job AttackMelee"
            // and nothing about the 40 cells the pawn then walked. Only a
            // swinging order walks at all; the game's AttackStatic stops the
            // pather dead (JobDriver_AttackStatic), so a shot names no walk
            // and a target in contact has none to name.
            if (swing && walk.Cells > 0)
            {
                result.Reachable = true;
                result.Reach = walk.ReachText();
                result.PathDanger = DangerText(walk.Worst);
            }
            // Whether the attack can land from where the pawn stands (fox #3737):
            // the order took and the job reads back, but a target behind a wall
            // or beyond the weapon leaves the pawn standing and firing nothing,
            // which reads as "the shot missed" for hours. The reason is the
            // game's own check, so the answer says what the game would say.
            // A swinging order is different (fox #4163): the melee job walks the
            // pawn to the target, so its note is the reason the shot is not the
            // answer and never "he stands"; only a shot that cannot land leaves
            // the pawn standing.
            if (swing)
            {
                result.Note = AttackBlockRules.MeleeNote(rangedFacts);
            }
            else
            {
                string block = BlockReason(pawn, target, false);
                if (block != null)
                {
                    result.Note = $"from {pawn.Position.x},{pawn.Position.z} the attack cannot land: {block}, "
                        + "so the pawn stands and does not fire until it can; move him into the open or closer first";
                }
            }
            if (string.IsNullOrEmpty(result.Note) && walk.Worst != Danger.None)
            {
                result.Note = $"the path crosses {walk.HazardText()}, tolerated at max_danger={DangerText(maxDanger)}";
            }
            return ApiResult<OrderResultDto>.Ok(Fill(pawn, result));
        }

        // RangedVerb is the equipped weapon's verb when it shoots; a club, fists
        // or teeth return null, so the pawn fights in contact.
        private static Verb RangedVerb(Pawn pawn)
        {
            Verb verb = pawn.equipment != null && pawn.equipment.PrimaryEq != null
                ? pawn.equipment.PrimaryEq.PrimaryVerb
                : null;
            return verb != null && verb.verbProps != null && !verb.verbProps.IsMeleeAttack ? verb : null;
        }

        // MeleeVerb is what the pawn swings with: the best tool of the weapon it
        // holds (a rifle's stock) or of its own body.
        private static Verb MeleeVerb(Pawn pawn, Thing target)
        {
            if (pawn.meleeVerbs == null || target == null) return null;
            return pawn.meleeVerbs.TryGetMeleeVerb(target);
        }

        // FactsOf reads one verb against one target, plus whether the pawn also
        // carries a ranged weapon and whether its melee verb reaches, so
        // AttackBlockRules can pick the job and word the answer.
        private static AttackBlockRules.Facts FactsOf(
            Pawn pawn, Thing target, Verb verb, Verb ranged, Verb melee)
        {
            return new AttackBlockRules.Facts
            {
                CanFight = PawnHelper.CanFight(pawn),
                HasVerb = verb != null,
                CanHit = verb != null && verb.CanHitTargetFrom(pawn.Position, target),
                Distance = pawn.Position.DistanceTo(target.Position),
                Range = verb != null ? verb.EffectiveRange : 0f,
                MinRange = verb != null && verb.verbProps != null
                    ? verb.verbProps.EffectiveMinRange(target, pawn)
                    : 0f,
                IsMelee = verb != null && verb.verbProps != null && verb.verbProps.IsMeleeAttack,
                HasRanged = ranged != null,
                HasMeleeVerb = melee != null,
                MeleeCanHit = melee != null && melee.CanHitTargetFrom(pawn.Position, target),
                From = $"{pawn.Position.x},{pawn.Position.z}",
                Target = $"{target.Position.x},{target.Position.z}",
            };
        }

        // BlockReason names the game's own gate between a pawn and its target,
        // or null when the attack can land (fox #3737). The verb the job uses
        // decides, so the reason matches what the game does instead of a second
        // opinion (fox #4160: a gunner swinging in contact must not read "too
        // close for the revolver" while the blow lands); only the wording is
        // ours (AttackBlockRules, game-free and tested).
        private static string BlockReason(Pawn pawn, Thing target, bool melee)
        {
            if (target == null || !target.Spawned || pawn.Map == null) return null;
            Verb ranged = RangedVerb(pawn);
            Verb meleeVerb = MeleeVerb(pawn, target);
            Verb verb = melee ? meleeVerb : ranged;
            AttackBlockRules.Facts facts = FactsOf(pawn, target, verb, ranged, meleeVerb);
            string reason = AttackBlockRules.Reason(facts);
            return reason.Length == 0 ? null : reason;
        }

        // AttackBlockedOf is the read's half of it: a drafted pawn whose job is
        // an attack says why that attack is not landing, so a run reads "no
        // line of sight" instead of "the shot missed" (fox #3737). The job's
        // own verb is read, so the line never contradicts the job next to it.
        // A swinging pawn that is still walking to its target is not blocked
        // (fox #4163): the melee job paths there, and "attack blocked" beside a
        // pawn on its way sent the run into a second order.
        private static string AttackBlockedOf(Pawn pawn)
        {
            if (!pawn.Drafted || pawn.CurJob == null || !IsFightJob(pawn.CurJob.def)) return "";
            Thing target = pawn.CurJob.targetA.Thing;
            if (target == null) return "";
            bool melee = pawn.CurJob.def.defName == AttackMeleeDef;
            if (melee && pawn.pather != null && pawn.pather.Moving) return "";
            return BlockReason(pawn, target, melee) ?? "";
        }

    }
}
