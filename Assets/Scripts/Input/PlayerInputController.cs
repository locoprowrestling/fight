using UnityEngine;

namespace LoCoFight
{
    /// Converts keyboard/controller commands into the same gameplay API used by the CPU.
    public class PlayerInputController : MonoBehaviour
    {
        const float GrappleDirectionDeadZone = 0.2f;

        WrestlerCore _core;
        readonly InputBuffer _buffer = new InputBuffer();
        readonly LegacyPlayerInputSource _input = new LegacyPlayerInputSource();
        Camera _camera;
        PlayerInputDevice _lastDevice = PlayerInputDevice.Keyboard;

        public void Bind(WrestlerCore core)
        {
            _core = core;
            _camera = Camera.main;
        }

        void Update()
        {
            var frame = _input.ReadFrame();
            HandleSystemInput(frame);
            if (_core == null) return;

            var mm = MatchManager.Instance;
            if (mm == null) return;

            if (frame.Device != _lastDevice)
            {
                _lastDevice = frame.Device;
                MatchHUD.TrySetInputDevice(_lastDevice);
            }

            if (mm.IsPaused)
            {
                StopGameplayInput();
                return;
            }

            // Handshake choices during the pre-match ritual.
            if (mm.State == MatchState.HandshakeSequence)
            {
                if (frame.HandshakeAcceptPressed) mm.HandshakeRespond(HandshakeResponse.Accept);
                else if (frame.HandshakeCheapShotPressed) mm.HandshakeRespond(HandshakeResponse.CheapShot);
                else if (frame.HandshakeRefusePressed) mm.HandshakeRespond(HandshakeResponse.Refuse);
                return;
            }

            HandlePinAndSubmissionMash(frame);
            if (!PlayerInputLogic.CanProcessGameplay(mm.IsPaused, mm.State))
            {
                StopGameplayInput();
                return;
            }

            HandleMovement(frame);
            HandleCombat(frame);
            _buffer.Tick();
        }

        void HandleSystemInput(PlayerInputFrame frame)
        {
            var mm = MatchManager.Instance;
            if (mm != null && mm.State == MatchState.Finished && frame.ResetPressed)
            {
                mm.RequestReset();
                return;
            }

            if (frame.DebugPressed) DebugOverlay.Toggle();
            if (mm != null && frame.PausePressed) mm.SetPaused(!mm.IsPaused);
        }

        void HandlePinAndSubmissionMash(PlayerInputFrame frame)
        {
            if (!frame.MashPressed) return;

            if (PinSystem.Instance != null && PinSystem.Instance.Active && PinSystem.Instance.Defender == _core)
                PinSystem.Instance.AddPlayerKickoutEffort();
            if (SubmissionSystem.Instance != null && SubmissionSystem.Instance.Active && SubmissionSystem.Instance.Defender == _core)
                SubmissionSystem.Instance.AddPlayerEscapeEffort();
        }

        void HandleMovement(PlayerInputFrame frame)
        {
            Vector3 input = new Vector3(frame.Move.x, 0f, frame.Move.y);

            // Camera-relative movement.
            if (_camera == null) _camera = Camera.main;
            if (_camera != null && input.sqrMagnitude > 0.01f)
            {
                Vector3 fwd = MathUtil.Flat(_camera.transform.forward).normalized;
                Vector3 right = MathUtil.Flat(_camera.transform.right).normalized;
                input = fwd * input.z + right * input.x;
            }

            _core.Motor.SetMoveInput(input, frame.RunHeld);

            // Roll away while downed: lateral movement + reversal.
            if (_core.States.Current == WrestlerState.Downed &&
                frame.ReversalPressed && Mathf.Abs(frame.Move.x) > 0.25f)
            {
                Vector3 side = _camera != null
                    ? MathUtil.Flat(_camera.transform.right).normalized * Mathf.Sign(frame.Move.x)
                    : Vector3.right * Mathf.Sign(frame.Move.x);
                var ring = RingInteractionSystem.Instance;
                Vector3 target = PlayerInputLogic.CalculateRollTarget(
                    _core.transform.position,
                    side,
                    1.5f);
                if (ring != null) target = ring.Bounds.ClampInside(target);
                _core.States.Set(WrestlerState.RollingAway, 0.5f);
                _core.Motor.Teleport(target);
            }
        }

        void HandleCombat(PlayerInputFrame frame)
        {
            bool inLock = _core.Combat.InGrappleLockAsAttacker;

            if (frame.LightPressed)
            {
                if (_core.DistanceToOpponent() <= WrestlerCombat.StrikeRange + 0.5f || _core.Motor.IsRunning)
                    _buffer.Buffer(PlayerAction.Light,
                        () => _core.Combat.TryGroundAttack() ||
                              _core.Combat.TryCornerStrike() ||
                              _core.Combat.TryRunningAttack() ||
                              _core.Combat.TryLightStrike());
            }

            if (inLock)
            {
                MoveDirection direction = PlayerInputLogic.ResolveMoveDirection(
                    frame.Move,
                    _core.transform.forward,
                    _core.transform.right,
                    GrappleDirectionDeadZone);
                switch (PlayerInputLogic.ResolveLockAction(frame.HeavyPressed, frame.GrapplePressed))
                {
                    case PlayerAction.Heavy:
                        _core.Combat.TryPowerGrappleFromLock(direction);
                        break;
                    case PlayerAction.Grapple:
                        _core.Combat.TryQuickGrappleFromLock(direction);
                        break;
                }
            }
            else if (frame.HeavyPressed)
            {
                _buffer.Buffer(PlayerAction.Heavy, () => _core.Combat.TryHeavyStrike());
            }
            else if (frame.GrapplePressed)
            {
                _buffer.Buffer(PlayerAction.Grapple,
                    () => _core.Combat.TryCornerGrapple() ||
                          _core.Combat.TryGrappleAttempt());
            }

            if (frame.ReversalPressed)
                _core.Combat.TryReversal();

            if (frame.DodgePressed)
                _core.Dodge.TryDodge();

            if (frame.SpecialPressed)
                _buffer.Buffer(PlayerAction.Special, () => _core.Combat.TrySpecial(), 0.10f);

            // Pins/submissions are not buffered: they only fire when valid right now.
            if (frame.PinPressed)
                _core.Combat.TryPin();

            if (frame.SubmissionPressed)
                _core.Combat.TrySubmission();
        }

        void StopGameplayInput()
        {
            _buffer.Clear();
            if (_core != null) _core.Motor.SetMoveInput(Vector3.zero, false);
        }
    }
}
