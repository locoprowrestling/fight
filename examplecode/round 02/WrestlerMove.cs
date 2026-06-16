// WrestlerMove.cs
// Place in: Assets/Scripts/Wrestling/
// ScriptableObject — one asset per move in the library.
// Create via: Right-click > Create > Wrestling > Move
//
// These are created individually for each of the 23+ moves observed in the
// Front Weak Grapple move library (and extended for all other categories).
// Assign them into WrestlerMoveSet slots per wrestler in the Inspector.

using UnityEngine;

// ── ENUMS ─────────────────────────────────────────────────────────────────────

/// Which position the attacker occupies relative to the opponent when the move begins.
public enum MovePosition
{
    FrontStanding,   // default — facing opponent standing
    RearStanding,    // behind opponent standing
    FrontGrounded,   // opponent face-up on mat
    RearGrounded,    // opponent face-down on mat
    Corner,          // opponent in turnbuckle
    Aerial,          // attacker launching from elevated position
    Running          // attacker in full sprint state
}

/// Mechanical class of the move — drives momentum gain multiplier and reversal category.
public enum MoveClass
{
    QuickGrapple,    // tap input — lighter moves, less momentum
    StrongGrapple,   // hold input — heavier moves, more momentum
    Submission,      // sustained hold with positional escape condition
    Strike,          // no grapple initiation required
    Aerial,          // requires elevated starting position
    Running          // requires IrishWhip rebound or manual sprint
}

/// Orientation of the opponent after the move resolves.
/// Used by opponent's WrestlerAnimationController.ReceiveImpact() to set GroundFaceUp.
public enum GroundedOrientation
{
    FaceUp,          // can be pinned; receives front ground grapples
    FaceDown,        // receives rear ground grapples; required for Boston Crab
    Seated,          // post-snapmare — unique vulnerable state
    StillStanding    // chops, uppercuts — opponent doesn't go down
}

// ── SCRIPTABLEOBJECT ──────────────────────────────────────────────────────────

[CreateAssetMenu(menuName = "Wrestling/Move", fileName = "Move_New")]
public class WrestlerMove : ScriptableObject
{
    // ── IDENTITY ──────────────────────────────────────────────────────────────
    [Header("Identity")]
    [Tooltip("Exact name as shown in the game's move library (e.g. 'Club to Neck').")]
    public string moveName;

    // ── ANIMATION ─────────────────────────────────────────────────────────────
    [Header("Animation")]
    [Tooltip("The animation clip for this move. Assigned here and swapped into the " +
             "Animator Controller slot via AnimatorOverrideController at runtime.")]
    public AnimationClip clip;

    [Tooltip("True if the clip drives the attacker's world position via root motion. " +
             "Moves that travel (scoop slams, suplexes) need this ON. " +
             "Moves where attacker stays planted (headbutt, uppercut) need this OFF.")]
    public bool hasRootMotion = true;

    [Tooltip("Approximate clip duration in seconds. Used for timing hitbox activation " +
             "and move cancellation windows.")]
    public float clipDuration = 1.0f;

    // ── MECHANICS ─────────────────────────────────────────────────────────────
    [Header("Mechanics")]
    public MovePosition position = MovePosition.FrontStanding;
    public MoveClass    moveClass = MoveClass.QuickGrapple;

    [Tooltip("True = requires FacingFront=true (front clinch). " +
             "False = requires FacingFront=false (rear clinch).")]
    public bool requiresFacingFront = true;

    [Tooltip("Whether the opponent transitions to a grounded state after this move.")]
    public bool causesKnockdown = true;

    [Tooltip("Orientation the opponent lands in. Drives ReceiveImpact(faceUp) on the opponent.")]
    public GroundedOrientation opponentLandOrientation = GroundedOrientation.FaceUp;

    [Tooltip("Which direction the opponent's body travels on impact. " +
             "Negative Z = backward (toward ropes), positive Z = forward (toward attacker's corner).")]
    public Vector3 opponentImpactDirection = new Vector3(0f, 0f, -1f);

    // ── MOMENTUM ──────────────────────────────────────────────────────────────
    [Header("Momentum")]
    [Range(0f, 1f)]
    [Tooltip("How much this move charges the momentum meter (0=none, 1=full meter).")]
    public float momentumGain = 0.14f;

    // ── IMPACT FEEDBACK ───────────────────────────────────────────────────────
    [Header("Impact Feedback")]
    [Tooltip("Heavy moves (powerbomb, finisher) should trigger camera shake on landing frame.")]
    public bool triggerCameraShake = false;

    [Range(0f, 1f)]
    public float cameraShakeIntensity = 0f;

    [Tooltip("Name of the Animation Event to fire at the impact frame of this clip. " +
             "Wire this to your hitbox activation system.")]
    public string impactEventName = "OnMoveImpact";

    // ── SOURCING NOTES ────────────────────────────────────────────────────────
    [Header("Sourcing")]
    [TextArea(3, 6)]
    [Tooltip("Body mechanic description for animator reference or mocap direction.")]
    public string mechanicDescription;

    [TextArea(2, 4)]
    [Tooltip("Search terms for Mixamo or similar animation library.")]
    public string mixamoSearchTerms;

    // ── UTILITY ───────────────────────────────────────────────────────────────

    /// Returns true if this move leaves the opponent in a state where a pinfall is valid.
    public bool AllowsPinfallImmediate =>
        causesKnockdown && opponentLandOrientation == GroundedOrientation.FaceUp;

    /// Returns true if this move sets up the Boston Crab / rear submission position.
    public bool AllowsRearSubmission =>
        causesKnockdown && opponentLandOrientation == GroundedOrientation.FaceDown;
}
