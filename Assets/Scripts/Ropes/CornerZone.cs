using UnityEngine;

namespace LoCoFight
{
    /// Corner pocket where the Cornered state and corner combos are valid.
    public class CornerZone : MonoBehaviour
    {
        [Tooltip("NW / NE / SW / SE label for debugging.")]
        public string cornerName;
        public float activationRange = 1.2f;

        public bool Contains(Vector3 position)
        {
            return MathUtil.FlatDistance(position, transform.position) <= activationRange;
        }
    }
}
