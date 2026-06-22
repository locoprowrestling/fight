using UnityEngine;

namespace LoCoFight
{
    /// Layered sprite ring for the side-on 2D view. Back ropes render behind
    /// wrestlers; front ropes render in front. Keyed to world coordinates.
    public static class Arena2DBackdrop
    {
        public static GameObject Build(float halfExtent)
        {
            var root = new GameObject("Arena2DBackdrop");

            // Mat floor: a wide low quad behind everything.
            Quad(root.transform, "Mat", new Vector3(0f, 0.25f, 0f),
                new Vector3(halfExtent * 2.4f, 1.4f, 1f), new Color(0.20f, 0.22f, 0.30f), -9000);

            // Back ropes (three stacked lines) behind wrestlers, placed at the back lane.
            float backZ = LaneSystem.LaneZ[LaneSystem.LaneCount - 1];
            for (int i = 0; i < 3; i++)
                Quad(root.transform, $"BackRope{i}", new Vector3(0f, 0.9f + i * 0.45f, backZ),
                    new Vector3(halfExtent * 2.2f, 0.05f, 1f), new Color(0.85f, 0.85f, 0.9f), -8000 - i);

            // Posts at the X extents.
            foreach (float sx in new[] { -1f, 1f })
                Quad(root.transform, "Post", new Vector3(sx * halfExtent, 1.1f, backZ),
                    new Vector3(0.18f, 2.2f, 1f), new Color(0.7f, 0.1f, 0.1f), -7900);

            // Front ropes in front of wrestlers, at the front lane.
            float frontZ = LaneSystem.LaneZ[0];
            for (int i = 0; i < 3; i++)
                Quad(root.transform, $"FrontRope{i}", new Vector3(0f, 0.9f + i * 0.45f, frontZ),
                    new Vector3(halfExtent * 2.2f, 0.05f, 1f), new Color(0.9f, 0.9f, 0.95f), 9000 + i);

            return root;
        }

        static void Quad(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprites.Box(color, 1f, 1f, new Vector2(0.5f, 0.5f));
            sr.sortingOrder = order;
        }
    }
}
