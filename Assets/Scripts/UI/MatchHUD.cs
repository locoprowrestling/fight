using UnityEngine;
using UnityEngine.UI;

namespace LoCoFight
{
    /// Match HUD built entirely in code: nameplates, portraits, meters,
    /// messages, counts, submission meter, winner banner, controls hint.
    public class MatchHUD : MonoBehaviour
    {
        public static MatchHUD Instance { get; private set; }

        WrestlerCore _player, _cpu;
        Text _playerName, _cpuName, _message, _count, _stateLabel, _winner, _controls, _submissionLabel, _prompts, _alert, _panelText;
        GameObject _controlsPanel;
        string _lastPrompt = "";
        bool _promptStateValid;
        bool _pFighting, _pDownedNear, _pReversalOpen, _pSpecialReady, _pInRange, _pStrongLock, _pPlayerDowned;
        PlayerInputController _playerInput;
        CombatContext _pContext;
        PlayerInputDevice _pDevice;
        Text _actionFeedback;
        float _feedbackClearAt;
        MeterBar _pHealth, _pStamina, _pMomentum, _cHealth, _cStamina, _cMomentum, _submission;
        RosterPortraitView _pPortrait, _cPortrait;
        float _messageClearAt;
        static Font _font;
        PlayerInputDevice _inputDevice = PlayerInputDevice.Keyboard;

        // ---- Static helpers safe to call from anywhere (no-ops if HUD missing) ----
        public static void TryShowMessage(string text, float duration = 2f)
        {
            if (Instance != null) Instance.ShowMessage(text, duration);
        }
        public static void TryShowCount(string text)
        {
            if (Instance != null && Instance._count != null) Instance._count.text = text;
        }
        public static void TrySetMatchState(string text)
        {
            if (Instance != null && Instance._stateLabel != null) Instance._stateLabel.text = text;
        }
        public static void TryShowWinner(string text)
        {
            if (Instance != null && Instance._winner != null) Instance._winner.text = text;
        }
        public static void TrySetInputDevice(PlayerInputDevice device)
        {
            if (Instance != null) Instance.SetInputDevice(device);
        }
        /// Last rendered contextual prompt line, for the F1 overlay.
        public static string CurrentPromptText =>
            Instance != null ? Instance._lastPrompt : "";

        /// Small bottom-center feedback: the move that just fired, or why a
        /// press did nothing. Suppressed while the submission UI owns the
        /// space.
        public static void TryShowActionFeedback(string text, bool warning)
        {
            if (Instance == null || Instance._actionFeedback == null) return;
            var subs = SubmissionSystem.Instance;
            if (subs != null && subs.Active) return;
            Instance._actionFeedback.text = text;
            Instance._actionFeedback.color = warning
                ? new Color(1f, 0.55f, 0.25f)
                : new Color(0.6f, 1f, 0.6f);
            Instance._feedbackClearAt = Time.unscaledTime + 1.1f;
        }

        public static void TryShowHandshakePrompt(float duration)
        {
            if (Instance == null) return;
            string text = Instance._inputDevice == PlayerInputDevice.Controller
                ? "A: shake  X: cheap shot  B: refuse"
                : "T: shake  J: cheap shot  K: refuse";
            Instance.ShowMessage(text, duration);
        }

        public static MatchHUD CreateHud()
        {
            var go = new GameObject("MatchHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            var hud = go.AddComponent<MatchHUD>();
            hud.Build();
            return hud;
        }

        void Awake() => Instance = this;
        void OnDestroy()
        {
            UnsubscribeReadiness();
            if (Instance == this) Instance = null;
        }

        static Font GetFont()
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return _font;
        }

