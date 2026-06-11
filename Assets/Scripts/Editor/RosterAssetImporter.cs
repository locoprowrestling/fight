using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LoCoFight.EditorTools
{
    /// Imports tas-*.png portraits from players-web/ into Assets/Art/RosterPortraits,
    /// configures them as sprites, and assigns them to RosterEntry assets.
    public static class RosterAssetImporter
    {
        public const string PortraitFolder = "Assets/Art/RosterPortraits";

        /// rosterId -> explicit display name (do not infer names from filenames).
        public static readonly (string id, string displayName)[] ExpectedRoster =
        {
            ("tas-anuka-gutierrez", "Anuka Gutierrez"),
            ("tas-avalon", "Michael Avalon"),
            ("tas-carter-cash", "Carter Cash"),
            ("tas-codah", "Codah Alexander"),
            ("tas-cody-devine", "Cody Devine"),
            ("tas-dean-mercer", "Dean Mercer"),
            ("tas-erza", "Erza Menagerie Tinker"),
            ("tas-franky-gonzales", "Franky Gonzales"),
            ("tas-hussy", "Hussy Steele"),
            ("tas-johnny-crash", "Johnny Crash"),
            ("tas-jt-staten", "JT Staten"),
            ("tas-major-glory", "Major Glory"),
            ("tas-morgana-lavey", "Morgana Lavey"),
            ("tas-nicky-hyde", "Nicky Hyde"),
            ("tas-vigilante-oai", "The Vigilante"),
            ("tas-zeak-gallent", "Zeak Gallent"),
        };

        static string SourceDir()
        {
            // Prefer the project-relative players-web folder, then the documented absolute path.
            string relative = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "players-web"));
            if (Directory.Exists(relative)) return relative;
            const string absolute = "/Users/gecko/locoprowrestling/fightgame/players-web";
            return Directory.Exists(absolute) ? absolute : null;
        }

        [MenuItem("Tools/LoCo Fight Game/Import TAS Roster Portraits")]
        public static void ImportPortraits()
        {
            string source = SourceDir();
            if (source == null)
            {
                Debug.LogError("[Importer] players-web source folder not found");
                return;
            }

            Directory.CreateDirectory(PortraitFolder);
            var found = Directory.GetFiles(source, "tas-*.png").Select(Path.GetFileName).ToList();

            foreach (string file in found)
                File.Copy(Path.Combine(source, file), Path.Combine(PortraitFolder, file), overwrite: true);
            AssetDatabase.Refresh();

            // Configure as sprites.
            foreach (string file in found)
            {
                string assetPath = $"{PortraitFolder}/{file}";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null &&
                    (importer.textureType != TextureImporterType.Sprite ||
                     importer.spriteImportMode != SpriteImportMode.Single))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    // Single mode is required: in Multiple mode with no slices,
                    // the texture has no Sprite sub-assets to load.
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
            }

            // Warnings: missing expected files / unexpected extras.
            foreach (var (id, _) in ExpectedRoster)
                if (!found.Contains(id + ".png"))
                    Debug.LogWarning($"[Importer] Missing expected portrait: {id}.png");
            foreach (string file in found)
                if (!ExpectedRoster.Any(e => e.id + ".png" == file))
                    Debug.LogWarning($"[Importer] Extra tas- file not mapped to a display name: {file}");

            AssignPortraitsToEntries();
            Debug.Log($"[Importer] Imported {found.Count} portraits into {PortraitFolder}");
        }

        public static void AssignPortraitsToEntries()
        {
            var entryGuids = AssetDatabase.FindAssets("t:RosterEntry");
            if (entryGuids.Length == 0)
            {
                Debug.LogWarning("[Importer] No RosterEntry assets found. Run Tools > LoCo Fight Game > Create Default Prototype Assets first.");
                return;
            }

            var seenIds = new HashSet<string>();
            foreach (string guid in entryGuids)
            {
                var entry = AssetDatabase.LoadAssetAtPath<RosterEntry>(AssetDatabase.GUIDToAssetPath(guid));
                if (entry == null) continue;
                if (!seenIds.Add(entry.rosterId))
                    Debug.LogWarning($"[Importer] Duplicate roster id: {entry.rosterId}");

                string spritePath = $"{PortraitFolder}/{entry.sourceImageFileName}";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[Importer] Missing portrait sprite for {entry.rosterId} at {spritePath}");
                    continue;
                }
                entry.portraitSprite = sprite;
                EditorUtility.SetDirty(entry);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Importer] Portrait sprites assigned to roster entries");
        }
    }
}
