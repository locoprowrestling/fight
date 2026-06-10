using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/Roster Database")]
    public class RosterDatabase : ScriptableObject
    {
        public List<RosterEntry> entries = new List<RosterEntry>();

        public RosterEntry Find(string rosterId)
        {
            foreach (var e in entries)
                if (e != null && e.rosterId == rosterId)
                    return e;
            return null;
        }

        public RosterEntry RandomEntry(string excludeRosterId = null)
        {
            var pool = new List<RosterEntry>();
            foreach (var e in entries)
                if (e != null && e.rosterId != excludeRosterId)
                    pool.Add(e);
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
