using NUnit.Framework;

namespace LoCoFight.EditorTests
{
    public class CombatPresentationRulesTests
    {
        [Test]
        public void StrongReversalIsStrongerThanBasicReversal()
        {
            var basic = CombatPresentationRules.For(
                CombatPresentationEvent.BasicReversal);
            var strong = CombatPresentationRules.For(
                CombatPresentationEvent.StrongReversal);

            Assert.That(strong.HitStopSeconds, Is.GreaterThan(basic.HitStopSeconds));
            Assert.That(strong.CameraStrength, Is.GreaterThan(basic.CameraStrength));
        }

        [Test]
        public void RopeBreakHasNoHeavyHitStop()
        {
            var result = CombatPresentationRules.For(
                CombatPresentationEvent.RopeBreak);
            Assert.That(result.HitStopSeconds, Is.LessThanOrEqualTo(0.02f));
        }

        [Test]
        public void SubmissionEscapeHasNoHeavyHitStop()
        {
            var result = CombatPresentationRules.For(
                CombatPresentationEvent.SubmissionEscape);
            Assert.That(result.HitStopSeconds, Is.LessThanOrEqualTo(0.02f));
        }

        [Test]
        public void ImpactHierarchyEscalatesWithTier()
        {
            var light = CombatPresentationRules.For(CombatPresentationEvent.LightImpact);
            var medium = CombatPresentationRules.For(CombatPresentationEvent.MediumImpact);
            var heavy = CombatPresentationRules.For(CombatPresentationEvent.HeavyImpact);
            var special = CombatPresentationRules.For(CombatPresentationEvent.SpecialImpact);

            Assert.That(medium.HitStopSeconds, Is.GreaterThan(light.HitStopSeconds));
            Assert.That(heavy.HitStopSeconds, Is.GreaterThan(medium.HitStopSeconds));
            Assert.That(special.HitStopSeconds, Is.GreaterThanOrEqualTo(heavy.HitStopSeconds));
            Assert.That(special.CameraStrength, Is.GreaterThanOrEqualTo(heavy.CameraStrength));
        }

        [Test]
        public void SpecialReadyNeverFreezesTheGame()
        {
            var result = CombatPresentationRules.For(
                CombatPresentationEvent.SpecialReady);
            Assert.That(result.HitStopSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void EveryEventResolvesToNonNegativeSettings()
        {
            foreach (CombatPresentationEvent evt in
                     System.Enum.GetValues(typeof(CombatPresentationEvent)))
            {
                var result = CombatPresentationRules.For(evt);
                Assert.That(result.HitStopSeconds, Is.GreaterThanOrEqualTo(0f), evt.ToString());
                Assert.That(result.CameraStrength, Is.GreaterThanOrEqualTo(0f), evt.ToString());
            }
        }
    }
}
