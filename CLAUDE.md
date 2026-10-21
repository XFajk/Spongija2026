# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Spongija2026 is a 3D turn-based strategy game based on Heroes of Might and Magic III (mechanics are added to or cut from it), with inspiration from HoMM IV and V (free mouse cursor, clickable 3D objects). It is built with **Godot 4.7.2 (.NET / C#)**. The current FPS player controller and test scene are placeholder code reused to test the CI/CD pipeline, not the intended gameplay. It targets .NET 8 (`Godot.NET.Sdk/4.7.2`, pinned in `global.json`). All gameplay code is C# in `src/`; scenes live in `scenes/`; art/media in `assets/`. There are no tests and no linter configured.

## Game design

Agreed design. Values without a number yet are TBD.

### Setting and scope

- LOTR, Years of the Trees: the Elves' Great Journey from Cuiviénen toward Aman.
- The game covers only the Great Forest (later Mirkwood), ending at Atyamar in the Anduin Vale. A second part (crossing the Anduin and the Misty Mountains) is optional, only if time allows.
- The player is Oromë (the Vala). The story starts at the Elves' settlement in front of the forest, where they waited after being slaughtered by dark creatures in the forest, until Oromë came.
- A tutorial in front of the forest teaches movement, combat, artifacts and camps.

### Adventure map

- One large 3D map. The hero is a `NavigationAgent3D`: click a point and he walks there along the navmesh.
- No days, weeks, seasons or movement points (movement is unlimited). No towns.
- Areas (east to west): the start, Eastern Woods, the Meadow (centre hub, camp, wild horses), then two routes: the Deep Thicket (north, spiders) or Shadow Hill (south, shadows and spirits, ruins of a Melkor stronghold, optional boss that drops the Valaróma artifact). The Mere/swamp has ore, a mana spring and the strongest scroll. The end is Atyamar (west). The map sketch is not in the repo; ask the user for it.
- Scale: 1 Godot unit = 1 m. Hero ~2.5 m, trees 5–8 m, mountains 15–35 m, camp 15–20 m, paths at least 6–10 m wide. Camera ~35 m up, pitched 50–55°. Sizes follow gameplay importance, not realism.

### Army

- Starting army: ~30 pilgrim elves and 5 warrior elves.
- 5 army slots with no stack cap. Stacks can be split across slots as in HoMM: an attack hits only one stack, so splitting limits losses but uses slots.
- Ways to get units:
  1. Camps: on every hero move, each camp measures the navmesh path distance to the hero and stores the maximum ever reached (`max_ever`). A visit pays out only new progress: `max_ever - already_paid` is converted to units (X distance = 1 pilgrim, Y pilgrims = 1 warrior; X and Y TBD), then `already_paid = max_ever`. This prevents farming by walking back and forth.
  2. Found on the map (e.g. lost elves).
  3. Surrender: if the player's army is much stronger, the fight can be skipped and beasts (wolves, spiders) offer to join; the player accepts or refuses. Demons and shadows never surrender.
  4. Crafting with found resources: horses + pilgrims → cavalry, silk + pilgrims → archers, ore + pilgrims → warriors (ratios TBD).

### Hero, artifacts, spells

- As in HoMM3, the hero doesn't fight. His stats (Attack, Defense, Power, Knowledge) boost the army, and Knowledge sets max mana.
- 3 artifact slots: neck, left hand, right hand. Artifacts boost hero stats. One artifact per significant forest area (~6).
- Mana works as in HoMM3. Hero mana refills at mana springs, which work like camps. Unit mana refills on its own.
- Spells are learned in exactly 2 ways:
  1. From an enemy caster unit that surrenders and joins: the hero learns its spell, castable on that unit's turn or as a hero spell. The hero keeps the spell even if the unit dies later.
  2. From scrolls with a Knowledge requirement. A spell is usable only while current Knowledge >= its requirement; recheck whenever artifacts change. Strong spells need artifact-boosted Knowledge, and removing the artifact locks the spell again.
- Combat: HoMM3-style turn-based battles with stacks (details TBD).

### UI still needed

- army management (slots, splitting), artifact equipment, spellbook.

## Commands

`godot` is not on PATH locally; use the Godot .NET (mono) editor binary, installed at `~/godot-versions/4.7.2-stable-mono/Godot_mono.app/Contents/MacOS/Godot`. To run a scene headless as a quick check, e.g. the battle test: `<godot> --headless --path . res://scenes/battle_test.tscn --quit-after 120`.

