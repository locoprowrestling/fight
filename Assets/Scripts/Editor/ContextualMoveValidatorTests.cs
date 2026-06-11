using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class ContextualMoveValidatorTests
    {
        [Test]
        public void ValidatePure_RejectsInsufficientStamina()
        {
            var result = ContextualMoveValidator.ValidatePure(
                moveExists: true,
                matchActive: true,
                attackerCanAct: true,
                targetStateValid: true,
                contextValid: true,
                currentStamina: 8f,
                requiredStamina: 10f);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.InsufficientStamina));
        }

        [Test]
        public void ValidatePure_RejectsMissingMoveBeforeAnythingElse()
        {
            var result = ContextualMoveValidator.ValidatePure(
                false, false, false, false, false, 0f, 10f);

            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.MissingMove));
        }

        [Test]
        public void ValidatePure_RejectsInactiveMatch()
        {
            var result = ContextualMoveValidator.ValidatePure(
                true, false, true, true, true, 100f, 10f);

            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.MatchInactive));
        }

        [Test]
        public void ValidatePure_AcceptsValidRequest()
        {
            var result = ContextualMoveValidator.ValidatePure(
                true, true, true, true, true, 20f, 10f);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.None));
        }
    }
}
