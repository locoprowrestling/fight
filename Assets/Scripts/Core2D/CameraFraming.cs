using UnityEngine;

namespace LoCoFight
{
    /// Orthographic two-target framing math. Pure logic; no scene state.
    public static class CameraFraming
    {
        public static float MidpointX(float xA, float xB) => (xA + xB) * 0.5f;

        public static float OrthographicSizeFor(
            float separationX, float baseSize, float sizePerUnit, float minSize, float maxSize) =>
            Mathf.Clamp(baseSize + separationX * sizePerUnit, minSize, maxSize);
    }
}
