using UnityEngine;

namespace LoCoFight
{
    /// Pure context resolution: which contextual move family applies right
    /// now, evaluated when an action is attempted. Priority is fixed:
    /// grapple lock > downed target > cornered target > rope-staggered
    /// target > rebounding attacker > standing.
    public static class CombatContextResolver
    {
        public static CombatContext ResolvePriority(
            bool grappleLock,
            bool targetDowned,
            bool targetCornered,
            bool targetRopeStaggered,
            bool attackerRebounding)
        {
            if (grappleLock) return CombatContext.GrappleLock;
            if (targetDowned) return CombatContext.GroundUpper;
            if (targetCornered) return CombatContext.Corner;
            if (targetRopeStaggered) return CombatContext.RopeStagger;
            if (attackerRebounding) return CombatContext.RopeRebound;
            return CombatContext.Standing;
        }

        /// Transient resolution from live wrestlers, evaluated at the moment an
        /// action is attempted. Geometry questions go to RingInteractionSystem;
        /// state questions go to the WrestlerStateMachine.
        public static CombatContext Resolve(WrestlerCore attacker, WrestlerCore defender)
        {
            bool grapple = attacker != null && attacker.Combat != null &&
                           attacker.Combat.InGrappleLockAsAttacker;
            bool downed = defender != null && defender.States.IsDowned;
            bool cornered = defender != null &&
                            defender.States.Current == WrestlerState.Cornered &&
                            RingInteractionSystem.Instance != null &&
                            RingInteractionSystem.Instance.IsInCornerZone(defender);
            bool ropeStaggered = defender != null &&
                                 defender.States.Current == WrestlerState.RopeStaggered &&
                                 RingInteractionSystem.Instance != null &&
                                 RingInteractionSystem.Instance.IsNearRope(
                                     defender, RingInteractionSystem.RopeContactRange + 0.2f);
            bool rebounding = attacker != null &&
                              (attacker.States.Current == WrestlerState.RopeReboundRun ||
                               attacker.States.Current == WrestlerState.RopeReboundReturn);

            CombatContext context = ResolvePriority(
                grapple, downed, cornered, ropeStaggered, rebounding);
            if (context == CombatContext.GroundUpper)
            {
                GroundTargetZone zone = ResolveGroundZone(
                    defender.transform.position,
                    defender.transform.forward,
                    attacker.transform.position,
                    0.2f);
                return zone == GroundTargetZone.Lower
                    ? CombatContext.GroundLower
                    : CombatContext.GroundUpper;
            }
            return context;
        }

        /// Upper/lower half of a downed defender, from the attacker's flat
        /// offset projected onto the defender's facing axis. Inside the
        /// threshold band (roughly side-on) neither zone applies.
        public static GroundTargetZone ResolveGroundZone(
            Vector3 defenderPosition,
            Vector3 defenderForward,
            Vector3 attackerPosition,
            float threshold)
        {
            Vector3 toAttacker = MathUtil.Flat(attackerPosition - defenderPosition);
            if (toAttacker.sqrMagnitude < 0.001f) return GroundTargetZone.None;

            float dot = Vector3.Dot(
                MathUtil.Flat(defenderForward).normalized,
                toAttacker.normalized);
            if (dot >= threshold) return GroundTargetZone.Upper;
            if (dot <= -threshold) return GroundTargetZone.Lower;
            return GroundTargetZone.None;
        }
    }
}
