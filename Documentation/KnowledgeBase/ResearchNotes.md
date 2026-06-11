# Informational Battle-System Research Notes

> **Status: informational only.** This page is not an implementation guide,
> specification, roadmap, backlog, acceptance criterion, or source of truth.
> It may inform project practices, but findings become authoritative only after
> they are validated against this game and explicitly promoted into the
> appropriate project documentation.

This is a short index of topics discussed in:

- [`research/pro_wrestling_battle_system_monolithic_research.md`](../../research/pro_wrestling_battle_system_monolithic_research.md)
- [`research/classic_grappling_game_modding_research_monolithic.md`](../../research/classic_grappling_game_modding_research_monolithic.md)

The raw documents include external references, speculative architecture,
example schemas, pseudocode, and generic build prompts. Those materials were
not written against the final repository state and may be incomplete,
incompatible, or obsolete.

## Topics covered

The research examines:

- wrestling combat as a combination of state, position, pacing, reversals,
  stamina, momentum, rules, and match drama
- timing-based grapple contests and tiered move escalation
- data-oriented move definitions, tags, traits, and rulesets
- pins and submissions as multi-step interactions
- rope, corner, apron, and outside-ring contexts
- finite-state machines, hierarchical state models, and behavior-tree concepts
- weighted CPU behavior and possible match-phase concepts
- crowd response, gameplay events, and broader match stipulations
- possible implementation layering and module boundaries
- context-specific move slots and moveset editing
- move compatibility contracts, graceful failure, and animation composition
- custom-wrestler identity, appearance, attire, moveset, and AI boundaries
- editor validation, debug inspection, and move-sandbox concepts

These are research subjects, not requirements.

## Relationship to the repository

Some research vocabulary resembles existing systems such as
`WrestlerStateMachine`, `MoveData`, `MatchRulesData`, `CPUWrestlerAI`, and
`RingInteractionSystem`. That resemblance is descriptive only:

- It does not prove that the research caused or governs the implementation.
- It does not require the current code to expand toward the research examples.
- It does not make unimplemented topics planned features.
- It does not override current code, design docs, tests, or established
  KnowledgeBase practices.

Several broad findings have been evaluated and adopted because they match the
current implementation:

- human and CPU controllers use the same `WrestlerCombat` action surface
- state, ring geometry, match rules, and presentation retain separate owners
- moves, traits, specials, rules, and AI tuning carry reusable variation
- move verification includes resulting states and positional interactions
- move selection remains divided into contextual categories rather than one
  universal list
- constrained moves declare compatibility requirements and a defined failure
  or fallback path
- gameplay data and combat routines own move effects and timing; animation
  drivers only present them
- roster presentation (`RosterEntry`) remains separate from reusable combat
  configuration (`WrestlerDefinition`)
- hidden combat state remains inspectable through structured logs and the F1
  `DebugOverlay`

The authoritative wording for these adopted findings is in
[BestPractices.md](BestPractices.md#combat-architecture), not here.

For current behavior and accepted project conventions, use:

- [DesignDoc.md](../DesignDoc.md)
- [BestPractices.md](BestPractices.md)
- [Templates.md](Templates.md)
- [Examples.md](Examples.md)
- [TestingChecklist.md](../TestingChecklist.md)
- the runtime code under `Assets/Scripts/`

## How to use these notes

The research may be used to identify practices, understand terminology,
compare alternatives, or start a future design discussion. Before adopting any
idea:

1. Re-evaluate it against the current code and project goals.
2. Decide whether it solves a real problem in this game without fighting the
   existing architecture.
3. Record the accepted practice in the appropriate authoritative doc with
   links to the code or behavior that supports it.
4. Define implementation and verification independently of the research text.

Do not cite the research alone as justification that a feature should be built
or that an existing system should be refactored.

## Source categories

The raw document references:

- Fire Pro Wrestling grappling and CPU-logic material
- WWE 2K patch notes
- general combat-design articles
- Game Programming Patterns material on state
- Unity ScriptableObject architecture articles
- behavior-tree research papers
- public move-list, custom-wrestler, moveset-editing, animation-splicing, and
  modding-community material

These links support the research discussion only. They are not project
requirements or endorsements of a specific architecture.
