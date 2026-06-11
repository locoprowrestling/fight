# Gameplay Mechanics: Technical Breakdown

Source: Visual analysis of gameplay footage (approx. 5:09+) cross-referenced with full mechanical analysis from retrospective

HUD ARCHITECTURE
The on-screen interface during active matches consists of three persistent elements:

Two character health/momentum meters — positioned at the bottom of the screen, one per participant, labeled by wrestler identity. These meters serve a dual function: they track damage state AND build toward a special/finisher-enabling threshold.
Match timer — centered between the two meters. Functions as a match clock; its role in determining outcomes (time-limit draws vs. pinfall/submission) implies it is a win-condition variable, not merely cosmetic.
"SPECIAL" state indicator — a discrete displayed state that activates on-screen when the momentum meter reaches full charge. This is not a continuous value readout; it is a threshold-triggered flag that signals finisher availability.

No persistent stamina bar separate from the health/momentum meter is visible — the meter appears to encode both damage taken and offensive momentum in a single unified value.

THE MOMENTUM / SPECIAL SYSTEM
The footage documents the full lifecycle of the momentum system:

Meter charges through offensive output — landing strikes, executing grapple moves, and sustaining offensive sequences appear to accelerate meter fill
Threshold reached — the meter reaches full charge and the game transitions the character into SPECIAL state, displayed explicitly on the HUD
Finisher becomes executable — only in SPECIAL state can the character's designated finisher move be triggered; it is not available at any other point in the match
Finisher execution — once triggered, the finisher animation plays with enhanced visual feedback (the previously documented ring-shake effect on high-impact slams confirms the engine applies a distinct physics/camera response to finisher-tier moves)
Meter presumably resets or depletes post-finisher — the system incentivizes building momentum before going for the kill rather than hunting finishers immediately

This is a resource-gating system: the finisher is not mapped to a freely accessible input. It is locked behind a sustained offensive performance requirement. This creates a meta-layer where the in-match economy of momentum is a strategic resource, not just a damage accumulation bar.

GRAPPLE SYSTEM (MECHANICALLY PRECISE)
From the combined footage and retrospective transcript, the grapple system operates on a two-tier input model with directional move selection:
Tier 1 — Quick Grapple (tap input):

Initiates a weak-power grapple clinch
From this state, a directional press + button selects from a pool of 5 distinct moves executable from the front clinch position
A mirrored pool of 5 distinct moves is accessible from the rear clinch (behind opponent)
Total unique moves accessible via quick grapple: up to 10 (5 front + 5 back)

Tier 2 — Strong Grapple (held input):

Initiates a power-tier grapple clinch, visually distinct from the quick grapple
Same directional selection logic applies: 5 front + 5 back = up to 10 more distinct moves
Strong grapple moves are higher damage/higher impact than their quick-tier equivalents
The footage confirms the strong grapple tier produces visually weightier animations (belly-to-belly suplex documented)

Combined accessible grapple move pool per wrestler: up to 20 unique grapple moves from standing position alone, before factoring in positional variants (grounded opponent, corner position, etc.).
The grapple initiation itself is not auto-resolved — the opponent's reversal window is active during the initiation animation, meaning the grapple attempt can be countered before the move executes.

REVERSAL SYSTEM (MECHANICALLY PRECISE)
The reversal system is context-sensitive and attack-type-dependent. This is the most skill-differentiated system in the game:

The reversal input is not universal — a single button does not counter all incoming attacks
The player must correctly identify the incoming attack type (strike vs. grapple) and execute the corresponding reversal input
Incorrect read = failed reversal = attack lands = damage taken
Correct read + correct input = reversal executes = momentum swing

Observed in footage: multiple reversal exchanges occur in sequence, producing reversal chains — each successful reversal resets the attacker/defender state and immediately creates a new attack/reversal window. This means the reversal system can generate extended back-and-forth sequences that reward players who can maintain correct read accuracy across multiple consecutive frames.
Implications of this design:

