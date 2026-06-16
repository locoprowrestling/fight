# WRESTLER MOVE LIBRARY — FRONT QUICK GRAPPLE CATEGORY
# Source: Move-list menu showcase video, 13:34–end
# Category confirmed: Front Weak (Quick) Grapple — standing, facing opponent
# Total moves observed: 23
#
# These are all moves available in the FRONT QUICK GRAPPLE pool.
# Any 5 can be assigned to a wrestler's quickFront[0–4] slots in a WrestlerMoveSet.
# Each move below should become one WrestlerMove ScriptableObject asset.
#
# WrestlerMove fields reference:
#   moveName                — paste exactly as shown
#   position                — MovePosition.FrontStanding (all of these)
#   moveClass               — MoveClass.QuickGrapple (all of these)
#   requiresFacingFront     — true (all of these)
#   causesKnockdown         — see per-move
#   opponentLandOrientation — see per-move
#   hasRootMotion           — see per-move
#   momentumGain            — suggested 0–1 value
#   triggerCameraShake      — see per-move
#   mixamoSearchTerms       — see per-move

---

## MOVE 01 — Club to Neck

| Field | Value |
|---|---|
| moveName | Club to Neck |
| causesKnockdown | true |
| opponentLandOrientation | FaceDown |
| hasRootMotion | true |
| clipDuration | ~0.9s |
| momentumGain | 0.12 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker grabs the opponent's left arm with their own left hand, controlling the opponent's balance. Simultaneously pivots to the side and delivers a downward right forearm strike to the back of the opponent's neck/upper trapezius. The combination of arm control and neck impact drives the opponent forward and down, landing prone (face-down).

**Landing orientation:** FACE-DOWN — sets up rear ground grapples and Boston Crab.

**Mixamo search:** "forearm smash", "neck club", "hammer blow"

---

## MOVE 02 — Elbow Strike

| Field | Value |
|---|---|
| moveName | Elbow Strike |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | false |
| clipDuration | ~0.45s |
| momentumGain | 0.10 |
| triggerCameraShake | false |

**Body Mechanics:**
From standing front grapple clinch, attacker delivers a sharp right elbow strike to the opponent's head or neck area. Short, compact motion — attacker does not travel. Opponent snaps backward and lands supine (face-up).

**Landing orientation:** FACE-UP — immediately pinnable, sets up front ground grapples.

**Mixamo search:** "elbow strike", "elbow smash", "combat elbow"

---

## MOVE 03 — Snapmare

| Field | Value |
|---|---|
| moveName | Snapmare |
| causesKnockdown | true |
| opponentLandOrientation | Seated |
| hasRootMotion | true |
| clipDuration | ~0.9s |
| momentumGain | 0.11 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker grabs behind the opponent's head/neck with both hands. Steps back and to the side while pulling, using leverage to flip the opponent forward over the attacker's shoulder. Opponent performs a forward roll and comes to rest in a seated position facing away from the attacker.

**Landing orientation:** SEATED — unique vulnerable state. Not pinnable until opponent lies flat.
Typically followed immediately by a grounded strike to the back of the head.

**Mixamo search:** "snapmare", "forward flip throw", "neck throw"

---

## MOVE 04 — One Hand Scoop Slam

| Field | Value |
|---|---|
| moveName | One Hand Scoop Slam |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | true |
| clipDuration | ~1.2s |
| momentumGain | 0.14 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker reaches their right arm between the opponent's legs from the front, hooks the inner thigh, and lifts the opponent horizontally across the body. With the opponent suspended back-first, attacker brings them down forcefully to the mat on their back. One-armed variant — more dynamic than standard Scoop Slam (see Move 15).

**Landing orientation:** FACE-UP.

**Mixamo search:** "scoop slam", "bodyslam", "power slam"

---

## MOVE 05 — Knee Sweep

| Field | Value |
|---|---|
| moveName | Knee Sweep |
| causesKnockdown | true |
| opponentLandOrientation | FaceDown |
| hasRootMotion | false |
| clipDuration | ~0.6s |
| momentumGain | 0.10 |
| triggerCameraShake | false |

**Body Mechanics:**
From front clinch, attacker delivers a sharp kick or sweep with the right foot to the back of the opponent's left knee. The knee buckles and the opponent pitches forward, landing prone (face-down). Attacker stays upright.

**Landing orientation:** FACE-DOWN — rear ground grapple setup.

**Mixamo search:** "leg sweep", "knee kick", "sweep takedown"

---

## MOVE 06 — Head Butt 01

| Field | Value |
|---|---|
| moveName | Head Butt 01 |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | false |
| clipDuration | ~0.45s |
| momentumGain | 0.10 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker grips the opponent's head with both hands. Pulls the opponent's face toward the attacker's forehead and delivers a swift headbutt impact to the opponent's face. Compact, explosive motion. Opponent staggers and falls backward, landing supine.

