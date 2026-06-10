using UnityEngine;

namespace LoCoFight
{
    /// Runtime meters and modifiers, initialized from WrestlerStatsData.
    public class WrestlerStatsRuntime : MonoBehaviour
    {
        public WrestlerStatsData Data { get; private set; }

        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public float Momentum { get; private set; }

        public float MaxHealth => Data != null ? Data.maxHealth : 100f;
        public float MaxStamina => Data != null ? Data.maxStamina : 100f;
        public float MaxMomentum => Data != null ? Data.maxMomentum : 100f;

        public float HealthPercent => MaxHealth <= 0f ? 0f : Health / MaxHealth;
        public float StaminaPercent => MaxStamina <= 0f ? 0f : Stamina / MaxStamina;
        public float MomentumPercent => MaxMomentum <= 0f ? 0f : Momentum / MaxMomentum;
        public bool HasFullMomentum => Momentum >= MaxMomentum - 0.01f;

        public float RecentDamage { get; private set; } // decays; weakens kickouts

        public event System.Action<float, WrestlerCore> OnDamaged;

        WrestlerCore _core;
        bool _suppressRecovery;

        public void Initialize(WrestlerCore core, WrestlerStatsData data)
        {
            _core = core;
            Data = data;
            ResetMeters();
        }

        public void ResetMeters()
        {
            Health = MaxHealth;
            Stamina = MaxStamina;
            Momentum = 0f;
            RecentDamage = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            bool highEffort = _core != null && (_core.States.Current == WrestlerState.Running ||
                _core.States.Current == WrestlerState.SubmissionApplying ||
                _core.States.Current == WrestlerState.SpecialActive);
            if (!highEffort && !_suppressRecovery && Data != null)
            {
                float mult = 1f;
                if (_core != null)
                {
                    mult *= _core.Buffs.StaminaRecoveryMult;
                    if (_core.Traits != null) mult *= _core.Traits.StaminaRecoveryMult;
                }
                Stamina = Mathf.Min(MaxStamina, Stamina + Data.staminaRecoveryPerSecond * mult * dt);
            }

            if (Data != null && Data.momentumDecayPerSecond > 0f)
                Momentum = Mathf.Max(0f, Momentum - Data.momentumDecayPerSecond * dt);

            RecentDamage = Mathf.Max(0f, RecentDamage - 8f * dt);
        }

        public void ApplyDamage(float amount, WrestlerCore source = null)
        {
            if (amount <= 0f) return;
            Health = Mathf.Clamp(Health - amount, 0f, MaxHealth);
            RecentDamage = Mathf.Min(60f, RecentDamage + amount);
            OnDamaged?.Invoke(amount, source);
            if (_core != null && _core.Anim != null) _core.Anim.TriggerHitReact();
        }

        public void Heal(float amount) => Health = Mathf.Clamp(Health + amount, 0f, MaxHealth);

        public bool SpendStamina(float amount)
        {
            if (Stamina < amount) return false;
            Stamina -= amount;
            return true;
        }

        public void DrainStamina(float amount) => Stamina = Mathf.Max(0f, Stamina - amount);

        public void AddMomentum(float amount)
        {
            if (amount > 0f && _core != null) amount *= _core.Buffs.MomentumGainMult;
            Momentum = Mathf.Clamp(Momentum + amount, 0f, MaxMomentum);
        }

        public void SpendAllMomentum() => Momentum = 0f;
        public void SpendMomentum(float amount) => Momentum = Mathf.Max(0f, Momentum - amount);
    }
}
