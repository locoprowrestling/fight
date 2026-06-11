using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class MovePacingRulesTests
    {
        [Test]
        public void RequiredStamina_UsesGreaterOfCostAndMinimum()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.staminaCost = 12f;
            move.minimumStamina = 20f;

            Assert.That(MovePacingRules.RequiredStamina(move), Is.EqualTo(20f));
        }

        [Test]
        public void RequiredStamina_MissingMoveIsNeverAffordable()
        {
            Assert.That(MovePacingRules.RequiredStamina(null), Is.EqualTo(float.PositiveInfinity));
        }

        [Test]
        public void CanAttempt_LightMoveRemainsAvailableWhenCostIsAffordable()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.tier = MoveTier.Light;
            move.staminaCost = 4f;

            Assert.That(MovePacingRules.CanAttempt(move, 5f), Is.True);
        }

        [Test]
        public void CanAttempt_HeavyMoveRequiresMinimumStamina()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.tier = MoveTier.Heavy;
            move.staminaCost = 15f;
            move.minimumStamina = 30f;

            Assert.That(MovePacingRules.CanAttempt(move, 20f), Is.False);
        }

        MoveData Costing(float cost)
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.staminaCost = cost;
            return move;
        }

        [Test]
        public void CheapestAffordable_PicksTheLowestAffordableRequirement()
        {
            var cheap = Costing(6f);
            var mid = Costing(12f);
            var dear = Costing(25f);

            var result = MovePacingRules.CheapestAffordable(
                new[] { dear, mid, cheap }, 15f);

            Assert.That(result, Is.SameAs(cheap));
        }

        [Test]
        public void CheapestAffordable_ReturnsNullWhenNothingIsAffordable()
        {
            Assert.That(
                MovePacingRules.CheapestAffordable(new[] { Costing(20f) }, 10f),
                Is.Null);
        }

        [Test]
        public void CheapestAffordable_ToleratesNullCandidatesAndEntries()
        {
            Assert.That(MovePacingRules.CheapestAffordable(null, 50f), Is.Null);
            Assert.That(
                MovePacingRules.CheapestAffordable(new MoveData[] { null, Costing(5f) }, 50f),
                Is.Not.Null);
        }
    }
}
