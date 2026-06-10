using UnityEngine;

namespace LoCoFight
{
    /// Holds visual references and builds the primitive placeholder body.
    /// Replace BuildPlaceholder with a real model instantiation later — gameplay
    /// never touches these meshes directly.
    public class WrestlerView : MonoBehaviour
    {
        public Transform visualRoot;
        public Transform torso;
        public Transform head;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;
        public Transform chestMarker;
        public Renderer torsoRenderer;

        public void BuildPlaceholder(Color color)
        {
            if (visualRoot != null) Destroy(visualRoot.gameObject);

            visualRoot = new GameObject("VisualRoot").transform;
            visualRoot.SetParent(transform, false);

            torso = Part(PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.9f, 0f), new Vector3(0.55f, 0.5f, 0.45f), color);
            torsoRenderer = torso.GetComponent<Renderer>();
            head = Part(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.62f, 0f), Vector3.one * 0.36f, color * 1.2f);
            leftArm = Part(PrimitiveType.Cube, "LeftArm", new Vector3(-0.42f, 0.95f, 0f), new Vector3(0.16f, 0.62f, 0.16f), color * 0.85f);
            rightArm = Part(PrimitiveType.Cube, "RightArm", new Vector3(0.42f, 0.95f, 0f), new Vector3(0.16f, 0.62f, 0.16f), color * 0.85f);
            leftLeg = Part(PrimitiveType.Cube, "LeftLeg", new Vector3(-0.16f, 0.3f, 0f), new Vector3(0.18f, 0.6f, 0.18f), color * 0.7f);
            rightLeg = Part(PrimitiveType.Cube, "RightLeg", new Vector3(0.16f, 0.3f, 0f), new Vector3(0.18f, 0.6f, 0.18f), color * 0.7f);
            // Bright chest marker so facing direction is always readable.
            chestMarker = Part(PrimitiveType.Cube, "ChestMarker", new Vector3(0f, 1.1f, 0.24f), new Vector3(0.18f, 0.18f, 0.1f), Color.yellow);
        }

        Transform Part(PrimitiveType type, string name, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            // Visual parts must not collide; the CharacterController handles collision.
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(visualRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = ArenaRig.MakeMaterial(color);
            return go.transform;
        }
    }
}
