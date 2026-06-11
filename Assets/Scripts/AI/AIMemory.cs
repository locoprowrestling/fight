using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LoCoFight
{
    /// Per-family attempt/success memory for the CPU. Successful repeats are
    /// penalized harder than whiffed attempts, and everything decays so a
    /// favorite move comes back after the opponent has had room to breathe.
    /// All rule methods take explicit timestamps so tests inject time;
    /// compatibility overloads use Time.time for live callers.
    public class AIMemory
    {
        class FamilyRecord
        {
            public float LastAttemptTime = -99f;
            public float LastSuccessTime = -99f;
            public int ConsecutiveAttempts;
            public int ConsecutiveSuccesses;

            public float LastTouchedTime =>
                Mathf.Max(LastAttemptTime, LastSuccessTime);
        }

        const float AttemptPenalty = 0.08f;
        const float SuccessPenalty = 0.15f;
        const float MaxPenalty = 0.6f;
        const float PenaltyDecaySeconds = 8f;
        const float StreakResetSeconds = 6f;

        readonly Dictionary<string, FamilyRecord> _families =
            new Dictionary<string, FamilyRecord>();

        public bool CanUse(string family, float cooldown, float now)
        {
            return !_families.TryGetValue(family, out var record) ||
                   now - record.LastTouchedTime >= cooldown;
        }

        public void NoteAttempt(string family, float now)
        {
            var record = RecordFor(family, now);
            record.ConsecutiveAttempts++;
            record.LastAttemptTime = now;
        }

        public void NoteSuccess(string family, float now)
        {
            var record = RecordFor(family, now);
            record.ConsecutiveSuccesses++;
            record.LastSuccessTime = now;
        }

        /// 0..MaxPenalty multiplier-penalty for repeating this family.
        public float RepetitionPenalty(string family, float now)
        {
            if (!_families.TryGetValue(family, out var record)) return 0f;
            float raw = record.ConsecutiveAttempts * AttemptPenalty +
                        record.ConsecutiveSuccesses * SuccessPenalty;
            float age = Mathf.Max(0f, now - record.LastTouchedTime);
            float decay = Mathf.Clamp01(1f - age / PenaltyDecaySeconds);
            return Mathf.Min(MaxPenalty, raw) * decay;
        }

        public string DebugSummary(float now)
        {
            if (_families.Count == 0) return "-";
            var sb = new StringBuilder();
            foreach (var pair in _families)
            {
                float penalty = RepetitionPenalty(pair.Key, now);
                if (penalty <= 0.005f) continue;
                sb.Append(pair.Key).Append('=').Append(penalty.ToString("0.00")).Append(' ');
            }
            return sb.Length > 0 ? sb.ToString().TrimEnd() : "-";
        }

        public void Clear() => _families.Clear();

        FamilyRecord RecordFor(string family, float now)
        {
            if (!_families.TryGetValue(family, out var record))
            {
                record = new FamilyRecord();
                _families[family] = record;
            }
            else if (now - record.LastTouchedTime > StreakResetSeconds)
            {
                record.ConsecutiveAttempts = 0;
                record.ConsecutiveSuccesses = 0;
            }
            return record;
        }

        // ---- Compatibility overloads for live (Time.time) callers ----

        public bool CanUse(string family, float cooldown) =>
            CanUse(family, cooldown, Time.time);

        public void Note(string family) => NoteAttempt(family, Time.time);
    }
}
