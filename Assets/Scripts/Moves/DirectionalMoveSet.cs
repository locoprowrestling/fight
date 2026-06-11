using System;
using System.Collections.Generic;

namespace LoCoFight
{
    /// Directional grapple buckets with neutral fallback. Left and right share
    /// the lateral bucket for this milestone. An empty requested bucket falls
    /// back to neutral; an empty neutral bucket rejects with null.
    [Serializable]
    public class DirectionalMoveSet
    {
        public List<MoveData> neutral = new List<MoveData>();
        public List<MoveData> forward = new List<MoveData>();
        public List<MoveData> backward = new List<MoveData>();
        public List<MoveData> lateral = new List<MoveData>();

        public MoveData Pick(MoveDirection direction, out bool usedFallback)
        {
            var requested = Bucket(direction);
            if (requested.Count > 0)
            {
                usedFallback = false;
                return requested[UnityEngine.Random.Range(0, requested.Count)];
            }

            usedFallback = direction != MoveDirection.Neutral && neutral.Count > 0;
            if (neutral.Count == 0) return null;
            return neutral[UnityEngine.Random.Range(0, neutral.Count)];
        }

        public IReadOnlyList<MoveData> Bucket(MoveDirection direction)
        {
            switch (direction)
            {
                case MoveDirection.Forward: return forward;
                case MoveDirection.Backward: return backward;
                case MoveDirection.Left:
                case MoveDirection.Right: return lateral;
                default: return neutral;
            }
        }

        public IEnumerable<MoveData> AllMoves()
        {
            foreach (var m in neutral) yield return m;
            foreach (var m in forward) yield return m;
            foreach (var m in backward) yield return m;
            foreach (var m in lateral) yield return m;
        }
    }
}
