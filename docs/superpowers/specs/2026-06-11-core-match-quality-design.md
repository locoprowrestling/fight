# Core Match Quality Milestone Design

**Status:** Approved design

**Date:** 2026-06-11

**Audience:** Internal design and engineering team

## Objective

Improve the existing one-player-versus-CPU match loop through deeper defensive
reads, clearer resource stakes, positional submission play, wrestler-specific
AI behavior, and stronger presentation hierarchy.

The milestone applies research lessons where they fit this project. Research
and `examplecode/` remain informational sources, not implementation
specifications. Current repository architecture, tests, and tracked design
documentation remain authoritative.

## Selected Approach

Build one vertical match-quality slice across six connected areas:

1. Hybrid directional-read reversals.
2. Persistent SPECIAL readiness with visible health, stamina, and momentum.
3. Position-driven submission escapes.
4. Focused AI personality modifiers.
5. Distinct impact and outcome presentation.
6. Observable tuning and regression verification.

This approach extends current systems instead of replacing them. It provides a
coherent playable result while keeping each phase independently testable.

## Design Principles

- Preserve simple controls and create depth through context.
- Reward anticipation without making basic defense inaccessible.
- Treat position as a resource during submissions.
- Keep player and CPU actions on the same gameplay APIs.
- Keep difficulty and personality as separate concerns.
- Make move importance readable through presentation, not damage inflation.
- Keep gameplay timing and outcomes independent of animation playback.
- Prefer bounded data and pure resolvers over wrestler-specific branches.

## Architecture

The existing ownership model remains intact:

```text
PlayerInputController / CPUWrestlerAI
    -> intent and directional read
WrestlerCombat / SubmissionSystem / SpecialController
    -> validation, costs, outcomes, state transitions
WrestlerStatsRuntime / RingInteractionSystem / MatchRulesData
    -> meter, geometry, and rule authority
IAnimationDriver / FeelSystem / MatchHUD
    -> presentation of resolved outcomes
```

No input controller, AI controller, HUD component, animation driver, or
animation event may apply damage, award momentum, spend stamina, resolve a rope
break, or decide a match result.

## Directional-Read Reversals

### Player Grammar

The reversal action remains one button:

- Keyboard: Space.
- Controller: right bumper.
- Held movement direction supplies an optional read.
- Neutral input requests a basic reversal.
- Directional input requests a stronger counter result when it matches the
  move's authored counter direction.

The read uses the same camera-relative direction pipeline as movement and
grapple selection. Raw stick or keyboard axes are never compared directly with
wrestler-local axes.

### Data

`MoveData` gains reversal presentation and read metadata:

- Preferred counter direction: neutral, toward, away, left, or right.
- Whether a directional strong counter is allowed.
- Basic reversal momentum gain.
- Strong-counter momentum gain.
- Basic attacker stagger duration.
- Strong-counter attacker stagger duration.
- Basic and strong separation distances.
- Presentation identifiers for basic and strong counter outcomes.

Defaults preserve current behavior for existing move assets. Missing metadata
must produce a valid basic reversal, not make a move impossible to reverse.

### Resolution

A pure `ReversalReadResolver` receives:

- Authored preferred direction.
- Submitted direction.
- Whether the input passed the directional dead zone.
- Whether the move supports a strong counter.

It returns one of:

- `Basic`
- `Strong`

Timing, state permission, cooldown, and stamina remain owned by
`WrestlerCombat.TryReversal`.

Rules:

- Valid timing plus sufficient stamina always permits a basic reversal.
- A correct directional read upgrades the result to strong.
- Neutral or incorrect direction resolves as basic.
- Failed timing changes no wrestler state and spends no stamina.
- Failed timing still applies the existing human reversal cooldown.
- Insufficient stamina changes no wrestler state.
- Stamina is spent exactly once after all validation succeeds.
- Strong counters grant more momentum, longer attacker stagger, clearer
  separation, and stronger presentation than basic reversals.
- Reversal results do not automatically create another reversal window.
  Chains emerge only when the resulting valid states permit a new attack.

### CPU Reads

The CPU calls the same reversal API with a selected direction.

- Difficulty controls whether the CPU reacts successfully and how quickly.
- Personality may bias read choices but cannot increase reversal accuracy.
- Incorrect CPU directional reads resolve as basic reversals after the normal
  accuracy check.
