using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class MoveChoreographyValidatorTests
    {
        [Test]
        public void Validate_PairedMoveRequiresBothStateKeys()
        {
            var data = ValidPaired();
            data.defenderStateKey = "";

            Assert.That(MoveChoreographyValidator.Validate(data),
                Has.Some.Contains("defender state"));
        }

        [Test]
        public void Validate_PhasesMustCoverNormalizedTimeline()
        {
            var data = ValidPaired();
            data.phases.Clear();
            data.phases.Add(new AnimationPhaseDefinition
            {
                phase = AnimationPhase.Setup,
                normalizedStart = 0f,
                normalizedEnd = 0.8f
            });

            Assert.That(MoveChoreographyValidator.Validate(data),
                Has.Some.Contains("cover normalized time"));
        }

        [Test]
        public void Validate_IntegratedPinRequiresFaceUpExit()
        {
            var data = ValidPaired();
            data.followUp = AnimationFollowUp.IntegratedPin;
            data.defenderExitPose = DefenderExitPose.FaceDown;

            Assert.That(MoveChoreographyValidator.Validate(data),
                Has.Some.Contains("face-up"));
        }

        [Test]
        public void Validate_NeedsVideoCannotReferenceProductionStates()
        {
            var data = ValidPaired();
            data.referenceStatus = ReferenceStatus.NeedsVideo;

            Assert.That(MoveChoreographyValidator.Validate(data),
                Has.Some.Contains("NeedsVideo"));
        }

        [Test]
        public void Validate_AcceptsCompletePairedChoreography()
        {
            Assert.That(MoveChoreographyValidator.Validate(ValidPaired()), Is.Empty);
        }

        static MoveChoreographyData ValidPaired()
        {
            var data = ScriptableObject.CreateInstance<MoveChoreographyData>();
            data.presentationId = "test-ddt";
            data.participantMode = AnimationParticipantMode.Paired;
            data.startFormation = AnimationStartFormation.FrontStanding;
            data.attackerStateKey = "test-ddt/attacker";
            data.defenderStateKey = "test-ddt/defender";
            data.authoredDuration = 1f;
            data.startDistance = 0.85f;
            data.defenderExitPose = DefenderExitPose.FaceDown;
            data.phases.Add(new AnimationPhaseDefinition
            {
                phase = AnimationPhase.Setup,
                normalizedStart = 0f,
                normalizedEnd = 0.35f
            });
            data.phases.Add(new AnimationPhaseDefinition
            {
                phase = AnimationPhase.Impact,
                normalizedStart = 0.35f,
                normalizedEnd = 1f
            });
            return data;
        }
    }
}
