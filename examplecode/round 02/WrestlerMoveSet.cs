// WrestlerMoveSet.cs
// Place in: Assets/Scripts/Wrestling/
// ScriptableObject — one asset per wrestler.
// Create via: Right-click > Create > Wrestling > MoveSet
//
// Assigns WrestlerMove assets to the 20 directional grapple slots
// (5 slots x QuickFront / QuickRear / StrongFront / StrongRear),
// plus finisher and submission.
//
// At runtime, ApplyToAnimator() creates an AnimatorOverrideController
// that swaps the placeholder clips in the base controller for the
// actual move clips assigned here — no Animator window changes needed.
//
// SLOT NAMING CONTRACT:
//   Placeholder clip names in WrestlerAnimatorBuilder MUST be:
//     SLOT_QuickFront_0 ... SLOT_QuickFront_4
//     SLOT_QuickRear_0  ... SLOT_QuickRear_4
//     SLOT_StrongFront_0 ... SLOT_StrongFront_4
//     SLOT_StrongRear_0  ... SLOT_StrongRear_4
//     SLOT_Finisher
//     SLOT_Submission

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wrestling/MoveSet", fileName = "MoveSet_NewWrestler")]
public class WrestlerMoveSet : ScriptableObject
{
    // ── GRAPPLE SLOT ASSIGNMENTS ─────────────────────────────────────────────

    [Header("Quick Grapple — Front  │  tap input, facing opponent  │  5 directional slots")]
    [Tooltip("Slot 0 = neutral (no direction held). Slots 1–4 = Up / Down / Left / Right.")]
    public WrestlerMove[] quickFront = new WrestlerMove[5];

    [Header("Quick Grapple — Rear  │  tap input, behind opponent  │  5 directional slots")]
    public WrestlerMove[] quickRear = new WrestlerMove[5];

    [Header("Strong Grapple — Front  │  hold input, facing opponent  │  5 directional slots")]
    public WrestlerMove[] strongFront = new WrestlerMove[5];

    [Header("Strong Grapple — Rear  │  hold input, behind opponent  │  5 directional slots")]
    public WrestlerMove[] strongRear = new WrestlerMove[5];

    [Header("Finisher  │  requires SPECIAL state (momentum meter full)")]
    public WrestlerMove finisher;

    [Header("Submission Hold  │  applied from grounded opponent positions")]
    public WrestlerMove submissionHold;

    // ── PUBLIC API ────────────────────────────────────────────────────────────

    /// Get the WrestlerMove assigned to a specific grapple tier, facing, and directional slot.
    /// Returns null if that slot is unassigned.
    public WrestlerMove GetMove(GrappleTier tier, bool facingFront, int slotIndex)
    {
        slotIndex = Mathf.Clamp(slotIndex, 0, 4);
        WrestlerMove[] pool = tier == GrappleTier.Quick
            ? (facingFront ? quickFront : quickRear)
            : (facingFront ? strongFront : strongRear);

        if (pool == null || slotIndex >= pool.Length) return null;
        return pool[slotIndex];
    }

    /// Apply this MoveSet to an Animator via AnimatorOverrideController.
    /// Call once in WrestlerAnimationController.Awake() after base controller is assigned.
    /// Swaps placeholder clips (created by WrestlerAnimatorBuilder) for actual move clips.
    public void ApplyToAnimator(Animator animator)
    {
        if (animator == null)
        {
            Debug.LogError("[WrestlerMoveSet] ApplyToAnimator called with null Animator.");
            return;
        }
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"[WrestlerMoveSet] {animator.name} has no RuntimeAnimatorController assigned.");
            return;
        }

        var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);

        // Pull the full list of (originalClip → overrideClip) pairs from the base controller
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        // Build lookup: placeholder clip name → replacement clip from this MoveSet
        var clipMap = BuildClipReplacementMap();

        // Match by placeholder clip name and inject the actual move clip
        for (int i = 0; i < overrides.Count; i++)
        {
            var originalClip = overrides[i].Key;
            if (originalClip != null && clipMap.TryGetValue(originalClip.name, out var replacement))
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, replacement);
        }

        overrideController.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = overrideController;

        Debug.Log($"[WrestlerMoveSet] '{name}' applied to {animator.name}. " +
                  $"{clipMap.Count} clip(s) overridden.");
    }

    /// Validate that all 20 grapple slots are filled. Logs warnings for empty slots.
    public bool Validate(bool logWarnings = true)
    {
        bool valid = true;
        string[] prefixes = { "quickFront", "quickRear", "strongFront", "strongRear" };
        WrestlerMove[][] sets = { quickFront, quickRear, strongFront, strongRear };

        for (int p = 0; p < 4; p++)
        {
            for (int s = 0; s < 5; s++)
            {
                if (sets[p][s] == null)
                {
                    if (logWarnings)
                        Debug.LogWarning($"[WrestlerMoveSet] '{name}': {prefixes[p]}[{s}] is unassigned.");
                    valid = false;
                }
                else if (sets[p][s].clip == null)
                {
                    if (logWarnings)
                        Debug.LogWarning($"[WrestlerMoveSet] '{name}': {prefixes[p]}[{s}] " +
                                         $"({sets[p][s].moveName}) has no AnimationClip.");
                    valid = false;
                }
            }
        }

        if (finisher == null && logWarnings)
            Debug.LogWarning($"[WrestlerMoveSet] '{name}': finisher slot is unassigned.");

        return valid;
    }

    // ── PRIVATE ───────────────────────────────────────────────────────────────

    /// Builds the clip replacement map: placeholderClipName → actual AnimationClip.
    /// Key format MUST match the placeholder clip names generated by WrestlerAnimatorBuilder.
    private Dictionary<string, AnimationClip> BuildClipReplacementMap()
    {
        var map = new Dictionary<string, AnimationClip>();

        string[] prefixes = { "QuickFront", "QuickRear", "StrongFront", "StrongRear" };
        WrestlerMove[][] sets = { quickFront, quickRear, strongFront, strongRear };

        for (int p = 0; p < 4; p++)
        {
            for (int s = 0; s < 5; s++)
            {
                var move = sets[p]?[s];
                if (move?.clip != null)
                    map[$"SLOT_{prefixes[p]}_{s}"] = move.clip;
            }
        }

        if (finisher?.clip != null)    map["SLOT_Finisher"]   = finisher.clip;
        if (submissionHold?.clip != null) map["SLOT_Submission"] = submissionHold.clip;

        return map;
    }
}
