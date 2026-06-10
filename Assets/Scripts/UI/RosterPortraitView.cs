using UnityEngine;
using UnityEngine.UI;

namespace LoCoFight
{
    /// Shows a roster portrait, or a flat placeholder color when the sprite is missing.
    public class RosterPortraitView : MonoBehaviour
    {
        public Image image;

        public void Show(Sprite portrait, Color fallback)
        {
            if (image == null) return;
            if (portrait != null)
            {
                image.sprite = portrait;
                image.color = Color.white;
            }
            else
            {
                image.sprite = MeterBar.WhiteSprite();
                image.color = fallback;
                Debug.LogWarning("[Roster] Missing portrait sprite — using placeholder color");
            }
        }

        public static RosterPortraitView Create(Transform parent, string name, Vector2 anchoredPos, float size, bool rightSide)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = rt.anchorMax = rightSide ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rt.pivot = rightSide ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            var view = go.AddComponent<RosterPortraitView>();
            view.image = go.GetComponent<Image>();
            return view;
        }
    }
}
