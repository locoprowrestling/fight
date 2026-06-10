using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Cody Devine's Cloud Cover: distract the referee, pull the hidden vape,
    /// exhale a stunning cloud in a cone. Being hit during the setup means
    /// getting caught and punished.
    public static class DirtySpecialExecutor
    {
        public static IEnumerator Run(SpecialContext ctx)
        {
            var self = ctx.Self;
            var target = ctx.Target;
            var d = ctx.Data;

            float healthAtStart = self.Stats.Health;

            // Phase 1: distract the referee.
            self.States.Set(WrestlerState.SpecialStartup);
            MatchManager.Instance.DistractReferee(d.refDistractionSetup + d.hiddenObjectSetup + d.cloudActiveDuration + 0.5f);
            yield return SpecialExecutor.Wait(ctx, d.refDistractionSetup);
            if (Caught(ctx, healthAtStart)) yield break;

            // Phase 2: pull the hidden object.
            yield return SpecialExecutor.Wait(ctx, d.hiddenObjectSetup);
            if (Caught(ctx, healthAtStart)) yield break;

            // Phase 3: exhale the cloud — cone hit check at fire time.
            ctx.Controller.ReversalWindowOpen = false;
            self.States.Set(WrestlerState.SpecialActive, d.cloudActiveDuration + 0.2f);
            yield return SpecialExecutor.Wait(ctx, d.cloudActiveDuration);
            if (ctx.Aborted) yield break;

            if (HitboxProbe.ConeCheck(self.transform, target.transform, d.coneRange, d.coneAngle) &&
                target.States.Profile.canBeStruck && !target.States.IsDowned)
            {
                target.Stats.ApplyDamage(d.damage, self);
                target.States.Set(WrestlerState.Stunned, d.stunDuration);
                self.Combat.EnterDowned(target, d.downedDuration);
                MatchHUD.TryShowMessage("Cloud Cover connects!");
                Debug.Log("[Special] Cloud Cover hit");
            }
            else
            {
                MatchHUD.TryShowMessage("Cloud Cover misses!");
                Debug.Log("[Special] Cloud Cover missed");
            }
            self.States.Set(WrestlerState.SpecialRecovery, 0.6f);
        }

        static bool Caught(SpecialContext ctx, float healthAtStart)
        {
            if (ctx.Aborted) return true;
            if (ctx.Self.Stats.Health < healthAtStart - 0.01f)
            {
                // Interrupted mid-cheat: punished.
                ctx.Self.Stats.SpendMomentum(ctx.Data.caughtPenaltyMomentumLoss);
                ctx.Self.States.Set(WrestlerState.Stunned, 1.25f);
                MatchHUD.TryShowMessage("Caught cheating!");
                Debug.Log("[Special] Cloud Cover interrupted — caught!");
                return true;
            }
            return false;
        }
    }
}