```sh
dotnet build                                   # compile C# only (fast check for errors)
godot --headless --import                      # (re)import assets; required before a headless export on a fresh checkout
godot --headless --export-release "Windows" build/windows/Spongija2026.exe
godot --headless --export-release "Linux"   build/linux/Spongija2026.x86_64
godot --headless --export-release "macOS"   build/mac/Spongija2026.zip
```

Export preset names (`Windows`, `Linux`, `macOS`) come from `export_presets.cfg`. Output goes to `build/` (gitignored).

## Branches & CI/CD

- `main` is the development branch. `stable` and `playtest` are release branches.
- `.github/workflows/itch-deploy.yml`: pushing to `stable` or `playtest` exports all three platforms and pushes them to itch.io via butler. Channels are `<platform>-<branch>`, e.g. `windows-playtest`. PRs into those branches only build, as a required check, and never deploy.
- The macOS build is exported on Linux and then re-signed ad-hoc with `codesign` on a macOS runner, followed by a headless smoke-launch. Godot's Linux-side signer produces a signature that macOS kills at launch, so don't remove this step.
- The Godot version is pinned in three places that must stay in sync: `GODOT_VERSION` in the workflow, `Spongija2026.csproj`, and `global.json`.

## Assets

Binary assets (images, audio, video, 3D models, fonts, zips) are stored with **Git LFS** (see `.gitattributes`). Line endings are normalized to LF. Code is under PolyForm Strict (`LICENSE`). Non-code assets are under CC BY-NC-ND (`ASSETS-LICENSE.md`).

## Architecture

**Scene flow:** `scenes/starting_scene.tscn` is the main scene. It plays an intro video via `SplashScreen` (a `VideoStreamPlayer`), which on `Finished` switches to its exported `BeginningScene` (currently `scenes/test.tscn`, a greybox level containing the player).

**Player** (`scenes/player/player.tscn`): the scripts depend on this exact node hierarchy and look each other up through `GetParent`/`GetNode` by name:

```text
Player (CharacterBody3D, FpsController)
├── MeshInstance3D   (CapsuleMesh)    ┐ heights tweened on crouch/stand
├── CollisionShape3D (CapsuleShape3D) ┘
└── Head (Node3D, LookAround)          - mouse yaw rotates Player, pitch rotates Head; Esc toggles mouse capture
    └── Eyes (Camera3D, EyesBobing)    - TopLevel camera that follows Head with slerp smoothing and adds a roll bob while walking
```

- `FpsController` runs two independent state machines in `_PhysicsProcess`. `BodyState` (Standing/Crouching) is driven by the `crouch` action and tweens the capsule mesh, collision shape and Head position. `MovementState` (Standing/Walking/Jumping/Falling) handles movement. Speed depends on body state plus `sprint`. Other scripts read state through `GetMovementState()`, `GetBodyState()` and `SprintKeyPressed()`.
- Input actions are defined in `project.godot`: `forward`/`backward`/`left`/`right` (WASD), `jump` (Space), `sprint` (Shift), `crouch` (C), plus the built-in `ui_cancel`.

**UI:** `src/ui/custom_controls/PieBar.cs` is a `[GlobalClass][Tool]` `Range` control that draws a radial fill, so it runs in the editor too.

### Battle system (`src/battle_system/`, `src/arena/`)

HoMM3-style turn-based battles on a square grid. Two layers, and the split matters:

