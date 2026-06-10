using UnityEngine;

namespace LoCoFight
{
    /// Stateless range / facing / cone checks used by combat and specials.
    public static class HitboxProbe
    {
        public static bool InRange(Transform a, Transform b, float range)
        {
            return MathUtil.FlatDistance(a.position, b.position) <= range;
        }

        public static float FacingDot(Transform attacker, Transform target)
        {
            Vector3 toTarget = MathUtil.FlatDirection(attacker.position, target.position);
            return Vector3.Dot(MathUtil.Flat(attacker.forward).normalized, toTarget);
        }

        /// True when target is within range and inside the attacker's forward cone.
        public static bool ConeCheck(Transform attacker, Transform target, float range, float coneAngleDegrees)
        {
            if (!InRange(attacker, target, range)) return false;
            float dot = FacingDot(attacker, target);
            float limit = Mathf.Cos(coneAngleDegrees * 0.5f * Mathf.Deg2Rad);
            return dot >= limit;
        }

        /// True when 'other' is behind or beside 'subject' (used by Dean's variant pick).
        public static bool IsBehindOrBeside(Transform subject, Transform other)
        {
            Vector3 toOther = MathUtil.FlatDirection(subject.position, other.position);
            float dot = Vector3.Dot(MathUtil.Flat(subject.forward).normalized, toOther);
            return dot < 0.35f;
        }

        /// True when the two are roughly side by side and facing the same direction (Patriot Plunge).
        public static bool SideBySideSameFacing(Transform a, Transform b, float maxDistance)
        {
            if (MathUtil.FlatDistance(a.position, b.position) > maxDistance) return false;
            float facing = Vector3.Dot(MathUtil.Flat(a.forward).normalized, MathUtil.Flat(b.forward).normalized);
            return facing > 0.5f;
        }
    }
}