**Landing orientation:** FACE-UP.

**Mixamo search:** "headbutt", "head smash"

---

## MOVE 07 — Head Butt 03

| Field | Value |
|---|---|
| moveName | Head Butt 03 |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | false |
| clipDuration | ~0.55s |
| momentumGain | 0.11 |
| triggerCameraShake | false |

**Body Mechanics:**
Variant of Move 06. Attacker uses both hands to pull the opponent's head/upper body forward toward them, creating a collision between foreheads rather than pulling opponent into attacker's head. Slightly longer setup — the pull-and-deliver rhythm is distinct from 01. Opponent falls backward.

**Landing orientation:** FACE-UP.

**Mixamo search:** "headbutt forward", "mutual headbutt"

---

## MOVE 08 — Eye Rake

| Field | Value |
|---|---|
| moveName | Eye Rake |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | false |
| clipDuration | ~0.5s |
| momentumGain | 0.09 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker raises their right hand and drags the fingers horizontally across the opponent's eyes. Opponent's hands instinctively rise to their face. Attacker then shoves or pushes the blinded opponent backward, sending them to the mat on their back. Low-damage heel move.

**Landing orientation:** FACE-UP.

**Mixamo search:** "eye rake", "face rake", "face push"

---

## MOVE 09 — European Uppercut

| Field | Value |
|---|---|
| moveName | European Uppercut |
| causesKnockdown | false |
| opponentLandOrientation | StillStanding |
| hasRootMotion | false |
| clipDuration | ~0.40s |
| momentumGain | 0.09 |
| triggerCameraShake | false |

**Body Mechanics:**
From grapple clinch, attacker pulls the opponent in and delivers a sharp, horizontal forearm smash (European-style uppercut) to the opponent's jaw/chin with the right forearm. Opponent's head snaps back. Move ends with opponent staggered but upright — no knockdown.

**Landing orientation:** STILL STANDING — opponent remains in staggered idle, can be chained.

**Note:** One of the few Quick Front moves that does NOT cause a full knockdown. Keep opponent state flag correct — IsGrounded should NOT be set true.

**Mixamo search:** "uppercut forearm", "European uppercut", "forearm smash standing"

---

## MOVE 10 — European Uppercut Spin

| Field | Value |
|---|---|
| moveName | European Uppercut Spin |
| causesKnockdown | false |
| opponentLandOrientation | StillStanding |
| hasRootMotion | true |
| clipDuration | ~0.70s |
| momentumGain | 0.11 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker grabs the opponent, executes a full 360-degree spinning rotation (clockwise), building rotational momentum, then delivers the European forearm uppercut to the jaw on the exit of the spin. More theatrical than Move 09 — the spin provides visual commitment. Opponent staggers but stays standing.

**Landing orientation:** STILL STANDING.
Root motion needed for the spin rotation arc.

**Mixamo search:** "spinning uppercut", "360 forearm", "spin strike"

---

## MOVE 11 — Elbow to Back of Head

| Field | Value |
|---|---|
| moveName | Elbow to Back of Head |
| causesKnockdown | true |
| opponentLandOrientation | FaceDown |
| hasRootMotion | false |
| clipDuration | ~0.5s |
| momentumGain | 0.11 |
| triggerCameraShake | false |

**Body Mechanics:**
From front clinch, attacker forces the opponent's head down (pushes down on back of neck/head), then drives a right elbow strike downward into the back of the opponent's head. The double motion — force the head down, then strike it — creates a neck-whip effect. Opponent pitches forward and lands prone (face-down).

**Landing orientation:** FACE-DOWN — note the difference from standard Elbow Strike (Move 02) which leaves opponent face-up. This move specifically sets up rear ground grapples.

**Mixamo search:** "elbow to back of head", "back of neck strike", "rabbit punch"

---

## MOVE 12 — Double Leg Takedown

| Field | Value |
|---|---|
| moveName | Double Leg Takedown |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | true |
| clipDuration | ~0.9s |
| momentumGain | 0.13 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker drops their level sharply — shoots forward and low. Both arms wrap around the back of the opponent's knees simultaneously. Attacker drives forward with legs, lifting the opponent off their feet. Opponent falls backward and lands flat on their back. A legitimate wrestling/MMA takedown.

**Landing orientation:** FACE-UP.
Root motion needed for the forward shot.

**Mixamo search:** "double leg takedown", "wrestling shoot", "leg sweep tackle"

---

## MOVE 13 — Chop 04

| Field | Value |
|---|---|
| moveName | Chop 04 |
| causesKnockdown | false |
| opponentLandOrientation | StillStanding |
| hasRootMotion | false |
| clipDuration | ~0.40s |
| momentumGain | 0.08 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker delivers an overhand right chop — arm raised above shoulder height and brought down in a chopping arc — striking the opponent's chest or shoulder with an open palm. Crowd-pop move. Opponent staggers backward but stays standing.

