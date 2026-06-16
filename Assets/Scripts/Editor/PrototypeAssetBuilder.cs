using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoCoFight.EditorTools
{
    /// Serializes the DefaultGameData factory output to assets (under Resources
    /// so the bootstrap auto-loads them) and creates the PrototypeMatch scene.
    public static class PrototypeAssetBuilder
    {
        const string Root = "Assets/Resources/LoCoData";

        [MenuItem("Tools/LoCo Fight Game/Create Default Prototype Assets")]
        public static void CreateDefaultAssets()
        {
            var set = DefaultGameData.CreateAll();

            // Structural validation gates generation so broken data never
            // shadows working assets under Resources/LoCoData.
            var errors = MoveDataValidator.ValidateAll(set);
            if (errors.Count > 0)
            {
                foreach (string error in errors) Debug.LogError($"[MoveData] {error}");
                return;
            }
            foreach (MoveData move in set.moves)
            {
                foreach (string warning in MoveDataValidator.ValidateWarnings(move, set.moveDatabase))
                    Debug.LogWarning($"[MoveData] {warning}");
            }
            foreach (MoveChoreographyData choreography in set.choreographies)
            {
                foreach (string error in MoveChoreographyValidator.Validate(choreography))
                {
                    Debug.LogError($"[Choreography] {error}");
                    return;
                }
            }

            EnsureFolder(Root);
            EnsureFolder($"{Root}/Moves");
            EnsureFolder($"{Root}/Choreography");
            EnsureFolder($"{Root}/Specials");
            EnsureFolder($"{Root}/Traits");
            EnsureFolder($"{Root}/Stats");
            EnsureFolder($"{Root}/Wrestlers");
            EnsureFolder($"{Root}/Roster");

            foreach (var c in set.choreographies)
                Save(c, $"{Root}/Choreography/{c.name}.asset");
            foreach (var m in set.moves) Save(m, $"{Root}/Moves/{m.name}.asset");
            Save(set.moveDatabase, $"{Root}/StarterMoveDatabase.asset");
            foreach (var s in set.specials) Save(s, $"{Root}/Specials/{s.name}.asset");
            foreach (var t in set.traits) Save(t, $"{Root}/Traits/{t.name}.asset");
            foreach (var s in set.stats) Save(s, $"{Root}/Stats/{s.name}.asset");
            foreach (var d in set.definitions) Save(d, $"{Root}/Wrestlers/{d.name}.asset");
            foreach (var e in set.entries) Save(e, $"{Root}/Roster/{e.name}.asset");
            Save(set.database, $"{Root}/RosterDatabase.asset");
            Save(set.standardRules, $"{Root}/StandardMatchRules.asset");
            Save(set.noRopeBreakRules, $"{Root}/NoRopeBreaksRules.asset");
            Save(set.hardcoreRules, $"{Root}/HardcoreRules.asset");
            Save(set.easy, $"{Root}/EasyDifficulty.asset");
            Save(set.normal, $"{Root}/NormalDifficulty.asset");
            Save(set.hard, $"{Root}/HardDifficulty.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Wire up portraits if they're already imported (and import if possible).
            RosterAssetImporter.ImportPortraits();

            Debug.Log($"[Builder] Default prototype assets created under {Root}");
        }

        [MenuItem("Tools/LoCo Fight Game/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrapGo = new GameObject("GameBootstrap");
            var bootstrap = bootstrapGo.AddComponent<GameBootstrap>();
            bootstrap.rosterDatabase = AssetDatabase.LoadAssetAtPath<RosterDatabase>($"{Root}/RosterDatabase.asset");
            bootstrap.matchRules = AssetDatabase.LoadAssetAtPath<MatchRulesData>($"{Root}/StandardMatchRules.asset");
            bootstrap.cpuDifficulty = AssetDatabase.LoadAssetAtPath<AIDifficultyData>($"{Root}/NormalDifficulty.asset");

            EnsureFolder("Assets/Scenes");
            const string scenePath = "Assets/Scenes/PrototypeMatch.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            // Make sure the scene is in build settings so reset-by-reload works in builds.
            var scenes = EditorBuildSettings.scenes;
            bool present = false;
            foreach (var s in scenes) if (s.path == scenePath) present = true;
            if (!present)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
                {
                    new EditorBuildSettingsScene(scenePath, true)
                };
                EditorBuildSettings.scenes = list.ToArray();
            }

            Debug.Log($"[Builder] Created {scenePath} — press Play to fight");
        }

        [MenuItem("Tools/LoCo Fight Game/Setup Everything (Assets + Scene)")]
        public static void SetupEverything()
        {
            CreateDefaultAssets();
            CreatePrototypeScene();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }

        static void Save(Object obj, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(obj, path);
        }
    }
}
