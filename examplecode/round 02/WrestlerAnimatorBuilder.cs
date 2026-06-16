// WrestlerAnimatorBuilder.cs  [UPDATED — v2]
// Place in: Assets/Editor/
// Run via: Wrestling > Build Wrestler Animator Controller
// Generates:
//   Assets/Animations/WrestlerAnimatorController.controller
//   Assets/Animations/Placeholders/SLOT_*.anim  (22 placeholder clips)
//
// ARCHITECTURE CHANGE FROM v1:
//   Grapple move states are now slot-based (QuickFront_Slot_0 through StrongRear_Slot_4)
//   rather than move-name-based. Each slot state is assigned a unique placeholder
//   AnimationClip. At runtime, WrestlerMoveSet.ApplyToAnimator() creates an
//   AnimatorOverrideController that swaps those placeholders for actual move clips,
//   enabling a full move library (23+ moves per category) where any 5 can be
//   assigned to a wrestler's slots without touching the Animator Controller.

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class WrestlerAnimatorBuilder
{
    private const string ControllerPath  = "Assets/Animations/WrestlerAnimatorController.controller";
    private const string PlaceholderDir  = "Assets/Animations/Placeholders";

    [MenuItem("Wrestling/Build Wrestler Animator Controller")]
    public static void Build()
    {
        // ── DIRECTORY SETUP ────────────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(PlaceholderDir))
            AssetDatabase.CreateFolder("Assets/Animations", "Placeholders");

        // Delete any existing controller so we get a clean build
        AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // ── PARAMETERS ─────────────────────────────────────────────────────────
        // Float
        controller.AddParameter("MoveSpeed",        AnimatorControllerParameterType.Float);

        // Int
        controller.AddParameter("GrappleTier",      AnimatorControllerParameterType.Int);   // 0=none 1=quick 2=strong
        controller.AddParameter("MoveIndex",         AnimatorControllerParameterType.Int);   // 0–4 directional slot
        controller.AddParameter("AttackType",        AnimatorControllerParameterType.Int);   // 0=none 1=punch 2=kick 3=dropkick
        controller.AddParameter("IrishWhipState",    AnimatorControllerParameterType.Int);   // 0=none 1=sending 2=rebounding
        controller.AddParameter("MatchOutcome",      AnimatorControllerParameterType.Int);   // 0=ongoing 1=win 2=loss

        // Bool
        controller.AddParameter("FacingFront",       AnimatorControllerParameterType.Bool);  // true=front clinch
        controller.AddParameter("IsGrounded",        AnimatorControllerParameterType.Bool);
        controller.AddParameter("GroundFaceUp",      AnimatorControllerParameterType.Bool);  // true=face-up false=face-down
        controller.AddParameter("SpecialState",      AnimatorControllerParameterType.Bool);  // momentum meter full
        controller.AddParameter("IsSubmitting",      AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsSubVictim",       AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsCageClimbing",    AnimatorControllerParameterType.Bool);
        controller.AddParameter("OnRopes",           AnimatorControllerParameterType.Bool);

        // Trigger
        controller.AddParameter("FinisherTrigger",   AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ReversalSuccess",   AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ImpactReceived",    AnimatorControllerParameterType.Trigger);
        controller.AddParameter("RopeBreak",         AnimatorControllerParameterType.Trigger);
        controller.AddParameter("PinfallAttempt",    AnimatorControllerParameterType.Trigger);
        controller.AddParameter("GetUpTrigger",      AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        // ── LOCOMOTION STATES ──────────────────────────────────────────────────
        var idle    = sm.AddState("Idle");    sm.defaultState = idle;
        var walkFwd = sm.AddState("WalkForward");
        var walkBwd = sm.AddState("WalkBackward");
        var run     = sm.AddState("Run");

        // ── STRIKE STATES ──────────────────────────────────────────────────────
        var sPunch    = sm.AddState("Strike_Punch");
        var sKick     = sm.AddState("Strike_Kick");
        var sDropkick = sm.AddState("Strike_Dropkick");
        var sReceive  = sm.AddState("Strike_Receive");

        // ── REVERSAL STATES ────────────────────────────────────────────────────
        // Two distinct states: one per attack class.
        // Player must correctly read incoming attack type for reversal to fire.
        var revStrike  = sm.AddState("Reversal_Strike");
        var revGrapple = sm.AddState("Reversal_Grapple");

        // ── GRAPPLE CLINCH INITIATION ──────────────────────────────────────────
        var grapQuick  = sm.AddState("Grapple_Quick_Attempt");
        var grapStrong = sm.AddState("Grapple_Strong_Attempt");

        // ── SLOT-BASED GRAPPLE MOVE STATES (20 total) ─────────────────────────
        // Each slot state has a unique placeholder AnimationClip.
        // WrestlerMoveSet.ApplyToAnimator() replaces these placeholders at runtime
        // with the actual move clips assigned to this wrestler's slots.
        //
        // Slot layout (matches WrestlerMoveSet fields):
        //   QuickFront  [0–4] — tap input, facing opponent
        //   QuickRear   [0–4] — tap input, behind opponent
        //   StrongFront [0–4] — hold input, facing opponent
        //   StrongRear  [0–4] — hold input, behind opponent

        string[]            slotPrefixes   = { "QuickFront", "QuickRear", "StrongFront", "StrongRear" };
        AnimatorState[][]   allSlotStates  = new AnimatorState[4][];

        for (int p = 0; p < 4; p++)
        {
            allSlotStates[p] = new AnimatorState[5];
            for (int s = 0; s < 5; s++)
            {
                string stateName = $"{slotPrefixes[p]}_Slot_{s}";
                string clipName  = $"SLOT_{slotPrefixes[p]}_{s}";

                var state = sm.AddState(stateName);
                state.motion = GetOrCreatePlaceholderClip(clipName);
                allSlotStates[p][s] = state;
            }
        }

        // Short references for transition wiring below
        AnimatorState[] qf = allSlotStates[0]; // QuickFront  slots 0–4
        AnimatorState[] qr = allSlotStates[1]; // QuickRear   slots 0–4
        AnimatorState[] sf = allSlotStates[2]; // StrongFront slots 0–4
        AnimatorState[] sr = allSlotStates[3]; // StrongRear  slots 0–4

        // ── SUBMISSION STATES ──────────────────────────────────────────────────
        var subApply   = sm.AddState("Sub_Apply");
        var subHold    = sm.AddState("Sub_Hold");
        subHold.motion = GetOrCreatePlaceholderClip("SLOT_Submission");

        var subVictim  = sm.AddState("Sub_Victim_Struggle");
        var subRopeBrk = sm.AddState("Sub_Victim_RopeBreak");
        var subTapOut  = sm.AddState("Sub_TapOut");

        // ── GROUNDED STATES ────────────────────────────────────────────────────
        var gndUp   = sm.AddState("Grounded_FaceUp");
        var gndDown = sm.AddState("Grounded_FaceDown");
        var getUp   = sm.AddState("GetUp");

        // ── IRISH WHIP STATES ──────────────────────────────────────────────────
        var iwSend    = sm.AddState("IrishWhip_Send");
        var iwRebound = sm.AddState("IrishWhip_Rebound");
        var ropeHit   = sm.AddState("Ropes_Hit");

        // ── FINISHER STATES ────────────────────────────────────────────────────
        var finTaunt   = sm.AddState("Finisher_Taunt");
        var finExec    = sm.AddState("Finisher_Execute");
        finExec.motion = GetOrCreatePlaceholderClip("SLOT_Finisher");
        var finReceive = sm.AddState("Finisher_Receive");

        // ── PINFALL STATES ─────────────────────────────────────────────────────
        var pinCover   = sm.AddState("Pinfall_Cover");
        var pinKickout = sm.AddState("Pinfall_Kickout");
        var pinLoss    = sm.AddState("Pinfall_Loss");

        // ── CAGE MATCH STATES ──────────────────────────────────────────────────
        var cageClimb       = sm.AddState("Cage_Climb");
        var cageInterrupted = sm.AddState("Cage_ClimbInterrupted");
        var cageEscape      = sm.AddState("Cage_Escape");
        var cagePulledDown  = sm.AddState("Cage_PulledDown");

        // ── MATCH OUTCOME STATES ───────────────────────────────────────────────
        var winCeleb = sm.AddState("Match_Win");
        var lossSell = sm.AddState("Match_Loss");
        /*var entrance =*/ sm.AddState("Entrance_Walk"); // reserved; not wired to avoid auto-trigger

        // ── TRANSITIONS ────────────────────────────────────────────────────────

        // Locomotion
        Tf(idle,    walkFwd, "MoveSpeed", AnimatorConditionMode.Greater,  0.10f);
        Tf(idle,    walkBwd, "MoveSpeed", AnimatorConditionMode.Less,    -0.10f);
        Tf(walkFwd, idle,    "MoveSpeed", AnimatorConditionMode.Less,     0.10f);
        Tf(walkBwd, idle,    "MoveSpeed", AnimatorConditionMode.Greater, -0.10f);
        Tf(walkFwd, run,     "MoveSpeed", AnimatorConditionMode.Greater,  0.80f);
        Tf(run,     walkFwd, "MoveSpeed", AnimatorConditionMode.Less,     0.80f);

        // Strikes from idle
        Ti(idle, sPunch,    "AttackType", AnimatorConditionMode.Equals, 1);
        Ti(idle, sKick,     "AttackType", AnimatorConditionMode.Equals, 2);
        Ti(idle, sDropkick, "AttackType", AnimatorConditionMode.Equals, 3);
        Exit(sPunch, idle); Exit(sKick, idle); Exit(sDropkick, idle);

        // Impact received (any locomotion state → receive)
        foreach (var s in new[] { idle, walkFwd, walkBwd, run })
            Tt(s, sReceive, "ImpactReceived");
        Tb(sReceive, gndUp, "IsGrounded", true);
        Exit(sReceive, idle);

        // Reversals — triggered from within the relevant attack state
        foreach (var s in new[] { sPunch, sKick, sDropkick })
            Tt(s, revStrike, "ReversalSuccess");
        foreach (var s in new[] { grapQuick, grapStrong })
            Tt(s, revGrapple, "ReversalSuccess");
        Exit(revStrike, idle); Exit(revGrapple, idle);

        // Grapple clinch initiations from idle
        Ti(idle, grapQuick,  "GrappleTier", AnimatorConditionMode.Equals, 1);
        Ti(idle, grapStrong, "GrappleTier", AnimatorConditionMode.Equals, 2);

        // Grapple slot selection: tier + FacingFront bool + MoveIndex → specific slot state
        for (int i = 0; i < 5; i++) TiGrapple(grapQuick,  qf[i], 1, true,  i);
        for (int i = 0; i < 5; i++) TiGrapple(grapQuick,  qr[i], 1, false, i);
        for (int i = 0; i < 5; i++) TiGrapple(grapStrong, sf[i], 2, true,  i);
        for (int i = 0; i < 5; i++) TiGrapple(grapStrong, sr[i], 2, false, i);

        // All 20 slot states exit to idle when clip completes
        foreach (var stateArr in allSlotStates)
            foreach (var s in stateArr)
                Exit(s, idle);

        // Grounded / GetUp
        Tb(gndUp,   getUp, "IsGrounded", false);
        Tb(gndDown, getUp, "IsGrounded", false);
        Tt(gndUp,   getUp, "GetUpTrigger");
        Tt(gndDown, getUp, "GetUpTrigger");
        Exit(getUp, idle);

        // Submission — applying side
        Tb(idle,    subApply,  "IsSubmitting", true);
        Exit(subApply, subHold);
        Tt(subHold, idle,  "RopeBreak");
        Tb(subHold, idle,  "IsSubmitting", false);

        // Submission — victim side (face-down required for Boston Crab archetype)
        Tb(gndDown,   subVictim,  "IsSubVictim", true);
        Tt(subVictim, subRopeBrk, "RopeBreak");
        Exit(subRopeBrk, idle);
        Exit(subTapOut,  lossSell);

        // Irish Whip chain
        Ti(idle, iwSend,    "IrishWhipState", AnimatorConditionMode.Equals, 1);
        Ti(idle, iwRebound, "IrishWhipState", AnimatorConditionMode.Equals, 2);
        Exit(iwSend, idle);
        Tb(iwRebound, ropeHit, "OnRopes", true);
        Exit(ropeHit, run);

        // Finisher — momentum-gated (SPECIAL state required)
        Tb(idle,     finTaunt, "SpecialState", true);
        Tt(finTaunt, finExec,  "FinisherTrigger");
        Tt(idle,     finExec,  "FinisherTrigger");    // direct trigger if taunt skipped
        Exit(finExec, idle);
        Tt(gndUp,   finReceive, "ImpactReceived");
        Tt(gndDown, finReceive, "ImpactReceived");
        Exit(finReceive, gndUp);

        // Pinfall
        Tt(idle,     pinCover,  "PinfallAttempt");
        Tt(pinCover, pinKickout,"ImpactReceived");
        Ti(pinCover, pinLoss,   "MatchOutcome", AnimatorConditionMode.Equals, 2);
        Exit(pinCover, idle);
        Exit(pinKickout, gndUp);

        // Cage match
        Tb(idle,     cageClimb,      "IsCageClimbing", true);
        Tb(cageClimb,cageEscape,     "IsCageClimbing", false);
        Tt(cageClimb,cageInterrupted,"ImpactReceived");
        Exit(cageInterrupted, cagePulledDown);
        Exit(cagePulledDown,  gndDown);
        Exit(cageEscape,      winCeleb);

        // Match outcomes
        Ti(idle,    winCeleb, "MatchOutcome", AnimatorConditionMode.Equals, 1);
        Ti(idle,    lossSell, "MatchOutcome", AnimatorConditionMode.Equals, 2);
        Ti(gndDown, lossSell, "MatchOutcome", AnimatorConditionMode.Equals, 2);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WrestlerAnimatorBuilder] Controller built at {ControllerPath}.\n" +
                  $"22 placeholder clips in {PlaceholderDir}/\n" +
                  $"Assign clips to wrestler slots via WrestlerMoveSet assets.\n" +
                  $"Apply per-wrestler overrides at runtime via WrestlerMoveSet.ApplyToAnimator().");
    }

    // ── PLACEHOLDER CLIP FACTORY ───────────────────────────────────────────────
    // Creates a minimal 1-frame AnimationClip asset to occupy the slot state.
    // These are the targets that AnimatorOverrideController replaces per wrestler.

    private static AnimationClip GetOrCreatePlaceholderClip(string clipName)
    {
        string path     = $"{PlaceholderDir}/{clipName}.anim";
        var    existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) return existing;

        var clip = new AnimationClip { name = clipName };
        // 1-frame dummy curve prevents Unity from stripping the empty clip
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.x",
            AnimationCurve.Constant(0f, 1f / 60f, 0f));

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ── TRANSITION HELPERS ─────────────────────────────────────────────────────

    static void Tf(AnimatorState f, AnimatorState t, string p, AnimatorConditionMode m, float v)
    { var tr = f.AddTransition(t); tr.hasExitTime = false; tr.duration = 0.10f; tr.AddCondition(m, v, p); }

    static void Ti(AnimatorState f, AnimatorState t, string p, AnimatorConditionMode m, int v)
    { var tr = f.AddTransition(t); tr.hasExitTime = false; tr.duration = 0.05f; tr.AddCondition(m, v, p); }

    static void Tb(AnimatorState f, AnimatorState t, string p, bool v)
    { var tr = f.AddTransition(t); tr.hasExitTime = false; tr.duration = 0.05f;
      tr.AddCondition(v ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, p); }

    static void Tt(AnimatorState f, AnimatorState t, string trigger)
    { var tr = f.AddTransition(t); tr.hasExitTime = false; tr.duration = 0.05f;
      tr.AddCondition(AnimatorConditionMode.If, 0f, trigger); }

    static void Exit(AnimatorState f, AnimatorState t)
    { var tr = f.AddTransition(t); tr.hasExitTime = true; tr.exitTime = 1.0f; tr.duration = 0.05f; }

    static void TiGrapple(AnimatorState f, AnimatorState t, int tier, bool front, int idx)
    { var tr = f.AddTransition(t); tr.hasExitTime = false; tr.duration = 0.05f;
      tr.AddCondition(AnimatorConditionMode.Equals, tier,  "GrappleTier");
      tr.AddCondition(front ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "FacingFront");
      tr.AddCondition(AnimatorConditionMode.Equals, idx,   "MoveIndex"); }
}
