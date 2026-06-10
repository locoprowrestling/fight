using UnityEngine;

namespace LoCoFight
{
    /// F2-toggled debug roster selector. Selections persist across the scene
    /// reload triggered by Start Match (statics survive within a play session).
    public class RosterSelectDebug : MonoBehaviour
    {
        public static string SelectedPlayerId;
        public static string SelectedCpuId;

        bool _open;
        Vector2 _scrollP, _scrollC;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2)) _open = !_open;
        }

        void OnGUI()
        {
            if (!_open) return;
            var mm = MatchManager.Instance;
            if (mm == null || mm.rosterDatabase == null) return;
            var db = mm.rosterDatabase;

            GUILayout.BeginArea(new Rect(Screen.width - 480, 100, 460, 460), GUI.skin.box);
            GUILayout.Label($"Roster Select  —  Player: {SelectedPlayerId ?? mm.defaultPlayerRosterId}   CPU: {SelectedCpuId ?? mm.defaultCpuRosterId}");
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(220));
            GUILayout.Label("Player");
            _scrollP = GUILayout.BeginScrollView(_scrollP, GUILayout.Height(320));
            foreach (var e in db.entries)
                if (e != null && GUILayout.Button(e.displayName)) SelectedPlayerId = e.rosterId;
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(220));
            GUILayout.Label("CPU");
            _scrollC = GUILayout.BeginScrollView(_scrollC, GUILayout.Height(320));
            foreach (var e in db.entries)
                if (e != null && GUILayout.Button(e.displayName)) SelectedCpuId = e.rosterId;
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Match")) mm.RequestReset();
            if (GUILayout.Button("Random CPU"))
            {
                var pick = db.RandomEntry(SelectedPlayerId ?? mm.defaultPlayerRosterId);
                if (pick != null) SelectedCpuId = pick.rosterId;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
