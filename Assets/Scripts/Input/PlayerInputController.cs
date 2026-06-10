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
            Vector3 input = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) input += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) input += Vector3.back;
            if (Input.GetKey(KeyCode.A)) input += Vector3.left;
            if (Input.GetKey(KeyCode.D)) input += Vector3.right;

            // Camera-relative movement.
            var cam = UnityEngine.Camera.main;
            if (cam != null && input.sqrMagnitude > 0.01f)
            {
                Vector3 fwd = MathUtil.Flat(cam.transform.forward).normalized;
                Vector3 right = MathUtil.Flat(cam.transform.right).normalized;
                input = fwd * input.z + right * input.x;
            }

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
