using System.Collections.Generic;
using UnityEngine;

namespace LoCoFight
{
    [CreateAssetMenu(menuName = "LoCo Fight Game/Special Ability")]
    public class SpecialAbilityData : ScriptableObject
    {
        [Header("Identity")]
        public string specialId;
        public string displayName;
        public string owningRosterId;
        public SpecialCategory category;
        public string specialVariant;
        [TextArea] public string description;

        [Header("Resource requirements")]
        public bool requiresFullMomentum = true;
        public float momentumCost = 100f;
        public bool spendsAllMomentum = true;
        public float staminaCost = 25f;
        public float healthThresholdOptional = 0f;
        public bool oncePerMatch = false;
        public float cooldown = 0f;

        [Header("Target requirements")]
        public bool requiresOpponentStanding;
        public bool requiresOpponentDowned;
        public bool requiresOpponentStunnedOk = true;   // standing specials usually also accept stunned/groggy
        public bool requiresOpponentCornered;
        public bool requiresOpponentNearRopes;
        public bool requiresOpponentInRopeStagger;
        public bool requiresOpponentHeadPosition;        // attacker near downed opponent's head (JT)
        public bool requiresFrontPosition;
        public bool requiresBackOrSidePosition;
        public bool requiresSideBySidePosition;
        public bool requiresGrappleLockOk = true;        // may snap out of an existing grapple lock
        public bool requiresTargetLiftable;

        [Header("Arena requirements")]
        public bool requiresTopCornerAnchor;
        public bool requiresMiddleCornerAnchor;
        public bool requiresRopeMiddleAnchor;
        public bool requiresRopeTrapZone;
        public bool requiresRopeReboundLane;
        public bool requiresCornerZone;
        public bool disallowCornerAnchor;

        [Header("Movement")]
        public bool usesJumpArc;
        public bool usesCrescentArc;
        public bool usesChargeMovement;
        public bool usesRopeRun;
        public bool startsCarryPhase;
        public float carryDuration = 0f;
        public float carryMoveSpeed = 2f;
        public int spinCount = 0;
        public float spinDuration = 0f;
        public bool canMiss;
        public float selfDamageOnMiss = 0f;
        public float selfDamageOnWallCollision = 0f;
        public float chargeMaxDuration = 0f;
        public float chargeMaxDistance = 0f;
        public float damageShort = 0f;
        public float damageMedium = 0f;

        [Header("Combat effects")]
        public float damage = 25f;
        public float staminaDamage = 0f;
        public float initialDamage = 0f;
        public float damagePerSecond = 0f;
        public float opponentStaminaDrainPerSecond = 0f;
        public float selfStaminaDrainPerSecond = 0f;
        public float submissionPressurePerSecond = 0f;
        public float submissionPressureBonus = 0f;       // 0.25 = +25%
        public bool causesDownedState = true;
        public float downedDuration = 3f;
        public float stunDuration = 0f;
        public bool canPinAfter = true;
        public float pinWindow = 2.5f;
        public bool autoPinOnHit;
        public float autoPinDelay = 0.25f;
        public bool startsSubmissionOnSuccess;
        public bool canSubmitOnlyIfNoRopeBreaks;
        public bool forceReleaseAtFiveIfRopeBreaksEnabled;
        public bool startsRefereeFiveCount;
        public bool startsRopeTrapState;
        public bool appliesKickoutPenaltyOnImmediatePin;
        public float kickoutPenaltyValue = 0f;           // 0.12 = -12%
        public string debuffId;
        public float debuffDuration = 0f;
        public string buffId;
        public float buffDuration = 0f;
        public bool canBeReversedAtStart = true;
        public bool canBeEscapedDuringLift = true;
        public bool noNormalReversalAfterFullLock = true;
        public bool benefitsFromRecentReversal;
        public bool benefitsFromRecentDodge;

        [Header("Counter stance (Anuka)")]
        public float counterWindow = 0f;
        public float counterWhiffRecovery = 0.65f;

        [Header("Rope / aerial")]
        public float landingTolerance = 1f;
        public float validLandingLaneWidth = 1.3f;
        public AerialAnchorType requiredLaunchAnchorType = AerialAnchorType.TopCorner;
        public float aerialSetupDuration = 0.75f;
        public float airborneDuration = 0.7f;
        public float ropeSpringSetupDuration = 0f;
        public float climbDuration = 0f;
        public float missRecoveryDuration = 2f;

        [Header("Dirty / referee")]
        public bool usesRefereeDistraction;
        public bool usesConeHitDetection;
        public float coneRange = 1.35f;
        public float coneAngle = 55f;
        public float caughtPenaltyMomentumLoss = 50f;
        public float refDistractionSetup = 0.6f;
        public float hiddenObjectSetup = 0.45f;
        public float cloudActiveDuration = 0.5f;

        [Header("Dodge (Vigilante)")]
        public bool canEscapeMajorMoves;
        public bool canEscapeNormallyUnescapableMoves;
        public List<MoveTag> escapableMoveTags = new List<MoveTag>();
        public List<MoveTag> dodgeResistantTags = new List<MoveTag>();
        public bool repositionOnSuccess = true;
        public float repositionDistance = 1.25f;
        public float invulnerabilityDuration = 0.35f;
        public bool hasOncePerMatchEmergencyVersion;
        public float manualTimingWindow = 0.18f;
        public float emergencyTimingWindow = 0.25f;
        public float failedDodgeStaminaCost = 6f;

        [Header("Sequence / combo")]
        public List<SequenceStep> sequenceSteps = new List<SequenceStep>();
        public List<ComboStep> comboSteps = new List<ComboStep>();

        [Header("Hold timing (rope trap)")]
        public float standardMaxHold = 5f;
        public float setupDuration = 0.55f;
        public float lockDuration = 0.35f;

        [Header("Animation / audio / VFX")]
        public string animationStateName;
        public string placeholderPoseName = "special";
        public string moveStartEventName;
        public string hitEventName;
        public string impactVfxEventName;
        public string crowdReactionEventName;
    }
}
