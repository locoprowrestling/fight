using NUnit.Framework;
using UnityEngine;

namespace LoCoFight.EditorTests
{
    public class PairedMoveCoordinatorTests
    {
        [Test]
        public void CalculateStartPose_FrontStandingFacesParticipantsTogether()
        {
            var choreography = ScriptableObject.CreateInstance<MoveChoreographyData>();
            choreography.startFormation = AnimationStartFormation.FrontStanding;
            choreography.startDistance = 0.85f;

            var pose = PairedMoveCoordinator.CalculateStartPose(
                Vector3.zero, Quaternion.identity, choreography);

            Assert.That(Vector3.Distance(
                pose.defenderPosition, new Vector3(0f, 0f, 0.85f)),
                Is.LessThan(0.001f));
            Assert.That(Vector3.Dot(Vector3.forward, pose.defenderRotation * Vector3.forward),
                Is.LessThan(-0.99f));
        }

        [Test]
        public void CalculateStartPose_RearStandingFacesSameDirection()
        {
            var choreography = ScriptableObject.CreateInstance<MoveChoreographyData>();
            choreography.startFormation = AnimationStartFormation.RearStanding;
            choreography.startDistance = 0.65f;

            var pose = PairedMoveCoordinator.CalculateStartPose(
                Vector3.zero, Quaternion.identity, choreography);

            Assert.That(Vector3.Dot(Vector3.forward, pose.defenderRotation * Vector3.forward),
                Is.GreaterThan(0.99f));
        }

        [Test]
        public void PlaceholderPose_LiftMoveUsesDifferentParticipantRoles()
        {
            var choreography = ScriptableObject.CreateInstance<MoveChoreographyData>();
            choreography.phases.Add(new AnimationPhaseDefinition
            {
                phase = AnimationPhase.Lift,
                normalizedStart = 0f,
                normalizedEnd = 0.5f
            });
            choreography.phases.Add(new AnimationPhaseDefinition
            {
                phase = AnimationPhase.Impact,
                normalizedStart = 0.5f,
                normalizedEnd = 1f
            });

            Assert.That(
                PairedMoveCoordinator.PlaceholderPoseFor(choreography, true),
                Is.EqualTo("paired-lift-attacker"));
            Assert.That(
                PairedMoveCoordinator.PlaceholderPoseFor(choreography, false),
                Is.EqualTo("paired-lift-defender"));
        }

        [Test]
        public void PlaceholderPose_SubmissionUsesHoldRoles()
        {
            var choreography = ScriptableObject.CreateInstance<MoveChoreographyData>();
            choreography.participantMode = AnimationParticipantMode.SubmissionPair;

            Assert.That(
                PairedMoveCoordinator.PlaceholderPoseFor(choreography, true),
                Is.EqualTo("paired-hold-attacker"));
            Assert.That(
                PairedMoveCoordinator.PlaceholderPoseFor(choreography, false),
                Is.EqualTo("paired-hold-defender"));
        }
    }
}
