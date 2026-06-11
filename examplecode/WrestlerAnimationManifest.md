# WRESTLER ANIMATION CLIP MANIFEST
# Maps every Animator Controller state → required animation clip
# Use this as your sourcing checklist (Mixamo, mocap, hand-keyed, etc.)
#
# Columns:
#   STATE NAME       — exact string used in WrestlerAnimatorBuilder.cs
#   CLIP NAME        — recommended file name for the animation clip asset
#   LOOP             — whether the clip should loop
#   ROOT MOTION      — whether the clip should drive character position
#   APPROX DURATION  — rough timing in seconds
#   NOTES            — mechanical context from video analysis

---

## LOCOMOTION

| State Name   | Clip Name              | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| Idle         | wrestler_idle          | YES  | NO          | 2.0s     | Neutral stance with subtle weight shift. Character is actively guarding — not fully relaxed. Slight heel-bounce acceptable. |
| WalkForward  | wrestler_walk_fwd      | YES  | YES         | 0.7s     | Deliberate forward stalk. Pacing is noticeably slower than a standard walk — the game's intentionally measured tempo. Arms partially raised. |
| WalkBackward | wrestler_walk_bwd      | YES  | YES         | 0.7s     | Reverse of WalkForward. Same cautious guard position. |
| Run          | wrestler_run           | YES  | YES         | 0.5s     | Full sprint triggered by Irish whip rebound or manual run input. Arms pump. |

---

## STRIKES

| State Name        | Clip Name                | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| Strike_Punch      | wrestler_strike_punch    | NO   | NO          | 0.4s     | Standing forearm/closed-fist strike. Short animation — designed for rapid chaining. |
| Strike_Kick       | wrestler_strike_kick     | NO   | NO          | 0.5s     | Standing boot/kick. Slightly longer wind-up than punch. |
| Strike_Dropkick   | wrestler_strike_dropkick | NO   | YES         | 0.8s     | Two-footed aerial strike. Requires forward momentum into launch. Lands attacker on mat — transition to Grounded_FaceDown on self after impact. |
| Strike_Receive    | wrestler_strike_receive  | NO   | NO          | 0.35s    | Impact sell for incoming strike. Severity (stagger vs. knockdown) determined by causesKnockdown flag in ReceiveImpact(). |
| Dropkick_Receive  | wrestler_dropkick_receive| NO   | YES         | 0.6s     | Larger sell for receiving dropkick. Back-stagger or stumble into ropes acceptable. |

---

## REVERSALS
# Reversals require two distinct clips — one per attack class.
# Observed mechanic: player must read attack TYPE (strike vs. grapple) and input accordingly.
# A successful reversal creates a momentum swing; the animation should feel reactive and sharp.

| State Name       | Clip Name                 | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| Reversal_Strike  | wrestler_reversal_strike  | NO   | NO          | 0.5s     | Counter-move for an incoming strike. Visually: catch or redirect of the limb, quick counter strike or throw. |
| Reversal_Grapple | wrestler_reversal_grapple | NO   | NO          | 0.6s     | Counter-move for an incoming grapple attempt. Visually: wrestler escapes the clinch attempt and repositions. |

---

## GRAPPLE CLINCH INITIATIONS

| State Name               | Clip Name                    | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| Grapple_Quick_Attempt    | wrestler_grapple_quick_init  | NO   | YES         | 0.3s     | Short reach forward, lock-up entry for quick/weak tier. Flows into the selected QuickFront/QuickRear move. |
| Grapple_Strong_Attempt   | wrestler_grapple_strong_init | NO   | YES         | 0.4s     | Deeper, more committed lunge forward for power-tier grapple. Flows into StrongFront/StrongRear move. |

---

## QUICK GRAPPLE MOVES — FRONT (tap input, facing opponent)
# These are the 5 directional slots for quick/weak front-facing grapple.
# Replace clip names with your actual move choices — these are archetypes.

| State Name                | Clip Name                      | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| QuickFront_0_Headlock     | wrestler_qf0_headlock          | NO   | YES         | 1.2s     | Side headlock takedown. Low-damage control move. |
| QuickFront_1_Snapmare     | wrestler_qf1_snapmare          | NO   | YES         | 0.9s     | Seated roll-through of opponent. Leaves opponent in seated grounded state. |
| QuickFront_2_ArmDrag      | wrestler_qf2_armdrag           | NO   | YES         | 1.0s     | Hip-toss variant using arm control. Opponent lands standing or grounded. |
| QuickFront_3_Bulldog      | wrestler_qf3_bulldog           | NO   | YES         | 1.1s     | Running face-plant: attacker grabs head and drops forward. Seen in video footage. |
| QuickFront_4_DDT          | wrestler_qf4_ddt               | NO   | YES         | 1.3s     | Front facelock into forward plant. Opponent lands on head. |

---

## QUICK GRAPPLE MOVES — REAR (tap input, behind opponent)