        void Build()
        {
            // Player panel (top-left).
            _pPortrait = RosterPortraitView.Create(transform, "PlayerPortrait", new Vector2(12, -12), 72, false);
            _playerName = MakeText("PlayerName", new Vector2(92, -14), 280, 26, TextAnchor.UpperLeft, 20, false);
            _pHealth = MeterBar.Create(transform, "PlayerHealth", new Vector2(92, -42), new Vector2(240, 14), new Color(0.85f, 0.2f, 0.2f));
            _pStamina = MeterBar.Create(transform, "PlayerStamina", new Vector2(92, -58), new Vector2(240, 9), new Color(0.95f, 0.85f, 0.2f));
            _pMomentum = MeterBar.Create(transform, "PlayerMomentum", new Vector2(92, -69), new Vector2(240, 9), new Color(0.3f, 0.6f, 1f));

            // CPU panel (top-right).
            _cPortrait = RosterPortraitView.Create(transform, "CpuPortrait", new Vector2(-12, -12), 72, true);
            _cpuName = MakeText("CpuName", new Vector2(-92, -14), 280, 26, TextAnchor.UpperRight, 20, true);
            _cHealth = MeterBar.Create(transform, "CpuHealth", new Vector2(-332, -42), new Vector2(240, 14), new Color(0.85f, 0.2f, 0.2f));
            _cStamina = MeterBar.Create(transform, "CpuStamina", new Vector2(-332, -58), new Vector2(240, 9), new Color(0.95f, 0.85f, 0.2f));
            _cMomentum = MeterBar.Create(transform, "CpuMomentum", new Vector2(-332, -69), new Vector2(240, 9), new Color(0.3f, 0.6f, 1f));
            // CPU bars anchor from the right.
            foreach (var bar in new[] { _cHealth, _cStamina, _cMomentum })
            {
                var rt = (RectTransform)bar.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
            }

            // Center elements.
            _stateLabel = MakeText("MatchState", new Vector2(0, -8), 400, 18, TextAnchor.UpperCenter, 13, false, centered: true);
            _message = MakeText("Message", new Vector2(0, -90), 800, 34, TextAnchor.UpperCenter, 26, false, centered: true);
            _count = MakeText("Count", new Vector2(0, -130), 300, 90, TextAnchor.UpperCenter, 72, false, centered: true);
            _winner = MakeText("Winner", new Vector2(0, -250), 1000, 44, TextAnchor.UpperCenter, 30, false, centered: true);
            _winner.color = new Color(1f, 0.85f, 0.2f);

            // Submission meter (bottom center).
            _submissionLabel = MakeText("SubmissionLabel", new Vector2(0, 86), 500, 20, TextAnchor.UpperCenter, 15, false, centered: true, bottom: true);
            _submission = MeterBar.Create(transform, "SubmissionMeter", new Vector2(-150, 60), new Vector2(300, 16), new Color(0.8f, 0.3f, 0.9f));
            var srt = (RectTransform)_submission.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0f, 0f);
            _submission.gameObject.SetActive(false);
            _submissionLabel.gameObject.SetActive(false);

            // Contextual button prompts (bottom-center) with an alert line
            // above them for reversal windows / ready specials. Both hide
            // while the submission meter occupies this part of the screen.
            _prompts = MakeText("ControlPrompts", new Vector2(0, 38), 760, 26, TextAnchor.LowerCenter, 17,
                false, centered: true, bottom: true);
            AddOutline(_prompts);
            _alert = MakeText("PromptAlert", new Vector2(0, 66), 760, 24, TextAnchor.LowerCenter, 17,
                false, centered: true, bottom: true);
            AddOutline(_alert);
            // What just happened / why nothing happened (auto-clears).
            _actionFeedback = MakeText("ActionFeedback", new Vector2(0, 92), 760, 24, TextAnchor.LowerCenter, 16,
                false, centered: true, bottom: true);
            AddOutline(_actionFeedback);

            // One-line hint (bottom-left); the full scheme lives on the
            // hold-Tab panel instead of a cramped two-line dump.
            _controls = MakeText("Controls", new Vector2(12, 12), 480, 20, TextAnchor.LowerLeft, 12, false, bottom: true);
            BuildControlsPanel();
            SetInputDevice(PlayerInputDevice.Keyboard);
        }

        static void AddOutline(Text text)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        void BuildControlsPanel()
        {
            _controlsPanel = new GameObject("ControlsPanel", typeof(RectTransform), typeof(Image));
            _controlsPanel.transform.SetParent(transform, false);
            var rt = (RectTransform)_controlsPanel.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680f, 360f);
            _controlsPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var textGo = new GameObject("PanelText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(_controlsPanel.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(26f, 16f);
            trt.offsetMax = new Vector2(-26f, -16f);
            _panelText = textGo.GetComponent<Text>();
            _panelText.font = GetFont();
            _panelText.fontSize = 16;
            _panelText.alignment = TextAnchor.MiddleLeft;
            _panelText.color = Color.white;
            _controlsPanel.SetActive(false);
        }

