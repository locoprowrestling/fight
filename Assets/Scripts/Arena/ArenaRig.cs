using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Owns every gameplay anchor in the arena and can build the whole primitive
    /// arena from code so the prototype runs with zero external assets.
    public class ArenaRig : MonoBehaviour
    {
        [Header("Ring")]
        public RingBoundary ringBoundary;

        [Header("Zones and anchors")]
        public List<RopeTrigger> ropeTriggers = new List<RopeTrigger>();
        public List<CornerZone> cornerZones = new List<CornerZone>();
        public List<RopeTrapZone> ropeTrapZones = new List<RopeTrapZone>();
        public List<RopeBreakZone> ropeBreakZones = new List<RopeBreakZone>();
        public List<RopeReboundAnchor> ropeReboundAnchors = new List<RopeReboundAnchor>();
        public List<AerialLaunchAnchor> aerialAnchors = new List<AerialLaunchAnchor>();

        [Header("Spawn / camera")]
        public Transform playerSpawn;
        public Transform cpuSpawn;
        public Transform centerRing;

        const float HalfExtent = 4f;
        const float MatTopY = 0.5f;
        const float LowRope = 0.9f, MidRope = 1.35f, HighRope = 1.8f;
        const float PostHeight = 2.1f;

        static Shader _cachedShader;
        static readonly Dictionary<Color32, Material> MaterialCache =
            new Dictionary<Color32, Material>();

        public static Material MakeMaterial(Color color)
        {
            if (_cachedShader == null)
            {
                _cachedShader = Shader.Find("Standard");
                if (_cachedShader == null) _cachedShader = Shader.Find("Universal Render Pipeline/Lit");
                if (_cachedShader == null) _cachedShader = Shader.Find("Diffuse");
            }

            Color32 key = color;
            if (MaterialCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var m = new Material(_cachedShader);
            if (m.HasProperty("_Color")) m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            MaterialCache[key] = m;
            return m;
        }

        /// Builds the complete primitive arena hierarchy and returns the populated rig.
        public static ArenaRig BuildPrimitiveArena()
        {
            var root = new GameObject("ArenaRig");
            var rig = root.AddComponent<ArenaRig>();

            rig.ringBoundary = root.AddComponent<RingBoundary>();
            rig.ringBoundary.halfExtent = HalfExtent;
            rig.ringBoundary.matTopY = MatTopY;

            var ringRoot = Child(root.transform, "RingRoot");
            BuildSurfaces(rig, ringRoot);
            BuildRopesAndPosts(rig, ringRoot);
            BuildZonesAndAnchors(rig, ringRoot);

            var anchors = Child(root.transform, "Anchors");
            rig.playerSpawn = Child(anchors, "PlayerSpawn"); rig.playerSpawn.position = new Vector3(-2f, 0.6f, 0f);
            rig.cpuSpawn = Child(anchors, "CPUSpawn"); rig.cpuSpawn.position = new Vector3(2f, 0.6f, 0f);
            rig.centerRing = Child(anchors, "CenterRing"); rig.centerRing.position = new Vector3(0f, MatTopY, 0f);

            return rig;
        }

        static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject Primitive(PrimitiveType type, Transform parent, string name,
            Vector3 pos, Vector3 scale, Color color, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
            if (!keepCollider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.Destroy(c);
            }
            return go;
        }

        static void BuildSurfaces(ArenaRig rig, Transform ringRoot)
        {
            Primitive(PrimitiveType.Cube, ringRoot, "FloorArea",
                new Vector3(0f, -0.05f, 0f), new Vector3(16f, 0.1f, 16f), new Color(0.25f, 0.25f, 0.28f));
            Primitive(PrimitiveType.Cube, ringRoot, "RingApron",
                new Vector3(0f, 0.225f, 0f), new Vector3(9f, 0.45f, 9f), new Color(0.15f, 0.15f, 0.4f));
            Primitive(PrimitiveType.Cube, ringRoot, "RingMat",
                new Vector3(0f, 0.25f, 0f), new Vector3(8f, 0.5f, 8f), new Color(0.75f, 0.72f, 0.65f));
        }

        static void BuildRopesAndPosts(ArenaRig rig, Transform ringRoot)
        {
            var ropes = Child(ringRoot, "Ropes");
            float[] heights = { LowRope, MidRope, HighRope };
            string[] levelNames = { "Low", "Mid", "High" };
            var ropeColor = new Color(0.8f, 0.2f, 0.2f);

            foreach (RopeSide side in System.Enum.GetValues(typeof(RopeSide)))
            {
                for (int level = 0; level < 3; level++)
                {
                    Vector3 pos; Vector3 scale; Quaternion rot;
                    float y = MatTopY + heights[level];
                    bool northSouth = side == RopeSide.North || side == RopeSide.South;
                    float edge = (side == RopeSide.North || side == RopeSide.East) ? HalfExtent : -HalfExtent;
                    if (northSouth)
                    {
                        pos = new Vector3(0f, y, edge);
                        rot = Quaternion.Euler(0f, 0f, 90f);
                        scale = new Vector3(0.08f, HalfExtent, 0.08f); // cylinder height axis = Y, length 8 after rotation
                    }
                    else
                    {
                        pos = new Vector3(edge, y, 0f);
                        rot = Quaternion.Euler(90f, 0f, 0f);
                        scale = new Vector3(0.08f, HalfExtent, 0.08f);
                    }

                    var rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    rope.name = $"Rope_{side}_{levelNames[level]}";
                    rope.transform.SetParent(ropes, false);
                    rope.transform.localPosition = pos;
                    rope.transform.localRotation = rot;
                    rope.transform.localScale = scale;
                    rope.GetComponent<Renderer>().sharedMaterial = MakeMaterial(ropeColor);

                    // Replace capsule collider with a thin box so movement collides cleanly.
                    // Cylinder mesh spans -1..1 on its height axis, so y=2 covers the full rope.
                    Object.Destroy(rope.GetComponent<Collider>());
                    var box = rope.AddComponent<BoxCollider>();
                    box.size = new Vector3(2.2f, 2f, 2.2f);

                    var trigger = rope.AddComponent<RopeTrigger>();
                    trigger.side = side;
                    trigger.ropeLevel = level;
                    rig.ropeTriggers.Add(trigger);
                }
            }

            var corners = Child(ringRoot, "Corners");
            foreach (var (name, x, z) in CornerDefs())
            {
                Primitive(PrimitiveType.Cylinder, corners, $"Corner_{name}",
                    new Vector3(x, MatTopY + PostHeight * 0.5f, z),
                    new Vector3(0.25f, PostHeight * 0.5f, 0.25f), new Color(0.2f, 0.2f, 0.22f));
            }
        }

        static void BuildZonesAndAnchors(ArenaRig rig, Transform ringRoot)
        {
            var turnbuckles = Child(ringRoot, "Turnbuckles");
            foreach (var (name, x, z) in CornerDefs())
            {
                var top = Child(turnbuckles, $"TopCorner_{name}");
                top.position = new Vector3(x, MatTopY + HighRope, z);
                var topAnchor = top.gameObject.AddComponent<AerialLaunchAnchor>();
                topAnchor.anchorType = AerialAnchorType.TopCorner;
                rig.aerialAnchors.Add(topAnchor);

                var mid = Child(turnbuckles, $"MiddleCorner_{name}");
                mid.position = new Vector3(x, MatTopY + MidRope, z);
                var midAnchor = mid.gameObject.AddComponent<AerialLaunchAnchor>();
                midAnchor.anchorType = AerialAnchorType.MiddleCorner;
                rig.aerialAnchors.Add(midAnchor);
            }

            var ropeMiddles = Child(ringRoot, "RopeMiddleAnchors");
            var rebounds = Child(ringRoot, "RopeReboundAnchors");
            var traps = Child(ringRoot, "RopeTrapZones");
            var breaks = Child(ringRoot, "RopeBreakZones");

            foreach (RopeSide side in System.Enum.GetValues(typeof(RopeSide)))
            {
                Vector3 mid = SideMiddle(side);
                Vector3 inward = InwardDir(side);

                var rm = Child(ropeMiddles, $"RopeMiddle_{side}");
                rm.position = new Vector3(mid.x, MatTopY + MidRope, mid.z);
                var rmAnchor = rm.gameObject.AddComponent<AerialLaunchAnchor>();
                rmAnchor.anchorType = AerialAnchorType.RopeMiddle;
                rmAnchor.ropeSide = side;
                rig.aerialAnchors.Add(rmAnchor);

                var rb = Child(rebounds, $"RopeRebound_{side}");
                rb.position = new Vector3(mid.x, MatTopY, mid.z);
                var rbAnchor = rb.gameObject.AddComponent<RopeReboundAnchor>();
                rbAnchor.ropeSide = side;
                rbAnchor.reboundDirection = inward;
                rig.ropeReboundAnchors.Add(rbAnchor);

                var tz = Child(traps, $"RopeTrap_{side}");
                tz.position = new Vector3(mid.x, MatTopY, mid.z);
                var trap = tz.gameObject.AddComponent<RopeTrapZone>();
                trap.side = side;
                var victim = Child(tz, "VictimAnchor");
                victim.position = tz.position + inward * 0.25f + Vector3.up * 0.6f;
                trap.victimAnchor = victim;
                var attacker = Child(tz, "AttackerAnchor");
                attacker.position = tz.position - inward * 0.55f + Vector3.up * 0.1f;
                trap.attackerAnchor = attacker;
                rig.ropeTrapZones.Add(trap);

                var bz = Child(breaks, $"RopeBreak_{side}");
                bz.position = new Vector3(mid.x, MatTopY, mid.z);
                var brk = bz.gameObject.AddComponent<RopeBreakZone>();
                brk.side = side;
                rig.ropeBreakZones.Add(brk);
            }

            var cornerZones = Child(ringRoot, "CornerZones");
            foreach (var (name, x, z) in CornerDefs())
            {
                var cz = Child(cornerZones, $"CornerZone_{name}");
                cz.position = new Vector3(x, MatTopY, z);
                var zone = cz.gameObject.AddComponent<CornerZone>();
                zone.cornerName = name;
                zone.activationRange = 1.2f;
                rig.cornerZones.Add(zone);
            }
        }

        static (string, float, float)[] CornerDefs() => new[]
        {
            ("NW", -HalfExtent, HalfExtent),
            ("NE", HalfExtent, HalfExtent),
            ("SW", -HalfExtent, -HalfExtent),
            ("SE", HalfExtent, -HalfExtent)
        };

        static Vector3 SideMiddle(RopeSide side)
        {
            switch (side)
            {
                case RopeSide.North: return new Vector3(0f, 0f, HalfExtent);
                case RopeSide.South: return new Vector3(0f, 0f, -HalfExtent);
                case RopeSide.East: return new Vector3(HalfExtent, 0f, 0f);
                default: return new Vector3(-HalfExtent, 0f, 0f);
            }
        }

        static Vector3 InwardDir(RopeSide side)
        {
            switch (side)
            {
                case RopeSide.North: return Vector3.back;
                case RopeSide.South: return Vector3.forward;
                case RopeSide.East: return Vector3.left;
                default: return Vector3.right;
            }
        }
    }
}