- **`Battle`** (`Battle.cs`, plain C#, no nodes) owns state and rules: the round queue, board occupancy, every query (reachability, melee/ranged targets, retaliation, spells) and mutation (`TryPlaceStack`, mana, `RemoveDeadStacks`). Player input and AI both go through it.
- **`Arena`** (`src/arena/Arena.cs`, `Node3D`) owns visuals and execution. It asks `Battle` and displays the answers; it never re-implements rules.

**Data** (`army/`):

- `Unit` (`Resource`): read-only stat sheet shared through `.tres` files. Never mutate it at runtime. `Size` is 1 (one tile) or 2 (2x2). `Model` is the unit's scene; its root must be a `UnitNode`, which Godot can't type-check, so check it when spawning.
- `Stack` (plain class): runtime state per stack: `Quantity`, `TopHealth`, `Mana`, `Effects`, `Anchor`, `Retaliated`, `NodeRepresentation`. Stat getters like `GetDefense(includeHero)` apply effect modifiers and the hero bonus (`Hero.ApplyBonus`, +10% per point, TBD).
- `Army`: optional `Hero` plus up to 5 `Stacks`. `AddStack` sets `Stack.Army`.
- `Hero` (plain class): Mana, Strength, Knowledge, Defense; `Spells` is a `List<SpellAbility>`. Spell availability is computed on every call (Knowledge >= `MinKnowledge`), so artifact changes apply immediately. One hero cast per round.
- `UnitNode` (`Node3D`): root of each unit's model scene. Being designed by the user; plan is that it exposes signals the abilities await. Subscribe to a signal *before* triggering the action, and make sure freed nodes don't leave an await hanging.

**Abilities and effects** (`abilities/`, `effects/`), all abstract `[GlobalClass]` Resources; concrete ones are subclasses:

- `IAbility.Use(user, target, arena)` returns a `Task`. Bases: `MeleeAttackAbility`, `LongRangeAttackAbility`, `SpellAbility` (`ManaCost`, `MinKnowledge`; used for both unit and hero spells), `PassiveAbility` → `RoundPassiveAbility` / `TurnPassiveAbility`. Change game state first, then await the animation.
- `IEffect.Apply(target, arena)`: `RoundEffect` / `TurnEffect` with stat modifiers and `Duration`. `Stack.AddEffect` duplicates the Resource, because runtime state must never live on a shared `.tres`.

**Board:**

- Grid of `Arena.AreaWidth` x `Arena.AreaHeight` (static, 20x12). `Stack.Anchor` is the top-left tile of the stack's footprint; the other tiles come from `Size`. Occupancy maps every covered tile to the same `Stack`, so `GetStackAt` works on any of them, and turns stay one per stack.
- `Battle.GetReachability(stack)` is a breadth-first search over anchors: 8 directions, up to `SpeedTier` steps, and a step is allowed only if the whole footprint `Fits` (in bounds, not on another stack; own tiles count as free). That stops 2x2 stacks squeezing through 1-wide gaps. It returns a `Reachability`: reachable anchors with where each was first reached from, so `GetPath(anchor)` gives a shortest path without searching again. `GetTiles()` gives tiles to highlight, `GetAnchorsCovering(tile)` the valid 2x2 squares under the mouse.
- Search once per turn: `Arena` stores it as `CurrentReachability` and passes it to the queries' optional `reachability` parameter. It's stale once the board changes.
- Adjacency compares whole footprints (`AreAdjacent(Stack, Stack)`).

**Turn flow** (`Arena.RunBattle`):

1. Before the battle: `SceneTransitions.EnterArena(tree, player, computer)` (`src/helpers/`) creates the arena and calls `SetArmies`. `BeginBattle()` creates the `Battle`; until the placement phase exists, `AutoBeginBattle` does this in `_Ready`, and the `Battle` constructor auto-places stacks (`PlaceArmy`).
2. Round queue order: `SpeedTier` descending, then Defense ascending (effects included, hero excluded), then `Quantity` ascending. Rebuilt every round.
3. Each turn waits one frame first (otherwise instantly finished turns freeze the game). Player stacks wait on a `TaskCompletionSource` completed by `SubmitPlayerTurn(TurnOutput)`; computer stacks use the AI stub.
4. `TurnOutput`: `MoveTo` (an anchor) runs first, then `HeroSpell`, then `Entries` grouped by wave. Entries in one wave run in parallel (`Task.WhenAll`); waves run in order. `TurnOutput.Add(ability, target)` sets the wave to the number of earlier entries on the same target.
5. After a melee hit, the target retaliates in the next wave if `Battle.CanRetaliate` (`Unit.CanRetaliate`, not `Stack.Retaliated` this round, alive, has melee). Retaliations don't trigger retaliations.
6. End of turn: turn passives, turn effects, expired effects removed, dead stacks removed. End of round: round passives and effects, then `StartNewRound` resets `Retaliated` and the hero's cast.

**Testing:** run `scenes/battle_test.tscn` (F6). `src/test/BattleTest.cs` builds two armies in code and enters the arena.

**Not built yet:** player input (cursors, clicks, spellbook), AI, spawning `UnitNode`s and animations (`Arena.AnimateMove` is a stub), the placement phase, concrete abilities and effects, the damage formula.

## Conventions

- **Keep this file current.** When a change makes anything in this file out of date (architecture, battle system, design decisions), update this file in the same change.

- Godot C# scripts are `public partial class` with the same name as the file. Each `.cs` file has a committed `.cs.uid` sidecar. Scenes reference scripts and resources by `uid://`, so keep uid files when moving or renaming files.
- Tunables are exposed via `[Export]` (grouped with `[ExportGroup]`/`[ExportSubgroup]`) rather than hardcoded.
- Prefer editing `.tscn` and `project.godot` through the Godot editor. If you edit them by hand, preserve `uid`/`unique_id` values.
