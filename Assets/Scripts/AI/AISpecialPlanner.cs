using UnityEngine;

namespace LoCoFight
{
    /// Figures out how the CPU should set up its wrestler-specific special:
    /// either fire it now, or walk toward the position that makes it valid.
    public static class AISpecialPlanner
    {
        /// Returns true when the special was activated. Otherwise outputs a
        /// position to walk toward (or null when no setup applies right now).
        public static bool PlanOrExecute(WrestlerCore self, WrestlerCore opp, out Vector3? moveTarget)
        {
            moveTarget = null;
            var special = self.Specials != null ? self.Specials.Data : null;
            if (special == null || !self.Stats.HasFullMomentum) return false;

            if (self.Specials.IsCurrentlyValid(out _))
                return self.Combat.TrySpecial();

            var ring = RingInteractionSystem.Instance;
            if (ring == null) return false;

            switch (special.category)
            {
                case SpecialCategory.SpecialAerial:
                    // Need the opponent downed and us near the launch anchor.
                    if (opp.States.IsDowned)
                    {
                        var anchor = ring.GetBestAerialLaunchAnchor(self, opp, special.requiredLaunchAnchorType)
                                     ?? ring.NearestAnchor(opp.transform.position, special.requiredLaunchAnchorType);
                        if (anchor != null)
                        {
                            var p = anchor.transform.position;
                            moveTarget = ring.Bounds.ClampInside(new Vector3(p.x, 0.5f, p.z), 0.4f);
                        }
                    }
                    break;

                case SpecialCategory.SpecialGroundedSequence:
                    // JT: stand at the downed opponent's head.
                    if (opp.States.IsDowned)
                        moveTarget = opp.transform.position +
                                     MathUtil.FlatDirection(opp.transform.position, self.transform.position) * 1.0f;
                    break;

                case SpecialCategory.SpecialSubmission:
                    if (opp.States.IsDowned)
                        moveTarget = opp.transform.position;
                    break;

                case SpecialCategory.SpecialRopeTrap:
                case SpecialCategory.SpecialCombo:
                    // Need the opponent against ropes / in a corner: close in and
                    // keep striking — knockback herds them there.
                    if (opp.States.IsStanding)
                        moveTarget = opp.transform.position;
                    break;

                case SpecialCategory.SpecialRush:
                    // Back off to build a charge lane.
                    moveTarget = ring.Bounds.ClampInside(
                        self.transform.position +
                        MathUtil.FlatDirection(opp.transform.position, self.transform.position) * 2.5f, 0.6f);
                    break;

                default:
                    // Front/side grapple finishers: get to grapple range.
                    if (opp.States.IsStanding)
                        moveTarget = opp.transform.position;
                    break;
            }
            return false;
        }
    }
}
