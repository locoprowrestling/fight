// WrestlerAnimationController.cs
// Place on: your Wrestler GameObject (requires an Animator component)
// This script is the single source of truth for animation state.
// Game logic calls its public methods; this class owns all Animator.Set* calls.

using UnityEngine;

// ── ENUMS matching Animator parameter values ───────────────────────────────────
public enum GrappleTier    { None = 0, Quick = 1, Strong = 2 }
public enum AttackType     { None = 0, Punch = 1, Kick = 2, Dropkick = 3 }
public enum IrishWhipState { None = 0, Sending = 1, Rebounding = 2 }
public enum MatchOutcome   { Ongoing = 0, Win = 1, Loss = 2 }

[RequireComponent(typeof(Animator))]
public class WrestlerAnimationController : MonoBehaviour
{
    // ── INSPECTOR ──────────────────────────────────────────────────────────────
    [Header("Momentum System")]
    [Range(0f, 1f)]
    public float momentum = 0f;

    [SerializeField] private float momentumDecayRate     = 0.008f; // per second, passive bleed
    [SerializeField] private float momentumPerStrike     = 0.10f;
    [SerializeField] private float momentumPerGrappleQ   = 0.14f;  // quick grapple move landed
    [SerializeField] private float momentumPerGrappleS   = 0.20f;  // strong grapple move landed
    [SerializeField] private float momentumPerReversal   = 0.28f;  // successful read reversal
    [SerializeField] private float momentumFinisherCost  = 1.00f;  // depletes entire meter

    // ── PRIVATE ────────────────────────────────────────────────────────────────
    private Animator _anim;

    // Hashed parameter IDs — avoids string lookup overhead every frame
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
    /// True when momentum meter is full — unlocks finisher execution.
    public bool IsSpecialState => momentum >= 1f;