**Landing orientation:** STILL STANDING — typically chained into another chop or strike.

**Mixamo search:** "chop chest", "open hand strike", "karate chop"

---

## MOVE 14 — Chop 02

| Field | Value |
|---|---|
| moveName | Chop 02 |
| causesKnockdown | false |
| opponentLandOrientation | StillStanding |
| hasRootMotion | false |
| clipDuration | ~0.38s |
| momentumGain | 0.08 |
| triggerCameraShake | false |

**Body Mechanics:**
Backhand chop variant. Attacker delivers a right-to-left backhand chop to the opponent's chest, striking with the back of the hand/wrist. Distinct wrist and forearm motion from Chop 04's overhand arc. Opponent staggers but remains standing.

**Landing orientation:** STILL STANDING.

**Mixamo search:** "backhand chop", "backhand slap chest", "reverse chop"

---

## MOVE 15 — Scoop Slam

| Field | Value |
|---|---|
| moveName | Scoop Slam |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | true |
| clipDuration | ~1.3s |
| momentumGain | 0.15 |
| triggerCameraShake | false |

**Body Mechanics:**
Standard two-arm scoop slam. Attacker places the right arm between the opponent's legs (under the waist), left arm around the opponent's neck or upper back. Lifts the opponent horizontally so opponent is parallel to the mat, suspended across the attacker's body. Rotates and drives opponent down back-first. Classic, high-visual-impact move.

**Landing orientation:** FACE-UP.
Root motion for lift trajectory.

**Mixamo search:** "scoop slam", "bodyslam", "slam"

---

## MOVE 16 — Throat Thrust

| Field | Value |
|---|---|
| moveName | Throat Thrust |
| causesKnockdown | false |
| opponentLandOrientation | StillStanding |
| hasRootMotion | false |
| clipDuration | ~0.32s |
| momentumGain | 0.08 |
| triggerCameraShake | false |

**Body Mechanics:**
Fastest move in this category. Attacker delivers a single, straight, stiff-finger or palm-edge thrust directly to the opponent's throat. No setup — immediate impact. Opponent clutches throat and staggers backward but remains standing. Very short clip.

**Landing orientation:** STILL STANDING.

**Mixamo search:** "throat strike", "throat chop", "larynx punch"

---

## MOVE 17 — Headlock Takedown

| Field | Value |
|---|---|
| moveName | Headlock Takedown |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | true |
| clipDuration | ~0.85s |
| momentumGain | 0.12 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker applies a side headlock with the right arm (opponent's head clamped under attacker's right armpit). Uses a sharp hip-twist/pivot to throw the opponent to the mat, controlling the head through the entire motion. Opponent lands on their back with head still briefly controlled.

**Landing orientation:** FACE-UP.
Root motion for the pivoting throw arc.

**Mixamo search:** "headlock takedown", "side headlock throw", "bulldog takedown"

---

## MOVE 18 — Hip Toss

| Field | Value |
|---|---|
| moveName | Hip Toss |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | true |
| clipDuration | ~0.90s |
| momentumGain | 0.13 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker hooks the opponent's right arm with their left arm, steps in close (hip-to-hip contact), and uses hip extension + arm pull to throw the opponent over the attacker's hip in a wide arc. Opponent flips and lands on their back. Clean, fundamental throw with significant airtime.

**Landing orientation:** FACE-UP.
Root motion for the step-in and throw arc.

**Mixamo search:** "hip toss", "judo hip throw", "ogoshi"

---

## MOVE 19 — Jawbreaker

| Field | Value |
|---|---|
| moveName | Jawbreaker |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | false |
| clipDuration | ~0.80s |
| momentumGain | 0.12 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker grabs the opponent's head with both hands. Drops suddenly to a seated or crouching position, pulling the opponent's face or jaw downward onto the top of the attacker's head or shoulder. The abrupt downward jerk creates whiplash in the opponent's jaw. Opponent bounces backward and falls supine.

**Landing orientation:** FACE-UP.
No significant travel — attacker drops in place.

**Mixamo search:** "jawbreaker", "chin buster", "jaw drop"

---

## MOVE 20 — Head Scissor Takedown 02

| Field | Value |
|---|---|
| moveName | Head Scissor Takedown 02 |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | true |
| clipDuration | ~1.1s |
| momentumGain | 0.14 |
| triggerCameraShake | false |

**Body Mechanics:**
Highest aerial content of the Front Quick Grapple category. Attacker jumps up, wraps both legs around the opponent's neck/head (scissoring), and uses the momentum of the jump combined with a hip twist to spin and throw the opponent. Opponent flips and lands on their back. This is the Hurricanrana/Head Scissor archetype.

