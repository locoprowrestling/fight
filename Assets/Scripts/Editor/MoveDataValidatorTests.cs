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

        [Test]
        public void Validate_StrongCounterWithoutDirectionIsError()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "directionless-counter";
            move.reversalWindowEnd = 0.1f;
            move.allowsStrongDirectionalCounter = true;
            move.preferredCounterDirection = ReversalReadDirection.Neutral;

            var messages = MoveDataValidator.Validate(move, null);
            Assert.That(messages, Has.Some.Contains("requires a direction"));
        }

        [Test]
        public void Validate_NegativeReversalMomentumIsError()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "negative-momentum";
            move.reversalWindowEnd = 0.1f;
            move.basicReversalMomentum = -1f;

            var messages = MoveDataValidator.Validate(move, null);
            Assert.That(messages, Has.Some.Contains("reversal momentum"));
        }

        [Test]
        public void Validate_NegativeReversalStaggerIsError()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "negative-stagger";
            move.reversalWindowEnd = 0.1f;
            move.strongReversalStagger = -0.5f;

            var messages = MoveDataValidator.Validate(move, null);
            Assert.That(messages, Has.Some.Contains("reversal stagger"));
        }

        [Test]
        public void Validate_NegativeReversalSeparationIsError()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "negative-separation";
            move.reversalWindowEnd = 0.1f;
            move.basicReversalSeparation = -0.25f;

            var messages = MoveDataValidator.Validate(move, null);
            Assert.That(messages, Has.Some.Contains("reversal separation"));
        }

        [Test]
        public void Validate_AcceptsWellFormedStrongCounter()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "readable-counter";
            move.startupTime = 0.2f;
            move.activeTime = 0.1f;
            move.recoveryTime = 0.3f;
            move.reversalWindowStart = 0.05f;
            move.reversalWindowEnd = 0.2f;
            move.allowsStrongDirectionalCounter = true;
            move.preferredCounterDirection = ReversalReadDirection.Away;

            Assert.That(MoveDataValidator.Validate(move, null), Is.Empty);
        }

        [Test]
        public void ValidateWarnings_HeavyMoveWithoutMinimumStaminaWarns()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "heavy";
            move.tier = MoveTier.Heavy;
            move.minimumStamina = 0f;

            var warnings = MoveDataValidator.ValidateWarnings(move, null);
            Assert.That(warnings, Has.Some.Contains("minimum stamina"));
        }

        [Test]
        public void ValidateWarnings_ShortHeavyRecoveryWarns()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "rushed-heavy";
            move.tier = MoveTier.Heavy;
            move.minimumStamina = 20f;
            move.recoveryTime = 0.2f;

            var warnings = MoveDataValidator.ValidateWarnings(move, null);
            Assert.That(warnings, Has.Some.Contains("recovery"));
        }

        [Test]
        public void ValidateWarnings_QuietForWellPacedMove()
        {
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.moveId = "paced";
            move.tier = MoveTier.Heavy;
            move.minimumStamina = 20f;
            move.recoveryTime = 0.6f;

            Assert.That(MoveDataValidator.ValidateWarnings(move, null), Is.Empty);
        }
    }
}
