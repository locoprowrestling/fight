using UnityEngine;

namespace LoCoFight
{
    /// Zone along a rope side where rope-trap specials (Morgana's Tarantula) are valid.
    public class RopeTrapZone : MonoBehaviour
    {
        public RopeSide side;
        [Tooltip("Where the trapped victim is snapped.")]
        public Transform victimAnchor;
        [Tooltip("Where the attacker is snapped (outside-leaning position).")]
        public Transform attackerAnchor;
    }
}
