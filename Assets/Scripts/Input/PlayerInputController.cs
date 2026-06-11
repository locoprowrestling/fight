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
        readonly PressTracker _strikeTracker = new PressTracker();
        readonly PressTracker _controlTracker = new PressTracker();

        public const float DownedControlRange = 1.2f; // matches TryPin/TrySubmission reach

        public string DebugStrikePhase => DescribePress(_strikeTracker);
        public string DebugControlPhase => DescribePress(_controlTracker);

        static string DescribePress(PressTracker tracker) =>
            tracker.IsDown
                ? $"down {tracker.HeldDuration:0.00}s committed={tracker.Committed}"
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
            CombatContext context = _core.Combat.CurrentContext;
            bool downedNear = _core.Opponent != null &&
                              _core.Opponent.States.IsDowned &&
                              _core.DistanceToOpponent() <= DownedControlRange;

            // Strike button: tap = light family, hold = heavy strike. In
            // contexts with no heavy variant (ground/corner/rope/rebound) the
            // attack fires on PRESS — no tap-release latency where the
            // contextual prompts matter most.
            PressKind strikeKind = _strikeTracker.Update(
                frame.LightPressed, frame.StrikeHeld, frame.StrikeReleased,
                Time.deltaTime, PlayerInputLogic.HoldThreshold);
            bool strikeInstantContext =
                context == CombatContext.GroundUpper || context == CombatContext.GroundLower ||
                context == CombatContext.Corner || context == CombatContext.RopeStagger ||
                context == CombatContext.RopeRebound;
            bool strikeRequested = strikeInstantContext
                ? frame.LightPressed
                : strikeKind == PressKind.Tap;
            if (frame.LightPressed && strikeInstantContext)
                _strikeTracker.Reset(); // the press is consumed; no later tap/hold

            if (strikeRequested)
            {
                if (_core.DistanceToOpponent() <= WrestlerCombat.StrikeRange + 0.5f || _core.Motor.IsRunning)
                    // Context precedence: ground, corner, rope stagger,
                    // rebound, ordinary running, standing light.
                    _buffer.Buffer(PlayerAction.Light,
                        () => _core.Combat.TryGroundAttack() ||
                              _core.Combat.TryCornerStrike() ||
                              _core.Combat.TryRopeStaggerAttack() ||
                              _core.Combat.TryRopeReboundAttack() ||
                              _core.Combat.TryRunningAttack() ||
                              _core.Combat.TryLightStrike());
            }
            else if (strikeKind == PressKind.HoldCommitted && !inLock)
            {
                _buffer.Buffer(PlayerAction.Heavy, () => _core.Combat.TryHeavyStrike());
            }

            // Control button: in a lock, tap = quick / hold = power grapple
            // (held direction picks the bucket); beside a downed opponent,
            // tap = pin / hold = submission (unbuffered — they only fire when
            // valid right now). Outside both, hold has no meaning, so the
            // grapple attempt fires on PRESS.
            PressKind controlKind = _controlTracker.Update(
                frame.ControlPressed, frame.ControlHeld, frame.ControlReleased,
                Time.deltaTime, PlayerInputLogic.HoldThreshold);
            if (inLock)
            {
                MoveDirection direction = ResolveGrappleDirection(frame);
                if (controlKind == PressKind.Tap)
                    _core.Combat.TryQuickGrappleFromLock(direction);
                else if (controlKind == PressKind.HoldCommitted)
                    _core.Combat.TryPowerGrappleFromLock(direction);
            }
            else if (downedNear)
            {
                if (controlKind == PressKind.Tap)
                    _core.Combat.TryPin();
                else if (controlKind == PressKind.HoldCommitted)
                    _core.Combat.TrySubmission();
            }
            else if (frame.ControlPressed)
            {
                _controlTracker.Reset(); // consumed on press
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
            _strikeTracker.Reset();
            _controlTracker.Reset();
            if (_core != null) _core.Motor.SetMoveInput(Vector3.zero, false);
        }
    }
}
