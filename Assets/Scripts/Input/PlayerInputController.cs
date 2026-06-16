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
        readonly PressTracker _controlTracker = new PressTracker();
        readonly GrappleInputIntent _grappleIntent = new GrappleInputIntent();
        float _grappleIntentExpiresAt;

        public const float DownedControlRange = 1.4f; // matches TryPin/TrySubmission reach

        public string DebugControlPhase =>
            _grappleIntent.Active
                ? $"grapple {_grappleIntent.Direction} pending={_grappleIntent.PendingAction}"
                : _controlTracker.IsDown
                ? $"down {_controlTracker.HeldDuration:0.00}s committed={_controlTracker.Committed}"
                : "up";

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
            // Submission defense replaces locomotion while the hold is
            // active: movement becomes crawl intent, mash stays escape
            // effort. Runs before the CanProcessGameplay gate because the
            // match state is SubmissionInProgress, not Active.
            if (HandleSubmissionDefense(frame)) return;
            if (!PlayerInputLogic.CanProcessGameplay(mm.IsPaused, mm.State))
            {
                StopGameplayInput();
                return;
            }

            HandleMovement(frame);
            HandleCombat(frame);
            _buffer.Tick();
        }

        bool _wasDefendingSubmission;

        bool HandleSubmissionDefense(PlayerInputFrame frame)
        {
            var subs = SubmissionSystem.Instance;
            bool defending = subs != null && subs.Active && subs.Defender == _core;
            if (!defending)
            {
                if (_wasDefendingSubmission && subs != null)
                    subs.ClearDefenderCrawlIntent(_core);
                _wasDefendingSubmission = false;
                return false;
            }

            _wasDefendingSubmission = true;
            Vector3 intent = new Vector3(frame.Move.x, 0f, frame.Move.y);
            if (_camera == null) _camera = Camera.main;
            if (_camera != null && intent.sqrMagnitude > 0.01f)
            {
                Vector3 fwd = MathUtil.Flat(_camera.transform.forward).normalized;
                Vector3 right = MathUtil.Flat(_camera.transform.right).normalized;
                intent = fwd * intent.z + right * intent.x;
            }
            subs.SetDefenderCrawlIntent(_core, intent);
            _core.Motor.SetMoveInput(Vector3.zero, false);
            return true;
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
                SubmissionSystem.Instance.AddEscapeEffort(_core);

            // Mash to rise: every press while downed shaves recovery time.
            if (_core.States.Current == WrestlerState.Downed)
                _core.States.ExtendTimeout(-0.15f);
        }

        void HandleMovement(PlayerInputFrame frame)
        {
            if (ShouldReserveMovementForGrapple(frame))
            {
                _core.Motor.SetMoveInput(Vector3.zero, false);
                _core.Motor.FaceOpponent();
                return;
            }

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

        bool ShouldReserveMovementForGrapple(PlayerInputFrame frame)
        {
            if (_core.Combat.InGrappleLockAsAttacker) return false;
            bool downedNear = _core.Opponent != null &&
                              _core.Opponent.States.IsDowned &&
                              _core.DistanceToOpponent() <= DownedControlRange;
            bool inAcquisitionRange = _core.DistanceToOpponent() <= WrestlerCombat.GrappleRange;
            return !downedNear && inAcquisitionRange &&
                   (frame.ControlPressed || _grappleIntent.Active);
        }

        void HandleCombat(PlayerInputFrame frame)
        {
            bool inLock = _core.Combat.InGrappleLockAsAttacker;
            bool downedNear = _core.Opponent != null &&
                              _core.Opponent.States.IsDowned &&
                              _core.DistanceToOpponent() <= DownedControlRange;

            // AKI grammar: one press = one move; direction is the only
            // modifier. Strikes fire the instant J is pressed — neutral =
            // light, held direction = heavy — with the contextual families
            // keeping their precedence.
            if (frame.LightPressed &&
                (_core.DistanceToOpponent() <= WrestlerCombat.StrikeRange + 0.5f || _core.Motor.IsRunning))
            {
                bool directional = PlayerInputLogic.ApplyDeadZone(
                    frame.Move, GrappleDirectionDeadZone) != Vector2.zero;
                _buffer.Buffer(PlayerAction.Light,
                    () => _core.Combat.TryGroundAttack() ||
                          _core.Combat.TryCornerStrike() ||
                          _core.Combat.TryRopeStaggerAttack() ||
                          _core.Combat.TryRopeReboundAttack() ||
                          _core.Combat.TryRunningAttack() ||
                          (directional ? _core.Combat.TryHeavyStrike()
                                       : _core.Combat.TryLightStrike()));
            }

            if (inLock)
            {
                MoveDirection direction = ResolveGrappleDirection(frame);
                if (!_grappleIntent.Active && frame.ControlPressed)
                {
                    _grappleIntent.Begin(
                        direction,
                        frame.ControlHeld,
                        Time.deltaTime,
                        PlayerInputLogic.HoldThreshold);
                }
                else
                {
                    _grappleIntent.Advance(
                        direction,
                        frame.ControlHeld,
                        frame.ControlReleased,
                        Time.deltaTime,
                        PlayerInputLogic.HoldThreshold);
                }

                MoveDirection intentDirection = _grappleIntent.Direction;
                GrapplePressAction grappleAction = _grappleIntent.ConsumeAction();
                if (grappleAction != GrapplePressAction.None)
                {
                    if (grappleAction == GrapplePressAction.Power)
                        _core.Combat.TryPowerGrappleFromLock(intentDirection);
                    else
                        _core.Combat.TryQuickGrappleFromLock(intentDirection);
                }
            }
            else if (downedNear)
            {
                _grappleIntent.Cancel();
                // Pin/submission keep tap/hold: deliberate, low-frequency
                // actions where a beat of deliberation reads correctly.
                PressKind controlKind = _controlTracker.Update(
                    frame.ControlPressed, frame.ControlHeld, frame.ControlReleased,
                    Time.deltaTime, PlayerInputLogic.HoldThreshold);
                if (controlKind == PressKind.Tap)
                    _core.Combat.TryPin();
                else if (controlKind == PressKind.HoldCommitted)
                    _core.Combat.TrySubmission();
            }
            else
            {
                MoveDirection direction = ResolveGrappleDirection(frame);
                if (frame.ControlPressed)
                {
                    _controlTracker.Reset();
                    _grappleIntent.Begin(
                        direction,
                        frame.ControlHeld,
                        Time.deltaTime,
                        PlayerInputLogic.HoldThreshold);
                    _grappleIntentExpiresAt =
                        Time.time + InputBuffer.DefaultWindow + PlayerInputLogic.HoldThreshold;
                    _buffer.Buffer(PlayerAction.Grapple,
                        () => _core.Combat.TryCornerGrapple() ||
                              _core.Combat.TryGrappleAttempt());

                    if (_core.Combat.Role == GrappleRole.Defender ||
                        (!_core.Combat.InGrappleLockAsAttacker &&
                         !_core.States.Profile.canGrapple))
                        _grappleIntent.Cancel();
                }
                else if (_grappleIntent.Active)
                {
                    _grappleIntent.Advance(
                        direction,
                        frame.ControlHeld,
                        frame.ControlReleased,
                        Time.deltaTime,
                        PlayerInputLogic.HoldThreshold);
                    if (Time.time > _grappleIntentExpiresAt)
                        _grappleIntent.Cancel();
                }
            }

            if (frame.ReversalPressed)
            {
                // Held movement supplies the optional directional read,
                // through the same camera-relative pipeline as grapples.
                MoveDirection read = ResolveGrappleDirection(frame);
                _core.Combat.TryReversal(read, read != MoveDirection.Neutral);
            }

            if (frame.DodgePressed)
                _core.Dodge.TryDodge();

            if (frame.SpecialPressed)
                _buffer.Buffer(PlayerAction.Special, () => _core.Combat.TrySpecial(), 0.10f);
        }

        MoveDirection ResolveGrappleDirection(PlayerInputFrame frame)
        {
            if (_camera == null) _camera = Camera.main;
            Vector3 camForward = _camera != null ? _camera.transform.forward : Vector3.forward;
            Vector3 camRight = _camera != null ? _camera.transform.right : Vector3.right;
            return PlayerInputLogic.ResolveMoveDirection(
                frame.Move, camForward, camRight, _core.transform.forward, GrappleDirectionDeadZone);
        }

        void StopGameplayInput()
        {
            _buffer.Clear();
            _controlTracker.Reset();
            _grappleIntent.Cancel();
            if (_core != null) _core.Motor.SetMoveInput(Vector3.zero, false);
            if (_core != null && SubmissionSystem.Instance != null)
                SubmissionSystem.Instance.ClearDefenderCrawlIntent(_core);
        }
    }
}
