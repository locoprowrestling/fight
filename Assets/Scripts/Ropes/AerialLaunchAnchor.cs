using UnityEngine;

namespace LoCoFight
{
    /// Launch point for aerial specials: top corners, middle corners, and rope-middles.
    public class AerialLaunchAnchor : MonoBehaviour
    {
        public AerialAnchorType anchorType;
        public RopeSide ropeSide; // meaningful for RopeMiddle anchors

        /// Ground position the attacker stands at before climbing/springing.
        public Vector3 ApproachPoint => new Vector3(transform.position.x, 0.5f, transform.position.z) +
                                        (Vector3.zero - MathUtil.Flat(transform.position)).normalized * 0.4f;
        public Vector3 LaunchPoint => transform.position;
    }
}
