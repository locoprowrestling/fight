using NUnit.Framework;

namespace LoCoFight.EditorTests
{
    public class AIDecisionWeightsTests
    {
        [Test]
        public void Apply_MultipliesBoundedFactors()
        {
            Assert.That(
                AIDecisionWeights.Apply(0.5f, 1.2f, 0.5f),
                Is.EqualTo(0.3f).Within(0.001f));
        }

        [Test]
        public void Apply_ClampsEveryInput()
        {
            // Base weight beyond 1, multiplier beyond bounds, negative penalty.
            Assert.That(
                AIDecisionWeights.Apply(5f, 9f, -3f),
                Is.EqualTo(1f));
            // Full repetition penalty zeroes the weight.
            Assert.That(
                AIDecisionWeights.Apply(1f, 1f, 1f),
                Is.EqualTo(0f));
            // Multiplier can never push below the personality floor.
            Assert.That(
                AIDecisionWeights.Apply(1f, 0f, 0f),
                Is.EqualTo(0.75f).Within(0.001f));
        }

        [Test]
        public void ChooseWeighted_IsDeterministicForASuppliedRoll()
        {
            var strike = new WeightedAIAction(AIState.AttemptLightStrike, 0.25f);
            var grapple = new WeightedAIAction(AIState.AttemptGrapple, 0.75f);

            Assert.That(
                AIDecisionWeights.ChooseWeighted(0.1f, strike, grapple),
                Is.EqualTo(AIState.AttemptLightStrike));
            Assert.That(
                AIDecisionWeights.ChooseWeighted(0.9f, strike, grapple),
                Is.EqualTo(AIState.AttemptGrapple));
            Assert.That(
                AIDecisionWeights.ChooseWeighted(0.9f, strike, grapple),
                Is.EqualTo(AIDecisionWeights.ChooseWeighted(0.9f, strike, grapple)));
        }

        [Test]
        public void ChooseWeighted_SkipsNonPositiveWeights()
        {
            var dead = new WeightedAIAction(AIState.AttemptHeavyStrike, 0f);
            var live = new WeightedAIAction(AIState.AttemptGrapple, 0.4f);

            Assert.That(
                AIDecisionWeights.ChooseWeighted(0f, dead, live),
                Is.EqualTo(AIState.AttemptGrapple));
        }

        [Test]
        public void ChooseWeighted_NoPositiveWeightFallsBackToIdleThink()
        {
            Assert.That(
                AIDecisionWeights.ChooseWeighted(
                    0.5f,
                    new WeightedAIAction(AIState.AttemptGrapple, 0f),
                    new WeightedAIAction(AIState.AttemptLightStrike, -2f)),
                Is.EqualTo(AIState.IdleThink));
            Assert.That(
                AIDecisionWeights.ChooseWeighted(0.5f),
                Is.EqualTo(AIState.IdleThink));
        }

        [Test]
        public void ChooseWeighted_EdgeRollsStayInRange()
        {
            var a = new WeightedAIAction(AIState.AttemptLightStrike, 0.5f);
            var b = new WeightedAIAction(AIState.AttemptGrapple, 0.5f);

            Assert.That(
                AIDecisionWeights.ChooseWeighted(0f, a, b),
                Is.EqualTo(AIState.AttemptLightStrike));
            Assert.That(
                AIDecisionWeights.ChooseWeighted(1f, a, b),
                Is.EqualTo(AIState.AttemptGrapple));
            Assert.That(
                AIDecisionWeights.ChooseWeighted(7f, a, b),
                Is.EqualTo(AIState.AttemptGrapple));
        }
    }
}
