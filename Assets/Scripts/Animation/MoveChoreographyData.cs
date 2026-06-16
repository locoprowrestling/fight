using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/Move Choreography")]
    public sealed class MoveChoreographyData : ScriptableObject
    {
        [Header("Identity")]
        public string presentationId;
        public ReferenceStatus referenceStatus = ReferenceStatus.Approved;

        [Header("Participants")]
        public AnimationParticipantMode participantMode;
        public AnimationStartFormation startFormation;
        public string attackerStateKey;
        public string defenderStateKey;

        [Header("Timing and formation")]
        public float authoredDuration = 1f;
        public float startDistance = 0.9f;
        public Vector3 defenderLocalOffset;
        public float defenderYaw;

        [Header("Outcome presentation")]
        public DefenderExitPose defenderExitPose;
        public AnimationFollowUp followUp;

        [Header("Phases")]
        public List<AnimationPhaseDefinition> phases =
            new List<AnimationPhaseDefinition>();

        public AnimationPhaseDefinition PhaseAt(float normalizedTime)
        {
            float time = Mathf.Clamp01(normalizedTime);
            for (int i = 0; i < phases.Count; i++)
            {
                AnimationPhaseDefinition phase = phases[i];
                if (time >= phase.normalizedStart &&
                    (time < phase.normalizedEnd ||
                     i == phases.Count - 1 && Mathf.Approximately(time, 1f)))
                    return phase;
            }
            return null;
        }
    }
}
