using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Scripted multi-step specials:
    ///  - RunSequence: JT Staten's Statutes in Stone (grounded rope-rebound elbow + auto pin)
    ///  - RunCombo:    Franky Gonzales' 6-7 Moves of Doom (corner combo)
    public static class SequenceSpecialExecutor
    {
        // ------------------------------------------------------------------
        // JT Staten — Statutes in Stone
        // ------------------------------------------------------------------
        public static IEnumerator RunSequence(SpecialContext ctx)
        {
            var self = ctx.Self;
            var target = ctx.Target;
            var d = ctx.Data;
            var ring = RingInteractionSystem.Instance;

            // Stand at the opponent's head, look up, click, chest slap.
            self.Motor.SetScriptedControl(true);
            self.States.Set(WrestlerState.SpecialStartup);
            Vector3 headPos = target.transform.position + target.transform.forward * 0.0f +
                              MathUtil.FlatDirection(target.transform.position, self.transform.position) * 0.8f;
            self.transform.position = headPos;
            self.Motor.FaceOpponent();

            yield return SpecialExecutor.Wait(ctx, d.setupDuration); // look up at sky + clicking motion
            if (ctx.Aborted || !target.States.IsDowned) { Cleanup(ctx); yield break; }

            // Chest slap: small damage, refreshes the downed timer so the run is honest.
            target.Stats.ApplyDamage(d.initialDamage, self);
            self.Combat.EnterDowned(target, 2.2f);
            Debug.Log("[Special] Statutes in Stone: chest slap");

            // Run to the nearest rebound rope.
            ctx.Controller.ReversalWindowOpen = false;
            var side = ring.GetNearestRopeSide(self.transform.position);
            var rebound = ring.GetReboundAnchor(side);
            Vector3 ropePoint = rebound != null ? rebound.RopeLinePosition : ring.Bounds.ClosestPointOnRope(side, self.transform.position);
            ropePoint = ring.Bounds.ClampInside(ropePoint, 0.4f);

            self.States.Set(WrestlerState.RopeReboundRun, 3f);
            yield return MoveAlong(ctx, self, ropePoint, 0.85f);
            if (ctx.Aborted) { Cleanup(ctx); yield break; }

            // Rebound and come back at the opponent's current spot.
            self.States.Set(WrestlerState.RopeReboundReturn, 2f);
            Vector3 elbowSpot = target.transform.position;
            yield return MoveAlong(ctx, self, elbowSpot, 0.55f);
            if (ctx.Aborted) { Cleanup(ctx); yield break; }

            // Elbow drop: the opponent may have rolled away or gotten up.
            bool hit = target.States.IsDowned &&
                       MathUtil.FlatDistance(self.transform.position, target.transform.position) <= d.landingTolerance;

            self.Motor.SetScriptedControl(false);

            if (hit)
            {
                target.Stats.ApplyDamage(d.damage, self);
                self.Combat.EnterDowned(target, d.downedDuration);
                if (d.appliesKickoutPenaltyOnImmediatePin)
                    target.Combat.SetPendingKickoutPenalty(d.kickoutPenaltyValue, 2.5f);
                Debug.Log("[Special] Statutes in Stone: elbow connects");

                if (d.autoPinOnHit)
                {
                    yield return new WaitForSeconds(d.autoPinDelay);
                    PinSystem.Instance.TryStartPin(self, target);
                }
            }
            else
            {
                self.Stats.ApplyDamage(d.selfDamageOnMiss);
                self.States.Set(WrestlerState.AerialLandingMiss, d.missRecoveryDuration);
                MatchHUD.TryShowMessage("Statutes in Stone misses!");
                Debug.Log("[Special] Statutes in Stone missed");
            }
        }

        static IEnumerator MoveAlong(SpecialContext ctx, WrestlerCore self, Vector3 destination, float duration)
        {
            Vector3 from = self.transform.position;
            destination.y = from.y;
            self.Motor.FaceDirection(destination - from);
            float t = 0f;
            while (t < duration && !ctx.Aborted)
            {
                t += Time.deltaTime;
                self.transform.position = Vector3.Lerp(from, destination, t / duration);
                yield return null;
            }
        }

        static void Cleanup(SpecialContext ctx)
        {
            ctx.Self.Motor.SetScriptedControl(false);
            ctx.Self.States.Set(WrestlerState.SpecialRecovery, 0.8f);
        }

        // ------------------------------------------------------------------
        // Franky Gonzales — 6-7 Moves of Doom
        // ------------------------------------------------------------------
        public static IEnumerator RunCombo(SpecialContext ctx)
        {
            var self = ctx.Self;
            var target = ctx.Target;
            var d = ctx.Data;

            self.States.Set(WrestlerState.SpecialStartup);
            self.Motor.FaceOpponent();
            yield return SpecialExecutor.Wait(ctx, 0.25f);
            if (ctx.Aborted) yield break;

            // Lock the victim into the corner combo.
            self.Motor.SetScriptedControl(true);
            target.Motor.SetScriptedControl(true);
            target.States.Set(WrestlerState.ComboSequence);
            self.States.Set(WrestlerState.ComboSequence);
            self.transform.position = target.transform.position +
                MathUtil.FlatDirection(target.transform.position, self.transform.position) * 0.9f;
            self.Motor.FaceOpponent();
            PairedMoveCoordinator.BeginPresentation(
                self,
                target,
                d.choreography);

            for (int i = 0; i < d.comboSteps.Count; i++)
            {
                var step = d.comboSteps[i];

                // Reversal windows: at the opener, plus any step flagged as a
                // desperation window before the final impact.
                ctx.Controller.ReversalWindowOpen = step.ReversalWindow;

                yield return SpecialExecutor.Wait(ctx, step.Duration);
                if (ctx.Aborted) yield break;

                ctx.Controller.ReversalWindowOpen = false;
                target.Stats.ApplyDamage(step.Damage, self);
                target.Stats.DrainStamina(step.StaminaDamage);
                self.Anim.PlayMove(step.AttackerStateKey, step.PoseName);
                target.Anim.PlayMove(
                    step.DefenderStateKey,
                    "paired-impact-defender");
                Debug.Log($"[Special] 6-7 Moves of Doom step {i + 1}: {step.StepName}");

                if (step.CausesDowned)
                {
                    target.Motor.SetScriptedControl(false);
                    self.Combat.EnterDowned(target, d.downedDuration);
                }
            }

            // Final: dazed debuff + immediate-pin penalty.
            if (!string.IsNullOrEmpty(d.debuffId))
                target.Buffs.Apply(SpecialExecutor.BuildEffect(d.debuffId, d.debuffDuration));
            if (d.appliesKickoutPenaltyOnImmediatePin)
                target.Combat.SetPendingKickoutPenalty(d.kickoutPenaltyValue, d.pinWindow + 1.5f);

            target.Motor.SetScriptedControl(false);
            self.Motor.SetScriptedControl(false);
            self.States.Set(WrestlerState.SpecialRecovery, 0.7f);
        }
    }
}