        static string PanelTextFor(PlayerInputDevice device)
        {
            return device == PlayerInputDevice.Controller
                ? "MOVE        left stick   (LB: run)\n" +
                  "STRIKE      X    fires on press — neutral: light, +direction: heavy\n" +
                  "TIE-UP      A    press: lock up — KEEP HELD through the lock-up for the strong set\n" +
                  "            in lock: A + direction = grapple move (instant)\n" +
                  "            near a downed opponent: tap = pin, hold = submission\n" +
                  "SPECIAL     Y    needs full momentum\n" +
                  "DODGE       B\n" +
                  "REVERSE     RB   also mash to kick out / escape\n" +
                  "PAUSE       Menu (also resets after a finish)"
                : "MOVE        W A S D   (Shift: run)\n" +
                  "STRIKE      J    fires on press — neutral: light, +direction: heavy\n" +
                  "TIE-UP      K    press: lock up — KEEP HELD through the lock-up for the strong set\n" +
                  "            in lock: K + direction = grapple move (instant)\n" +
                  "            near a downed opponent: tap = pin, hold = submission\n" +
                  "SPECIAL     L    needs full momentum\n" +
                  "DODGE       ;    (or Alt)\n" +
                  "REVERSE     Space   also mash to kick out / escape\n" +
                  "TAUNT       T    handshake: T accept / J cheap shot / K refuse\n" +
                  "MISC        F1 debug — F2 roster — F3 CPU mode — Esc pause — R reset";
        }