**Landing orientation:** FACE-UP.
Root motion needed for jump trajectory. Attacker also lands and should have a follow-through to feet.

**Note:** Despite being in the Quick Grapple category, this is the most mechanically complex move in the pool. The attacker becomes briefly airborne.

**Mixamo search:** "hurricanrana", "head scissors takedown", "frankensteiner", "head scissor flip"

---

## MOVE 21 — Falling Powerslam

| Field | Value |
|---|---|
| moveName | Falling Powerslam |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | true |
| clipDuration | ~1.4s |
| momentumGain | 0.16 |
| triggerCameraShake | true |
| cameraShakeIntensity | 0.35 |

**Body Mechanics:**
Heaviest move in this category — borderline strong-grapple weight. Attacker scoops the opponent up horizontally (similar to Scoop Slam, Move 15), but instead of slamming them down while upright, the attacker falls forward with the opponent, driving them into the mat with the full weight of both bodies. Attacker lands on top of the opponent briefly.

**Landing orientation:** FACE-UP.
Camera shake warranted — this is the biggest impact in the Quick Front pool.
Root motion for the forward fall arc.

**Mixamo search:** "falling powerslam", "running powerslam", "running slam"

---

## MOVE 22 — Gordbuster 02

| Field | Value |
|---|---|
| moveName | Gordbuster 02 |
| causesKnockdown | true |
| opponentLandOrientation | FaceDown |
| hasRootMotion | true |
| clipDuration | ~1.4s |
| momentumGain | 0.15 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker lifts the opponent into a vertical suplex position — opponent is inverted, head pointing toward the mat. However, instead of completing the suplex and falling backward, attacker falls forward, dropping the opponent face-first/chest-first across the mat or top rope. The opponent's face/sternum absorbs the impact.

**Landing orientation:** FACE-DOWN — one of the more unusual landing orientations in this category (most Front Quick moves land face-up).
Sets up rear ground grapples.
Root motion for the forward drop.

**Mixamo search:** "gory neckbreaker", "front suplex drop", "face-first drop"

---

## MOVE 23 — Overhand Punch

| Field | Value |
|---|---|
| moveName | Overhand Punch |
| causesKnockdown | true |
| opponentLandOrientation | FaceUp |
| hasRootMotion | false |
| clipDuration | ~0.45s |
| momentumGain | 0.10 |
| triggerCameraShake | false |

**Body Mechanics:**
Attacker throws a looping overhand right punch to the opponent's head — wide arc from above shoulder height, connecting with the top or side of the skull. More powerful than the Elbow Strike (Move 02) but slightly slower. Opponent staggers and falls backward to the mat, landing face-up.

**Landing orientation:** FACE-UP.

**Mixamo search:** "overhand punch", "looping punch", "haymaker"

---

## QUICK REFERENCE — LANDING ORIENTATION SUMMARY

| Orientation | Moves |
|---|---|
| **Face-Up** (pinnable, front ground grapples) | 02, 03*, 04, 06, 07, 08, 12, 15, 17, 18, 19, 20, 21, 23 |
| **Face-Down** (rear ground grapples, Boston Crab) | 01, 05, 11, 22 |
| **Seated** (unique; Snapmare only) | 03 |
| **Still Standing** (no knockdown; chain moves) | 09, 10, 13, 14, 16 |

*Move 03 (Snapmare) uses Seated orientation — not strictly Face-Up but not Face-Down either.

## QUICK REFERENCE — CAMERA SHAKE

Only Move 21 (Falling Powerslam) triggers camera shake at `intensity = 0.35`.
All other Front Quick moves are light enough that camera shake would read as excessive.

## QUICK REFERENCE — ROOT MOTION REQUIRED

YES: 01, 03, 04, 10, 12, 15, 17, 18, 20, 21, 22
NO:  02, 05, 06, 07, 08, 09, 11, 13, 14, 16, 19, 23

## SUGGESTED SLOT ASSIGNMENTS — EXAMPLE BUILDOUT

A balanced quick-front loadout for a power/technical hybrid wrestler:

| Slot | Move | Why |
|---|---|---|
| quickFront[0] (neutral) | Hip Toss (18) | High-value, safe opener from neutral |
| quickFront[1] (up)      | Snapmare (03) | Sets up grounded follow-up from front |
| quickFront[2] (down)    | Club to Neck (01) | Creates face-down for Boston Crab chain |
| quickFront[3] (left)    | Scoop Slam (15) | Standard impact move |
| quickFront[4] (right)   | Falling Powerslam (21) | Highest-impact quick move, camera shake |

---

*23 moves documented. All confirmed from direct video analysis of Front Weak Grapple menu, 13:34+.*
*Rear Quick, Front Strong, Rear Strong, and aerial categories to be populated from additional video sourcing.*