    // ── LIFECYCLE ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    private void Update()
    {
        // Passive momentum decay (wrestler cools off when not attacking)
        if (momentum > 0f && !IsSpecialState)
            momentum = Mathf.Max(0f, momentum - momentumDecayRate * Time.deltaTime);

        // Keep SpecialState bool in sync with the meter threshold
        _anim.SetBool(P_SpecialState, IsSpecialState);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LOCOMOTION
    // ══════════════════════════════════════════════════════════════════════════

    /// <param name="speed">
    /// Positive = forward, negative = backward, 0 = idle.
    /// Values above 0.8 transition to run state.
    /// </param>
    public void SetMoveSpeed(float speed)
    {
        _anim.SetFloat(P_MoveSpeed, speed);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STRIKE SYSTEM
    // ══════════════════════════════════════════════════════════════════════════

    /// Trigger an outgoing strike animation.
    /// Call ClearStrike() or OnStrikeConnected() after resolution.
    public void ExecuteStrike(AttackType type)
    {
        _anim.SetInteger(P_AttackType, (int)type);
    }

    /// Called by game logic when the strike animation has resolved
    /// (hit, blocked, or whiffed). Clears AttackType and awards momentum.
    public void OnStrikeConnected()
    {
        AddMomentum(momentumPerStrike);
        _anim.SetInteger(P_AttackType, 0);
    }

    public void ClearStrike()
    {
        _anim.SetInteger(P_AttackType, 0);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GRAPPLE SYSTEM
    // The grapple system is a two-step process:
    //   1. InitiateGrapple()    — opens the clinch window, plays approach/lock-up anim
    //   2. ExecuteGrappleMove() — selects the specific move from the 5-slot pool
    // ══════════════════════════════════════════════════════════════════════════

    /// Step 1: Initiate clinch.
    /// <param name="tier">Quick or Strong. Drives which 5-slot move pool is active.</param>
    /// <param name="facingFront">True = attacked from front, false = from rear.</param>
    public void InitiateGrapple(GrappleTier tier, bool facingFront)
    {
        _anim.SetInteger(P_GrappleTier, (int)tier);
        _anim.SetBool(P_FacingFront, facingFront);
    }

    /// Step 2: Select specific move by directional index (0–4).
    /// Corresponds to the 5 directional slots per tier/position.
    public void ExecuteGrappleMove(int moveIndex)
    {
        _anim.SetInteger(P_MoveIndex, Mathf.Clamp(moveIndex, 0, 4));
    }

    /// Call after the grapple move animation has resolved.
    public void OnGrappleMoveComplete(GrappleTier tier)
    {
        float gain = tier == GrappleTier.Strong ? momentumPerGrappleS : momentumPerGrappleQ;
        AddMomentum(gain);
        _anim.SetInteger(P_GrappleTier, 0);
        _anim.SetInteger(P_MoveIndex, 0);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // REVERSAL SYSTEM
    // Context-sensitive: player MUST correctly identify the incoming attack type.
    // An incorrect read fires nothing — the attack resolves normally.
    // A correct read fires ReversalSuccess trigger and awards significant momentum.
    // ══════════════════════════════════════════════════════════════════════════

    /// <param name="incomingType">
    /// The actual attack type the opponent is performing.
    /// 1 = strike class, 2 = grapple class.
    /// </param>
    /// <param name="playerDeclaredType">
    /// The type the player's input declared they are reversing.
    /// Must match incomingType exactly for the reversal to succeed.
    /// </param>
    public void AttemptReversal(int incomingType, int playerDeclaredType)
    {
        if (incomingType != playerDeclaredType) return; // Wrong read — no reversal
        _anim.SetTrigger(P_ReversalSuccess);
        AddMomentum(momentumPerReversal);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IMPACT / DAMAGE RECEIVED
    // ══════════════════════════════════════════════════════════════════════════

    /// Called when this wrestler is hit.
    /// <param name="causesKnockdown">True → wrestler enters grounded state.</param>
    /// <param name="faceUp">Grounded orientation. Boston Crab requires face-down.</param>
    public void ReceiveImpact(bool causesKnockdown, bool faceUp = true)
    {
        _anim.SetTrigger(P_ImpactReceived);
        if (causesKnockdown)
        {
            _anim.SetBool(P_IsGrounded,   true);
            _anim.SetBool(P_GroundFaceUp, faceUp);
        }
    }

    /// Player or AI initiates the get-up from grounded state.
    public void GetUp()
    {
        _anim.SetBool(P_IsGrounded, false);
        _anim.SetTrigger(P_GetUpTrigger);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SUBMISSION SYSTEM
    // Applying side: call ApplySubmission() → hold persists until
    //   (a) ReleaseSubmission() called by game logic, or
    //   (b) TriggerRopeBreak() fires from ring boundary collision.
    // Victim side: set via SetSubmissionVictim(true) — victim struggles.
    //   Escape condition: victim's collider touches rope trigger → TriggerRopeBreak().
    // ══════════════════════════════════════════════════════════════════════════

    public void ApplySubmission()
    {
        _anim.SetBool(P_IsSubmitting, true);
    }

    public void ReleaseSubmission()
    {
        _anim.SetBool(P_IsSubmitting, false);
    }

    /// Sync with opponent's submission state.
    public void SetSubmissionVictim(bool isVictim)
    {
        _anim.SetBool(P_IsSubVictim, isVictim);
    }

    /// Fired when the victim's body enters a rope-break trigger zone.
    /// Breaks hold on both wrestlers simultaneously.
    public void TriggerRopeBreak()
    {
        _anim.SetTrigger(P_RopeBreak);
        ReleaseSubmission();
        SetSubmissionVictim(false);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IRISH WHIP SYSTEM
    // Whip sender and rebound receiver are separate wrestleers:
    //   Sender calls SendIrishWhip().
    //   Receiver calls ReceiveIrishWhipRebound() then HitRopes() on rope contact.
    // ══════════════════════════════════════════════════════════════════════════

    /// The wrestler initiating the whip (grabbing and throwing opponent into ropes).
    public void SendIrishWhip()
    {
        _anim.SetInteger(P_IrishWhipState, (int)IrishWhipState.Sending);
    }

    /// The wrestler being whipped — enters run toward the ropes.
    public void ReceiveIrishWhipRebound()
    {
        _anim.SetInteger(P_IrishWhipState, (int)IrishWhipState.Rebounding);
    }

    /// Call when the rebounding wrestler's body collider contacts a rope trigger.
    public void HitRopes()
    {
        _anim.SetBool(P_OnRopes, true);
    }

    /// Call when the wrestler pushes off the ropes and re-enters the ring.
    public void LeaveRopes()
    {
        _anim.SetBool(P_OnRopes, false);
        _anim.SetInteger(P_IrishWhipState, 0);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FINISHER SYSTEM (MOMENTUM-GATED)
    // The finisher is only executable when IsSpecialState == true (meter full).
    // Attempting to fire it when the meter is not full is silently rejected.
    // Execution depletes the full meter (momentumFinisherCost = 1.0).
    // ══════════════════════════════════════════════════════════════════════════

    /// Attempt to execute the finisher. No-ops if meter is not full.
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

    /// Initiating wrestler drops into cover position.
    public void AttemptPinfall()
    {
        _anim.SetTrigger(P_PinfallAttempt);
    }

    /// Defending wrestler kicks out — interrupts pinfall cover animation.
    public void Kickout()
    {
        _anim.SetTrigger(P_ImpactReceived);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CAGE MATCH SYSTEM
    // Cage wall is an interactable vertical surface.
    // StartCageClimb() begins the ascent anim.
    // CompleteCageEscape() fires when the wrestler clears the top.
    // InterruptCageClimb() fires when opponent lands a hit during climb.
    // ══════════════════════════════════════════════════════════════════════════

    public void StartCageClimb()
    {
        _anim.SetBool(P_IsCageClimbing, true);
    }

    /// Called when the wrestler successfully clears the cage top.
    public void CompleteCageEscape()
    {
        _anim.SetBool(P_IsCageClimbing, false);
    }

    /// Called when the climbing wrestler receives a hit — pulls them off the wall.
    public void InterruptCageClimb()
    {
        _anim.SetTrigger(P_ImpactReceived); // reuses impact trigger; Cage_ClimbInterrupted handles it
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MATCH OUTCOME
    // ══════════════════════════════════════════════════════════════════════════

    public void SetMatchOutcome(MatchOutcome outcome)
    {
        _anim.SetInteger(P_MatchOutcome, (int)outcome);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MOMENTUM UTILITY
    // ══════════════════════════════════════════════════════════════════════════

    private void AddMomentum(float amount)
    {
        momentum = Mathf.Min(1f, momentum + amount);
    }

    public void ResetMomentum()
    {
        momentum = 0f;
        _anim.SetBool(P_SpecialState, false);
    }

    /// Expose current momentum as a 0–1 float for UI meter display.
    public float GetMomentumNormalized() => momentum;
}