| State Name                  | Clip Name                        | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| QuickRear_0_RearNeckbreaker | wrestler_qr0_rear_neckbreaker    | NO   | YES         | 1.0s     | Standing rear neckbreaker. Quick snap from behind. |
| QuickRear_1_BackSuplex      | wrestler_qr1_back_suplex         | NO   | YES         | 1.2s     | Arching back suplex. Attacker and opponent land together — attacker rolls through. |
| QuickRear_2_HammerLock      | wrestler_qr2_hammerlock          | NO   | YES         | 0.9s     | Arm manipulation into grounded control. Used as setup for other moves. |
| QuickRear_3_Rollup          | wrestler_qr3_rollup              | NO   | YES         | 1.0s     | Schoolboy rollup. Opponent lands in pinfall-eligible position — trigger PinfallAttempt immediately after. |
| QuickRear_4_RearBodyslam    | wrestler_qr4_rear_bodyslam       | NO   | YES         | 1.1s     | Rear lift and plant. Opponent lands flat. |

---

## STRONG GRAPPLE MOVES — FRONT (hold input, facing opponent)

| State Name                   | Clip Name                         | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| StrongFront_0_BellyToBelly   | wrestler_sf0_belly_to_belly       | NO   | YES         | 1.5s     | Overhead belly-to-belly suplex. CONFIRMED in video footage. High-impact — should shake camera on land. |
| StrongFront_1_Powerbomb      | wrestler_sf1_powerbomb            | NO   | YES         | 1.8s     | Full scoop-and-plant powerbomb. Maximum visual impact. Jackknife variant available as cosmetic. |
| StrongFront_2_Piledriver     | wrestler_sf2_piledriver           | NO   | YES         | 1.6s     | Inverted vertical drop, opponent lands head-first. |
| StrongFront_3_VerticalSuplex | wrestler_sf3_vertical_suplex      | NO   | YES         | 1.5s     | Classic delayed vertical suplex. Attacker holds opponent inverted briefly. |
| StrongFront_4_ShoulderBreaker | wrestler_sf4_shoulder_breaker    | NO   | YES         | 1.3s     | Limb-targeting move. Sets up shoulder/arm submissions contextually. |

---

## STRONG GRAPPLE MOVES — REAR (hold input, behind opponent)

| State Name                | Clip Name                      | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| StrongRear_0_GermanSuplex | wrestler_sr0_german_suplex     | NO   | YES         | 1.6s     | Bridge-locked waist-hold suplex. Attacker bridges into potential pin position. |
| StrongRear_1_BackBodyDrop  | wrestler_sr1_back_body_drop   | NO   | YES         | 1.4s     | Opponent launched over attacker's back. High-arc flight. |
| StrongRear_2_SleepHold    | wrestler_sr2_sleep_hold        | NO   | YES         | 0.8s+    | Choke/sleep hold applied from rear. Loops in Sub_Hold state until escape or tapout. |
| StrongRear_3_Backdrop     | wrestler_sr3_backdrop          | NO   | YES         | 1.5s     | Reverse suplex variant from rear waist-lock. |
| StrongRear_4_OlympicSlam  | wrestler_sr4_olympic_slam      | NO   | YES         | 1.7s     | Chest-to-chest overhead release. Referenced in video analysis. |

---

## SUBMISSION SYSTEM

| State Name              | Clip Name                     | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| Sub_Apply               | wrestler_sub_apply            | NO   | YES         | 0.8s     | Transition clip from standing into the hold position. Attacker moves into position. |
| Sub_Hold                | wrestler_sub_hold             | YES  | NO          | 2.0s     | Looping sustained hold — Boston Crab archetype. Attacker bridges/pulls. Loops until RopeBreak trigger or IsSubmitting=false. |
| Sub_Victim_Struggle     | wrestler_sub_victim_struggle  | YES  | NO          | 1.5s     | Victim writhes and reaches. Loops until escape condition. Must be face-down (GroundFaceUp=false) for Boston Crab. |
| Sub_Victim_RopeBreak    | wrestler_sub_victim_rope_break| NO   | YES         | 0.6s     | Victim drags/rolls to rope contact and breaks hold. Short reactive clip. |
| Sub_TapOut              | wrestler_sub_tap_out          | NO   | NO          | 1.0s     | Victim taps mat repeatedly. Transitions to Match_Loss. |

---

## GROUNDED STATES

| State Name          | Clip Name                   | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| Grounded_FaceUp     | wrestler_grounded_face_up   | YES  | NO          | 2.0s     | Lying supine. Required orientation for pinfall covers on the victim. |
| Grounded_FaceDown   | wrestler_grounded_face_down | YES  | NO          | 2.0s     | Lying prone. Required orientation for Boston Crab (Sub_Victim_Struggle). |
| GetUp               | wrestler_get_up             | NO   | NO          | 0.7s     | Rising from grounded state. Transitions to Idle. Can be interrupted by ImpactReceived. |

---

## IRISH WHIP CHAIN

| State Name          | Clip Name                   | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| IrishWhip_Send      | wrestler_irishwhip_send     | NO   | NO          | 0.5s     | Grab and fling of opponent toward ropes. Sender returns to Idle. |
| IrishWhip_Rebound   | wrestler_irishwhip_rebound  | NO   | YES         | 0.4s     | Receiver accelerates toward ropes. Flows into Ropes_Hit on rope contact. |
| Ropes_Hit           | wrestler_ropes_bounce       | NO   | NO          | 0.3s     | Body makes contact with ring ropes, absorbs, and pushes off. Transitions to Run. |

