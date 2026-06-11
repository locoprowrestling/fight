using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Anuka Gutierrez's Trap-and-Snap Armbar: a counter stance. If the opponent
    /// attacks during the window, the attack is cancelled and Anuka flows
    /// straight into a grounded armbar submission.
    public static class CounterSpecialExecutor
    {
        public static IEnumerator Run(SpecialContext ctx)
        {
            var self = ctx.Self;
            var target = ctx.Target;
            var d = ctx.Data;

            self.States.Set(WrestlerState.SpecialStartup, d.counterWindow + 0.2f);
            ctx.Controller.ReversalWindowOpen = false; // the stance itself is the gamble
            MatchHUD.TryShowMessage($"{self.DisplayName} sets a trap...");

            float t = 0f;
            while (t < d.counterWindow && !ctx.Aborted)
            {
                t += Time.deltaTime;

                bool incomingStrikeOrGrapple =
                    (target.Combat.CurrentMove != null && target.Combat.MoveElapsed <=
                        target.Combat.CurrentMove.startupTime + target.Combat.CurrentMove.activeTime) ||
                    Time.time - target.Combat.LastGrappleAttemptTime <= 0.15f;

                bool inReach = MathUtil.FlatDistance(self.transform.position, target.transform.position) <= 1.6f;

                // Running attacks are too much force to trap cleanly.
                bool runningAttack = target.Combat.CurrentMove != null && target.Combat.CurrentMove.requiresRunning;

                if (incomingStrikeOrGrapple && inReach && !runningAttack)
                {
                    // Counter success: cancel their attack, trap the limb.
                    target.Combat.InterruptMove();
                    target.Stats.ApplyDamage(d.initialDamage, self);
                    self.Combat.EnterDowned(target, 2.5f);
                    self.Anim.TriggerReversal(
                        strong: false, ReversalSystem.DefaultBasicPresentationId);
                    MatchHUD.TryShowMessage("Trapped! Armbar locked!");
                    Debug.Log("[Special] Trap-and-Snap counter success");

                    ctx.Controller.EscapePhase = SpecialEscapePhase.FullLock;
                    SubmissionSystem.Instance.TryStart(self, target,
                        d.submissionPressurePerSecond, d.damagePerSecond, d.displayName,
                        d.submissionPressureBonus, 0f, 0f,
                        ignoreRopeBreak: false, canWin: true);
                    yield break;
                }
                yield return null;
            }
            if (ctx.Aborted) yield break;

            // Whiffed stance: short punishable recovery.
            self.States.Set(WrestlerState.SpecialRecovery, d.counterWhiffRecovery);
            Debug.Log("[Special] Counter stance whiffed");
        }
    }
}