- CPU reads must use controlled randomness that can be replaced with a
  deterministic source in tests.

## Momentum And SPECIAL Readiness

### Resource Rules

- Health, stamina, and momentum remain separate gameplay resources.
- All three bars remain persistently visible.
- Full momentum creates persistent SPECIAL readiness, not a temporary wrestler
  state.
- Readiness remains until a successful special spends momentum or an explicit
  authored effect changes it.
- Failed special validation spends no momentum or stamina.
- Successful activation spends each resource exactly once.
- Existing special-specific position, target, rule, stamina, and cooldown
  requirements remain authoritative.

### Presentation

When momentum first reaches full:

- The momentum bar changes to a distinct ready treatment.
- The HUD displays `SPECIAL READY`.
- A short presentation event signals the transition once.

The ready treatment remains visible while momentum stays full. It must not
retrigger every frame.

## Position-Driven Submissions

### Player Intent

While defending a submission:

- Movement input becomes crawl intent.
- Mash input continues to add active escape effort.
- The defender may combine crawl and active effort.
- Normal locomotion remains disabled until the hold releases.

### Crawl Resolution

`SubmissionSystem` owns crawl resolution and exposes an intent API shared by
human and CPU defenders.

Each update:

1. Find the nearest legal rope through `RingInteractionSystem`.
2. Convert submitted movement into a direction relative to that rope.
3. Calculate crawl strength from direction quality, defender stamina,
   submission resistance, buffs, and authored escape penalties.
4. Drain stamina for successful crawl effort.
5. Move the attacker-defender pair through scripted gameplay-root movement.
6. Clamp the pair to legal ring bounds.
7. Resolve rope contact through the existing rope-break rule.

Rules:

- Moving toward the nearest legal rope provides the strongest crawl.
- Sideways intent provides reduced progress.
- Moving away provides no crawl progress.
- Low stamina weakens crawl and active escape effort.
- The attacker remains attached to the defender while the pair moves.
- When rope breaks are active, legal rope contact immediately releases the
  hold.
- When rope breaks are disabled, rope contact does not release the hold.
- With rope breaks disabled, crawl intent still contributes reduced escape
  effort so movement is not a dead input.
- Attacker stamina drain remains a limit on indefinite holds.
- Center-ring submissions remain more dangerous than rope-adjacent holds.

### Release Invariants

Tap-out, active escape, rope break, cancellation, reset, and match end must all:

- Clear submission ownership.
- Stop scripted pair movement.
- Restore a valid match state.
- Restore valid attacker and defender wrestler states.
- Clear transient crawl intent.
- Emit one presentation outcome.

## Focused AI Personality

### Separation From Difficulty

`AIDifficultyData` continues to control:

- Reaction delay.
- Reversal accuracy.
- Dodge accuracy.
- Kickout and submission escape bonuses.

`AIPersonality` controls decision preference only. It must not grant hidden
accuracy, damage, stamina, health, or timing bonuses.

### Personality Profiles

A pure `AIPersonalityProfile` maps the existing personality enum to bounded
modifiers for:

- Aggression.
- Strike preference.
- Grapple preference.
- Power-move preference.
- Ground-offense preference.
- Submission preference.
- Pin urgency.
- Special setup preference.
- Rope and corner strategy.
- Risk tolerance.
- Breather frequency.
- Repetition tolerance.

The initial milestone uses fixed profiles in code rather than adding a new
editor or ScriptableObject asset family.

### Decision Rules

- Difficulty values provide the base decision envelope.
- Personality modifies contextual action weights inside bounded ranges.
- Tactically obvious opportunities may override personality: legal rope break,
  credible late pin, valid special setup, and active defensive reactions.
- Repeating an action family increases a temporary repetition penalty.
- Repetition memory distinguishes attempts from successful outcomes.
- Successful repeated actions receive a larger penalty than failed attempts.
- The CPU must always transition out of failed `Try*` actions and return control
  to `Decide`.
- F1 diagnostics expose personality, selected modifiers, recent actions, and
  the final selected action family.

## Impact And Outcome Presentation

### Event Model

Extend the presentation channel with semantic outcomes:

