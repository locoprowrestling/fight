using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Johnny Crash's Human Wrecking Ball: forward charge whose damage scales
    /// with distance traveled. Hitting the ropes instead of the opponent hurts.
    public static class RushSpecialExecutor
    {
        public static IEnumerator Run(SpecialContext ctx)
        {
            var self = ctx.Self;
            var target = ctx.Target;
            var d = ctx.Data;
            var ring = RingInteractionSystem.Instance;

            self.States.Set(WrestlerState.SpecialStartup);
            self.Motor.FaceOpponent();
            yield return SpecialExecutor.Wait(ctx, 0.35f);
            if (ctx.Aborted) yield break;

            ctx.Controller.ReversalWindowOpen = false;
            self.States.Set(WrestlerState.SpecialActive);
            self.Motor.SetScriptedControl(true);

            Vector3 dir = MathUtil.Flat(self.transform.forward).normalized;
            float traveled = 0f;
            float elapsed = 0f;
            float speed = 7.5f;
            bool hit = false;
            bool wall = false;

            while (elapsed < d.chargeMaxDuration && traveled < d.chargeMaxDistance && !ctx.Aborted)
            {
                elapsed += Time.deltaTime;
                float step = speed * Time.deltaTime;
                self.transform.position += dir * step;
                traveled += step;

                if (target.States.Profile.canBeStruck && !target.States.IsDowned &&
                    MathUtil.FlatDistance(self.transform.position, target.transform.position) <= 0.9f)
                {
                    hit = true;
                    break;
                }
                if (ring != null && !ring.Bounds.IsInside(self.transform.position, 0.45f))
                {
                    wall = true;
                    break;
                }
                yield return null;
            }
            if (ctx.Aborted) yield break;

            self.Motor.SetScriptedControl(false);

            if (hit)
            {
                PairedMoveCoordinator.BeginPresentation(
                    self,
                    target,
                    d.choreography);
                // Damage tiers by distance traveled.
                float damage = traveled < 1.8f ? d.damageShort : traveled < 3.6f ? d.damageMedium : d.damage;
                bool fullImpact = traveled >= 3.6f;
                target.Stats.ApplyDamage(damage, self);
                self.Combat.EnterDowned(target, d.downedDuration);
                if (fullImpact)
                {
                    target.Buffs.Apply(SpecialExecutor.BuildEffect("crushed", 3f));
                    target.Combat.SetPendingKickoutPenalty(0.10f, d.pinWindow + 1.5f);
                }
                MatchHUD.TryShowMessage("Human Wrecking Ball!");
                Debug.Log($"[Special] Wrecking Ball hit at {traveled:0.0} units for {damage}");
                self.States.Set(WrestlerState.SpecialRecovery, 0.6f);
            }
            else
            {
                if (wall) self.Stats.ApplyDamage(d.selfDamageOnWallCollision);
                self.States.Set(WrestlerState.Stunned, 1.0f);
                MatchHUD.TryShowMessage("Wrecking Ball crashes!");
                Debug.Log("[Special] Wrecking Ball missed");
            }
        }
    }
}
