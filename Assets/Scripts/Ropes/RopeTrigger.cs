using UnityEngine;

namespace LoCoFight
{
    /// Marker on a physical rope segment. Colliders on the rope block movement;
    /// gameplay queries go through RingInteractionSystem.
    public class RopeTrigger : MonoBehaviour
    {
        public RopeSide side;
        public int ropeLevel; // 0 = low, 1 = mid, 2 = high
    }
}
