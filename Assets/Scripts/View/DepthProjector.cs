using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Projects a wrestler's 3D world position into the flat side-on view:
    /// billboards the rig to face the camera, flips it by facing, applies the
    /// depth screen-offset and scale, sets sprite sorting, and drives a shadow.
    public class DepthProjector : MonoBehaviour
    {
        [Header("Depth")]
        public float yFactor = 0.35f;
        public float scalePerUnit = 0.06f;
        public float minScale = 0.8f;
        public float maxScale = 1.15f;
        public int sortUnitsPerStep = 100;

        [Header("Shadow")]
        public bool drawShadow = true;
        public float shadowWidth = 0.7f;

        Transform _root;
        Transform _visual;
        readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        SpriteRenderer _shadow;
        bool _facingRight = true;
        float _baseVisualY;

        public void Bind(Transform wrestlerRoot, Transform visualRoot)
        {
            _root = wrestlerRoot;
            _visual = visualRoot;
            _baseVisualY = visualRoot.localPosition.y;
            _renderers.Clear();
            visualRoot.GetComponentsInChildren(true, _renderers);
            if (drawShadow) CreateShadow();
        }

        void CreateShadow()
        {
            var go = new GameObject("Shadow");
            go.transform.SetParent(_root, false);
            _shadow = go.AddComponent<SpriteRenderer>();
            _shadow.sprite = PlaceholderSprites.Ellipse(new Color(0f, 0f, 0f, 0.35f));
            _shadow.sortingOrder = -10000;
            go.transform.localScale = new Vector3(shadowWidth, shadowWidth * 0.35f, 1f);
        }

        void LateUpdate()
        {
            if (_root == null || _visual == null) return;

            float z = _root.position.z;

            // Billboard: cancel any gameplay-transform rotation so the rig faces the camera.
            _visual.rotation = Quaternion.identity;

            // Facing from the gameplay transform's forward.x (updated by the motor).
            float fx = _root.forward.x;
            if (Mathf.Abs(fx) > 0.01f) _facingRight = fx >= 0f;

            float depthScale = DepthProjection.DepthScale(z, scalePerUnit, minScale, maxScale);
            float sign = FacingUtil.FlipScaleX(_facingRight);
            _visual.localScale = new Vector3(sign * depthScale, depthScale, 1f);

            var lp = _visual.localPosition;
            lp.y = _baseVisualY + DepthProjection.ScreenYOffset(z, yFactor);
            _visual.localPosition = lp;

            int order = DepthProjection.SortingOrder(z, sortUnitsPerStep);
            for (int i = 0; i < _renderers.Count; i++)
                _renderers[i].sortingOrder = order + _renderers[i].sortingOrder % 100;

            if (_shadow != null)
            {
                var sp = _shadow.transform.localPosition;
                sp.x = 0f;
                sp.y = 0.02f;
                sp.z = 0f;
                _shadow.transform.localPosition = sp;
                _shadow.sortingOrder = order - 1;
            }
        }
    }
}
