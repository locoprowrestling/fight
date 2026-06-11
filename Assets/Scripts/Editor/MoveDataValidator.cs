using System.Collections.Generic;

namespace LoCoFight
{
    /// Editor-only structural validation for authored move data. Lives in the
    /// LoCoFight namespace (not EditorTools) so the asset builder and tests
    /// share the same type without adapters. Errors block asset generation;
    /// see ValidateWarnings (added with pacing rules) for advisory checks.
    public static class MoveDataValidator
    {
        public static List<string> Validate(MoveData move, MoveDatabase database)
        {
            var errors = new List<string>();
            if (move == null)
            {
                errors.Add("Move reference is null.");
                return errors;
            }

            string id = string.IsNullOrWhiteSpace(move.moveId)
                ? move.name
                : move.moveId;
            if (string.IsNullOrWhiteSpace(move.moveId))
                errors.Add($"{id}: moveId is required.");
            if (move.reversalWindowStart < 0f)
                errors.Add($"{id}: reversal window start cannot be negative.");
            if (move.reversalWindowEnd < move.reversalWindowStart)
                errors.Add($"{id}: reversal window end precedes its start.");
            if (move.reversalWindowEnd > move.TotalDuration)
                errors.Add($"{id}: reversal window exceeds total move duration.");
            if (move.HasTag(MoveTag.Lift) && !move.requiresLift)
                errors.Add($"{id}: Lift tag requires lift validation.");
            if (move.requiredGroundZone != GroundTargetZone.None &&
                !move.requiresTargetDowned)
                errors.Add($"{id}: ground zone requires a downed target.");
            if (move.requiresTargetCornered &&
                move.category != MoveCategory.CornerStrike &&
                move.category != MoveCategory.CornerGrapple)
                errors.Add($"{id}: corner requirement is assigned to a non-corner move.");
            if (move.requiresTargetRopeStaggered &&
                move.category != MoveCategory.RopeStaggerAttack)
                errors.Add($"{id}: rope-stagger requirement is assigned to another family.");
            if (move.allowsStrongDirectionalCounter &&
                move.preferredCounterDirection == ReversalReadDirection.Neutral)
                errors.Add($"{id}: strong directional counter requires a direction.");
            if (move.basicReversalMomentum < 0f || move.strongReversalMomentum < 0f)
                errors.Add($"{id}: reversal momentum cannot be negative.");
            if (move.basicReversalStagger < 0f || move.strongReversalStagger < 0f)
                errors.Add($"{id}: reversal stagger cannot be negative.");
            if (move.basicReversalSeparation < 0f || move.strongReversalSeparation < 0f)
                errors.Add($"{id}: reversal separation cannot be negative.");
            return errors;
        }

        public static List<string> ValidateDirectionalSet(
            string label,
            DirectionalMoveSet set)
        {
            var errors = new List<string>();
            bool hasDirectional =
                set.forward.Count > 0 ||
                set.backward.Count > 0 ||
                set.lateral.Count > 0;
            if (hasDirectional && set.neutral.Count == 0)
                errors.Add($"{label}: directional set requires a neutral fallback.");
            return errors;
        }

        /// Advisory pacing checks. These log as warnings and never block asset
        /// generation unless the data is structurally invalid (see Validate).
        public static List<string> ValidateWarnings(
            MoveData move,
            MoveDatabase database)
        {
            var warnings = new List<string>();
            if (move == null) return warnings;
            string id = string.IsNullOrWhiteSpace(move.moveId) ? move.name : move.moveId;

            if (move.tier == MoveTier.Heavy && move.minimumStamina <= 0f)
                warnings.Add($"{id}: heavy move has no minimum stamina.");
            if (move.tier == MoveTier.Heavy && move.recoveryTime < 0.45f)
                warnings.Add($"{id}: heavy move recovery is shorter than 0.45 seconds.");
            if (move.tier == MoveTier.Light && move.minimumStamina > move.staminaCost)
                warnings.Add($"{id}: light move minimum stamina exceeds its cost.");
            if (move.tier == MoveTier.Special && database != null)
                warnings.Add($"{id}: special-tier MoveData belongs in SpecialController data.");

            return warnings;
        }

        public static List<string> ValidateAll(DefaultGameDataSet set)
        {
            var errors = new List<string>();
            foreach (MoveData move in set.moves)
                errors.AddRange(Validate(move, set.moveDatabase));
            errors.AddRange(ValidateDirectionalSet(
                "Quick grapples", set.moveDatabase.directionalQuickGrapples));
            errors.AddRange(ValidateDirectionalSet(
                "Power grapples", set.moveDatabase.directionalPowerGrapples));
            return errors;
        }
    }
}
