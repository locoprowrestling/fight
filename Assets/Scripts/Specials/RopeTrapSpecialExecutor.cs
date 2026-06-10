using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Morgana Lavey's Tarantula: rope-trap hold that is an illegal five-count
    /// hold under standard rules, and a true submission under no-rope-break or
    /// hardcore rules.
    public static class RopeTrapSpecialExecutor
    {
        public static IEnumerator Run(SpecialContext ctx)
        {
            var self = ctx.Self;
            var target = ctx.Target;
            var d = ctx.Data;
            var ring = RingInteractionSystem.Instance;
            var rules = MatchManager.Instance.Rules;

            var zone = ring.GetNearestRopeTrapZone(target.transform.position, RingInteractionSystem.RopeTrapRange);
            if (zone == null) yield break;

            // Setup is escapable / reversible.
            self.States.Set(WrestlerState.RopeTrapSetup, d.setupDuration + 0.5f);
            yield return SpecialExecutor.Wait(ctx, d.setupDuration);
            if (ctx.Aborted) yield break;

            ctx.Controller.ReversalWindowOpen = false;
            ctx.Controller.EscapePhase = SpecialEscapePhase.FullLock;

            // Snap both wrestlers into the trap.
            self.Motor.SetScriptedControl(true);
            target.Motor.SetScriptedControl(true);
            target.transform.position = zone.victimAnchor.position;
            self.transform.position = zone.attackerAnchor.position;
            self.States.Set(WrestlerState.RopeTrapLocked);
            target.States.Set(WrestlerState.RopeTrapLocked);
            yield return SpecialExecutor.Wait(ctx, d.lockDuration);
            if (ctx.Aborted) yield break;

            Debug.Log($"[Special] Rope trap locked: {d.displayName}");

            bool fiveCountMode = rules.RopeBreaksActive && rules.refereeFiveCountEnabled && d.startsRefereeFiveCount;
            if (fiveCountMode)
            {
                // Illegal hold: damage over the five-count, auto-release at 5, cannot win.
                bool released = false;
                RefereeCountSystem.Instance.StartFiveCount(rules.refereeFiveCountSeconds, () => released = true);
                float maxHold = d.standardMaxHold + 0.5f;
                float t = 0f;
                while (!released && t < maxHold && !ctx.Aborted)
                {
                    t += Time.deltaTime;
                    target.Stats.ApplyDamage(d.damagePerSecond * Time.deltaTime, self);
                    target.Stats.DrainStamina(d.opponentStaminaDrainPerSecond * Time.deltaTime);
                    self.Stats.DrainStamina(d.selfStaminaDrainPerSecond * Time.deltaTime);
                    yield return null;
                }
                RefereeCountSystem.Instance.Cancel();
                ReleaseTrap(ctx);
            }
            else if (rules.ropeTrapSubmissionAllowed || d.canSubmitOnlyIfNoRopeBreaks)
            {
                // True submission under no-rope-break / hardcore rules.
                self.Motor.SetScriptedControl(false);
                target.Motor.SetScriptedControl(false);
                SubmissionSystem.Instance.TryStart(self, target,
                    d.submissionPressurePerSecond, d.damagePerSecond, d.displayName,
                    0f, d.opponentStaminaDrainPerSecond, d.selfStaminaDrainPerSecond,
                    ignoreRopeBreak: true, canWin: true);
                MatchHUD.TryShowMessage("No rope break!");
            }
            else
            {
                ReleaseTrap(ctx);
            }
        }

        static void ReleaseTrap(SpecialContext ctx)
        {
            ctx.Self.Motor.SetScriptedControl(false);
            ctx.Target.Motor.SetScriptedControl(false);
            var ring = RingInteractionSystem.Instance;
            // Drop both back inside the ring next to the ropes.
            ctx.Self.Motor.Teleport(ring.Bounds.ClampInside(ctx.Self.transform.position, 0.6f));
            ctx.Target.Motor.Teleport(ring.Bounds.ClampInside(ctx.Target.transform.position, 0.6f));
            ctx.Self.States.Set(WrestlerState.SpecialRecovery, 0.8f);
            ctx.Target.States.Set(WrestlerState.RopeStaggered, 1.0f);
        }
    }
}