- Light impact.
- Heavy impact.
- Basic reversal.
- Strong reversal.
- Special impact.
- Submission escape.
- Rope break.
- Tap-out.
- SPECIAL became ready.

`FeelSystem` and `IAnimationDriver` consume these outcomes after gameplay
resolution.

### Presentation Hierarchy

- Light impacts remain brief and restrained.
- Heavy impacts receive stronger hit-stop and camera impulse.
- Basic reversals read as quick defensive escapes.
- Strong reversals receive a distinct pose, stronger camera impulse, and brief
  hit-stop.
- Submission escape and rope break emphasize release and separation without a
  heavy slam response.
- Specials receive the strongest impact treatment.
- Disabling `FeelSystem` leaves damage, timing, meters, positions, states, and
  match results unchanged.

## Animation Reference Integration

The files under `examplecode/` are conceptual reference material.

Useful concepts:

- A single Animator-facing adapter.
- A tracked clip manifest with state, loop, duration, root-motion, and purpose
  metadata.
- Distinct basic and strong reversal presentation.
- Separate submission apply, hold, struggle, rope-break, and tap-out
  presentation.
- Explicit SPECIAL-ready presentation.
- Paired attacker and defender animation contracts for grapples and specials.
- Presentation markers at authored contact and impact moments.

Rejected ownership patterns:

- Animation code owning momentum, stamina, damage, or match state.
- Animation events deciding whether a gameplay hit occurs.
- Rope collision inside animation code resolving a submission.
- One hardcoded Animator state per move slot.
- Root motion controlling authoritative wrestler position.
- Separate strike and grapple reversal inputs.

### Driver Contract

`IAnimationDriver` gains semantic presentation methods for:

- Basic reversal.
- Strong reversal.
- SPECIAL readiness on and off.
- Submission application.
- Submission struggle.
- Submission rope break.
- Submission escape.
- Submission tap-out.

The placeholder driver implements these with procedural poses and color
feedback. A future `AnimatorAnimationDriver` maps them to Animator parameters
without changing gameplay systems.

### Animation Manifest

Create a tracked project-specific animation manifest that documents:

- Gameplay state or semantic event.
- Attacker and defender roles.
- Loop behavior.
- Expected authored duration.
- In-place or visual-root-only movement requirements.
- Presentation markers for audio, VFX, and camera synchronization.
- Required exit and interruption behavior.

Animation markers may synchronize presentation but cannot apply gameplay
effects. Authoritative timing remains in `MoveData`, `SpecialAbilityData`, and
the owning gameplay systems.

This milestone continues using in-place animation and scripted gameplay-root
movement. Authoritative root motion is deferred.

## Failure Handling

- Missing reversal metadata falls back to a basic reversal.
- Invalid reversal direction never blocks a valid basic reversal.
- Invalid submission crawl intent produces no movement or stamina drain.
- Missing rope geometry disables crawl movement safely and records a diagnostic.
- Missing personality data falls back to `Balanced`.
- Missing presentation handlers become no-ops and never block gameplay.
- Every rejected player action has an actionable HUD or F1 diagnostic when
  appropriate.
- Every failed CPU action leaves a valid state and schedules a new decision.

## Implementation Phases

### Phase 1: Reversal Foundation

- Add reversal read data and pure resolution.
- Add edit-mode tests for neutral, correct, incorrect, late, and unaffordable
  attempts.
- Migrate existing move defaults without changing current reversal legality.

### Phase 2: Player, CPU, And Presentation Integration

- Pass camera-relative direction from player input.
- Add personality-aware CPU read selection.
- Add basic and strong reversal outcomes.
- Add HUD and F1 reversal diagnostics.
- Add semantic reversal presentation.

### Phase 3: Persistent SPECIAL Readiness

- Add transition detection for reaching and leaving full momentum.
- Add persistent momentum-bar treatment and one-shot ready feedback.
- Verify special validation and exact-once spending.

### Phase 4: Position-Driven Submissions

- Add shared crawl and effort intent.
- Add paired scripted movement toward legal ropes.
- Add stamina-sensitive escape calculations.
- Add release-path cleanup and diagnostics.
- Add player and CPU submission behavior.

### Phase 5: Focused AI Personality

- Add pure personality profiles.
- Apply bounded modifiers to existing decisions.
- Expand action memory with attempt and success history.
- Add personality diagnostics and deterministic tests.

