// WrestlerAnimationController.cs  [UPDATED — v2]
// Place on: Wrestler GameObject (requires Animator component)
// Drop your WrestlerMoveSet ScriptableObject into the moveSet field in the Inspector.
//
// CHANGES FROM v1:
//   - Awake() calls moveSet.ApplyToAnimator() to apply AnimatorOverrideController
//   - OnGrappleMoveComplete() now reads WrestlerMove metadata (knockdown, orientation,
//     momentum gain, camera shake) from the MoveSet rather than using flat constants
//   - Exposes OnCameraShakeRequested event for camera controller subscription
//   - OnStrikeConnected() uses flat momentum (strikes aren't in the MoveSet)
//   - Exposes LastExecutedMove for game logic to read post-resolution state

using UnityEngine;

// ── ENUMS ─────────────────────────────────────────────────────────────────────
public enum GrappleTier    { None = 0, Quick = 1, Strong = 2 }
public enum AttackType     { None = 0, Punch = 1, Kick = 2, Dropkick = 3 }
public enum IrishWhipState { None = 0, Sending = 1, Rebounding = 2 }
public enum MatchOutcome   { Ongoing = 0, Win = 1, Loss = 2 }

[RequireComponent(typeof(Animator))]
public class WrestlerAnimationController : MonoBehaviour
{
    // ── INSPECTOR ──────────────────────────────────────────────────────────────
    [Header("Move Assignment")]
    [Tooltip("Per-wrestler ScriptableObject that maps WrestlerMove assets to the 20 grapple slots. " +
             "Drives AnimatorOverrideController on Awake.")]
    public WrestlerMoveSet moveSet;

    [Header("Momentum System")]
    [Range(0f, 1f)]
    public float momentum = 0f;

    [SerializeField] private float momentumDecayRate    = 0.008f;  // per second, passive
    [SerializeField] private float momentumPerStrike    = 0.10f;   // flat gain for any strike
    [SerializeField] private float momentumPerReversal  = 0.28f;   // flat gain for correct reversal read
    [SerializeField] private float momentumFinisherCost = 1.00f;   // meter depleted on finisher fire

    // ── EVENTS ─────────────────────────────────────────────────────────────────
    /// Subscribe in your camera controller. Fires with intensity 0–1 on heavy move impact.
    public System.Action<float> OnCameraShakeRequested;

    /// Subscribe in your game logic to read move metadata after a grapple move resolves.
    /// Signature: (attacker, move, slotIndex)
    public System.Action<WrestlerAnimationController, WrestlerMove, int> OnGrappleMoveResolved;

    // ── PRIVATE STATE ──────────────────────────────────────────────────────────
    private Animator     _anim;
    private WrestlerMove _lastExecutedMove;
    private GrappleTier  _currentGrappleTier;
    private bool         _currentGrappleFacingFront;
    private int          _currentGrappleMoveIndex;

    // Cached parameter IDs — no string lookup overhead per-frame
    private static readonly int P_MoveSpeed       = Animator.StringToHash("MoveSpeed");
    private static readonly int P_GrappleTier     = Animator.StringToHash("GrappleTier");
    private static readonly int P_MoveIndex       = Animator.StringToHash("MoveIndex");
    private static readonly int P_FacingFront     = Animator.StringToHash("FacingFront");
    private static readonly int P_AttackType      = Animator.StringToHash("AttackType");
    private static readonly int P_IsGrounded      = Animator.StringToHash("IsGrounded");
    private static readonly int P_GroundFaceUp    = Animator.StringToHash("GroundFaceUp");
    private static readonly int P_SpecialState    = Animator.StringToHash("SpecialState");
    private static readonly int P_IsSubmitting    = Animator.StringToHash("IsSubmitting");
    private static readonly int P_IsSubVictim     = Animator.StringToHash("IsSubVictim");
    private static readonly int P_MatchOutcome    = Animator.StringToHash("MatchOutcome");
    private static readonly int P_IsCageClimbing  = Animator.StringToHash("IsCageClimbing");
    private static readonly int P_IrishWhipState  = Animator.StringToHash("IrishWhipState");
    private static readonly int P_OnRopes         = Animator.StringToHash("OnRopes");
    private static readonly int P_FinisherTrigger = Animator.StringToHash("FinisherTrigger");
    private static readonly int P_ReversalSuccess = Animator.StringToHash("ReversalSuccess");
    private static readonly int P_ImpactReceived  = Animator.StringToHash("ImpactReceived");
    private static readonly int P_RopeBreak       = Animator.StringToHash("RopeBreak");
    private static readonly int P_PinfallAttempt  = Animator.StringToHash("PinfallAttempt");
    private static readonly int P_GetUpTrigger    = Animator.StringToHash("GetUpTrigger");