---

## FINISHER SYSTEM

| State Name        | Clip Name                  | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| Finisher_Taunt    | wrestler_finisher_taunt    | YES  | NO          | 2.0s     | Looping taunt played when momentum meter is full. Signals Special state visually. Classic examples: Rock raises eyebrow, Austin stomps. |
| Finisher_Execute  | wrestler_finisher_execute  | NO   | YES         | 2.0s     | The finisher move itself. CONFIRMED in footage: Rock Bottom causes ring-shake on impact — trigger camera shake event at landing frame. Per-wrestler variant. |
| Finisher_Receive  | wrestler_finisher_receive  | NO   | YES         | 1.8s     | Victim's sell of the finisher. High-impact collapse. Should match the attacker's Finisher_Execute landing point. |

---

## PINFALL SYSTEM

| State Name      | Clip Name                | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| Pinfall_Cover   | wrestler_pinfall_cover   | NO   | YES         | 2.5s     | Attacker drops onto grounded opponent, hooks leg (or similar). Plays through referee 3-count window. Interruptible by Kickout trigger. |
| Pinfall_Kickout | wrestler_pinfall_kickout | NO   | NO          | 0.5s     | Victim's explosive shoulder raise off mat at count 2. Transitions to Grounded_FaceUp. |
| Pinfall_Loss    | wrestler_pinfall_loss    | NO   | NO          | 3.0s     | Three-count completes — loser stays down. Transitions to Match_Loss. |

---

## CAGE MATCH STATES

| State Name             | Clip Name                    | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| Cage_Climb             | wrestler_cage_climb          | YES  | YES         | 1.5s/loop| Repeated climbing motion up vertical cage surface. Root motion drives upward Y position. Interruptible at any frame. |
| Cage_ClimbInterrupted  | wrestler_cage_interrupted    | NO   | NO          | 0.4s     | Short reactive flinch when hit mid-climb. Transitions to Cage_PulledDown. |
| Cage_Escape            | wrestler_cage_escape         | NO   | YES         | 1.2s     | Wrestler clears top of cage and drops to floor outside. Gravity-assisted landing. |
| Cage_PulledDown        | wrestler_cage_pulled_down    | NO   | YES         | 0.8s     | Opponent pulls climber off cage — wrestler falls to mat inside. Transitions to Grounded_FaceDown. |

---

## MATCH RESOLUTION

| State Name   | Clip Name              | Loop | Root Motion | Duration | Notes |
|---|---|---|---|---|---|
| Match_Win    | wrestler_win           | YES  | NO          | 3.0s+    | Victory celebration. Loops until scene transition. Per-wrestler variant strongly recommended. |
| Match_Loss   | wrestler_loss          | YES  | NO          | 3.0s+    | Defeated sell. Wrestler down or barely standing. Loops until scene transition. |
| Entrance_Walk | wrestler_entrance_walk | YES  | YES         | varies   | Ring entrance walk. Full arena lighting and music context. Can be extended with pose/taunt clips. |

---

## SOURCING NOTES

### Mixamo compatibility
All states above are compatible with Mixamo's humanoid rig if you retarget to a Unity Humanoid Avatar. Recommended Mixamo search terms per category:

- Grapple moves: search "suplex", "slam", "takedown", "DDT", "powerbomb"
- Strike moves: search "punch", "kick", "dropkick"
- Grounded/getup: search "knocked down", "get up", "floor"
- Submissions: search "choke hold", "ground and pound" (then modify)
- Cage climb: search "climbing wall", "ladder climb" (vertical root motion needed)
- Celebration/loss: search "victory", "defeated", "taunt"

### Root motion vs. in-place
- Root motion ON: any move where the attacker physically travels (grapple moves, Irish whip, finisher)
- Root motion OFF: any move where the attacker is stationary (standing strikes, grounded states, submission holds)
- Cage_Climb requires Y-axis root motion only — lock XZ to prevent drift along wall face

### Camera shake integration
Attach a camera shake event at the landing frame of:
  - StrongFront_0_BellyToBelly
  - StrongFront_1_Powerbomb
  - Finisher_Execute (landing frame)
This mirrors the observed ring-shake feedback on high-impact moves.

### Animation event hooks (suggested)
Add Unity Animation Events at these points:
  - Strike_Punch / Strike_Kick: "OnStrikeHitFrame" at impact frame → hitbox activation
  - All grapple moves: "OnGrappleMoveComplete" at clip end → calls WrestlerAnimationController.OnGrappleMoveComplete()
  - Sub_Hold: "OnSubmissionTick" every N frames → applies submission escape pressure
  - Finisher_Execute: "OnFinisherImpact" at landing frame → camera shake + crowd audio spike
  - Pinfall_Cover: "OnRefCount1", "OnRefCount2", "OnRefCount3" → count audio/visual sync

---
*Total unique clips required: 53*
*Looping clips: 15 | One-shot clips: 38*
