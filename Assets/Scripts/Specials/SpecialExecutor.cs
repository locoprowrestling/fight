using System.Collections;
using UnityEngine;

namespace LoCoFight
{
    /// Shared helpers plus the generic power-grapple and submission special paths
    /// (Hussy, Nicky, Dean, Major Glory, Avalon).
    public static class SpecialExecutor
    {
        public static IEnumerator Wait(SpecialContext ctx, float seconds)
        {
            float t = 0f;
            while (t < seconds && !ctx.Aborted)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        public static void ApplyOnHitEffects(SpecialContext ctx, float damage)
        {
            var d = ctx.Data;
            var target = ctx.Target;
            float mult = ctx.Self.Stats.Data != null && d.category == SpecialCategory.SpecialAerial
                ? ctx.Self.Stats.Data.baseAerialDamageModifier : 1f;
            target.Stats.ApplyDamage(damage * mult, ctx.Self);
            if (d.staminaDamage > 0f) target.Stats.DrainStamina(d.staminaDamage);

            if (d.causesDownedState)
                ctx.Self.Combat.EnterDowned(target, d.downedDuration);
            else if (d.stunDuration > 0f)
                target.States.Set(WrestlerState.Stunned, d.stunDuration);

            if (d.appliesKickoutPenaltyOnImmediatePin && d.kickoutPenaltyValue > 0f)
                target.Combat.SetPendingKickoutPenalty(d.kickoutPenaltyValue, d.pinWindow + 1.5f);

            if (!string.IsNullOrEmpty(d.debuffId) && d.debuffDuration > 0f)
                target.Buffs.Apply(BuildEffect(d.debuffId, d.debuffDuration));
            if (!string.IsNullOrEmpty(d.buffId) && d.buffDuration > 0f)
                ctx.Self.Buffs.Apply(BuildEffect(d.buffId, d.buffDuration));

            if (ctx.Self.Traits != null) ctx.Self.Traits.OnCleanOffense(null, isSpecial: true);
            Debug.Log($"[Special] {d.displayName} hits for {damage:0.#}");
        }

        /// Known buff/debuff ids used by roster specials.
        public static StatusEffect BuildEffect(string id, float duration)
        {
            var e = new StatusEffect(id, duration);
            switch (id)
            {
                case "back-damage": e.StaminaRecoveryMult = 0.8f; e.GetUpSpeedMult = 0.85f; e.UiLabel = "Back damage"; break;
                case "crushed": e.KickoutMult = 0.9f; e.StaminaRecoveryMult = 0.85f; e.UiLabel = "Crushed"; break;
                case "disoriented": e.ReversalLeniencyBonus = -0.12f; e.KickoutMult = 0.9f; e.UiLabel = "Disoriented"; break;
                case "dazed": e.KickoutMult = 0.88f; e.UiLabel = "Dazed"; break;
                case "slowed-leg": e.MoveSpeedMult = 0.85f; e.UiLabel = "Leg damage"; break;
                case "technical-advantage": e.ReversalStaminaCostMult = 0.8f; e.UiLabel = "Technical advantage"; break;
                case "agility-recovery": e.GetUpSpeedMult = 1.15f; e.StaminaRecoveryMult = 1.15f; e.UiLabel = "Agile recovery"; break;
                case "clean-follow-up": e.MomentumGainMult = 1.08f; e.UiLabel = "Clean follow-up"; break;
                case "honor-tested": e.MomentumGainMult = 1.15f; e.ReversalStaminaCostMult = 0.9f; e.UiLabel = "Honor tested"; break;
                default: e.UiLabel = id; break;
            }
            return e;
        }

        // ------------------------------------------------------------------
        // Power grapple specials: grab -> lift -> (carry / spin / drop) -> impact.
        // Variants are driven entirely by data: carryDuration, spinCount, and
        // specialVariant "dual-position" (Dean) / "side-by-side" (Major Glory).
        // ------------------------------------------------------------------
        public static IEnumerator RunPowerGrappleSpecial(SpecialContext ctx)
        {
            var self = ctx.Self;
            var target = ctx.Target;
            var d = ctx.Data;

            // Dean's Final Notice: behind/beside = Rear Naked Choke, front = Chokeslam.
            if (d.specialVariant == "dual-position" &&
                HitboxProbe.IsBehindOrBeside(target.transform, self.transform) &&
                self.Combat.Role != GrappleRole.Attacker)
            {
                yield return RunChokeVariant(ctx);
                yield break;
            }

            // Lift check up-front for lift-based finishers.
            if (d.requiresTargetLiftable &&
                !CombatResolver.ValidateLift(self, target, LiftStrengthClass.Low, out string liftReason))
            {
                MatchHUD.TryShowMessage(liftReason);
                self.States.Set(WrestlerState.Stunned, 0.45f);
                self.Stats.DrainStamina(10f);
                if (target.Traits != null) target.Traits.NotifyLiftBlocked(self);
                yield break;
            }

            // Nicky's Hide-the-Pain synergy: faster startup after a recent reversal.
            float startupScale = 1f;
            if (d.benefitsFromRecentReversal && Time.time - self.Combat.LastReversalTime <= 3f)
            {
                startupScale = 0.85f;
                MatchHUD.TryShowMessage($"{self.DisplayName} saw it coming!");
            }

            self.Combat.ReleaseGrapple();
            self.States.Set(WrestlerState.SpecialStartup);
            self.Motor.SetScriptedControl(true);
            target.Motor.SetScriptedControl(true);

            // Grab + snap.
            Vector3 dir = MathUtil.FlatDirection(self.transform.position, target.transform.position);
            self.Motor.FaceOpponent();
            yield return Wait(ctx, 0.35f * startupScale);
            if (ctx.Aborted) yield break;

            if (d.requiresSideBySidePosition)
            {
                // Patriot Plunge: snap side-by-side, both facing the same way.
                target.transform.position = self.transform.position + self.transform.right * 0.8f;
                target.transform.rotation = self.transform.rotation;
            }
            else
            {
                target.transform.position = self.transform.position + dir * 0.9f;
            }

            // Lift phase (escapable by Vanishing Dodge).
            ctx.Controller.EscapePhase = d.canBeEscapedDuringLift ? SpecialEscapePhase.Lift : SpecialEscapePhase.FullLock;
            ctx.Controller.ReversalWindowOpen = false;
            self.States.Set(WrestlerState.CarryLift);
            target.States.Set(WrestlerState.CarryLift);
            float liftTime = (d.startsCarryPhase || d.spinCount > 0) ? 0.5f : 0.35f;
            yield return Wait(ctx, liftTime * startupScale);
            if (ctx.Aborted) yield break;

            ctx.Controller.EscapePhase = SpecialEscapePhase.FullLock;

            // Carry parade (Hussy).
            if (d.startsCarryPhase && d.carryDuration > 0f)
            {
                self.States.Set(WrestlerState.CarryParade);
                target.States.Set(WrestlerState.CarryParade);
                float t = 0f;
                while (t < d.carryDuration && !ctx.Aborted)
                {
                    t += Time.deltaTime;
                    self.transform.Rotate(0f, 90f * Time.deltaTime, 0f);
                    self.transform.position += self.transform.forward * d.carryMoveSpeed * Time.deltaTime;
                    var ring = RingInteractionSystem.Instance;
                    if (ring != null) self.transform.position = ring.Bounds.ClampInside(self.transform.position);
                    target.transform.position = self.transform.position + Vector3.up * 1.2f + self.transform.forward * 0.3f;
                    yield return null;
                }
                if (ctx.Aborted) yield break;
            }

            // Spin (Nicky).
            if (d.spinCount > 0 && d.spinDuration > 0f)
            {
                float t = 0f;
                float degreesPerSecond = 360f * d.spinCount / d.spinDuration;
                while (t < d.spinDuration && !ctx.Aborted)
                {
                    t += Time.deltaTime;
                    self.transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f);
                    target.transform.position = self.transform.position + Vector3.up * 1.3f;
                    yield return null;
                }
                if (ctx.Aborted) yield break;
            }

            // Impact.
            self.States.Set(WrestlerState.SpecialActive);
            yield return Wait(ctx, 0.45f);
            if (ctx.Aborted) yield break;

            target.Motor.SetScriptedControl(false);
            target.Motor.Teleport(self.transform.position + self.transform.forward * 0.9f);

            float damage = d.damage;
            // Major Glory's comeback scaling.
            if (d.specialVariant == "side-by-side")
            {
                if (self.Stats.HealthPercent < 0.2f) { damage += 5f; target.Combat.SetPendingKickoutPenalty(0.15f, d.pinWindow + 1.5f); }
                else if (self.Stats.HealthPercent < 0.4f) { damage += 3f; target.Combat.SetPendingKickoutPenalty(0.10f, d.pinWindow + 1.5f); }
            }

            ApplyOnHitEffects(ctx, damage);
            self.Motor.SetScriptedControl(false);
            self.States.Set(WrestlerState.SpecialRecovery, 0.8f);
        }

