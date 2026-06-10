using UnityEngine;

namespace LoCoFight
{
    public enum RopeContactType
    {
        None,
        LightContact,
        RunningContact,
        ForcedIntoRopes,
        GrappleThrownIntoRopes,
        DownedNearRopes,
        SubmissionNearRopes
    }

    /// Snapshot of a wrestler's relationship to the nearest rope side.
    public struct RopeContactInfo
    {
        public RopeSide ropeSide;
        public Vector3 contactPoint;
        public Vector3 inwardDirection;
        public Vector3 outwardDirection;
        public Transform nearestRopeMiddleAnchor;
        public Transform nearestCornerAnchor;
        public bool isNearCorner;
        public float distanceToRope;
        public float distanceToCorner;
    }
}
