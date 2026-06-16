using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    public static class MoveChoreographyValidator
    {
        const float TimelineTolerance = 0.001f;

        public static List<string> Validate(MoveChoreographyData data)
        {
            var errors = new List<string>();
            if (data == null)
            {
                errors.Add("Choreography reference is null.");
                return errors;
            }

            string id = string.IsNullOrWhiteSpace(data.presentationId)
                ? data.name
                : data.presentationId;
            if (string.IsNullOrWhiteSpace(data.presentationId))
                errors.Add($"{id}: presentationId is required.");
            if (data.authoredDuration <= 0f)
                errors.Add($"{id}: authored duration must be positive.");
            if (data.startDistance <= 0f)
                errors.Add($"{id}: start distance must be positive.");

            bool paired = data.participantMode != AnimationParticipantMode.Solo;
            if (paired && string.IsNullOrWhiteSpace(data.attackerStateKey))
                errors.Add($"{id}: paired choreography requires an attacker state.");
            if (paired && string.IsNullOrWhiteSpace(data.defenderStateKey))
                errors.Add($"{id}: paired choreography requires a defender state.");

            if (data.referenceStatus == ReferenceStatus.NeedsVideo &&
                (!string.IsNullOrWhiteSpace(data.attackerStateKey) ||
                 !string.IsNullOrWhiteSpace(data.defenderStateKey)))
                errors.Add($"{id}: NeedsVideo choreography cannot reference production states.");

            if (data.followUp == AnimationFollowUp.IntegratedPin &&
                data.defenderExitPose != DefenderExitPose.FaceUp)
                errors.Add($"{id}: integrated pin requires a face-up defender exit.");
            if (data.followUp == AnimationFollowUp.Submission &&
                data.participantMode != AnimationParticipantMode.SubmissionPair)
                errors.Add($"{id}: submission follow-up requires a submission pair.");

            ValidateTimeline(id, data.phases, errors);
            ValidateTopCorner(id, data, errors);
            return errors;
        }

        static void ValidateTimeline(
            string id,
            List<AnimationPhaseDefinition> phases,
            List<string> errors)
        {
            if (phases == null || phases.Count == 0)
            {
                errors.Add($"{id}: phases must cover normalized time 0 through 1.");
                return;
            }

            float expectedStart = 0f;
            for (int i = 0; i < phases.Count; i++)
            {
                AnimationPhaseDefinition phase = phases[i];
                if (phase == null)
                {
                    errors.Add($"{id}: phase {i} is null.");
                    continue;
                }
                if (phase.normalizedStart < 0f || phase.normalizedEnd > 1f ||
                    phase.normalizedEnd <= phase.normalizedStart)
                    errors.Add($"{id}: phase {i} has an invalid normalized range.");
                if (Mathf.Abs(phase.normalizedStart - expectedStart) > TimelineTolerance)
                    errors.Add($"{id}: phases must cover normalized time without gaps or overlaps.");
                expectedStart = phase.normalizedEnd;
            }

            if (Mathf.Abs(expectedStart - 1f) > TimelineTolerance)
                errors.Add($"{id}: phases must cover normalized time through 1.");
        }

        static void ValidateTopCorner(
            string id,
            MoveChoreographyData data,
            List<string> errors)
        {
            if (data.startFormation != AnimationStartFormation.TopCornerPair)
                return;

            bool setup = false;
            bool contact = false;
            bool impact = false;
            foreach (AnimationPhaseDefinition phase in data.phases)
            {
                if (phase == null) continue;
                setup |= phase.phase == AnimationPhase.Setup;
                contact |= phase.phase == AnimationPhase.Contact;
                impact |= phase.phase == AnimationPhase.Impact;
            }
            if (!setup || !contact || !impact)
                errors.Add($"{id}: top-corner choreography requires setup, contact, and impact phases.");
        }
    }
}
