using UnityEngine;

namespace LoCoFight
{
    /// Generates simple solid sprites at runtime so the rig works before real art exists.
    public static class PlaceholderSprites
    {
        const float PixelsPerUnit = 100f;

        public static Sprite Box(Color color, float widthUnits, float heightUnits, Vector2 pivot)
        {
            int w = Mathf.Max(2, Mathf.RoundToInt(widthUnits * PixelsPerUnit));
            int h = Mathf.Max(2, Mathf.RoundToInt(heightUnits * PixelsPerUnit));
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            // 1px darker outline for readability.
            Color edge = color * 0.55f; edge.a = 1f;
            for (int x = 0; x < w; x++) { px[x] = edge; px[(h - 1) * w + x] = edge; }
            for (int y = 0; y < h; y++) { px[y * w] = edge; px[y * w + (w - 1)] = edge; }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, PixelsPerUnit);
        }

        public static Sprite Ellipse(Color color)
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[s * s];
            float r = s * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - r) / r, dy = (y - r) / r;
                    px[y * s + x] = (dx * dx + dy * dy) <= 1f ? color : Color.clear;
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