        Text MakeText(string name, Vector2 pos, float w, float h, TextAnchor anchor, int size,
            bool rightSide, bool centered = false, bool bottom = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(w, h);
            float ax = centered ? 0.5f : rightSide ? 1f : 0f;
            float ay = bottom ? 0f : 1f;
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(centered ? 0.5f : ax, ay);
            rt.anchoredPosition = pos;
            var text = go.GetComponent<Text>();
            text.font = GetFont();
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        public void BindWrestlers(WrestlerCore player, WrestlerCore cpu)
        {
            UnsubscribeReadiness();
            _player = player;
            _cpu = cpu;
            _playerInput = player.GetComponent<PlayerInputController>();
            _playerName.text = player.DisplayName;
            _cpuName.text = cpu.DisplayName;
            _pPortrait.Show(player.Entry != null ? player.Entry.portraitSprite : null, new Color(0.2f, 0.5f, 1f));
            _cPortrait.Show(cpu.Entry != null ? cpu.Entry.portraitSprite : null, new Color(1f, 0.3f, 0.2f));

            // Event-driven SPECIAL readiness: the bar treatment persists while
            // ready; the announcement fires once per transition, not per frame.
            _player.Stats.OnSpecialReadyChanged += HandlePlayerSpecialReady;
            _cpu.Stats.OnSpecialReadyChanged += HandleCpuSpecialReady;
            HandlePlayerSpecialReady(_player.Stats.IsSpecialReady);
            HandleCpuSpecialReady(_cpu.Stats.IsSpecialReady);
        }

        void UnsubscribeReadiness()
        {
            if (_player != null) _player.Stats.OnSpecialReadyChanged -= HandlePlayerSpecialReady;
            if (_cpu != null) _cpu.Stats.OnSpecialReadyChanged -= HandleCpuSpecialReady;
        }

        void HandlePlayerSpecialReady(bool ready)
        {
            if (_pMomentum != null) _pMomentum.SetReady(ready);
            if (ready) ShowMessage("SPECIAL READY");
        }

        void HandleCpuSpecialReady(bool ready)
        {
            if (_cMomentum != null) _cMomentum.SetReady(ready);
        }

        public void ShowMessage(string text, float duration = 2f)
        {
            _message.text = text;
            _messageClearAt = Time.unscaledTime + duration;
        }

        void SetInputDevice(PlayerInputDevice device)
        {
            _inputDevice = device;
            if (_controls == null) return;
            _controls.text = device == PlayerInputDevice.Controller
                ? "Hold View: controls   |   Menu: pause"
                : "Hold Tab: controls   |   F1: debug   |   Esc: pause";
            if (_panelText != null) _panelText.text = PanelTextFor(device);
        }

        void Update()
        {
            if (_message != null && _message.text.Length > 0 && Time.unscaledTime > _messageClearAt)
                _message.text = "";
            if (_actionFeedback != null && _actionFeedback.text.Length > 0 && Time.unscaledTime > _feedbackClearAt)
                _actionFeedback.text = "";

            if (_player != null)
            {
                _pHealth.SetValue(_player.Stats.HealthPercent);
                _pStamina.SetValue(_player.Stats.StaminaPercent);
                _pMomentum.SetValue(_player.Stats.MomentumPercent);
            }
            if (_cpu != null)
            {
                _cHealth.SetValue(_cpu.Stats.HealthPercent);
                _cStamina.SetValue(_cpu.Stats.StaminaPercent);
                _cMomentum.SetValue(_cpu.Stats.MomentumPercent);
            }

            var subs = SubmissionSystem.Instance;
            bool subActive = subs != null && subs.Active;

            // Hold Tab (View on pad) for the full controls panel. Read here
            // directly, same debug-view convention as F1/F3.
            bool showPanel = Input.GetKey(KeyCode.Tab) || Input.GetKey(KeyCode.JoystickButton6);
            if (_controlsPanel != null && _controlsPanel.activeSelf != showPanel)
                _controlsPanel.SetActive(showPanel);

            // Contextual prompts + alert line: per-frame state checks (a
            // 0.2 s poll visibly lagged short-lived contexts), strings only
            // rebuilt on change. Hidden while the submission meter owns this
            // screen space.
            if (_prompts != null && _player != null)
            {
                var mm = MatchManager.Instance;
                bool fighting = mm != null && mm.IsCombatAllowed && !subActive;
                CombatContext context = fighting ? _player.Combat.CurrentContext : CombatContext.Standing;
                bool inRange = fighting &&
                               _player.DistanceToOpponent() <= ControlPromptLogic.PromptRange;
                bool downedNear = fighting && _cpu != null && _cpu.States.IsDowned &&
                                  _player.DistanceToOpponent() <= PlayerInputController.DownedControlRange;
                bool reversalOpen = fighting && _cpu != null &&
                                    (_cpu.Combat.IsReversalWindowOpenFor(_player) ||
                                     (_cpu.Specials != null && _cpu.Specials.ReversalWindowOpen));
                bool specialReady = fighting && _player.Stats.HasFullMomentum;
                bool strongLock = fighting && _playerInput != null && _playerInput.PowerLockArmed;
                bool playerDowned = fighting && _player.States.Current == WrestlerState.Downed;

                bool changed = !_promptStateValid || fighting != _pFighting || context != _pContext ||
                               downedNear != _pDownedNear || reversalOpen != _pReversalOpen ||
                               specialReady != _pSpecialReady || inRange != _pInRange ||
                               strongLock != _pStrongLock || playerDowned != _pPlayerDowned ||
                               _inputDevice != _pDevice;
                if (changed)
                {
                    _promptStateValid = true;
                    _pFighting = fighting;
                    _pContext = context;
                    _pDownedNear = downedNear;
                    _pReversalOpen = reversalOpen;
                    _pSpecialReady = specialReady;
                    _pInRange = inRange;
                    _pStrongLock = strongLock;
                    _pPlayerDowned = playerDowned;
                    _pDevice = _inputDevice;

                    _lastPrompt = fighting
                        ? ControlPromptLogic.StrikePrompt(context, inRange, _inputDevice) + "      " +
                          ControlPromptLogic.ControlPrompt(context, downedNear, inRange, strongLock, _inputDevice)
                        : "";
                    _prompts.text = _lastPrompt;

                    if (reversalOpen)
                    {
                        _alert.text = _inputDevice == PlayerInputDevice.Controller ? "[RB] REVERSE!" : "[Space] REVERSE!";
                        _alert.color = Color.cyan;
                    }
                    else if (playerDowned)
                    {
                        _alert.text = _inputDevice == PlayerInputDevice.Controller
                            ? "Mash to get up!  (stick + RB: roll away)"
                            : "Mash to get up!  (A/D + Space: roll away)";
                        _alert.color = Color.yellow;
                    }
                    else if (specialReady)
                    {
                        _alert.text = _inputDevice == PlayerInputDevice.Controller ? "[Y] Special ready" : "[L] Special ready";
                        _alert.color = new Color(1f, 0.7f, 0.15f);
                    }
                    else
                    {
                        _alert.text = "";
                    }
                }
            }
            if (_submission.gameObject.activeSelf != subActive)
            {
                _submission.gameObject.SetActive(subActive);
                _submissionLabel.gameObject.SetActive(subActive);
            }
            if (subActive)
            {
                _submission.SetValue(subs.Pressure / SubmissionSystem.SubmitThreshold);
                _submissionLabel.text = subs.Defender != null && subs.Defender.IsPlayer
                    ? $"{subs.HoldLabel}  — mash to escape, crawl to ropes  (escape {subs.Escape:0}%)"
                    : $"{subs.HoldLabel}  (escape {subs.Escape:0}%)";
            }
        }
    }
}
