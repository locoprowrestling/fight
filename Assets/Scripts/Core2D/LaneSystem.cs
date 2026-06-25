using UnityEngine;

namespace LoCoFight
{
    /// Depth-lane geometry for the side-on false-2D field. Pure logic; no scene state.
    public static class LaneSystem
    {
        public const int LaneCount = 3;

        /// Index 0 = front rope (nearest camera), 1 = mid, 2 = back rope.
        public static readonly float[] LaneZ = { -1.2f, 0f, 1.2f };

        /// Max world-Z difference at which strikes and grapples may connect.
        public const float StrikeAlignmentTolerance = 0.6f;

        public static int NearestLaneIndex(float z)
        {
            int best = 0;
            float bestDist = Mathf.Abs(z - LaneZ[0]);
            for (int i = 1; i < LaneCount; i++)
            {
                float d = Mathf.Abs(z - LaneZ[i]);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        public static float SnapZ(float z) => LaneZ[NearestLaneIndex(z)];

        public static int StepLane(int index, int direction) =>
            Mathf.Clamp(index + direction, 0, LaneCount - 1);

        public static bool LanesAligned(float zA, float zB, float tolerance) =>
            Mathf.Abs(zA - zB) <= tolerance;

        public static bool LanesAligned(Transform a, Transform b, float tolerance) =>
            LanesAligned(a.position.z, b.position.z, tolerance);
    }
}
