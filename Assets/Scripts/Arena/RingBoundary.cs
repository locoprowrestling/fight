using UnityEngine;

namespace LoCoFight
{
    /// Axis-aligned square ring bounds. Centralizes all "where is the ring edge" math.
    public class RingBoundary : MonoBehaviour
    {
        public float halfExtent = 4f;
        public float matTopY = 0.5f;

        public bool IsInside(Vector3 pos, float margin = 0f)
        {
            return Mathf.Abs(pos.x) <= halfExtent - margin && Mathf.Abs(pos.z) <= halfExtent - margin;
        }

        /// Distance from a point to the nearest rope line (positive inside the ring).
        public float DistanceToNearestRope(Vector3 pos, out RopeSide side)
        {
            float dN = halfExtent - pos.z;
            float dS = halfExtent + pos.z;
            float dE = halfExtent - pos.x;
            float dW = halfExtent + pos.x;
            side = RopeSide.North;
            float best = dN;
            if (dS < best) { best = dS; side = RopeSide.South; }
            if (dE < best) { best = dE; side = RopeSide.East; }
            if (dW < best) { best = dW; side = RopeSide.West; }
            return best;
        }

        public Vector3 RopeInwardDirection(RopeSide side)
        {
            switch (side)
            {
                case RopeSide.North: return Vector3.back;
                case RopeSide.South: return Vector3.forward;
                case RopeSide.East: return Vector3.left;
                default: return Vector3.right;
            }
        }

        public Vector3 ClosestPointOnRope(RopeSide side, Vector3 from)
        {
            switch (side)
            {
                case RopeSide.North: return new Vector3(Mathf.Clamp(from.x, -halfExtent, halfExtent), matTopY, halfExtent);
                case RopeSide.South: return new Vector3(Mathf.Clamp(from.x, -halfExtent, halfExtent), matTopY, -halfExtent);
                case RopeSide.East: return new Vector3(halfExtent, matTopY, Mathf.Clamp(from.z, -halfExtent, halfExtent));
                default: return new Vector3(-halfExtent, matTopY, Mathf.Clamp(from.z, -halfExtent, halfExtent));
            }
        }

        public Vector3 ClampInside(Vector3 pos, float margin = 0.3f)
        {
            pos.x = Mathf.Clamp(pos.x, -halfExtent + margin, halfExtent - margin);
            pos.z = Mathf.Clamp(pos.z, -halfExtent + margin, halfExtent - margin);
            return pos;
        }
    }
}
