using UnityEngine;

namespace LoCoFight
{
    /// Resolves the data set the match runs on. Prefers saved assets
    /// (created by Tools > LoCo Fight Game > Create Default Prototype Assets);
    /// falls back to the in-memory DefaultGameData factory so the scene always plays.
    public static class RosterLoader
    {
        public static RosterDatabase LoadDatabase(out DefaultGameDataSet fallbackSet)
        {
            fallbackSet = null;
            var db = Resources.Load<RosterDatabase>("LoCoData/RosterDatabase");
            if (db != null && db.entries.Count > 0)
            {
                Debug.Log($"[Roster] Loaded RosterDatabase asset with {db.entries.Count} entries");
                WarnMissingPortraits(db);
                return db;
            }

            Debug.LogWarning("[Roster] No RosterDatabase asset found — building default roster in memory. " +
                             "Run Tools > LoCo Fight Game > Create Default Prototype Assets to persist it.");
            fallbackSet = DefaultGameData.CreateAll();
            WarnMissingPortraits(fallbackSet.database);
            return fallbackSet.database;
        }

        static void WarnMissingPortraits(RosterDatabase db)
        {
            foreach (var e in db.entries)
                if (e != null && e.portraitSprite == null)
                    Debug.LogWarning($"[Roster] Entry '{e.rosterId}' has no portrait sprite (expected {e.sourceImageFileName})");
        }
    }
}