    // ── PROPERTIES ─────────────────────────────────────────────────────────────
    /// True when momentum is at 1.0 — finisher becomes executable.
    public bool IsSpecialState => momentum >= 1f;

    /// The last WrestlerMove that fully resolved. Use in game logic for chaining decisions.
    public WrestlerMove LastExecutedMove => _lastExecutedMove;

    // ── LIFECYCLE ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        _anim = GetComponent<Animator>();

        // Apply the per-wrestler AnimatorOverrideController before any animations play
        if (moveSet != null)
        {
            moveSet.ApplyToAnimator(_anim);
        }
        else
        {
            Debug.LogWarning($"[WrestlerAnimationController] {name}: No MoveSet assigned. " +
                             $"Grapple slot states will play placeholder clips.");
        }
    }

    private void Update()
    {
        // Passive momentum decay — wrestler cools if not actively attacking
        if (momentum > 0f && !IsSpecialState)
            momentum = Mathf.Max(0f, momentum - momentumDecayRate * Time.deltaTime);

        // Keep Animator bool in sync
        _anim.SetBool(P_SpecialState, IsSpecialState);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LOCOMOTION
    // ══════════════════════════════════════════════════════════════════════════

    /// <param name="speed">Positive = forward, negative = backward.
    /// Above 0.8 transitions to Run. 0 = Idle.</param>
    public void SetMoveSpeed(float speed) => _anim.SetFloat(P_MoveSpeed, speed);

    // ══════════════════════════════════════════════════════════════════════════
    // STRIKE SYSTEM
    // ══════════════════════════════════════════════════════════════════════════

    public void ExecuteStrike(AttackType type) => _anim.SetInteger(P_AttackType, (int)type);
    public void ClearStrike()                  => _anim.SetInteger(P_AttackType, 0);

    /// Call when the strike animation's impact frame fires.
    public void OnStrikeConnected()
    {
        AddMomentum(momentumPerStrike);
        ClearStrike();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GRAPPLE SYSTEM
    // Two-step: InitiateGrapple() opens the clinch → ExecuteGrappleMove() selects slot.
    // ══════════════════════════════════════════════════════════════════════════

    /// Step 1: Enter grapple clinch state.
    public void InitiateGrapple(GrappleTier tier, bool facingFront)
    {
        _currentGrappleTier        = tier;
        _currentGrappleFacingFront = facingFront;
        _anim.SetInteger(P_GrappleTier, (int)tier);
        _anim.SetBool(P_FacingFront,   facingFront);
    }

    /// Step 2: Select move from the active slot pool (0–4 = directional input).
    public void ExecuteGrappleMove(int slotIndex)
    {
        _currentGrappleMoveIndex = Mathf.Clamp(slotIndex, 0, 4);
        _anim.SetInteger(P_MoveIndex, _currentGrappleMoveIndex);
    }

    /// Call when the grapple move animation clip has fully played.
    /// Reads WrestlerMove metadata from the MoveSet to propagate state.
    /// <param name="opponentController">The opponent's WrestlerAnimationController,
    /// so this method can call ReceiveImpact on the correct target.</param>
    public void OnGrappleMoveComplete(WrestlerAnimationController opponentController = null)
    {
        var move = moveSet?.GetMove(_currentGrappleTier, _currentGrappleFacingFront, _currentGrappleMoveIndex);

        if (move != null)
        {
            _lastExecutedMove = move;
            AddMomentum(move.momentumGain);

            // Propagate knockdown and orientation to opponent
            if (opponentController != null && move.causesKnockdown)
            {
                bool faceUp = move.opponentLandOrientation == GroundedOrientation.FaceUp
                           || move.opponentLandOrientation == GroundedOrientation.Seated;
                opponentController.ReceiveImpact(causesKnockdown: true, faceUp: faceUp);
            }

            // Camera shake on impact
            if (move.triggerCameraShake)
                OnCameraShakeRequested?.Invoke(move.cameraShakeIntensity);

            // Fire resolved event for external game logic
            OnGrappleMoveResolved?.Invoke(this, move, _currentGrappleMoveIndex);
        }

        // Clear grapple parameters
        _anim.SetInteger(P_GrappleTier, 0);
        _anim.SetInteger(P_MoveIndex,   0);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // REVERSAL SYSTEM
    // Context-sensitive: player must correctly identify the incoming attack type.
    // Wrong read = no reversal, attack resolves. Correct read = momentum swing.
    // ══════════════════════════════════════════════════════════════════════════

    /// <param name="incomingAttackType">1 = strike class, 2 = grapple class.</param>
    /// <param name="playerDeclaredType">What the player's reversal input declared.</param>
    public void AttemptReversal(int incomingAttackType, int playerDeclaredType)
    {
        if (incomingAttackType != playerDeclaredType) return; // Wrong read — attack lands
        _anim.SetTrigger(P_ReversalSuccess);
        AddMomentum(momentumPerReversal);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IMPACT / DAMAGE
    // ══════════════════════════════════════════════════════════════════════════

    /// <param name="causesKnockdown">True → enters Grounded state.</param>
    /// <param name="faceUp">True = Grounded_FaceUp (pinnable, front-ground grapples).
    /// False = Grounded_FaceDown (rear-ground grapples, Boston Crab).</param>
    public void ReceiveImpact(bool causesKnockdown, bool faceUp = true)
    {
        _anim.SetTrigger(P_ImpactReceived);
        if (causesKnockdown)
        {
            _anim.SetBool(P_IsGrounded,   true);
            _anim.SetBool(P_GroundFaceUp, faceUp);
        }
    }

    public void GetUp()
    {
        _anim.SetBool(P_IsGrounded, false);
        _anim.SetTrigger(P_GetUpTrigger);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SUBMISSION SYSTEM
    // ══════════════════════════════════════════════════════════════════════════

    public void ApplySubmission()           => _anim.SetBool(P_IsSubmitting, true);
    public void ReleaseSubmission()         => _anim.SetBool(P_IsSubmitting, false);
    public void SetSubmissionVictim(bool v) => _anim.SetBool(P_IsSubVictim,  v);

    /// Fire when victim's body collider enters a rope trigger zone.
    /// Simultaneously breaks hold on both attacker and victim.
    public void TriggerRopeBreak()
    {
        _anim.SetTrigger(P_RopeBreak);
        ReleaseSubmission();
        SetSubmissionVictim(false);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IRISH WHIP SYSTEM
    // ══════════════════════════════════════════════════════════════════════════

    public void SendIrishWhip()           => _anim.SetInteger(P_IrishWhipState, 1);
    public void ReceiveIrishWhipRebound() => _anim.SetInteger(P_IrishWhipState, 2);
    public void HitRopes()                => _anim.SetBool(P_OnRopes, true);
    public void LeaveRopes()
    {
        _anim.SetBool(P_OnRopes, false);
        _anim.SetInteger(P_IrishWhipState, 0);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FINISHER SYSTEM (MOMENTUM-GATED)
    // ══════════════════════════════════════════════════════════════════════════

    /// Attempt finisher. Returns false and no-ops if momentum meter is not full.
    public bool TryExecuteFinisher()
    {
        if (!IsSpecialState) return false;
        _anim.SetTrigger(P_FinisherTrigger);
        momentum = Mathf.Max(0f, momentum - momentumFinisherCost);
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PINFALL SYSTEM
    // ══════════════════════════════════════════════════════════════════════════

    public void AttemptPinfall() => _anim.SetTrigger(P_PinfallAttempt);
    public void Kickout()        => _anim.SetTrigger(P_ImpactReceived);

    // ══════════════════════════════════════════════════════════════════════════
    // CAGE MATCH SYSTEM
    // ══════════════════════════════════════════════════════════════════════════

    public void StartCageClimb()     => _anim.SetBool(P_IsCageClimbing, true);
    public void CompleteCageEscape() => _anim.SetBool(P_IsCageClimbing, false);
    public void InterruptCageClimb() => _anim.SetTrigger(P_ImpactReceived);

    // ══════════════════════════════════════════════════════════════════════════
    // MATCH OUTCOME
    // ══════════════════════════════════════════════════════════════════════════

    public void SetMatchOutcome(MatchOutcome outcome)
        => _anim.SetInteger(P_MatchOutcome, (int)outcome);

    // ══════════════════════════════════════════════════════════════════════════
    // MOMENTUM UTILITY
    // ══════════════════════════════════════════════════════════════════════════

    private void AddMomentum(float amount)
        => momentum = Mathf.Min(1f, momentum + amount);

    public void ResetMomentum()
    {
        momentum = 0f;
        _anim.SetBool(P_SpecialState, false);
    }

    /// Returns current momentum as 0–1 float for UI meter rendering.
    public float GetMomentumNormalized() => momentum;
}
