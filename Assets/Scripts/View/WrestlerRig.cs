using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Builds and holds the code-built paper-doll SpriteRenderer hierarchy.
    /// Loads real parts from Resources/Parts/<id>/<slot> when present, else uses placeholders.
    public class WrestlerRig : MonoBehaviour
    {
        public Transform Root { get; private set; }
        readonly Dictionary<RigSlot, Transform> _joints = new Dictionary<RigSlot, Transform>();

        public Transform Joint(RigSlot slot) => _joints.TryGetValue(slot, out var t) ? t : null;

        // Slot layout: local position of each joint relative to its parent (rig units),
        // sprite size (w,h), pivot (0..1), and base sorting offset (near limbs in front).
        struct SlotDef
        {
            public RigSlot slot, parent;
            public Vector3 local;
            public float w, h;
            public Vector2 pivot;
            public int sort;
            public bool useParent; // parent is another slot vs the root
        }

        static readonly SlotDef[] Layout =
        {
            // slot,            parent,          local pos,                 w,    h,    pivot,                 sort, useParent
            Def(RigSlot.Pelvis,       RigSlot.Pelvis, new Vector3(0f,0.78f,0f), 0.42f,0.22f, new Vector2(0.5f,0.5f),  0, false),
            Def(RigSlot.Torso,        RigSlot.Pelvis, new Vector3(0f,0.11f,0f), 0.50f,0.55f, new Vector2(0.5f,0f),   10, true),
            Def(RigSlot.Head,         RigSlot.Torso,  new Vector3(0f,0.55f,0f), 0.34f,0.34f, new Vector2(0.5f,0f),   12, true),
            Def(RigSlot.Headpiece,    RigSlot.Head,   new Vector3(0f,0.10f,0f), 0.40f,0.30f, new Vector2(0.5f,0.2f), 13, true),

            Def(RigSlot.UpperArmFar,  RigSlot.Torso,  new Vector3(-0.10f,0.50f,0f), 0.16f,0.34f, new Vector2(0.5f,1f), -5, true),
            Def(RigSlot.ForearmFar,   RigSlot.UpperArmFar, new Vector3(0f,-0.30f,0f), 0.14f,0.30f, new Vector2(0.5f,1f), -5, true),
            Def(RigSlot.HandFar,      RigSlot.ForearmFar,  new Vector3(0f,-0.26f,0f), 0.16f,0.16f, new Vector2(0.5f,1f), -5, true),

            Def(RigSlot.UpperArmNear, RigSlot.Torso,  new Vector3(0.10f,0.50f,0f), 0.17f,0.35f, new Vector2(0.5f,1f), 20, true),
            Def(RigSlot.ForearmNear,  RigSlot.UpperArmNear, new Vector3(0f,-0.31f,0f), 0.15f,0.31f, new Vector2(0.5f,1f), 20, true),
            Def(RigSlot.HandNear,     RigSlot.ForearmNear,  new Vector3(0f,-0.27f,0f), 0.17f,0.17f, new Vector2(0.5f,1f), 20, true),

            Def(RigSlot.ThighFar,     RigSlot.Pelvis, new Vector3(-0.10f,-0.05f,0f), 0.18f,0.34f, new Vector2(0.5f,1f), -3, true),
            Def(RigSlot.ShinFar,      RigSlot.ThighFar, new Vector3(0f,-0.30f,0f), 0.16f,0.32f, new Vector2(0.5f,1f), -3, true),
            Def(RigSlot.FootFar,      RigSlot.ShinFar,  new Vector3(0.04f,-0.28f,0f), 0.22f,0.12f, new Vector2(0.3f,1f), -3, true),

            Def(RigSlot.ThighNear,    RigSlot.Pelvis, new Vector3(0.10f,-0.05f,0f), 0.19f,0.35f, new Vector2(0.5f,1f), 15, true),
            Def(RigSlot.ShinNear,     RigSlot.ThighNear, new Vector3(0f,-0.31f,0f), 0.17f,0.33f, new Vector2(0.5f,1f), 15, true),
            Def(RigSlot.FootNear,     RigSlot.ShinNear,  new Vector3(0.04f,-0.29f,0f), 0.23f,0.13f, new Vector2(0.3f,1f), 15, true),
        };

        static SlotDef Def(RigSlot s, RigSlot p, Vector3 local, float w, float h, Vector2 pivot, int sort, bool useParent)
            => new SlotDef { slot = s, parent = p, local = local, w = w, h = h, pivot = pivot, sort = sort, useParent = useParent };

        public static WrestlerRig Build(Transform parent, string characterId, Color primary)
        {
            var rootGo = new GameObject("Rig2D");
            rootGo.transform.SetParent(parent, false);
            var rig = rootGo.AddComponent<WrestlerRig>();
            rig.Root = rootGo.transform;

            foreach (var d in Layout)
            {
                Transform parentT = d.slot == RigSlot.Pelvis
                    ? rig.Root
                    : (d.useParent ? rig._joints[d.parent] : rig.Root);

                var go = new GameObject(d.slot.ToString());
                go.transform.SetParent(parentT, false);
                go.transform.localPosition = d.local;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = LoadPart(characterId, d.slot) ?? PlaceholderSprites.Box(TintFor(d.slot, primary), d.w, d.h, d.pivot);
                sr.sortingOrder = d.sort;
                rig._joints[d.slot] = go.transform;
            }

            return rig;
        }

        static Sprite LoadPart(string characterId, RigSlot slot)
        {
            string slugId = characterId.StartsWith("tas-") ? characterId.Substring(4) : characterId;
            return Resources.Load<Sprite>($"Parts/{slugId}/{SlotFile(slot)}");
        }

        static string SlotFile(RigSlot slot)
        {
            switch (slot)
            {
                case RigSlot.UpperArmNear: return "upper-arm-near";
                case RigSlot.ForearmNear: return "forearm-near";
                case RigSlot.HandNear: return "hand-near";
                case RigSlot.UpperArmFar: return "upper-arm-far";
                case RigSlot.ForearmFar: return "forearm-far";
                case RigSlot.HandFar: return "hand-far";
                case RigSlot.ThighNear: return "thigh-near";
                case RigSlot.ShinNear: return "shin-near";
                case RigSlot.FootNear: return "foot-near";
                case RigSlot.ThighFar: return "thigh-far";
                case RigSlot.ShinFar: return "shin-far";
                case RigSlot.FootFar: return "foot-far";
                default: return slot.ToString().ToLowerInvariant();
            }
        }

        static Color TintFor(RigSlot slot, Color primary)
        {
            switch (slot)
            {
                case RigSlot.Head: return new Color(0.95f, 0.78f, 0.62f);
                case RigSlot.Headpiece: return primary * 0.7f;
                case RigSlot.HandNear:
                case RigSlot.HandFar: return new Color(0.95f, 0.78f, 0.62f);
                case RigSlot.UpperArmFar:
                case RigSlot.ForearmFar:
                case RigSlot.ThighFar:
                case RigSlot.ShinFar:
                case RigSlot.FootFar: return primary * 0.8f;
                default: return primary;
            }
        }
    }
}
