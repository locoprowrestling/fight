using System.Reflection;
using NUnit.Framework;

namespace LoCoFight.EditorTests
{
    public class AIPersonalityProfileTests
    {
        [Test]
        public void For_EveryPersonalityReturnsBoundedProfile()
        {
            foreach (AIPersonality personality in
                     System.Enum.GetValues(typeof(AIPersonality)))
            {
                var profile = AIPersonalityProfiles.For(personality);
                Assert.That(profile.Aggression, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.Strike, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.Grapple, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.PowerMove, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.GroundOffense, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.Submission, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.PinUrgency, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.SpecialSetup, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.RopeCornerStrategy, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.RiskTolerance, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.BreatherFrequency, Is.InRange(0.75f, 1.25f), personality.ToString());
                Assert.That(profile.RepetitionTolerance, Is.InRange(0.5f, 1.5f), personality.ToString());
            }
        }

        [Test]
        public void Constructor_ClampsEveryMultiplier()
        {
            var profile = new AIPersonalityProfile(
                aggression: 5f, strike: -1f, grapple: 2f, powerMove: 0f,
                groundOffense: 9f, submission: 0.1f, pinUrgency: 3f,
                specialSetup: -4f, ropeCornerStrategy: 2f, riskTolerance: 0f,
                breatherFrequency: 99f, repetitionTolerance: 12f);

            Assert.That(profile.Aggression, Is.EqualTo(1.25f));
            Assert.That(profile.Strike, Is.EqualTo(0.75f));
            Assert.That(profile.RepetitionTolerance, Is.EqualTo(1.5f));
        }

        [Test]
        public void For_TechnicianFavorsGrapplesAndSubmissionsOverStrikes()
        {
            var p = AIPersonalityProfiles.For(AIPersonality.Technician);
            Assert.That(p.Grapple, Is.GreaterThan(p.Strike));
            Assert.That(p.Submission, Is.GreaterThan(1f));
        }

        [Test]
        public void For_PowerhouseFavorsPowerMovesWithLowerRisk()
        {
            var p = AIPersonalityProfiles.For(AIPersonality.Powerhouse);
            Assert.That(p.PowerMove, Is.GreaterThan(1f));
            Assert.That(p.RiskTolerance, Is.LessThan(1f));
        }

        [Test]
        public void For_HighFlyerFavorsRopeStrategyAndRisk()
        {
            var p = AIPersonalityProfiles.For(AIPersonality.HighFlyer);
            Assert.That(p.RopeCornerStrategy, Is.GreaterThan(1f));
            Assert.That(p.RiskTolerance, Is.GreaterThan(1f));
        }

        [Test]
        public void For_ShowmanBreathesMoreAndSetsUpSpecials()
        {
            var p = AIPersonalityProfiles.For(AIPersonality.Showman);
            Assert.That(p.BreatherFrequency, Is.GreaterThan(1f));
            Assert.That(p.SpecialSetup, Is.GreaterThan(1f));
        }

        [Test]
        public void For_EvasiveHasLowerAggression()
        {
            var p = AIPersonalityProfiles.For(AIPersonality.Evasive);
            Assert.That(p.Aggression, Is.LessThan(1f));
        }

        [Test]
        public void For_BrawlerFavorsStrikesOverGrapples()
        {
            var p = AIPersonalityProfiles.For(AIPersonality.Brawler);
            Assert.That(p.Strike, Is.GreaterThan(p.Grapple));
        }

        [Test]
        public void For_UnknownValueReturnsBalanced()
        {
            var p = AIPersonalityProfiles.For((AIPersonality)999);
            Assert.That(p.Aggression, Is.EqualTo(1f));
            Assert.That(p.Strike, Is.EqualTo(1f));
            Assert.That(p.Grapple, Is.EqualTo(1f));
            Assert.That(p.RepetitionTolerance, Is.EqualTo(1f));
        }

        [Test]
        public void Profile_ContainsOnlyDecisionMultipliersNoAccuracy()
        {
            // Difficulty owns reversal/dodge accuracy; profiles must never
            // smuggle accuracy or timing fields in.
            var members = typeof(AIPersonalityProfile).GetMembers(
                BindingFlags.Public | BindingFlags.Instance);
            foreach (var member in members)
            {
                string name = member.Name.ToLowerInvariant();
                Assert.That(name, Does.Not.Contain("accuracy"));
                Assert.That(name, Does.Not.Contain("dodge"));
                Assert.That(name, Does.Not.Contain("reaction"));
                Assert.That(name, Does.Not.Contain("kickout"));
            }

            foreach (var field in typeof(AIPersonalityProfile).GetFields(
                BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.That(field.FieldType, Is.EqualTo(typeof(float)),
                    $"{field.Name} must be a bounded float multiplier");
                Assert.That(field.IsInitOnly, Is.True,
                    $"{field.Name} must be immutable");
            }
        }
    }
}
