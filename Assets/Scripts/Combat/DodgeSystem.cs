using UnityEngine;

namespace LoCoFight
{
    /// Standard sidestep dodge for everyone, plus the enhanced Vanishing Dodge
    /// when a dodge SpecialAbilityData is assigned (The Vigilante).
    public class DodgeSystem : MonoBehaviour
    {
        public const float StandardCooldown = 1.0f;
        public const float StandardStaminaCost = 8f;

        WrestlerCore _core;
        SpecialAbilityData _vanishData;
        float _cooldownUntil;
        float _vanishCooldownUntil;
        bool _emergencyUsed;

        public bool HasVanishingDodge => _vanishData != null;
        public float VanishCooldownRemaining => Mathf.Max(0f, _vanishCooldownUntil - Time.time);

        public void Bind(WrestlerCore core, SpecialAbilityData vanishData)
        {
            _core = core;
            _vanishData = vanishData;
        }

        public void ResetForMatch()
        {
            _cooldownUntil = 0f;
            _vanishCooldownUntil = 0f;
            _emergencyUsed = false;
        }

        public bool TryDodge()
        {
            if (_core.Opponent == null) return false;

            // Vanishing Dodge first: it can escape moves a normal dodge cannot.
            if (HasVanishingDodge && TryVanishingDodge()) return true;

            if (!_core.States.Profile.canDodge) return false;
            if (Time.time < _cooldownUntil) return false;
            if (!_core.Stats.SpendStamina(StandardStaminaCost)) return false;

            _cooldownUntil = Time.time + StandardCooldown;
            _core.States.Set(WrestlerState.Dodging, 0.4f);
            _core.Anim.TriggerDodge();
            SideStep(1.0f);
            _core.Combat.NotifyDodged();
            return true;
        }

        bool TryVanishingDodge()
        {
            if (Time.time < _vanishCooldownUntil) return false;

            var opp = _core.Opponent;
            bool escapable = IsEscapableThreat(opp, out bool isMajor);
            if (!escapable) return false;

            bool needsEmergency = isMajor && OpponentPastNormalEscape(opp);
            if (needsEmergency)
            {
                if (!_vanishData.hasOncePerMatchEmergencyVersion || _emergencyUsed) return false;
                _emergencyUsed = true;
            }

            if (!_core.Stats.SpendStamina(_vanishData.staminaCost))
            {
                _core.Stats.DrainStamina(_vanishData.failedDodgeStaminaCost);
                return false;
            }

            _vanishCooldownUntil = Time.time + _vanishData.cooldown;

            // Break out of whatever the opponent was doing to us.
            if (opp.Specials.IsExecuting) opp.Specials.OnTargetEscaped(_core);
            else opp.Combat.InterruptByReversal(_core);

            _core.States.Set(WrestlerState.Dodging, _vanishData.invulnerabilityDuration);
            _core.Anim.TriggerDodge();
            SideStep(_vanishData.repositionDistance);
            _core.Combat.NotifyDodged();
            MatchHUD.TryShowMessage("Vanish!");
            Debug.Log($"[Dodge] {_core.DisplayName} vanishes!");
            return true;
        }

        bool IsEscapableThreat(WrestlerCore opp, out bool isMajor)
        {
            isMajor = false;
            if (opp == null) return false;

            // Specials: escapable during startup / lift phases.
            if (opp.Specials != null && opp.Specials.IsExecuting)
            {
                isMajor = true;
                return opp.Specials.EscapePhase == SpecialEscapePhase.Startup ||
                       opp.Specials.EscapePhase == SpecialEscapePhase.Lift ||
                       (_vanishData.canEscapeNormallyUnescapableMoves && opp.Specials.EscapePhase != SpecialEscapePhase.None);
            }

            // Power grapples / carries / running attacks during startup.
            var move = opp.Combat.CurrentMove;
            if (move != null)
            {
                bool tagged = false;
                foreach (var t in _vanishData.escapableMoveTags)
                    if (move.HasTag(t)) { tagged = true; break; }
                bool earlyPhase = opp.Combat.MoveElapsed <= move.startupTime + 0.1f;
                bool runningTimed = move.requiresRunning && opp.Combat.MoveElapsed <= _vanishData.manualTimingWindow + move.startupTime;
                return (tagged && earlyPhase) || runningTimed ||
                       (move.category == MoveCategory.PowerGrapple && earlyPhase);
            }
            return false;
        }

        bool OpponentPastNormalEscape(WrestlerCore opp) =>
            opp.Specials != null && opp.Specials.EscapePhase == SpecialEscapePhase.Lift;

        void SideStep(float distance)
        {
            Vector3 side = Vector3.Cross(Vector3.up,
                MathUtil.FlatDirection(transform.position, _core.Opponent.transform.position));
            if (Random.value > 0.5f) side = -side;
            var ring = RingInteractionSystem.Instance;
            Vector3 target = transform.position + side * distance;
            if (ring != null) target = ring.Bounds.ClampInside(target);
            _core.Motor.Teleport(target);
            _core.Motor.FaceOpponent();
        }
    }
}
