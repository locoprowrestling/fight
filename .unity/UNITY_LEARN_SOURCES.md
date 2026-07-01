# Relevant Unity Learn Sources

Accessed: 2026-06-10

This catalog is intentionally selective. It covers the systems present in the
LoCo Fight Game and omits unrelated 2D, XR, multiplayer, monetization, and
services material.

## Core Learning Paths

### Unity Essentials

Source: https://learn.unity.com/pathway/unity-essentials

Use for Editor navigation, GameObjects/components, scenes, transforms, cameras,
materials, lighting, physics fundamentals, audio, publishing, and basic project
workflow. Useful baseline for anyone modifying the generated prototype scene.

### Junior Programmer

Source: https://learn.unity.com/pathway/junior-programmer

Use for C# scripting, MonoBehaviour lifecycle, object-oriented organization,
data persistence, debugging, optimization, and project architecture. This is
the most broadly relevant pathway for the current codebase.

### Creative Core

Source: https://learn.unity.com/pathway/creative-core

Use selectively for animation, cameras, lighting, VFX, audio, UI, and
post-processing when replacing placeholder presentation.

## Project Architecture and Data

### Introduction to ScriptableObjects

Source: https://learn.unity.com/tutorial/introduction-to-scriptable-objects

Relevant to every data asset type in this project. Apply the distinction between
shared authored data and per-match mutable state.

### Implement data persistence between scenes

Source: https://learn.unity.com/tutorial/implement-data-persistence-between-scenes

Relevant to `MatchManager` reset behavior, roster selection persistence, and
future transitions between menus, character select, and matches. Do not add
persistence to combat runtime objects unless the design requires it.

## C# Gameplay

### Create with Code

Source: https://learn.unity.com/course/create-with-code

Use its units on player control, basic gameplay, sound/effects, mechanics, and
UI as a practical introduction. Patterns are introductory; preserve this
project's stronger separation of data, runtime systems, and presentation.

### Design patterns in Unity 6

Source: https://learn.unity.com/course/design-patterns-unity-6/tutorial/build-a-modular-codebase-with-mvc-and-mvp-programming-patterns

Relevant to the existing presentation boundary, special executor subclasses,
shared combat APIs, and future menu architecture. Patterns are tools, not a
reason to rebuild working code.

## Physics and Interaction

### Basic gameplay collisions

Source: https://learn.unity.com/pathway/junior-programmer/unit/gameplay-mechanics/tutorial/lab-4-basic-gameplay?version=6.0

Use for Rigidbody, collider, physics material, collision, and trigger concepts.
Relevant to rope, corner, rope-break, and aerial launch zones. Layer and tag
assumptions should remain explicit.

### Trigger callback example

Source: https://learn.unity.com/pathway/junior-programmer/unit/user-interface/tutorial/lesson-5-1-clicky-mouse-1?version=6.0

Contains a current Unity 6 example of `OnTriggerEnter` setup and requirements.

## Animation and Character Presentation

### Animator component

Source: https://learn.unity.com/course/introduction-to-3d-animation-systems/unit/the-animator/tutorial/3-1-introduction-to-the-animator-component?version=2019.4

Use when replacing `PlaceholderAnimationDriver` with imported rigs and Animator
Controllers. Relevant to mapping gameplay states to presentation without moving
gameplay authority into the Animator.

### Blend Trees

Source: https://learn.unity.com/course/introduction-to-3d-animation-systems/unit/the-animator/tutorial/3-4-creating-and-configuring-blend-trees?version=2019.4

Relevant to idle/walk/run and directional locomotion. Coordinate carefully with
`WrestlerMotor` and root-motion ownership.

### Animation scripting

Source: https://learn.unity.com/course/introduction-to-animation-scripting/tutorial/4-1-introduction-to-general-animation-scripting?version=2019.4

Relevant to driving Animator parameters from `IAnimationDriver`. This is an
older lesson, so verify exact Unity 6 APIs.

## AI and Movement

### AI Navigation

Source: https://learn.unity.com/course/roll-a-ball/tutorial/adding-ai-navigation?version=6.0

Relevant because `com.unity.ai.navigation` is installed. Best suited to future
larger spaces; covers NavMesh surfaces, agents, static obstacles, and dynamic
obstacles. The ring AI currently needs custom combat positioning.

## Camera

Use the camera material in the Creative Core pathway above for perspective,
composition, movement, and Cinemachine concepts. The project currently uses a
custom two-target match camera and does not declare Cinemachine in
`Packages/manifest.json`.

## User Interface

### Working with UI in Unity

Source: https://learn.unity.com/tutorial/working-with-ui-in-unity

Relevant to the runtime uGUI match HUD, meter bars, portraits, scaling, anchors,
and input/raycast behavior.

### Manage screen size and anchors

Source: https://learn.unity.com/tutorial/manage-screen-size-and-anchors?version=6.0

Relevant to making the match HUD robust across window sizes and aspect ratios.

### Optimizing Unity UI

Source: https://learn.unity.com/course/introduction-to-ui-in-unity/tutorial/optimizing-unity-ui

Relevant if HUD updates or future menus become measurable CPU, batching, or
fill-rate costs. Profile before applying optimization advice.

## Editor Extensibility

### Editor scripting

Source: https://learn.unity.com/tutorial/editor-scripting

Relevant to `PrototypeAssetBuilder` and `RosterAssetImporter`: menu items,
custom inspectors/windows, serialized properties, asset creation, and workflow
automation.

## Testing and Debugging

Use the debugging, code quality, and profiling units in the Junior Programmer
pathway. The project's F1 overlay should supplement rather than replace
structured tests. The Unity Test Framework package is installed, but no current
dedicated Unity Learn lesson was verified during this ingestion; use current
Unity package documentation for exact test APIs.

## Profiling and Optimization

### Profile code to identify issues

Source: https://learn.unity.com/pathway/junior-programmer/unit/apply-object-oriented-principles/tutorial/profile-code-to-identify-issues-2?version=6.0

Use for CPU, rendering, memory, audio, physics, and GC investigation.

### Introduction to optimization in Unity

Source: https://learn.unity.com/tutorial/introduction-to-optimization-in-unity

Relevant after behavior is correct, especially when real models, animation,
effects, crowd rendering, and audio increase frame cost.

### Performance optimization

Source: https://learn.unity.com/course/performance-and-optimisation

Use as a deeper pass for profiling methodology, assets, scripting, physics,
graphics, UI, and platform-specific constraints. Confirm lesson version before
applying exact settings.

## Suggested Study Order for This Repository

1. Unity Essentials sections on GameObjects, scenes, physics, cameras, and UI.
2. Junior Programmer sections on C#, lifecycle, OOP, debugging, and data.
3. ScriptableObject tutorials.
4. Physics/triggers for ring mechanics.
5. Animation systems before integrating real wrestler models.
6. Unity Test Framework before broad gameplay refactors.
7. Profiling only after representative art and animation are present.
8. AI Navigation only if gameplay expands beyond ring-local positioning.
