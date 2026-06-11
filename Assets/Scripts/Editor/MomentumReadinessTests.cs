using NUnit.Framework;

namespace LoCoFight.EditorTests
{
    public class MomentumReadinessTests
    {
        [Test]
        public void UpdateReadiness_FiresOnlyOnThresholdTransitions()
        {
            Assert.That(
                WrestlerStatsRuntime.ResolveReadinessTransition(false, 1f),
                Is.EqualTo(1));
            Assert.That(
                WrestlerStatsRuntime.ResolveReadinessTransition(true, 1f),
                Is.EqualTo(0));
            Assert.That(
                WrestlerStatsRuntime.ResolveReadinessTransition(true, 0.5f),
                Is.EqualTo(-1));
        }

        [Test]
        public void UpdateReadiness_NotReadyStaysQuietBelowThreshold()
        {
            Assert.That(
                WrestlerStatsRuntime.ResolveReadinessTransition(false, 0f),
                Is.EqualTo(0));
            Assert.That(
                WrestlerStatsRuntime.ResolveReadinessTransition(false, 0.99f),
                Is.EqualTo(0));
        }

        [Test]
        public void UpdateReadiness_ThresholdHasSmallTolerance()
        {
            // Momentum is clamped to max, but float math may land a hair
            // under; the transition must still fire.
            Assert.That(
                WrestlerStatsRuntime.ResolveReadinessTransition(false, 0.99995f),
                Is.EqualTo(1));
        }
    }
}
