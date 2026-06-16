using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/Move Data")]
    public class MoveData : ScriptableObject
    {
        [Header("Identity")]
        public string moveId;
        public string displayName;
        public MoveCategory category;
        public List<MoveTag> tags = new List<MoveTag>();

        [Header("Timing (seconds)")]
        public float startupTime = 0.2f;
        public float activeTime = 0.1f;
        public float recoveryTime = 0.3f;
        public float reversalWindowStart = 0.05f;
        public float reversalWindowEnd = 0.25f;

        public float TotalDuration => startupTime + activeTime + recoveryTime;

        [Header("Costs / Effects")]
        public float staminaCost = 5f;
        public float damage = 5f;
        public float staminaDamage = 0f;
        public float momentumGainOnHit = 5f;
        // Legacy basic-reversal momentum; read only as a fallback when
        // basicReversalMomentum <= 0 (kept for serialized compatibility).
        public float momentumGainOnReversal = 8f;
        public float stunDuration = 0.3f;
        public float downedDuration = 0f;
        public bool causesDownedState = false;
        public bool canPinAfter = false;
        public bool canSubmitAfter = false;
        public float knockbackDistance = 0.4f;

        [Header("Range / Position")]
        public float range = 1.35f;
        public float requiredFacingDot = 0.4f;
        public bool snapToGrapplePosition = false;
        public bool requiresLift = false;
        public LiftStrengthClass minimumLiftClass = LiftStrengthClass.Low;
        public bool failIfTargetTooHeavy = true;
        public MoveData fallbackMoveIfLiftFails;

        [Header("Pacing")]
        public MoveTier tier = MoveTier.Light;
        [Tooltip("Stamina the attacker must HAVE to attempt the move (only staminaCost is spent).")]
        public float minimumStamina = 0f;

        [Header("Context")]
        public bool requiresTargetDowned = false;
        public GroundTargetZone requiredGroundZone = GroundTargetZone.None;
        public bool requiresTargetCornered = false;
        public bool requiresTargetRopeStaggered = false;

        [Header("Conditional damage")]
        [Tooltip("Big Boot style: also knocks down when target health is below this percent (0 = never).")]
        public float downsBelowHealthPercent = 0f;
        [Tooltip("Submission moves: pressure applied per second while held.")]
        public float submissionPressurePerSecond = 0f;
        [Tooltip("Submission moves: damage per second while held.")]
        public float damagePerSecond = 0f;

        [Header("Rope / Corner")]
        public bool requiresOpponentNearRopes = false;
        public bool requiresOpponentInRopeStagger = false;
        public bool requiresCornerZone = false;
        public bool requiresRopeRebound = false;
        public bool requiresRunning = false;
        public bool causesRopeStagger = false;
        public bool canTriggerRopeBreak = true;
        public bool ignoresRopeBreak = false;

        [Header("Reversal read")]
        [Tooltip("Camera-relative direction that upgrades a reversal of this move to a strong counter.")]
        public ReversalReadDirection preferredCounterDirection =
            ReversalReadDirection.Neutral;
        public bool allowsStrongDirectionalCounter;
        public float basicReversalMomentum = 8f;
        public float strongReversalMomentum = 14f;
        public float basicReversalStagger = 0.8f;
        public float strongReversalStagger = 1.2f;
        public float basicReversalSeparation = 0.7f;
        public float strongReversalSeparation = 1.25f;
        public string basicReversalPresentationId = "reversal-basic";
        public string strongReversalPresentationId = "reversal-strong";

        [Header("Audio / VFX hooks (event names, unused by placeholder)")]
        public string moveStartEventName;
        public string hitEventName;
        public string impactVfxEventName;
        public string crowdReactionEventName;

        [Header("Animation")]
        public MoveChoreographyData choreography;
        public string animationStateName;
        public string placeholderPoseName = "strike";
        public float animationSpeed = 1f;

        public bool HasTag(MoveTag tag) => tags.Contains(tag);
    }
}