        static IEnumerator RunChokeVariant(SpecialContext ctx)
        {
            var self = ctx.Self;
            var target = ctx.Target;
            var d = ctx.Data;

            self.States.Set(WrestlerState.SpecialStartup);
            yield return Wait(ctx, 0.4f);
            if (ctx.Aborted) yield break;

            ctx.Controller.ReversalWindowOpen = false;
            // Weaker escapes when the target is gassed.
            float escapePenalty = target.Stats.StaminaPercent < 0.3f ? 0.2f : 0f;
            SubmissionSystem.Instance.TryStart(self, target,
                d.submissionPressurePerSecond, 0f, d.displayName + " (Choke)",
                d.submissionPressureBonus, d.opponentStaminaDrainPerSecond,
                d.selfStaminaDrainPerSecond, ignoreRopeBreak: false, canWin: true,
                initialDamage: d.initialDamage, escapePenalty: escapePenalty);
        }

        // ------------------------------------------------------------------
        // Submission specials (Avalon's Spotlight Crab).
        // ------------------------------------------------------------------
        public static IEnumerator RunSubmissionSpecial(SpecialContext ctx)
        {
            var self = ctx.Self;
            var target = ctx.Target;
            var d = ctx.Data;

            self.States.Set(WrestlerState.SpecialStartup);
            self.Motor.FaceOpponent();
            yield return Wait(ctx, d.setupDuration); // theatrical setup, interruptible
            if (ctx.Aborted) yield break;
            if (!target.States.IsDowned && d.requiresOpponentDowned) yield break;

            ctx.Controller.ReversalWindowOpen = false;
            if (!string.IsNullOrEmpty(d.debuffId) && d.debuffDuration > 0f)
                target.Buffs.Apply(BuildEffect(d.debuffId, d.debuffDuration));

            SubmissionSystem.Instance.TryStart(self, target,
                d.submissionPressurePerSecond, d.damagePerSecond, d.displayName,
                d.submissionPressureBonus, d.opponentStaminaDrainPerSecond,
                d.selfStaminaDrainPerSecond, ignoreRopeBreak: false, canWin: true,
                initialDamage: d.initialDamage);
        }
    }
}