The skill ceiling is defined by opponent-reading accuracy, not input execution speed
Two human players with equal mechanical skill but different opponent-reading ability will produce meaningfully different match outcomes
CPU opponents require the player to learn AI attack patterns — the system rewards game knowledge, not reflex alone

SUBMISSION SYSTEM
The Boston Crab documented in footage confirms a submission hold mechanic with the following observable properties:

Submission hold is initiated from a grapple state (not a strike) — requires achieving the appropriate grapple clinch first
Once applied, the hold is a sustained state — the game enters a distinct phase where the trapped opponent must escape
Rope break mechanic is implemented: if the defending wrestler's body contacts the ring ropes during a submission hold, the hold is broken by referee stoppage — this is a positional escape condition, not a button-mash escape condition
Rope breaks introduce ring geometry as a defensive resource: a wrestler near the ropes has a passive escape option that a wrestler in ring center does not

The rope break also implies the game tracks wrestler proximity to ring boundaries as a continuous variable, not just as an out-of-ring flag.

IRISH WHIP & RING MOVEMENT
Not directly documented in footage detail, but the match flow implies an Irish whip system is present (standard for the engine lineage). The predecessor games in the same engine family established: Irish whip to ropes → rebound → selectable strike or grapple on return. The slower match pace documented in the retrospective analysis is consistent with an engine where positioning and whip management are deliberate choices rather than incidental movement.

CAGE MATCH — WIN CONDITION SYSTEM
The cage match footage introduces a parallel win-condition architecture:
Standard match win conditions:

Pinfall (three-count)
Submission (tap-out)
Possible time-limit expiry

Cage match win condition:

Escape — the cage wall is a climbable interactive object; the first wrestler to clear the top of the cage and land on the floor outside wins
Pinfall may also be valid inside the cage (not fully confirmed from footage alone, but consistent with match type conventions)

Cage as environmental object:

The cage wall has climb state — wrestler can initiate a climb animation on the wall itself
Climb progress is interruptible — opponent can pursue and intercept a climbing wrestler
This creates a positional race dynamic layered on top of the standard combat system: offensive players must choose between continuing to damage the opponent vs. attempting escape; defensive players must choose between recovering vs. pursuing and pulling down a climbing opponent

The cage match therefore introduces a secondary resource (ring position relative to the wall) that runs parallel to the momentum/health economy of the base combat system.

MOVE IMPACT FEEDBACK SYSTEM
Across both match types, the engine deploys distinct feedback layers to communicate move weight:

Standard moves: normal animation with contextual sell from the receiving wrestler
Finisher/signature moves: enhanced animation + ring shake effect (camera/environment response to impact) — confirmed in retrospective footage of the Rock Bottom
Submission holds: distinct sustained animation state, separate from standard move flow
Aerial moves: available from elevated structures (ladder, cage wall top) — physics state for the receiving wrestler differs from standing-position receive

This tiered feedback system means the game communicates damage economy visually without the player needing to watch a number. The feel of a finisher landing is mechanically distinct from a standard suplex.

POST-MATCH SYSTEMS

Replay: a post-match replay of the finishing sequence is automatically shown — no player input required. This is a presentation layer reward for executing the match-ending move.
Result screen: displays match outcome with two navigation options — Quit (exit to menu) or Rematch (instant replay of same match configuration). The Rematch option reduces friction for iterative play and implicitly supports the two-player couch co-op use case.

SUMMARY: THE MECHANICAL STACK
LayerSystemKey PropertyDamageHealth/Momentum meterUnified — tracks damage AND special chargeOffenseGrapple tiers (quick/strong)20 accessible moves per wrestler, no combo memorizationDefenseContext-sensitive reversalsAttack-type read required — not universalFinisherMomentum-gated SPECIAL stateResource-locked, not freely accessibleSubmissionsHold → rope break escapeRing geometry is a defensive variableMatch varianceMultiple win conditionsCage escape, pinfall, submission, timeFeedbackTiered impact animationFinisher ≠ regular move, visually unambiguousMeta-loopPost-match Rematch optionLow friction, supports repeated sessions
