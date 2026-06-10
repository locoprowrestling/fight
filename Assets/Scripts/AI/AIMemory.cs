using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Tracks when the CPU last used each action so it doesn't spam one move.
    public class AIMemory
    {
        readonly Dictionary<string, float> _lastUsed = new Dictionary<string, float>();

        public bool CanUse(string action, float cooldown)
        {
            return !_lastUsed.TryGetValue(action, out float t) || Time.time - t >= cooldown;
        }

        public void Note(string action) => _lastUsed[action] = Time.time;

        public void Clear() => _lastUsed.Clear();
    }
}
