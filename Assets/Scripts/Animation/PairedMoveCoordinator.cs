using UnityEngine;

namespace LoCoFight
{
    public struct PairedStartPose
    {
        public Vector3 defenderPosition;
        public Quaternion defenderRotation;
    }

    /// Computes deterministic paired-move formations. Runtime ownership and
    /// cleanup stay with WrestlerCombat and the special executors.
    public static class PairedMoveCoordinator
    {
        public static string PlaceholderPoseFor(
            MoveChoreographyData choreography,
            bool attacker)
        {
            if (choreography == null)
                return attacker ? "grapple" : "paired-impact-defender";
            if (choreography.participantMode == AnimationParticipantMode.SubmissionPair)
                return attacker ? "paired-hold-attacker" : "paired-hold-defender";

            bool hasLift = HasPhase(choreography, AnimationPhase.Lift) ||
                           HasPhase(choreography, AnimationPhase.Carry) ||
                           HasPhase(choreography, AnimationPhase.Rotation);
            if (hasLift)
                return attacker ? "paired-lift-attacker" : "paired-lift-defender";
            return attacker ? "paired-impact-attacker" : "paired-impact-defender";
        }

        public static bool BeginPresentation(
            WrestlerCore attacker,
            WrestlerCore defender,
            MoveChoreographyData choreography,
            float playbackSpeed = 1f)
        {
            if (attacker == null || defender == null || choreography == null)
                return false;
            if (choreography.referenceStatus != ReferenceStatus.Approved)
                return false;

            PairedStartPose pose = CalculateStartPose(
                attacker.transform.position,
                attacker.transform.rotation,
                choreography);
            defender.Motor.Teleport(pose.defenderPosition);
            defender.transform.rotation = pose.defenderRotation;

            attacker.Anim.PlayMove(
                choreography.attackerStateKey,
                PlaceholderPoseFor(choreography, true),
                playbackSpeed);
            defender.Anim.PlayMove(
                choreography.defenderStateKey,
                PlaceholderPoseFor(choreography, false),
                playbackSpeed);
            return true;
        }

        public static PairedStartPose CalculateStartPose(
            Vector3 attackerPosition,
            Quaternion attackerRotation,
            MoveChoreographyData choreography)
        {
            float distance = choreography != null
                ? Mathf.Max(0.1f, choreography.startDistance)
                : 0.9f;
            Vector3 localOffset = choreography != null
                ? choreography.defenderLocalOffset
                : Vector3.zero;
            AnimationStartFormation formation = choreography != null
                ? choreography.startFormation
                : AnimationStartFormation.FrontStanding;

            Vector3 defaultOffset;
            float yaw;
            switch (formation)
            {
                case AnimationStartFormation.RearStanding:
                case AnimationStartFormation.CornerRear:
                    defaultOffset = Vector3.forward * distance;
                    yaw = 0f;
                    break;
                case AnimationStartFormation.SideBySide:
                    defaultOffset = Vector3.right * distance;
                    yaw = 0f;
                    break;
                case AnimationStartFormation.GroundHeadFaceUp:
                    defaultOffset = Vector3.forward * distance;
                    yaw = 180f;
                    break;
                case AnimationStartFormation.GroundBodyFaceDown:
                case AnimationStartFormation.GroundLegs:
                    defaultOffset = Vector3.forward * distance;
                    yaw = 0f;
                    break;
                default:
                    defaultOffset = Vector3.forward * distance;
                    yaw = 180f;
                    break;
            }

            if (localOffset.sqrMagnitude > 0.0001f)
                defaultOffset = localOffset;
            if (choreography != null && !Mathf.Approximately(choreography.defenderYaw, 0f))
                yaw = choreography.defenderYaw;

            return new PairedStartPose
            {
                defenderPosition = attackerPosition + attackerRotation * defaultOffset,
                defenderRotation = attackerRotation * Quaternion.Euler(0f, yaw, 0f)
            };
        }

        static bool HasPhase(
            MoveChoreographyData choreography,
            AnimationPhase wanted)
        {
            for (int i = 0; i < choreography.phases.Count; i++)
            {
                AnimationPhaseDefinition phase = choreography.phases[i];
                if (phase != null && phase.phase == wanted)
                    return true;
            }
            return false;
        }
    }
}
