using UnityEngine;

namespace LoCoFight
{
    /// Fakes depth for the side-on view. Pure logic; no scene state.
    /// Convention: smaller z is nearer the camera (front lane), larger z is farther (back).
    public static class DepthProjection
    {
        /// Farther lanes draw higher on screen.
        public static float ScreenYOffset(float z, float yFactor) => z * yFactor;

        /// Farther lanes draw smaller. Mid lane (z = 0) is scale 1.
        public static float DepthScale(float z, float scalePerUnit, float minScale, float maxScale) =>
            Mathf.Clamp(1f - z * scalePerUnit, minScale, maxScale);

        /// Nearer lanes get a higher sorting order so they overlap farther ones.
        public static int SortingOrder(float z, int unitsPerStep) =>
            Mathf.RoundToInt(-z * unitsPerStep);
    }
}
