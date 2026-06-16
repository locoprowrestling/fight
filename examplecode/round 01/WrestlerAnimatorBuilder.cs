// WrestlerAnimatorBuilder.cs
// Place in: Assets/Editor/
// Run via: Wrestling > Build Wrestler Animator Controller
// Generates: Assets/Animations/WrestlerAnimatorController.controller
//
// State machine derived from observed gameplay mechanics:
//   - Dual-tier grapple system (Quick/Strong, 5 moves x Front/Rear per tier = 20 total)
//   - Context-sensitive reversal (attack-type-dependent, not universal)
//   - Momentum-gated SPECIAL/finisher system
//   - Submission hold with rope-break positional escape
//   - Irish whip rebound chain
//   - Cage match climb/interrupt/escape states
//   - Unified health+momentum meter driving SpecialState bool

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class WrestlerAnimatorBuilder
{
    [MenuItem("Wrestling/Build Wrestler Animator Controller")]
    public static void Build()
    {
        // Ensure output directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        var controller = AnimatorController.CreateAnimatorControllerAtPath(
            "Assets/Animations/WrestlerAnimatorController.controller");

        // ── PARAMETERS ──────────────────────────────────────────────────────────
        // Float
        controller.AddParameter("MoveSpeed",       AnimatorControllerParameterType.Float);

        // Int
        controller.AddParameter("GrappleTier",     AnimatorControllerParameterType.Int);  // 0=none 1=quick 2=strong
        controller.AddParameter("MoveIndex",        AnimatorControllerParameterType.Int);  // 0-4 directional selection
        controller.AddParameter("AttackType",       AnimatorControllerParameterType.Int);  // 0=none 1=punch 2=kick 3=dropkick
        controller.AddParameter("IrishWhipState",   AnimatorControllerParameterType.Int);  // 0=none 1=sending 2=rebounding
        controller.AddParameter("MatchOutcome",     AnimatorControllerParameterType.Int);  // 0=ongoing 1=win 2=loss

        // Bool
        controller.AddParameter("FacingFront",      AnimatorControllerParameterType.Bool); // true=front clinch false=rear
        controller.AddParameter("IsGrounded",       AnimatorControllerParameterType.Bool);
        controller.AddParameter("GroundFaceUp",     AnimatorControllerParameterType.Bool); // true=face-up false=face-down
        controller.AddParameter("SpecialState",     AnimatorControllerParameterType.Bool); // momentum meter full
        controller.AddParameter("IsSubmitting",     AnimatorControllerParameterType.Bool); // applying a submission
        controller.AddParameter("IsSubVictim",      AnimatorControllerParameterType.Bool); // receiving a submission
        controller.AddParameter("IsCageClimbing",   AnimatorControllerParameterType.Bool);
        controller.AddParameter("OnRopes",          AnimatorControllerParameterType.Bool);

        // Trigger
        controller.AddParameter("FinisherTrigger",  AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ReversalSuccess",  AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ImpactReceived",   AnimatorControllerParameterType.Trigger);
        controller.AddParameter("RopeBreak",        AnimatorControllerParameterType.Trigger);
        controller.AddParameter("PinfallAttempt",   AnimatorControllerParameterType.Trigger);
        controller.AddParameter("GetUpTrigger",     AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        // ── STATES ──────────────────────────────────────────────────────────────

        // Locomotion
        var idle    = sm.AddState("Idle");       sm.defaultState = idle;
        var walkFwd = sm.AddState("WalkForward");
        var walkBwd = sm.AddState("WalkBackward");
        var run     = sm.AddState("Run");

        // Strikes
        var sPunch    = sm.AddState("Strike_Punch");
        var sKick     = sm.AddState("Strike_Kick");
        var sDropkick = sm.AddState("Strike_Dropkick");
        var sReceive  = sm.AddState("Strike_Receive");

        // Reversals — one per attack class (strike or grapple)
        var revStrike  = sm.AddState("Reversal_Strike");
        var revGrapple = sm.AddState("Reversal_Grapple");

        // Grapple clinch initiations
        var grapQuick  = sm.AddState("Grapple_Quick_Attempt");
        var grapStrong = sm.AddState("Grapple_Strong_Attempt");

        // Quick grapple moves — Front (5 directional slots)
        var qf = new AnimatorState[5];
        qf[0] = sm.AddState("QuickFront_0_Headlock");
        qf[1] = sm.AddState("QuickFront_1_Snapmare");
        qf[2] = sm.AddState("QuickFront_2_ArmDrag");
        qf[3] = sm.AddState("QuickFront_3_Bulldog");
        qf[4] = sm.AddState("QuickFront_4_DDT");

        // Quick grapple moves — Rear (5 directional slots)
        var qr = new AnimatorState[5];
        qr[0] = sm.AddState("QuickRear_0_RearNeckbreaker");
        qr[1] = sm.AddState("QuickRear_1_BackSuplex");
        qr[2] = sm.AddState("QuickRear_2_HammerLock");
        qr[3] = sm.AddState("QuickRear_3_Rollup");
        qr[4] = sm.AddState("QuickRear_4_RearBodyslam");

        // Strong grapple moves — Front (5 directional slots)
        var sf = new AnimatorState[5];
        sf[0] = sm.AddState("StrongFront_0_BellyToBelly");
        sf[1] = sm.AddState("StrongFront_1_Powerbomb");
        sf[2] = sm.AddState("StrongFront_2_Piledriver");
        sf[3] = sm.AddState("StrongFront_3_VerticalSuplex");
        sf[4] = sm.AddState("StrongFront_4_ShoulderBreaker");

        // Strong grapple moves — Rear (5 directional slots)
        var sr = new AnimatorState[5];
        sr[0] = sm.AddState("StrongRear_0_GermanSuplex");
        sr[1] = sm.AddState("StrongRear_1_BackBodyDrop");
        sr[2] = sm.AddState("StrongRear_2_SleepHold");
        sr[3] = sm.AddState("StrongRear_3_Backdrop");
        sr[4] = sm.AddState("StrongRear_4_OlympicSlam");

        // Submission (Boston Crab archetype — extendable to other holds)
        var subApply   = sm.AddState("Sub_Apply");
        var subHold    = sm.AddState("Sub_Hold");
        var subVictim  = sm.AddState("Sub_Victim_Struggle");
        var subRopeBrk = sm.AddState("Sub_Victim_RopeBreak");
        var subTapOut  = sm.AddState("Sub_TapOut");

        // Grounded states
        var gndUp   = sm.AddState("Grounded_FaceUp");
        var gndDown = sm.AddState("Grounded_FaceDown");
        var getUp   = sm.AddState("GetUp");

        // Irish Whip chain
        var iwSend    = sm.AddState("IrishWhip_Send");
        var iwRebound = sm.AddState("IrishWhip_Rebound");
        var ropeHit   = sm.AddState("Ropes_Hit");

        // Finisher (momentum-gated)
        var finTaunt   = sm.AddState("Finisher_Taunt");
        var finExec    = sm.AddState("Finisher_Execute");
        var finReceive = sm.AddState("Finisher_Receive");

        // Pinfall
        var pinCover   = sm.AddState("Pinfall_Cover");
        var pinKickout = sm.AddState("Pinfall_Kickout");
        var pinLoss    = sm.AddState("Pinfall_Loss");

        // Cage match
        var cageClimb       = sm.AddState("Cage_Climb");
        var cageInterrupted = sm.AddState("Cage_ClimbInterrupted");
        var cageEscape      = sm.AddState("Cage_Escape");
        var cagePulledDown  = sm.AddState("Cage_PulledDown");

        // Match resolution
        var winCeleb = sm.AddState("Match_Win");
        var lossSell = sm.AddState("Match_Loss");
        var entrance = sm.AddState("Entrance_Walk");

        // ── TRANSITIONS ─────────────────────────────────────────────────────────

        // Locomotion
        Tf(idle,    walkFwd, "MoveSpeed", AnimatorConditionMode.Greater,  0.10f);
        Tf(idle,    walkBwd, "MoveSpeed", AnimatorConditionMode.Less,    -0.10f);
        Tf(walkFwd, idle,    "MoveSpeed", AnimatorConditionMode.Less,     0.10f);
        Tf(walkBwd, idle,    "MoveSpeed", AnimatorConditionMode.Greater, -0.10f);
        Tf(walkFwd, run,     "MoveSpeed", AnimatorConditionMode.Greater,  0.80f);
        Tf(run,     walkFwd, "MoveSpeed", AnimatorConditionMode.Less,     0.80f);

        // Strikes from idle (AttackType enum)
        Ti(idle, sPunch,    "AttackType", AnimatorConditionMode.Equals, 1);
        Ti(idle, sKick,     "AttackType", AnimatorConditionMode.Equals, 2);
        Ti(idle, sDropkick, "AttackType", AnimatorConditionMode.Equals, 3);
        Exit(sPunch,    idle);
        Exit(sKick,     idle);
        Exit(sDropkick, idle);

        // Impact received — any locomotion state → strike receive
        foreach (var s in new[]{ idle, walkFwd, walkBwd, run })
            Tt(s, sReceive, "ImpactReceived");
        Tb(sReceive, gndUp, "IsGrounded", true);
        Exit(sReceive, idle);

        // Reversals — triggered from the attack state they counter
        // Strike reversal: fired when player correctly reads an incoming strike
        foreach (var s in new[]{ sPunch, sKick, sDropkick })
            Tt(s, revStrike, "ReversalSuccess");
        // Grapple reversal: fired when player correctly reads an incoming grapple
        foreach (var s in new[]{ grapQuick, grapStrong })
            Tt(s, revGrapple, "ReversalSuccess");
        Exit(revStrike,  idle);
        Exit(revGrapple, idle);

        // Grapple initiation from idle
        Ti(idle, grapQuick,  "GrappleTier", AnimatorConditionMode.Equals, 1);
        Ti(idle, grapStrong, "GrappleTier", AnimatorConditionMode.Equals, 2);

        // Grapple move selection: tier + FacingFront + MoveIndex → specific move state
        for (int i = 0; i < 5; i++) TiGrapple(grapQuick,  qf[i], 1, true,  i);
        for (int i = 0; i < 5; i++) TiGrapple(grapQuick,  qr[i], 1, false, i);
        for (int i = 0; i < 5; i++) TiGrapple(grapStrong, sf[i], 2, true,  i);
        for (int i = 0; i < 5; i++) TiGrapple(grapStrong, sr[i], 2, false, i);

        // All 20 grapple move states return to idle on exit
        foreach (var s in new[]{ qf[0],qf[1],qf[2],qf[3],qf[4],
                                  qr[0],qr[1],qr[2],qr[3],qr[4],
                                  sf[0],sf[1],sf[2],sf[3],sf[4],
                                  sr[0],sr[1],sr[2],sr[3],sr[4] })
            Exit(s, idle);

        // Grounded / GetUp
        Tb(gndUp,   getUp, "IsGrounded", false);
        Tb(gndDown, getUp, "IsGrounded", false);
        Tt(gndUp,   getUp, "GetUpTrigger");
        Tt(gndDown, getUp, "GetUpTrigger");
        Exit(getUp, idle);

        // Submission — apply side
        Tb(idle, subApply, "IsSubmitting", true);
        Exit(subApply, subHold);                           // apply → hold on clip end
        Tt(subHold, idle, "RopeBreak");                    // rope break exits hold
        Tb(subHold, idle, "IsSubmitting", false);          // programmer-driven release

        // Submission — victim side (grounded face-down required by Boston Crab archetype)
        Tb(gndDown,   subVictim,  "IsSubVictim", true);
        Tt(subVictim, subRopeBrk, "RopeBreak");            // body contacted ropes → break
        Exit(subRopeBrk, idle);
        Exit(subTapOut,  lossSell);                        // tap → loss state

        // Irish Whip chain
        Ti(idle, iwSend,    "IrishWhipState", AnimatorConditionMode.Equals, 1);
        Ti(idle, iwRebound, "IrishWhipState", AnimatorConditionMode.Equals, 2);
        Exit(iwSend, idle);
        Tb(iwRebound, ropeHit, "OnRopes", true);           // rebound → ropes contact
        Exit(ropeHit, run);                                // ropes → burst into run

        // Finisher system — SPECIAL state is prerequisite
        Tb(idle, finTaunt, "SpecialState", true);           // enter taunt when meter full
        Tt(finTaunt, finExec, "FinisherTrigger");           // player inputs finisher from taunt
        Tt(idle,    finExec, "FinisherTrigger");            // or directly if SpecialState already set
        Exit(finExec, idle);
        // Finisher receive (victim side)
        Tt(gndUp,   finReceive, "ImpactReceived");
        Tt(gndDown, finReceive, "ImpactReceived");
        Exit(finReceive, gndUp);

        // Pinfall
        Tt(idle, pinCover, "PinfallAttempt");
        Tt(pinCover, pinKickout, "ImpactReceived");         // kickout interrupts cover
        Ti(pinCover, pinLoss, "MatchOutcome", AnimatorConditionMode.Equals, 2);
        Exit(pinCover,   idle);
        Exit(pinKickout, gndUp);

        // Cage match states
        Tb(idle, cageClimb, "IsCageClimbing", true);
        Tb(cageClimb, cageEscape, "IsCageClimbing", false); // reached top → escape
        Tt(cageClimb, cageInterrupted, "ImpactReceived");   // opponent hits climber
        Exit(cageInterrupted, cagePulledDown);
        Exit(cagePulledDown,  gndDown);
        Exit(cageEscape,      winCeleb);

        // Match outcome resolution
        Ti(idle,    winCeleb, "MatchOutcome", AnimatorConditionMode.Equals, 1);
        Ti(idle,    lossSell,  "MatchOutcome", AnimatorConditionMode.Equals, 2);
        Ti(gndDown, lossSell,  "MatchOutcome", AnimatorConditionMode.Equals, 2);

        AssetDatabase.SaveAssets();
        Debug.Log("[WrestlerAnimatorBuilder] Controller built → Assets/Animations/WrestlerAnimatorController.controller");
    }

    // ── TRANSITION HELPERS ──────────────────────────────────────────────────────

    static void Tf(AnimatorState f, AnimatorState t, string p, AnimatorConditionMode m, float v)
    {
        var tr = f.AddTransition(t); tr.hasExitTime = false; tr.duration = 0.10f;
        tr.AddCondition(m, v, p);
    }
    static void Ti(AnimatorState f, AnimatorState t, string p, AnimatorConditionMode m, int v)
    {
        var tr = f.AddTransition(t); tr.hasExitTime = false; tr.duration = 0.05f;
        tr.AddCondition(m, v, p);
    }
    static void Tb(AnimatorState f, AnimatorState t, string p, bool v)
    {
        var tr = f.AddTransition(t); tr.hasExitTime = false; tr.duration = 0.05f;
        tr.AddCondition(v ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, p);
    }
    static void Tt(AnimatorState f, AnimatorState t, string trigger)
    {
        var tr = f.AddTransition(t); tr.hasExitTime = false; tr.duration = 0.05f;
        tr.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }
    static void Exit(AnimatorState f, AnimatorState t)
    {
        var tr = f.AddTransition(t); tr.hasExitTime = true; tr.exitTime = 1.0f; tr.duration = 0.05f;
    }
    static void TiGrapple(AnimatorState f, AnimatorState t, int tier, bool front, int idx)
    {
        var tr = f.AddTransition(t); tr.hasExitTime = false; tr.duration = 0.05f;
        tr.AddCondition(AnimatorConditionMode.Equals, tier, "GrappleTier");
        tr.AddCondition(front ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "FacingFront");
        tr.AddCondition(AnimatorConditionMode.Equals, idx, "MoveIndex");
    }
}
