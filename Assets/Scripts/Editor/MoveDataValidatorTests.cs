using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class MoveDataValidatorTests
    {
        [Test]
        public void Validate_ReversalWindowOutsideDurationIsError()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "bad-window";
            move.startupTime = 0.1f;
            move.activeTime = 0.1f;
            move.recoveryTime = 0.1f;
            move.reversalWindowEnd = 0.5f;

            var messages = MoveDataValidator.Validate(move, null);
            Assert.That(messages, Has.Some.Contains("reversal window"));
        }

        [Test]
        public void Validate_LiftTagWithoutLiftValidationIsError()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "untagged-lift";
            move.reversalWindowEnd = 0.1f;
            move.tags.Add(MoveTag.Lift);
            move.requiresLift = false;

            var messages = MoveDataValidator.Validate(move, null);
            Assert.That(messages, Has.Some.Contains("lift"));
        }

        [Test]
        public void Validate_GroundZoneWithoutDownedRequirementIsError()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "floating-zone";
            move.reversalWindowEnd = 0.1f;
            move.requiredGroundZone = GroundTargetZone.Upper;
            move.requiresTargetDowned = false;

            var messages = MoveDataValidator.Validate(move, null);
            Assert.That(messages, Has.Some.Contains("ground zone"));
        }

        [Test]
        public void Validate_AcceptsWellFormedMove()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "fine";
            move.startupTime = 0.2f;
            move.activeTime = 0.1f;
            move.recoveryTime = 0.3f;
            move.reversalWindowStart = 0.05f;
            move.reversalWindowEnd = 0.2f;

            Assert.That(MoveDataValidator.Validate(move, null), Is.Empty);
        }

        [Test]
        public void Validate_DirectionalSetRequiresNeutralFallback()
        {
            var set = new DirectionalMoveSet();
            set.forward.Add(ScriptableObject.CreateInstance<MoveData>());

            var messages = MoveDataValidator.ValidateDirectionalSet("quick", set);
            Assert.That(messages, Has.Some.Contains("neutral"));
        }

        [Test]
        public void Validate_DirectionalSetWithNeutralContentPasses()
        {
            var set = new DirectionalMoveSet();
            set.forward.Add(ScriptableObject.CreateInstance<MoveData>());
            set.neutral.Add(ScriptableObject.CreateInstance<MoveData>());

            Assert.That(MoveDataValidator.ValidateDirectionalSet("quick", set), Is.Empty);
        }
    }
}
