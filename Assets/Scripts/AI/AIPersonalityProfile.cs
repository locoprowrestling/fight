using UnityEngine;

namespace LoCoFight
{
    /// Immutable, bounded decision-preference multipliers for one AI
    /// personality. Difficulty (AIDifficultyData) owns accuracy, reaction
    /// time, and escape bonuses; profiles only reshape which valid action the
    /// CPU prefers, never how well it executes.
    public readonly struct AIPersonalityProfile
    {
        public const float MinMultiplier = 0.75f;
        public const float MaxMultiplier = 1.25f;
        public const float MinRepetitionTolerance = 0.5f;
        public const float MaxRepetitionTolerance = 1.5f;

        public readonly float Aggression;
        public readonly float Strike;
        public readonly float Grapple;
        public readonly float PowerMove;
        public readonly float GroundOffense;
        public readonly float Submission;
        public readonly float PinUrgency;
        public readonly float SpecialSetup;
        public readonly float RopeCornerStrategy;
        public readonly float RiskTolerance;
        public readonly float BreatherFrequency;
        public readonly float RepetitionTolerance;

        public AIPersonalityProfile(
            float aggression,
            float strike,
            float grapple,
            float powerMove,
            float groundOffense,
            float submission,
            float pinUrgency,
            float specialSetup,
            float ropeCornerStrategy,
            float riskTolerance,
            float breatherFrequency,
            float repetitionTolerance)
        {
            Aggression = Bound(aggression);
            Strike = Bound(strike);
            Grapple = Bound(grapple);
            PowerMove = Bound(powerMove);
            GroundOffense = Bound(groundOffense);
            Submission = Bound(submission);
            PinUrgency = Bound(pinUrgency);
            SpecialSetup = Bound(specialSetup);
            RopeCornerStrategy = Bound(ropeCornerStrategy);
            RiskTolerance = Bound(riskTolerance);
            BreatherFrequency = Bound(breatherFrequency);
            RepetitionTolerance = Mathf.Clamp(
                repetitionTolerance, MinRepetitionTolerance, MaxRepetitionTolerance);
        }

        static float Bound(float value) =>
            Mathf.Clamp(value, MinMultiplier, MaxMultiplier);
    }

    /// Fixed in-code profiles for the existing personality enum. Unknown or
    /// future values fall back to Balanced (all 1).
    public static class AIPersonalityProfiles
    {
        public static readonly AIPersonalityProfile Balanced =
            new AIPersonalityProfile(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f);

        public static AIPersonalityProfile For(AIPersonality personality)
        {
            switch (personality)
            {
                case AIPersonality.Technician:
                    // Mat wrestler: chains holds and ground work, varies
                    // offense, takes few wild risks.
                    return new AIPersonalityProfile(
                        aggression: 1.0f, strike: 0.85f, grapple: 1.2f,
                        powerMove: 0.95f, groundOffense: 1.1f, submission: 1.25f,
                        pinUrgency: 1.05f, specialSetup: 1.05f,
                        ropeCornerStrategy: 0.95f, riskTolerance: 0.9f,
                        breatherFrequency: 1.0f, repetitionTolerance: 0.8f);

                case AIPersonality.HighFlyer:
                    // Lives off the ropes and corners and accepts the risk.
                    return new AIPersonalityProfile(
                        aggression: 1.1f, strike: 1.0f, grapple: 0.9f,
                        powerMove: 0.8f, groundOffense: 0.9f, submission: 0.8f,
                        pinUrgency: 1.1f, specialSetup: 1.1f,
                        ropeCornerStrategy: 1.25f, riskTolerance: 1.25f,
                        breatherFrequency: 0.85f, repetitionTolerance: 1.0f);

                case AIPersonality.Powerhouse:
                    // Plants feet, throws people, repeats what works.
                    return new AIPersonalityProfile(
                        aggression: 1.05f, strike: 0.95f, grapple: 1.1f,
                        powerMove: 1.25f, groundOffense: 1.05f, submission: 0.9f,
                        pinUrgency: 1.0f, specialSetup: 0.95f,
                        ropeCornerStrategy: 0.85f, riskTolerance: 0.8f,
                        breatherFrequency: 1.1f, repetitionTolerance: 1.2f);

                case AIPersonality.Brawler:
                    // Swings first, rests little, happily spams punches.
                    return new AIPersonalityProfile(
                        aggression: 1.2f, strike: 1.25f, grapple: 0.85f,
                        powerMove: 1.05f, groundOffense: 1.1f, submission: 0.75f,
                        pinUrgency: 0.95f, specialSetup: 0.9f,
                        ropeCornerStrategy: 1.0f, riskTolerance: 1.1f,
                        breatherFrequency: 0.8f, repetitionTolerance: 1.3f);

                case AIPersonality.Trickster:
                    // Opportunist: angles, quick pins, never the same trick
                    // twice in a row.
                    return new AIPersonalityProfile(
                        aggression: 0.95f, strike: 0.95f, grapple: 1.0f,
                        powerMove: 0.85f, groundOffense: 1.0f, submission: 0.95f,
                        pinUrgency: 1.15f, specialSetup: 1.1f,
                        ropeCornerStrategy: 1.2f, riskTolerance: 1.15f,
                        breatherFrequency: 0.95f, repetitionTolerance: 0.7f);

                case AIPersonality.Evasive:
                    // Avoids engagement, picks safe moments, breathes often.
                    return new AIPersonalityProfile(
                        aggression: 0.75f, strike: 0.9f, grapple: 0.9f,
                        powerMove: 0.8f, groundOffense: 0.85f, submission: 0.95f,
                        pinUrgency: 1.1f, specialSetup: 1.0f,
                        ropeCornerStrategy: 1.1f, riskTolerance: 0.75f,
                        breatherFrequency: 1.25f, repetitionTolerance: 0.9f);

                case AIPersonality.Showman:
                    // Performs: big setups, dramatic pauses, hates repeating
                    // a bit the crowd already saw.
                    return new AIPersonalityProfile(
                        aggression: 1.0f, strike: 1.0f, grapple: 1.0f,
                        powerMove: 1.05f, groundOffense: 0.95f, submission: 0.85f,
                        pinUrgency: 0.9f, specialSetup: 1.25f,
                        ropeCornerStrategy: 1.1f, riskTolerance: 1.15f,
                        breatherFrequency: 1.2f, repetitionTolerance: 0.75f);

                case AIPersonality.Balanced:
                default:
                    return Balanced;
            }
        }
    }
}
