using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class SubmissionEscapeRulesTests
    {
        [TestCase(1f, 1f)]
        [TestCase(0.5f, 1f)]
        [TestCase(0f, 0.35f)]
        [TestCase(-0.25f, 0f)]
        [TestCase(-1f, 0f)]
        public void DirectionQuality_MapsDotToCrawlStrength(float dot, float expected)
        {
            Assert.That(
                SubmissionEscapeRules.DirectionQuality(dot),
                Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void CrawlRate_TowardExceedsSidewaysExceedsAway()
        {
            float toward = SubmissionEscapeRules.CrawlRate(
                SubmissionEscapeRules.DirectionQuality(1f), 1f, 0.5f);
            float sideways = SubmissionEscapeRules.CrawlRate(
                SubmissionEscapeRules.DirectionQuality(0f), 1f, 0.5f);
            float away = SubmissionEscapeRules.CrawlRate(
                SubmissionEscapeRules.DirectionQuality(-1f), 1f, 0.5f);

            Assert.That(toward, Is.GreaterThan(sideways));
            Assert.That(sideways, Is.GreaterThan(away));
            Assert.That(away, Is.EqualTo(0f));
        }

        [Test]
        public void CrawlRate_LowStaminaIsWeaker()
        {
            float full = SubmissionEscapeRules.CrawlRate(1f, 1f, 0.5f);
            float tired = SubmissionEscapeRules.CrawlRate(1f, 0.1f, 0.5f);
            Assert.That(tired, Is.LessThan(full));
            Assert.That(tired, Is.GreaterThan(0f));
        }

        [Test]
        public void ActiveEscapePerPress_LowStaminaIsWeaker()
        {
            float fresh = SubmissionEscapeRules.ActiveEscapePerPress(1f, 1f, 0f);
            float tired = SubmissionEscapeRules.ActiveEscapePerPress(0.05f, 1f, 0f);
            Assert.That(tired, Is.LessThan(fresh));
            Assert.That(tired, Is.GreaterThan(0f));
        }

        [Test]
        public void ActiveEscapePerPress_PenaltyAndMultiplierAreBounded()
        {
            Assert.That(
                SubmissionEscapeRules.ActiveEscapePerPress(1f, 1f, 1.5f),
                Is.EqualTo(0f));
            Assert.That(
                SubmissionEscapeRules.ActiveEscapePerPress(1f, -2f, 0f),
                Is.EqualTo(0f));
        }

        [Test]
        public void CrawlWithoutRopeBreaksConvertsToReducedEscape()
        {
            Assert.That(
                SubmissionEscapeRules.CrawlEscapeRate(1f, ropeBreaksActive: false),
                Is.GreaterThan(0f));
        }

        [Test]
        public void CrawlEscapeRate_ZeroWhileRopeBreaksAreActive()
        {
            Assert.That(
                SubmissionEscapeRules.CrawlEscapeRate(1f, ropeBreaksActive: true),
                Is.EqualTo(0f));
        }

        [Test]
        public void CrawlEscapeRate_NoQualityNoEscape()
        {
            Assert.That(
                SubmissionEscapeRules.CrawlEscapeRate(0f, ropeBreaksActive: false),
                Is.EqualTo(0f));
        }

        [Test]
        public void ApplyPairDelta_PreservesAttackerDefenderOffset()
        {
            Vector3 attacker = new Vector3(1f, 0f, 2f);
            Vector3 defender = new Vector3(0.4f, 0f, 1.2f);
            Vector3 offsetBefore = attacker - defender;
            Vector3 delta = new Vector3(0.3f, 0f, -0.15f);

            SubmissionEscapeRules.ApplyPairDelta(
                ref attacker, ref defender, delta);

            Assert.That(
                (attacker - defender - offsetBefore).magnitude,
                Is.LessThan(0.0001f));
            Assert.That(
                (defender - new Vector3(0.7f, 0f, 1.05f)).magnitude,
                Is.LessThan(0.0001f));
        }
    }
}
