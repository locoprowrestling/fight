using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    /// Tracks timed StatusEffects and exposes aggregated modifiers.
    public class BuffDebuffController : MonoBehaviour
    {
        readonly List<StatusEffect> _active = new List<StatusEffect>();

        public IReadOnlyList<StatusEffect> Active => _active;

        public void Apply(StatusEffect effect)
        {
            _active.RemoveAll(e => e.Id == effect.Id); // refresh instead of stacking same id
            _active.Add(effect);
        }

        public bool Has(string id) => _active.Exists(e => e.Id == id);
        public void Remove(string id) => _active.RemoveAll(e => e.Id == id);
        public void Clear() => _active.Clear();

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].Remaining -= dt;
                if (_active[i].Remaining <= 0f) _active.RemoveAt(i);
            }
        }

        public float StaminaRecoveryMult { get { float v = 1f; foreach (var e in _active) v *= e.StaminaRecoveryMult; return v; } }
        public float KickoutMult { get { float v = 1f; foreach (var e in _active) v *= e.KickoutMult; return v; } }
        public float MomentumGainMult { get { float v = 1f; foreach (var e in _active) v *= e.MomentumGainMult; return v; } }
        public float GetUpSpeedMult { get { float v = 1f; foreach (var e in _active) v *= e.GetUpSpeedMult; return v; } }
        public float MoveSpeedMult { get { float v = 1f; foreach (var e in _active) v *= e.MoveSpeedMult; return v; } }
        public float ReversalStaminaCostMult { get { float v = 1f; foreach (var e in _active) v *= e.ReversalStaminaCostMult; return v; } }
        public float ReversalLeniencyBonus { get { float v = 0f; foreach (var e in _active) v += e.ReversalLeniencyBonus; return v; } }
        public float SubmissionEscapeMult { get { float v = 1f; foreach (var e in _active) v *= e.SubmissionEscapeMult; return v; } }
    }
}
