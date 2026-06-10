using UnityEngine;

namespace LoCoFight
{
    /// Checks every positional/arena/rule requirement of a special.
    /// Pure queries — never mutates state.
    public static class SpecialRequirementValidator
    {
        public static bool Validate(WrestlerCore self, WrestlerCore target, SpecialAbilityData data, out string reason)
        {
            reason = "";
            if (target == null) { reason = "No opponent"; return false; }
            var ring = RingInteractionSystem.Instance;
            var rules = MatchManager.Instance != null ? MatchManager.Instance.Rules : null;

            if (!self.States.IsStanding && self.States.Current != WrestlerState.GrappleLock)
            { reason = "Not in position"; return false; }

            if (data.healthThresholdOptional > 0f && self.Stats.HealthPercent > data.healthThresholdOptional)
            { reason = "Health too high"; return false; }

            // ---- Target state ----
            if (data.requiresOpponentDowned && !target.States.IsDowned)
            { reason = "Opponent must be down"; return false; }

            // Melee specials need the target in reach (rush/aerial/counter manage their own range).
            bool melee = data.category == SpecialCategory.SpecialPowerGrapple ||
                         data.category == SpecialCategory.SpecialSubmission ||
                         data.category == SpecialCategory.SpecialCombo;
            if (melee && MathUtil.FlatDistance(self.transform.position, target.transform.position) > 1.6f)
            { reason = "Too far away"; return false; }

            if (data.requiresOpponentStanding)
            {
                bool ok = target.States.IsStanding || target.States.Current == WrestlerState.GrappleLock ||
                          (data.requiresOpponentStunnedOk && target.States.IsGroggy);
                if (!ok) { reason = "Opponent must be standing"; return false; }
            }

            if (data.requiresOpponentCornered)
            {
                bool inCorner = target.States.Current == WrestlerState.Cornered ||
                                (ring != null && ring.IsInCornerZone(target) && target.States.IsStanding);
                if (!inCorner) { reason = "Opponent must be cornered"; return false; }
            }

            if (data.requiresOpponentInRopeStagger && target.States.Current != WrestlerState.RopeStaggered)
            {
                // Rope traps also accept a target standing right against the ropes.
                if (!(data.requiresOpponentNearRopes && ring != null && ring.IsNearRope(target, 0.8f) && target.States.IsStanding))
                { reason = "Opponent must be staggered on ropes"; return false; }
            }
            else if (data.requiresOpponentNearRopes && ring != null && !ring.IsNearRope(target, 1.0f))
            { reason = "Opponent must be near ropes"; return false; }

            // ---- Relative position ----
            if (data.requiresOpponentHeadPosition)
            {
                if (MathUtil.FlatDistance(self.transform.position, target.transform.position) > 1.4f)
                { reason = "Stand at opponent's head"; return false; }
            }

            if (data.requiresFrontPosition &&
                HitboxProbe.FacingDot(self.transform, target.transform) < 0.3f)
            { reason = "Must face opponent"; return false; }

            if (data.requiresSideBySidePosition)
            {
                bool sideBySide = HitboxProbe.SideBySideSameFacing(self.transform, target.transform, 1.4f);
                bool inLock = self.Combat.Role != GrappleRole.None;
                if (!sideBySide && !(inLock && data.requiresGrappleLockOk))
                { reason = "Need side-by-side position"; return false; }
            }

            if (data.requiresBackOrSidePosition && !HitboxProbe.IsBehindOrBeside(target.transform, self.transform))
            { reason = "Need back/side position"; return false; }

            if (data.requiresTargetLiftable &&
                !CombatResolver.ValidateLift(self, target, LiftStrengthClass.Low, out string liftReason))
            { reason = liftReason; return false; }

            // ---- Arena ----
            if (ring != null)
            {
                if (data.requiresTopCornerAnchor || data.requiresMiddleCornerAnchor || data.requiresRopeMiddleAnchor)
                {
                    if (!ring.IsValidAerialTarget(self, target, data))
                    { reason = "No valid launch position"; return false; }
                }

                if (data.requiresRopeTrapZone && !ring.IsValidRopeTrapTarget(self, target))
                { reason = "No rope trap position"; return false; }

                if (data.requiresCornerZone && !ring.IsInCornerZone(target))
                { reason = "Opponent not in corner"; return false; }

                if (data.requiresRopeReboundLane &&
                    !ring.IsValidRopeReboundLane(self.transform.position, target.transform.position))
                { reason = "No clear rope lane"; return false; }

                if (data.usesChargeMovement && !ring.Bounds.IsInside(target.transform.position, 0.2f))
                { reason = "No rush lane"; return false; }
            }

            // ---- Rules ----
            if (data.usesRefereeDistraction && rules != null && (!rules.allowDirtyMoves || !rules.allowRefDistraction))
            { reason = "Dirty moves not allowed"; return false; }

            if (data.usesConeHitDetection &&
                !HitboxProbe.ConeCheck(self.transform, target.transform, data.coneRange, data.coneAngle))
            { reason = "Opponent not in front"; return false; }

            return true;
        }
    }
}
