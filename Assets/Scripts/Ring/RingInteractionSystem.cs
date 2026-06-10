using UnityEngine;

namespace LoCoFight
{
    /// Central query API for all rope/corner/aerial interactions.
    /// Combat, AI, specials, and match rules ask this system; nothing else
    /// does rope math on its own.
    public class RingInteractionSystem : MonoBehaviour
    {
        public static RingInteractionSystem Instance { get; private set; }

        public ArenaRig Rig { get; private set; }
        public RingBoundary Bounds => Rig.ringBoundary;

        public const float RopeContactRange = 0.35f;
        public const float RopeReboundActivationRange = 0.5f;
        public const float CornerActivationRange = 1.2f;
        public const float TurnbuckleClimbRange = 1.2f;
        public const float RopeTrapRange = 1.0f;

        public void Init(ArenaRig rig)
        {
            Instance = this;
            Rig = rig;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        float RopeBreakRange()
        {
            var mm = MatchManager.Instance;
            return mm != null && mm.Rules != null ? mm.Rules.ropeBreakRange : 0.65f;
        }

        public RopeSide GetNearestRopeSide(Vector3 position)
        {
            Bounds.DistanceToNearestRope(position, out var side);
            return side;
        }

        public RopeContactInfo GetNearestRopeContactInfo(WrestlerCore wrestler)
        {
            Vector3 pos = wrestler.transform.position;
            float dist = Bounds.DistanceToNearestRope(pos, out var side);
            var info = new RopeContactInfo
            {
                ropeSide = side,
                distanceToRope = dist,
                contactPoint = Bounds.ClosestPointOnRope(side, pos),
                inwardDirection = Bounds.RopeInwardDirection(side),
                outwardDirection = -Bounds.RopeInwardDirection(side),
                nearestRopeMiddleAnchor = GetNearestRopeMiddleAnchor(pos),
                nearestCornerAnchor = NearestCornerZone(pos) != null ? NearestCornerZone(pos).transform : null
            };
            var corner = NearestCornerZone(pos);
            info.distanceToCorner = corner != null ? MathUtil.FlatDistance(pos, corner.transform.position) : 99f;
            info.isNearCorner = info.distanceToCorner <= CornerActivationRange;
            return info;
        }

        public bool IsNearRope(WrestlerCore wrestler, float range)
        {
            return Bounds.DistanceToNearestRope(wrestler.transform.position, out _) <= range;
        }

        public bool IsNearCorner(WrestlerCore wrestler, float range)
        {
            var corner = NearestCornerZone(wrestler.transform.position);
            return corner != null && MathUtil.FlatDistance(wrestler.transform.position, corner.transform.position) <= range;
        }

        /// Pin/submission rope break: root within rope-break range of any rope line.
        public bool IsInRopeBreak(WrestlerCore wrestler)
        {
            return Bounds.DistanceToNearestRope(wrestler.transform.position, out _) <= RopeBreakRange();
        }

        public bool IsInRopeTrapZone(WrestlerCore wrestler)
        {
            return GetNearestRopeTrapZone(wrestler.transform.position, RopeTrapRange) != null;
        }

        public bool IsInCornerZone(WrestlerCore wrestler)
        {
            var corner = NearestCornerZone(wrestler.transform.position);
            return corner != null && corner.Contains(wrestler.transform.position);
        }

        public CornerZone NearestCornerZone(Vector3 position)
        {
            CornerZone best = null;
            float bestDist = float.MaxValue;
            foreach (var c in Rig.cornerZones)
            {
                float d = MathUtil.FlatDistance(position, c.transform.position);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        public RopeTrapZone GetNearestRopeTrapZone(Vector3 position, float maxRange = float.MaxValue)
        {
            RopeTrapZone best = null;
            float bestDist = maxRange;
            foreach (var z in Rig.ropeTrapZones)
            {
                float d = MathUtil.FlatDistance(position, z.transform.position);
                if (d < bestDist) { bestDist = d; best = z; }
            }
            return best;
        }

        public Transform GetNearestTopCornerAnchor(Vector3 position) => NearestAnchor(position, AerialAnchorType.TopCorner)?.transform;
        public Transform GetNearestMiddleCornerAnchor(Vector3 position) => NearestAnchor(position, AerialAnchorType.MiddleCorner)?.transform;
        public Transform GetNearestRopeMiddleAnchor(Vector3 position) => NearestAnchor(position, AerialAnchorType.RopeMiddle)?.transform;

        public AerialLaunchAnchor NearestAnchor(Vector3 position, AerialAnchorType type)
        {
            AerialLaunchAnchor best = null;
            float bestDist = float.MaxValue;
            foreach (var a in Rig.aerialAnchors)
            {
                if (a.anchorType != type) continue;
                float d = MathUtil.FlatDistance(position, a.transform.position);
                if (d < bestDist) { bestDist = d; best = a; }
            }
            return best;
        }

        /// Best launch anchor of the requested type that is close to the attacker
        /// and within landing reach of the defender.
        public AerialLaunchAnchor GetBestAerialLaunchAnchor(WrestlerCore attacker, WrestlerCore defender, AerialAnchorType type)
        {
            AerialLaunchAnchor best = null;
            float bestScore = float.MaxValue;
            foreach (var a in Rig.aerialAnchors)
            {
                if (a.anchorType != type) continue;
                float toAttacker = MathUtil.FlatDistance(attacker.transform.position, a.transform.position);
                float toDefender = MathUtil.FlatDistance(defender.transform.position, a.transform.position);
                if (toDefender > 5.5f) continue; // landing path too long
                float score = toAttacker + toDefender * 0.5f;
                if (score < bestScore) { bestScore = score; best = a; }
            }
            return best;
        }

        public bool IsValidAerialTarget(WrestlerCore attacker, WrestlerCore defender, SpecialAbilityData special)
        {
            if (defender == null || !defender.States.IsDowned) return false;
            if (!Bounds.IsInside(defender.transform.position)) return false;
            var anchor = NearestAnchor(attacker.transform.position, special.requiredLaunchAnchorType);
            if (anchor == null) return false;
            float climbDist = MathUtil.FlatDistance(attacker.transform.position, anchor.transform.position);
            if (climbDist > TurnbuckleClimbRange) return false;
            if (special.disallowCornerAnchor && special.requiredLaunchAnchorType == AerialAnchorType.RopeMiddle)
            {
                // Make sure the rope-middle anchor isn't actually a corner-adjacent spot.
                var corner = NearestCornerZone(anchor.transform.position);
                if (corner != null && MathUtil.FlatDistance(anchor.transform.position, corner.transform.position) < 1.5f)
                    return false;
            }
            float landDist = MathUtil.FlatDistance(anchor.transform.position, defender.transform.position);
            return landDist <= 5.5f;
        }

        public bool IsValidRopeTrapTarget(WrestlerCore attacker, WrestlerCore defender)
        {
            var zone = GetNearestRopeTrapZone(defender.transform.position, RopeTrapRange);
            if (zone == null) return false;
            float rangeBonus = attacker.Traits != null ? attacker.Traits.TarantulaRangeBonus : 0f;
            return MathUtil.FlatDistance(attacker.transform.position, defender.transform.position) <= 1.6f + rangeBonus;
        }

        /// A rebound lane is valid when the path from start back through target stays inside the ring.
        public bool IsValidRopeReboundLane(Vector3 start, Vector3 target)
        {
            if (!Bounds.IsInside(target, 0.2f)) return false;
            Vector3 mid = (start + target) * 0.5f;
            return Bounds.IsInside(mid, 0.1f);
        }

        public RopeReboundAnchor GetReboundAnchor(RopeSide side)
        {
            foreach (var r in Rig.ropeReboundAnchors)
                if (r.ropeSide == side)
                    return r;
            return null;
        }
    }
}
