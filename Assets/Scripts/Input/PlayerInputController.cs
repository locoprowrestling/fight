using UnityEngine;

namespace LoCoFight
{
    /// Keyboard input for the human wrestler. Movement is camera-relative.
    /// W/A/S/D move, Shift run, J light, K heavy, L grapple, U special,
    /// I pin, O submission, Space reversal/kickout, Alt dodge/escape,
    /// T taunt/handshake, R reset, F1 debug, Esc pause.
    public class PlayerInputController : MonoBehaviour
    {
        WrestlerCore _core;
        readonly InputBuffer _buffer = new InputBuffer();
        bool _paused;

        public void Bind(WrestlerCore core) => _core = core;

        void Update()
        {
            HandleSystemKeys();
            if (_core == null) return;

            var mm = MatchManager.Instance;

            // Handshake choices during the pre-match ritual.
            if (mm != null && mm.State == MatchState.HandshakeSequence)
            {
                if (Input.GetKeyDown(KeyCode.T)) mm.HandshakeRespond(HandshakeResponse.Accept);
                if (Input.GetKeyDown(KeyCode.J)) mm.HandshakeRespond(HandshakeResponse.CheapShot);
                if (Input.GetKeyDown(KeyCode.L)) mm.HandshakeRespond(HandshakeResponse.Refuse);
                return;
            }

            HandlePinAndSubmissionMash();
            HandleMovement();
            HandleCombat();
            _buffer.Tick();
        }

        void HandleSystemKeys()
        {
            if (Input.GetKeyDown(KeyCode.R) && MatchManager.Instance != null &&
                MatchManager.Instance.State == MatchState.Finished)
                MatchManager.Instance.RequestReset();

            if (Input.GetKeyDown(KeyCode.F1)) DebugOverlay.Toggle();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _paused = !_paused;
                Time.timeScale = _paused ? 0f : 1f;
                MatchHUD.TryShowMessage(_paused ? "Paused" : "Resumed", 1f);
            }
        }

        void HandlePinAndSubmissionMash()
        {
            bool mash = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftAlt) ||
                        Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
                        Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D);
            if (!mash) return;

            if (PinSystem.Instance != null && PinSystem.Instance.Active && PinSystem.Instance.Defender == _core)
                PinSystem.Instance.AddPlayerKickoutEffort();
            if (SubmissionSystem.Instance != null && SubmissionSystem.Instance.Active && SubmissionSystem.Instance.Defender == _core)
                SubmissionSystem.Instance.AddPlayerEscapeEffort();
        }

        void HandleMovement()
        {
            float x = 0f;
            float depth = 0f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.W)) depth += 1f; // toward back lane (+Z)
            if (Input.GetKey(KeyCode.S)) depth -= 1f; // toward front lane (-Z)

            // Horizontal is free. Depth is lane-biased: when the player is not
            // pressing W/S, pull gently toward the nearest lane center so the
            // wrestler settles onto a lane. World Z stays continuous.
            float zNow = _core.transform.position.z;
            float zMove;
            if (Mathf.Abs(depth) > 0.01f)
            {
                zMove = depth;
            }
            else
            {
                float snapTarget = LaneSystem.SnapZ(zNow);
                zMove = Mathf.Clamp(snapTarget - zNow, -1f, 1f);
                if (Mathf.Abs(zMove) < 0.02f) zMove = 0f;
            }

            Vector3 input = new Vector3(x, 0f, zMove);
            if (input.sqrMagnitude > 1f) input.Normalize();

            _core.Motor.SetMoveInput(input, Input.GetKey(KeyCode.LeftShift));

            // Roll away while downed: direction key + Space.
            if (_core.States.Current == WrestlerState.Downed && Input.GetKeyDown(KeyCode.Space) &&
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)))
            {
                Vector3 side = Input.GetKey(KeyCode.A) ? Vector3.left : Vector3.right;
                var ring = RingInteractionSystem.Instance;
                Vector3 target = transform.position + side * 1.5f;
                if (ring != null) target = ring.Bounds.ClampInside(target);
                _core.States.Set(WrestlerState.RollingAway, 0.5f);
                _core.Motor.Teleport(target);
            }
        }

        void HandleCombat()
        {
            bool inLock = _core.Combat.InGrappleLockAsAttacker;

            if (Input.GetKeyDown(KeyCode.J))
            {
                if (_core.DistanceToOpponent() <= WrestlerCombat.StrikeRange + 0.5f || _core.Motor.IsRunning)
                    _buffer.Buffer("light", () => _core.Combat.TryRunningAttack() || _core.Combat.TryLightStrike());
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                if (inLock) _core.Combat.TryPowerGrappleFromLock();
                else _buffer.Buffer("heavy", () => _core.Combat.TryHeavyStrike());
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                if (inLock) _core.Combat.TryQuickGrappleFromLock();
                else _buffer.Buffer("grapple", () => _core.Combat.TryGrappleAttempt());
            }

            // Hold L during a lock = power grapple modifier.
            if (inLock && Input.GetKey(KeyCode.L) && Input.GetKeyDown(KeyCode.K))
                _core.Combat.TryPowerGrappleFromLock();

            if (Input.GetKeyDown(KeyCode.Space))
                _core.Combat.TryReversal();

            if (Input.GetKeyDown(KeyCode.LeftAlt))
                _core.Dodge.TryDodge();

            if (Input.GetKeyDown(KeyCode.U))
                _buffer.Buffer("special", () => _core.Combat.TrySpecial(), 0.10f);

            // Pins/submissions are not buffered: they only fire when valid right now.
            if (Input.GetKeyDown(KeyCode.I))
                _core.Combat.TryPin();

            if (Input.GetKeyDown(KeyCode.O))
                _core.Combat.TrySubmission();
        }
    }
}
