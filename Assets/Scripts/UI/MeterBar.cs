using UnityEngine;
using UnityEngine.UI;

namespace LoCoFight
{
    /// Minimal fill-bar built from two Images.
    public class MeterBar : MonoBehaviour
    {
        public Image fill;

        Color _baseColor;

        public void SetValue(float normalized)
        {
            if (fill != null) fill.fillAmount = Mathf.Clamp01(normalized);
        }

        /// Persistent ready treatment (e.g. full-momentum SPECIAL): swaps the
        /// fill to a hot accent color and back without touching the value.
        public void SetReady(bool ready)
        {
            if (fill == null) return;
            fill.color = ready
                ? new Color(1f, 0.65f, 0.1f)
                : _baseColor;
        }

        public static MeterBar Create(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = size;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;

            var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(root.transform, false);
            Stretch((RectTransform)bgGo.transform);
            bgGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(root.transform, false);
            Stretch((RectTransform)fillGo.transform);
            var img = fillGo.GetComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.sprite = WhiteSprite();

            var bar = root.AddComponent<MeterBar>();
            bar.fill = img;
            bar._baseColor = color;
            return bar;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Sprite _white;
        public static Sprite WhiteSprite()
        {
            if (_white == null)
            {
                var tex = new Texture2D(2, 2);
                tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                tex.Apply();
                _white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            }
            return _white;
        }
    }
}
