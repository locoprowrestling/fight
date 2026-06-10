using UnityEngine;

namespace LoCoFight
{
    /// Defines an Irish-whip style rebound lane for one rope side.
    public class RopeReboundAnchor : MonoBehaviour
    {
        public RopeSide ropeSide;
        [Tooltip("Direction back into the ring after the bounce.")]
        public Vector3 reboundDirection = Vector3.forward;
        public float validLaneWidth = 1.25f;

        public Vector3 RopeLinePosition => transform.position;
        public Vector3 CenterReturnLane => Vector3.zero;
    }
}
