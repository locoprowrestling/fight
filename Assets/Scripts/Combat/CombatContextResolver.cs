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
