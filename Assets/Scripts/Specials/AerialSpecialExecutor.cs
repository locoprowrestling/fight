using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Top-corner (Carter, Codah, Zeak), middle-corner (Vigilante), and
    /// rope-middle spring (Erza) aerial finishers. Missable; the defender can
    /// roll away while the attacker climbs.
    public static class AerialSpecialExecutor
    {
        public static IEnumerator Run(SpecialContext ctx)
        {
            var self = ctx.Self;
            var target = ctx.Target;
            var d = ctx.Data;
            var ring = RingInteractionSystem.Instance;

            var anchor = ring.GetBestAerialLaunchAnchor(self, target, d.requiredLaunchAnchorType)
                         ?? ring.NearestAnchor(self.transform.position, d.requiredLaunchAnchorType);
            if (anchor == null) yield break;

            // Vigilante synergy: faster setup and friendlier landing after a recent vanish.
            float setupScale = 1f;
            float toleranceBonus = 0f;
            if (d.benefitsFromRecentDodge && Time.time - self.Combat.LastDodgeTime <= 4f)
            {
                setupScale = 0.85f;
                toleranceBonus = 0.15f;
                MatchHUD.TryShowMessage("Nowhere to hide!");
            }

            self.Motor.SetScriptedControl(true);

            // Climb / spring setup. Interruptible: a hit aborts via SpecialController,
            // and the defender's roll-away makes the landing miss.
            self.States.Set(d.requiredLaunchAnchorType == AerialAnchorType.RopeMiddle
                ? WrestlerState.AerialSetup : WrestlerState.TurnbuckleClimb);
            float setup = (d.climbDuration > 0f ? d.climbDuration : d.aerialSetupDuration) * setupScale
                          + d.ropeSpringSetupDuration * setupScale;
            Vector3 climbStart = self.transform.position;
            Vector3 launchPos = anchor.transform.position;
            float t = 0f;
            while (t < setup && !ctx.Aborted)
            {
                t += Time.deltaTime;
                self.transform.position = Vector3.Lerp(climbStart, launchPos, t / setup);
                yield return null;
            }
            if (ctx.Aborted) yield break;

            // Airborne. No standard reversal once fully in the air.
            ctx.Controller.ReversalWindowOpen = false;
            ctx.Controller.EscapePhase = SpecialEscapePhase.FullLock;
            self.States.Set(WrestlerState.AerialAirborne);
            self.Anim.TriggerAerialLaunch();

            Vector3 landTarget = target.transform.position; // committed at launch
            landTarget.y = 0.5f;
            t = 0f;
            float air = d.airborneDuration;
            float arcHeight = d.usesCrescentArc ? 1.6f : 2.2f;
            Vector3 from = self.transform.position;
            while (t < air)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / air);
                Vector3 p = Vector3.Lerp(from, landTarget, k);
                p.y += Mathf.Sin(k * Mathf.PI) * arcHeight;
                self.transform.position = p;
                yield return null;
            }

            // Landing resolution.
            bool hit = target.States.IsDowned &&
                       MathUtil.FlatDistance(self.transform.position, target.transform.position)
                           <= d.landingTolerance + toleranceBonus;

            self.Motor.SetScriptedControl(false);
            self.Anim.TriggerAerialLanding(hit);

            if (hit)
            {
                SpecialExecutor.ApplyOnHitEffects(ctx, d.damage);
                self.States.Set(WrestlerState.AerialLandingHit, 0.6f);
            }
            else
            {
                self.Stats.ApplyDamage(d.selfDamageOnMiss);
                self.States.Set(WrestlerState.AerialLandingMiss, d.missRecoveryDuration);
                MatchHUD.TryShowMessage($"{d.displayName} misses!");
                Debug.Log($"[Special] {d.displayName} missed");
            }
        }
    }
}
