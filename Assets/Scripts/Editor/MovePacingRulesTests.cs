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
    }
}
