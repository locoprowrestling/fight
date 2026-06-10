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
        Text _playerName, _cpuName, _message, _count, _stateLabel, _winner, _controls, _submissionLabel;
        MeterBar _pHealth, _pStamina, _pMomentum, _cHealth, _cStamina, _cMomentum, _submission;
        RosterPortraitView _pPortrait, _cPortrait;
        float _messageClearAt;
        static Font _font;

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
        void OnDestroy() { if (Instance == this) Instance = null; }

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

            // Controls hint (bottom-left).
            _controls = MakeText("Controls", new Vector2(12, 12), 640, 60, TextAnchor.LowerLeft, 12, false, bottom: true);
            _controls.text = "WASD move | Shift run | J light | K heavy | L grapple | Space reversal | Alt dodge\nU special | I pin | O submission | mash Space when pinned | F1 debug | R reset";
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
            _player = player;
            _cpu = cpu;
            _playerName.text = player.DisplayName;
            _cpuName.text = cpu.DisplayName;
            _pPortrait.Show(player.Entry != null ? player.Entry.portraitSprite : null, new Color(0.2f, 0.5f, 1f));
            _cPortrait.Show(cpu.Entry != null ? cpu.Entry.portraitSprite : null, new Color(1f, 0.3f, 0.2f));
        }

        public void ShowMessage(string text, float duration = 2f)
        {
            _message.text = text;
            _messageClearAt = Time.unscaledTime + duration;
        }

        void Update()
        {
            if (_message != null && _message.text.Length > 0 && Time.unscaledTime > _messageClearAt)
                _message.text = "";

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
            if (_submission.gameObject.activeSelf != subActive)
            {
                _submission.gameObject.SetActive(subActive);
                _submissionLabel.gameObject.SetActive(subActive);
            }
            if (subActive)
            {
                _submission.SetValue(subs.Pressure / SubmissionSystem.SubmitThreshold);
                _submissionLabel.text = $"{subs.HoldLabel}  (escape {subs.Escape:0}%)";
            }
        }
    }
}
