using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Short input buffer: if a press can't execute yet (e.g. mid-recovery),
    /// keep retrying it until the buffer window expires.
    public class InputBuffer
    {
        public const float DefaultWindow = 0.15f;

        class Entry
        {
            public PlayerAction Action;
            public System.Func<bool> Attempt;
            public float Expires;
        }

        readonly List<Entry> _entries = new List<Entry>();

        public void Buffer(PlayerAction action, System.Func<bool> attempt, float window = DefaultWindow)
        {
            if (attempt()) return; // executed immediately, no buffering needed
            _entries.RemoveAll(e => e.Action == action);
            _entries.Add(new Entry { Action = action, Attempt = attempt, Expires = Time.time + window });
        }

        public void Tick()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (Time.time > _entries[i].Expires) { _entries.RemoveAt(i); continue; }
                if (_entries[i].Attempt()) _entries.RemoveAt(i);
            }
        }

        public void Clear() => _entries.Clear();
    }
}
