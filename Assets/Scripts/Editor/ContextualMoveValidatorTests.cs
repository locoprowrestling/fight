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

        [Test]
        public void ValidateGround_RejectsWrongZone()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.requiresTargetDowned = true;
            move.requiredGroundZone = GroundTargetZone.Upper;

            var result = ContextualMoveValidator.ValidateGround(
                move, targetDowned: true, actualZone: GroundTargetZone.Lower,
                inRange: true, currentStamina: 100f);

            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.WrongGroundZone));
        }

        [Test]
        public void ValidateGround_RejectsStandingTarget()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.requiresTargetDowned = true;
            move.requiredGroundZone = GroundTargetZone.Upper;

            var result = ContextualMoveValidator.ValidateGround(
                move, false, GroundTargetZone.Upper, true, 100f);

            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.WrongTargetState));
        }

        [Test]
        public void ValidateGround_RejectsOutOfRangeBeforeStamina()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.requiresTargetDowned = true;
            move.requiredGroundZone = GroundTargetZone.Lower;

            var result = ContextualMoveValidator.ValidateGround(
                move, true, GroundTargetZone.Lower, false, 0f);

            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.OutOfRange));
        }

        [Test]
        public void ValidateGround_AcceptsMatchingZone()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.requiresTargetDowned = true;
            move.requiredGroundZone = GroundTargetZone.Lower;

            var result = ContextualMoveValidator.ValidateGround(
                move, true, GroundTargetZone.Lower, true, 100f);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateCorner_RequiresStateAndGeometry()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.requiresTargetCornered = true;

            Assert.That(
                ContextualMoveValidator.ValidateCorner(
                    move, targetCornered: true, inCornerZone: false,
                    inRange: true, currentStamina: 100f).Reason,
                Is.EqualTo(MoveRejectionReason.NotInCorner));

            Assert.That(
                ContextualMoveValidator.ValidateCorner(
                    move, false, true, true, 100f).Reason,
                Is.EqualTo(MoveRejectionReason.WrongTargetState));
        }

        [Test]
        public void ValidateCorner_AcceptsCorneredTargetInZone()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.requiresTargetCornered = true;

            Assert.That(
                ContextualMoveValidator.ValidateCorner(
                    move, true, true, true, 100f).IsValid,
                Is.True);
        }

        [Test]
        public void ValidateRopeStagger_RejectsNormalStunnedTarget()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.requiresTargetRopeStaggered = true;

            var result = ContextualMoveValidator.ValidateRope(
                move, targetRopeStaggered: false, targetNearRope: true,
                attackerRebounding: false, inRange: true, currentStamina: 100f);

            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.WrongTargetState));
        }

        [Test]
        public void ValidateRopeStagger_RejectsTargetAwayFromRopes()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.requiresTargetRopeStaggered = true;
            move.requiresOpponentNearRopes = true;

            var result = ContextualMoveValidator.ValidateRope(
                move, true, false, false, true, 100f);

            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.NotNearRopes));
        }

        [Test]
        public void ValidateRebound_RequiresReboundState()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.requiresRopeRebound = true;

            var result = ContextualMoveValidator.ValidateRope(
                move, false, false, attackerRebounding: false,
                inRange: true, currentStamina: 100f);

            Assert.That(result.Reason, Is.EqualTo(MoveRejectionReason.NotRebounding));
        }

        [Test]
        public void ValidateRebound_AcceptsReboundingAttacker()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.requiresRopeRebound = true;

            var result = ContextualMoveValidator.ValidateRope(
                move, false, false, true, true, 100f);

            Assert.That(result.IsValid, Is.True);
        }
    }
}
