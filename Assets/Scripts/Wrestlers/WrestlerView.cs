using UnityEngine;

namespace LoCoFight
{
    /// Builds and holds the articulated primitive humanoid. Every bendable
    /// joint (pelvis, spine, neck, shoulders, elbows, hips, knees) is an empty
    /// pivot placed at the anatomical joint; limb meshes hang off their pivot
    /// so rotating the pivot bends the body there. All meshes are Unity
    /// primitives with colliders stripped — the CharacterController on the
    /// wrestler root is the only collision volume. Replace BuildPlaceholder
    /// with a real model instantiation later; gameplay never touches these
    /// transforms directly.
    public class WrestlerView : MonoBehaviour
    {
        public Transform visualRoot;

        [Header("Joint pivots (the animation driver rotates these)")]
        public Transform pelvis;   // whole-torso bend at the waist/hips
        public Transform spine;    // chest
        public Transform neck;     // head
        public Transform leftShoulder;
        public Transform leftElbow;
        public Transform rightShoulder;
        public Transform rightElbow;
        public Transform leftHip;
        public Transform leftKnee;
        public Transform rightHip;
        public Transform rightKnee;

        [Header("Mesh references")]
        public Transform head;
        public Transform chestMarker;
        public Renderer torsoRenderer;

        /// Standing height of the rig in local units, before weight-class scale.
        public const float RigHeight = 1.78f;

        /// Widths (x/z) of torso and limbs scale with weight class.
        public static float BulkFor(WeightClass weight) => weight switch
        {
            WeightClass.Lightweight => 0.88f,
            WeightClass.Heavyweight => 1.14f,
            WeightClass.SuperHeavyweight => 1.28f,
            _ => 1f,
        };

        /// Uniform overall scale with weight class (never non-uniform — a
        /// non-uniform root scale shears rotated child joints).
        public static float HeightFor(WeightClass weight) => weight switch
        {
            WeightClass.Lightweight => 0.96f,
            WeightClass.Heavyweight => 1.04f,
            WeightClass.SuperHeavyweight => 1.07f,
            _ => 1f,
        };

        public void BuildPlaceholder(Color color) => BuildPlaceholder(color, WeightClass.Middleweight);

        public void BuildPlaceholder(Color color, WeightClass weight)
        {
            if (visualRoot != null) Destroy(visualRoot.gameObject);

            float bulk = BulkFor(weight);

            visualRoot = new GameObject("VisualRoot").transform;
            visualRoot.SetParent(transform, false);
            visualRoot.localScale = Vector3.one * HeightFor(weight);

            // Trunk chain: pelvis → spine → neck. Pelvis sits at hip height so
            // bending it folds the whole upper body like a waist.
            pelvis = Joint(visualRoot, "Pelvis", new Vector3(0f, 0.98f, 0f));
            Part(PrimitiveType.Cube, "Hips", pelvis, new Vector3(0f, -0.05f, 0f), Bulked(0.32f, 0.20f, 0.22f, bulk), color * 0.5f);

            spine = Joint(pelvis, "Spine", new Vector3(0f, 0.08f, 0f));
            var chest = Part(PrimitiveType.Capsule, "Chest", spine, new Vector3(0f, 0.22f, 0f), Bulked(0.42f, 0.23f, 0.28f, bulk), color);
            torsoRenderer = chest.GetComponent<Renderer>();
            // Bright chest marker so facing direction is always readable.
            chestMarker = Part(PrimitiveType.Cube, "ChestMarker", spine, new Vector3(0f, 0.30f, 0.14f * bulk), new Vector3(0.16f, 0.16f, 0.08f), Color.yellow);

            neck = Joint(spine, "Neck", new Vector3(0f, 0.44f, 0f));
            head = Part(PrimitiveType.Sphere, "Head", neck, new Vector3(0f, 0.13f, 0.01f), Vector3.one * 0.30f, color * 1.2f);

            float shoulderX = 0.245f * Mathf.Lerp(1f, bulk, 0.7f);
            BuildArm(out leftShoulder, out leftElbow, "Left", -shoulderX, bulk, color);
            BuildArm(out rightShoulder, out rightElbow, "Right", shoulderX, bulk, color);

            float hipX = 0.105f * Mathf.Lerp(1f, bulk, 0.5f);
            BuildLeg(out leftHip, out leftKnee, "Left", -hipX, bulk, color);
            BuildLeg(out rightHip, out rightKnee, "Right", hipX, bulk, color);
        }

        // Shoulder pivot at the top of the arm; elbow pivot halfway down.
        // Arm hangs along -Y, so rotating shoulder X=-90 raises it forward.
        void BuildArm(out Transform shoulder, out Transform elbow, string side, float x, float bulk, Color color)
        {
            shoulder = Joint(spine, side + "Shoulder", new Vector3(x, 0.38f, 0f));
            Part(PrimitiveType.Sphere, side + "ShoulderPad", shoulder, Vector3.zero, Vector3.one * 0.16f * bulk, color * 0.9f);
            Part(PrimitiveType.Capsule, side + "UpperArm", shoulder, new Vector3(0f, -0.15f, 0f), Bulked(0.13f, 0.15f, 0.13f, bulk), color * 0.85f);
            elbow = Joint(shoulder, side + "Elbow", new Vector3(0f, -0.30f, 0f));
            Part(PrimitiveType.Capsule, side + "Forearm", elbow, new Vector3(0f, -0.14f, 0f), Bulked(0.11f, 0.14f, 0.11f, bulk), color * 0.85f);
            Part(PrimitiveType.Sphere, side + "Hand", elbow, new Vector3(0f, -0.30f, 0f), Vector3.one * 0.11f, color * 1.15f);
        }

        // Hip pivot at the top of the thigh; knee pivot at mid-leg with the
        // boot parented to it so a bent knee carries the foot along.
        void BuildLeg(out Transform hip, out Transform knee, string side, float x, float bulk, Color color)
        {
            hip = Joint(pelvis, side + "Hip", new Vector3(x, -0.06f, 0f));
            Part(PrimitiveType.Capsule, side + "Thigh", hip, new Vector3(0f, -0.21f, 0f), Bulked(0.17f, 0.21f, 0.17f, bulk), color * 0.7f);
            knee = Joint(hip, side + "Knee", new Vector3(0f, -0.42f, 0f));
            Part(PrimitiveType.Capsule, side + "Shin", knee, new Vector3(0f, -0.21f, 0f), Bulked(0.13f, 0.21f, 0.13f, bulk), color * 0.7f);
            Part(PrimitiveType.Cube, side + "Boot", knee, new Vector3(0f, -0.44f, 0.05f), new Vector3(0.13f, 0.09f, 0.26f), color * 0.4f);
        }

        static Transform Joint(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        static Vector3 Bulked(float x, float y, float z, float bulk) => new Vector3(x * bulk, y, z * bulk);

        Transform Part(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            // Visual parts must not collide; the CharacterController handles collision.
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = ArenaRig.MakeMaterial(color);
            return go.transform;
        }
    }
}
