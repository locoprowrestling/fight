using UnityEngine;

namespace LoCoFight
{
    /// F1-toggled OnGUI debug readout: states, AI state, rope/corner info,
    /// special validity, counts, and active effects.
    public class DebugOverlay : MonoBehaviour
    {
        public static bool Enabled { get; private set; }

        public static void Toggle() => Enabled = !Enabled;

        void OnGUI()
        {
            if (!Enabled) return;
            var mm = MatchManager.Instance;
            if (mm == null || mm.Player == null || mm.Cpu == null) return;

            GUILayout.BeginArea(new Rect(10, 100, 460, 660), GUI.skin.box);
            GUILayout.Label($"Match: {mm.State}   Referee: {mm.RefereeAttention}");
            GUILayout.Label($"Distance: {mm.Player.DistanceToOpponent():0.00}");
            DrawWrestler(mm.Player, "PLAYER");
            DrawWrestler(mm.Cpu, "CPU");

            var ai = mm.Cpu.GetComponent<CPUWrestlerAI>();
            if (ai != null)
            {
                GUILayout.Label($"AI state: {ai.CurrentState}  behavior(F3): {ai.BehaviorMode}");
                GUILayout.Label($"AI: {ai.PersonalityKind} state={ai.CurrentState} " +
                                $"selected={ai.LastSelectedFamily}");
                GUILayout.Label($"AI weights: {ai.LastWeightsDebug}");
                GUILayout.Label($"AI memory: {ai.MemoryDebug}");
            }

            var pic = mm.Player.GetComponent<PlayerInputController>();
            if (pic != null)
                GUILayout.Label($"Press: control {pic.DebugControlPhase} | powerLock={pic.PowerLockArmed}");
            GUILayout.Label($"Prompts: {MatchHUD.CurrentPromptText}");
            GUILayout.Label($"Feel: enabled={FeelSystem.Enabled} last impact: {FeelSystem.LastImpactDebug}");

            var pins = PinSystem.Instance;
            if (pins != null && pins.Active) GUILayout.Label($"Pin: count {pins.CurrentCount}  elapsed {pins.Elapsed:0.0}");
            var subs = SubmissionSystem.Instance;
            if (subs != null && subs.Active)
                GUILayout.Label($"Submission: {subs.HoldLabel} pressure {subs.Pressure:0} " +
                                $"escape {subs.Escape:0} rope {subs.LastRopeDistance:0.0} crawl {subs.LastCrawlRate:0.00}");
            var refSys = RefereeCountSystem.Instance;
            if (refSys != null && refSys.Counting) GUILayout.Label($"Referee count: {refSys.CurrentCount}");
            GUILayout.EndArea();
        }

        void DrawWrestler(WrestlerCore w, string label)
        {
            var ring = RingInteractionSystem.Instance;
            var info = ring != null ? ring.GetNearestRopeContactInfo(w) : default;
            GUILayout.Label($"--- {label}: {w.DisplayName} ---");
            GUILayout.Label($"State: {w.States.Current} ({w.States.TimeInState:0.0}s)  Move: {w.Combat.LastMoveName}");
            GUILayout.Label($"HP {w.Stats.Health:0}/{w.Stats.MaxHealth:0}  STA {w.Stats.Stamina:0}  " +
                            $"MOM {w.Stats.Momentum:0}  specialReady={w.Stats.IsSpecialReady}");
            if (ring != null)
            {
                GUILayout.Label($"Rope: {info.ropeSide} d={info.distanceToRope:0.00}  corner d={info.distanceToCorner:0.00} " +
                                $"break={ring.IsInRopeBreak(w)} trap={ring.IsInRopeTrapZone(w)} cornerZone={ring.IsInCornerZone(w)}");
            }
            if (w.Specials != null && w.Specials.Data != null)
            {
                bool valid = w.Specials.IsCurrentlyValid(out string reason);
                GUILayout.Label($"Special: {w.Specials.Data.displayName} valid={valid} {(valid ? "" : reason)}");
            }
            if (w.Buffs.Active.Count > 0)
            {
                string effects = "";
                foreach (var e in w.Buffs.Active) effects += $"{e.Id}({e.Remaining:0.0}s) ";
                GUILayout.Label($"Effects: {effects}");
            }
            GUILayout.Label($"ReversalWindow open vs opp: {w.Combat.IsReversalWindowOpenFor(w.Opponent ?? w)}");
            GUILayout.Label($"Reversal: read={w.Combat.LastReversalRead} " +
                            $"outcome={w.Combat.LastReversalOutcome} " +
                            $"presentation={w.Combat.LastReversalPresentationId}");
            var snapshot = w.Combat.LastContextSnapshot;
            GUILayout.Label(
                $"Context: {w.Combat.CurrentContext} zone={snapshot.GroundZone} " +
                $"dir={snapshot.Direction} family={snapshot.RequestedFamily}");
            GUILayout.Label(
                $"Selected: {snapshot.SelectedMove} tier={snapshot.Tier} " +
                $"valid={snapshot.Validation.IsValid} reason={snapshot.Validation.Reason} " +
                $"fallback={snapshot.UsedFallback}");
        }
    }
}
