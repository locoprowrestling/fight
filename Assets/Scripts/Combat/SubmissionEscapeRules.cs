using UnityEngine;

namespace LoCoFight
{
    /// Pure submission-defense math: crawl direction quality, stamina
    /// scaling, active escape effort, and the no-rope-break escape
    /// conversion. SubmissionSystem owns when these run; ring clamping stays
    /// with RingInteractionSystem.
    public static class SubmissionEscapeRules
    {
        public const float BaseCrawlSpeed = 0.45f;
        public const float CrawlStaminaPerSecond = 7f;
        public const float NoRopeBreakEscapePerSecond = 5f;

        /// Maps the dot between crawl intent and the to-rope direction onto
        /// three readable bands: toward = full, sideways = reduced, away or
        /// missing = nothing.
        public static float DirectionQuality(float dot)
        {
            if (dot >= 0.5f) return 1f;
            if (dot > -0.25f) return 0.35f;
            return 0f;
        }

        /// An exhausted defender still crawls, just much slower.
        public static float StaminaFactor(float staminaPercent) =>
            Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(staminaPercent));

        public static float CrawlRate(
            float directionQuality,
            float staminaPercent,
            float submissionResistance) =>
            BaseCrawlSpeed *
            Mathf.Clamp01(directionQuality) *
            StaminaFactor(staminaPercent) *
            Mathf.Lerp(0.8f, 1.2f, Mathf.Clamp01(submissionResistance));

        /// One mash press worth of escape meter. escapePenalty comes from the
        /// authored hold; escapeMultiplier from buffs/traits.
        public static float ActiveEscapePerPress(
            float staminaPercent,
            float escapeMultiplier,
            float escapePenalty) =>
            4f *
            StaminaFactor(staminaPercent) *
            Mathf.Max(0f, escapeMultiplier) *
            Mathf.Clamp01(1f - escapePenalty);

        /// With rope breaks disabled, crawl intent converts to reduced escape
        /// meter per second so movement is never a dead input. While rope
        /// breaks are active the crawl pays off in position instead.
        public static float CrawlEscapeRate(
            float directionQuality,
            bool ropeBreaksActive) =>
            ropeBreaksActive
                ? 0f
                : NoRopeBreakEscapePerSecond * Mathf.Clamp01(directionQuality);

        /// Moves the attached pair by one shared delta, preserving their
        /// relative offset. Callers clamp the results to ring bounds.
        public static void ApplyPairDelta(
            ref Vector3 attackerPosition,
            ref Vector3 defenderPosition,
            Vector3 delta)
        {
            attackerPosition += delta;
            defenderPosition += delta;
        }
    }
}