### Phase 6: Presentation Contract And Tuning

- Extend `IAnimationDriver` and `FeelSystem` semantic outcomes.
- Update the procedural placeholder implementation.
- Add the project animation manifest.
- Tune authored reversal, stamina, momentum, submission, and personality data.
- Run the complete regression matrix.

## Automated Verification

- Correct directional read resolves strong.
- Neutral and incorrect directional reads resolve basic.
- Camera-relative reads remain consistent across multiple camera yaw values.
- Late and invalid reversal attempts spend no stamina.
- Valid reversals spend stamina once.
- Missing move metadata preserves basic reversal behavior.
- Full momentum remains ready until explicitly spent.
- Failed special validation spends no resources.
- Successful special activation spends each resource once.
- Submission movement toward the nearest rope exceeds sideways movement.
- Movement away from the rope produces no crawl.
- Low stamina reduces crawl and active escape contribution.
- Rope contact releases only when rope breaks are active.
- Every submission exit clears ownership and scripted movement.
- Personality modifiers remain inside defined bounds.
- Personality changes preference without changing reaction accuracy.
- Repetition memory penalizes repeated successful families.
- Disabling presentation systems leaves resolved gameplay snapshots unchanged.

## Play-Mode Verification

- Directional reversal reads are understandable without frame-perfect input.
- Strong counters are visibly and positionally distinct from basic reversals.
- Reversal attempts never leave either wrestler in a stale grapple role.
- Health, stamina, and momentum remain readable.
- SPECIAL readiness persists until a successful activation.
- Center-ring submissions feel dangerous.
- Rope proximity creates a meaningful defensive advantage.
- Crawl input visibly moves the pair without separating them.
- CPU wrestlers show recognizable preferences without becoming deterministic.
- Light, heavy, reversal, submission-release, and special outcomes remain
  visually distinct.
- Pins, kickouts, contextual offense, rope offense, specials, pause, reset, and
  match completion do not regress.
- A no-feel-system run produces identical gameplay outcomes.

## Milestone Exit Criteria

- One-button directional reversals support basic and strong outcomes.
- Player and CPU use the same reversal resolution path.
- SPECIAL readiness is persistent, visible, and spends resources exactly once.
- Submission defense combines position, stamina, and active effort.
- Existing wrestler personalities visibly affect CPU decisions.
- Presentation communicates outcome hierarchy without owning gameplay.
- Project-specific animation requirements are documented.
- Automated tests and Unity play-mode scenarios pass.
- Documentation and generated data remain synchronized with code defaults.

## Deferred Scope

- Separate strike and grapple reversal buttons.
- Automatic counter-chain windows.
- Full behavior trees or an AI profile editor.
- Match-phase AI tables.
- Limb-specific health and submission targeting.
- New animation or audio asset production.
- Authoritative root motion.
- Replay and post-match result flow.
- Story, persistent injury, CAW, and economy systems.
- Cage, ladder, rumble, backstage, and additional match types.

## Sources

- `research/gameplay_analysis.md`: momentum gating, reversal reads, positional
  rope breaks, and impact hierarchy.
- `research/wrestling_game_analysis.md`: input legibility, contextual depth,
  presentation importance, and technical stability.
- `research/aki_wrestling_games_development_modding_research_ingestion_pack.md`:
  weighted AI context, consistent control grammar, reversible momentum, and
  wrestler differentiation.
- `research/classic_grappling_game_modding_research_monolithic.md`: move
  compatibility, reversal metadata, AI profiles, and system modularity.
- `research/pro_wrestling_battle_system_monolithic_research.md`: modular
  reversal, momentum, stamina, submission, and AI guidance.
- `examplecode/WrestlerAnimationController.cs`,
  `examplecode/WrestlerAnimatorBuilder.cs`, and
  `examplecode/WrestlerAnimationManifest.md`: conceptual animation and
  presentation contracts only.
- `Documentation/KnowledgeBase/BestPractices.md`: authoritative repository
  ownership and control-symmetry rules.
- `Documentation/FutureAssetIntegration.md`: `IAnimationDriver` boundary and
  future Animator integration.
- `docs/superpowers/specs/2026-06-11-game-feel-design.md`: existing
  presentation-only feel architecture.
