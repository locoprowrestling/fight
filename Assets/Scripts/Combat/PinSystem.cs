using UnityEngine;

namespace LoCoFight
{
    /// One pin attempt at a time. Counts 1-2-3, supports human mash kickouts,
    /// CPU formula kickouts, rope breaks, and special-move kickout penalties.
    public class PinSystem : MonoBehaviour
    {
        public static PinSystem Instance { get; private set; }

        public bool Active { get; private set; }
        public WrestlerCore Attacker { get; private set; }
        public WrestlerCore Defender { get; private set; }
        public int CurrentCount { get; private set; }
        public float Elapsed { get; private set; }

        float _effort;
        float _requiredEffort;
        float _penalty;
        float _cpuTick;
        const float PinRange = 1.4f; // scaled with the 1.25x bodies

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        MatchRulesData Rules => MatchManager.Instance.Rules;

        public bool TryStartPin(WrestlerCore attacker, WrestlerCore defender)
        {
            if (Active) return false;
            if (MatchManager.Instance == null || !MatchManager.Instance.IsCombatAllowed) return false;
            if (Rules == null || !Rules.pinfallsEnabled) return false;
            if (!defender.States.Profile.canBePinned) return false;
            if (!attacker.States.Profile.canAttack && attacker.States.Current != WrestlerState.AerialLandingHit) return false;
            if (MathUtil.FlatDistance(attacker.transform.position, defender.transform.position) > PinRange) return false;

            Active = true;
            Attacker = attacker;
            Defender = defender;
            CurrentCount = 0;
            Elapsed = 0f;
            _effort = 0f;
            _cpuTick = 0f;
            _penalty = defender.Combat.ConsumeKickoutPenalty();
            _requiredEffort = ComputeRequiredEffort();

            attacker.Combat.ForceRelease();
            defender.Combat.ForceRelease();
            attacker.Motor.Teleport(defender.transform.position +
                MathUtil.FlatDirection(defender.transform.position, attacker.transform.position) * 0.7f);
            attacker.States.Set(WrestlerState.Pinning);
            defender.States.Set(WrestlerState.Pinned);

            MatchManager.Instance.SetState(MatchState.PinInProgress);
            MatchHUD.TryShowMessage($"{attacker.DisplayName} goes for the pin!");
            Debug.Log($"[Pin] {attacker.DisplayName} pins {defender.DisplayName} (penalty {_penalty:0.##})");
            return true;
        }

        float ComputeRequiredEffort()
        {
            var s = Defender.Stats;
            float skill = s.Data != null ? s.Data.kickoutSkill : 0.5f;
            // Healthy + rested = very few presses; hurt + tired = a lot.
            float strength = s.HealthPercent * 0.5f + s.StaminaPercent * 0.3f + skill * 0.2f
                             - (s.RecentDamage / 60f) * 0.3f;
            float required = Mathf.Lerp(16f, 3f, Mathf.Clamp01(strength));
            required *= 1f + _penalty;
            required /= Mathf.Max(0.2f, Defender.Buffs.KickoutMult);
            return Mathf.Clamp(required, 2f, 22f);
        }

        /// Human defender mashes Reversal / Dodge / movement keys.
        public void AddPlayerKickoutEffort()
        {
            if (!Active || Defender == null || !Defender.IsPlayer) return;
            float bonus = Defender.Traits != null ? Defender.Traits.GetKickoutBonus(Elapsed >= 2.0f) : 0f;
            _effort += 1f * (1f + bonus);
        }

        void Update()
        {
            if (!Active) return;
            Elapsed += Time.deltaTime;

            // Rope break interrupts when rules allow it.
            if (Rules.RopeBreaksActive && RingInteractionSystem.Instance.IsInRopeBreak(Defender))
            {
                EndPin(ropeBreak: true);
                return;
            }

            int count = Mathf.FloorToInt(Elapsed / (Rules.standardPinCountSeconds / 3f)) ;
            if (count != CurrentCount)
            {
                CurrentCount = count;
                if (CurrentCount >= 1 && CurrentCount <= 3)
                    MatchHUD.TryShowCount(CurrentCount.ToString());
            }

            if (!Defender.IsPlayer)
            {
                _cpuTick += Time.deltaTime;
                if (_cpuTick >= 0.25f)
                {
                    _cpuTick -= 0.25f;
                    if (Random.value < CpuKickoutChance())
                    {
                        Kickout();
                        return;
                    }
                }
            }
            else if (_effort >= _requiredEffort)
            {
                Kickout();
                return;
            }

            if (Elapsed >= Rules.standardPinCountSeconds)
            {
                Active = false;
                Debug.Log($"[Pin] Three count! {Attacker.DisplayName} wins");
                MatchManager.Instance.AnnounceWin(Attacker, WinCondition.Pinfall);
            }
        }

        /// CPU kickout chance per 0.25s tick:
        /// base + health*0.35 + stamina*0.25 + skill*0.20 - recentDamage*0.30
        /// + difficulty modifier + trait bonus - active penalty, clamped 0.05..0.95.
        float CpuKickoutChance()
        {
            var s = Defender.Stats;
            float skill = s.Data != null ? s.Data.kickoutSkill : 0.5f;
            float difficulty = MatchManager.Instance.CpuDifficulty != null ? MatchManager.Instance.CpuDifficulty.kickoutBonus : 0f;
            float traitBonus = Defender.Traits != null ? Defender.Traits.GetKickoutBonus(Elapsed >= 2.0f) : 0f;
            float chance = 0.08f
                + s.HealthPercent * 0.35f
                + s.StaminaPercent * 0.25f
                + skill * 0.20f
                - (s.RecentDamage / 60f) * 0.30f
                + difficulty
                + traitBonus
                - _penalty;
            chance *= Defender.Buffs.KickoutMult;
            return Mathf.Clamp(chance, 0.05f, 0.95f);
        }

        void Kickout()
        {
            Debug.Log($"[Pin] {Defender.DisplayName} kicks out at {CurrentCount}");
            MatchHUD.TryShowMessage("Kickout!");
            if (Defender.Traits != null) Defender.Traits.NotifyKickout(Elapsed >= 2.0f);
            Defender.Stats.AddMomentum(5f);
            EndPin(ropeBreak: false, kickout: true);
        }

        void EndPin(bool ropeBreak, bool kickout = false)
        {
            Active = false;
            if (ropeBreak)
            {
                MatchHUD.TryShowMessage("Rope break!");
                Debug.Log("[Pin] Rope break!");
            }
            MatchHUD.TryShowCount("");
            if (kickout)
            {
                // A kickout SHOVES the attacker off: they reposition while
                // the defender rises, so the escape can't chain straight into
                // the next pin.
                if (Attacker != null)
                {
                    Vector3 away = MathUtil.FlatDirection(
                        Defender.transform.position, Attacker.transform.position);
                    Attacker.Motor.ApplyKnockback(away, 1.4f);
                    Attacker.States.Set(WrestlerState.GrappleMoveRecovery, 1.0f);
                }
                if (Defender != null) Defender.States.Set(WrestlerState.Downed, 0.45f);
            }
            else
            {
                if (Attacker != null) Attacker.States.Set(WrestlerState.GrappleMoveRecovery, 0.6f);
                if (Defender != null) Defender.States.Set(WrestlerState.Downed, 0.9f);
            }
            if (MatchManager.Instance.State == MatchState.PinInProgress)
                MatchManager.Instance.SetState(MatchState.Active);
            Attacker = null;
            Defender = null;
        }

        public void CancelIfActive()
        {
            if (Active) EndPin(false);
        }
    }
}
