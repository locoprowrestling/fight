namespace LoCoFight
{
    /// Semantic, resolved combat outcomes that presentation systems consume.
    /// Gameplay systems emit these after outcomes are final; FeelSystem,
    /// IAnimationDriver, and the HUD only ever react to them. MediumImpact is
    /// the MoveTier.Medium slot inside the light/heavy hierarchy.
    public enum CombatPresentationEvent
    {
        LightImpact,
        MediumImpact,
        HeavyImpact,
        BasicReversal,
        StrongReversal,
        SpecialImpact,
        SubmissionEscape,
        RopeBreak,
        TapOut,
        SpecialReady
    }

    /// Feel values for one event. Pure presentation: no gameplay quantities
    /// go in and none come out.
    public readonly struct PresentationFeelSettings
    {
        public readonly float HitStopSeconds;
        public readonly float CameraStrength;

        public PresentationFeelSettings(float hitStopSeconds, float cameraStrength)
        {
            HitStopSeconds = hitStopSeconds;
            CameraStrength = cameraStrength;
        }
    }

    /// The authored presentation hierarchy: light impacts stay restrained,
    /// heavy and special impacts carry weight, reversals escalate from quick
    /// escape to highlight counter, and releases read as relief rather than
    /// slams.
    public static class CombatPresentationRules
    {
        public static PresentationFeelSettings For(CombatPresentationEvent evt)
        {
            switch (evt)
            {
                case CombatPresentationEvent.LightImpact:
                    return new PresentationFeelSettings(0.03f, 0f);
                case CombatPresentationEvent.MediumImpact:
                    return new PresentationFeelSettings(0.05f, 0f);
                case CombatPresentationEvent.HeavyImpact:
                    return new PresentationFeelSettings(0.08f, 0.2f);
                case CombatPresentationEvent.BasicReversal:
                    return new PresentationFeelSettings(0.02f, 0.1f);
                case CombatPresentationEvent.StrongReversal:
                    return new PresentationFeelSettings(0.06f, 0.3f);
                case CombatPresentationEvent.SpecialImpact:
                    return new PresentationFeelSettings(0.1f, 0.35f);
                case CombatPresentationEvent.SubmissionEscape:
                    return new PresentationFeelSettings(0.02f, 0.12f);
                case CombatPresentationEvent.RopeBreak:
                    return new PresentationFeelSettings(0.015f, 0.1f);
                case CombatPresentationEvent.TapOut:
                    return new PresentationFeelSettings(0f, 0.25f);
                case CombatPresentationEvent.SpecialReady:
                default:
                    return new PresentationFeelSettings(0f, 0f);
            }
        }
    }
}
