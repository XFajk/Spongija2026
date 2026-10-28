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

**Player and interaction** (`src/player/`, `src/interaction_system/`):

- `PlayerCamera` (`Camera3D`) casts a ray from the mouse every frame against collision layer 4 (areas included). A hit that implements `IInteractable` (`Interact`, `Focus`, `FocusLost`) gets focus; `Focus`/`FocusLost` are sent only when the focus changes. A click starts an interaction, which continues while `interact` (left mouse) is held and the ray stays on the object; the object loses focus meanwhile. `ArenaCamera` is the arena's subclass.
- `Interactable` (a `Node` child of the object, which forwards its `IInteractable` calls to it) turns the calls into signals: `InteractionStarted`, then `Interacting`/`PhysicsInteracting` every frame, and `InteractionFinished` on the first frame without an `Interact` call. Don't add members to `IInteractable` for this; put them on `Interactable`.
- `InteractionLock` keeps a running interaction going (signals still every frame) even when the ray misses or the button is released, until it's lifted. Meanwhile the static `Interactable.LockedInteractable` is set and the camera interacts with nothing else, but focus still follows the mouse unless `FocusLock` is set too. `InteractionExitFlag` ends an interaction right away.
- Hidden nodes still collide, so the ray would still hit them: tiles use `Enabled` (visibility and collision shape together) instead of `Show`/`Hide`.

**Spellbook** (`ArenaUserInterface`, `BookInterface` in `scenes/ui/arena_user_interface.tscn`): Arena calls `UpdateSpellButtons(stack)` at the start of a player turn: Use Spell is enabled only if the stack has spells of its own, Use Hero's Spell only if its hero knows any (`GetKnownSpells`); `UpdateSpellButtons(null)` in `ClearTurnInput` disables both between player turns. They hide the battle interface and open the book with the current stack's `Unit.Spells` (disabled without the mana) or the hero's `GetKnownSpells()` (disabled unless `Hero.CanCast`). `BookInterface/ManaLable` shows the caster's mana as `Mana: current/max` (the hero's, or the stack's for its own spells), set when the book opens. The `SpellButton`s (`src/ui/SpellButton.cs`, on `scenes/ui/spell_button.tscn`; setting `Spell` shows its `Icon` and `Name`) are collected in reading order, 12 per spread; buttons past the last spell are hidden. The `SpellBook` `AnimatedSprite2D` plays `open` (frames 0 to 47), Next plays `flip_page` forward, Back plays it backwards; the buttons are filled before and shown after each animation, Next only if more spells follow, Back only after the first spread. Input to the book is ignored while it animates. A click off the book closes it (in `_Input`, so it never reaches the battle). Picking a spell closes the book and emits `ArenaUserInterface.SpellPicked(spell, isHeroSpell)`, which starts `Arena.BeginSpellTargeting`: the normal turn input is cleared (`ClearTurnTargets`) and turned off (`IsPickingSpellTarget`), the tiles under the `GetSpellTargets` stacks show (red for enemies), and hovering a target (unit or tile) shows the spell's `Icon` as the cursor (`ShowSpellCursor`, a `Sprite2D` scaled to `SpellCursorSize`). Clicking one calls `TrySubmitSpell`. A unit spell becomes the turn's action (a `TurnOutput` entry; mana is spent when it runs). A hero spell doesn't end the turn, as in HoMM: `CastHeroSpell` plays it right away (`SpendHeroMana`, then `SpellAbility.UseFromHero(caster, origin, target, arena)`: `origin` is the global position of `Arena.HeroSpellCastPosition`, a marker where the hero stands off the board, and `caster` the acting stack; by default it calls `Use`, and `FireBallSpell` overrides it to fly from `origin`; `IsPlayerTurn` is false meanwhile, so no input is taken), removes dead stacks, searches `CurrentReachability` again (`RunBattle` executes the turn with it, not the turn-start one) and shows the turn input again; if the battle is over or the stack died, the turn ends. Use Hero's Spell is disabled once the hero has cast this round. `TurnOutput.HeroSpell` is now only for the computer's turns. Right click, Escape (`Arena._Input`) or the Cancel Spell button (`BattleInterface/CancelSpell`, shown only while targeting) cancel back to the normal input (`CancelSpellTargeting`, `ShowTurnTargets`). The player's own units take clicks only as spell targets.

**Stack labels** (`ArenaUserInterface`, `#region Stack labels`): `Arena.SpawnUnitNode` calls `AddStackLabel(stack, playerSide)`, which instantiates `scenes/ui/stack_lable.tscn` (root `StackLabel`, `src/ui/StackLabel.cs`: the unit's `Name`, the `Quantity` on a tag whose image is `PlayerTagTexture` or `ComputerTagTexture` by side; it ignores the mouse, so clicks reach the units and tiles). `Arena.RemoveDeadStacks` calls `RemoveStackLabel`. Every frame the labels refresh their quantity and move to `Arena.GetStackLabelCanvasPoint(stack)`: under the stack's feet on the screen (the footprint center's projected X, the lowest projected footprint corner's Y), so it's the same for 1x1 and 2x2 stacks from any camera angle, from the unit's current position, so it follows moves and the setup drag. Projection goes through `Arena.ProjectToCanvas` (the camera's `UnprojectPosition`, then `ViewportRect.ViewportToCanvas`; the sword cursor uses it too). `StackLabel.PlaceAt` centers the name and tag together under that point, plus `ScreenOffset`; `Setup` shrinks the name's box to its text, so the tag sits right after it. They sit in a `StackLabels` control behind the rest of the interface, hidden while the spellbook is open.

**Tooltips:** two kinds, each a scene with a script; their labels are found by unique name (`%Name`), so the layout can change freely.

- `StackTooltip` (`scenes/ui/stack_tooltip.tscn`): Arena calls `ArenaUserInterface.SetHoveredStack(GetFocusedStack())` every frame. After the mouse rests on a stack for `StackTooltipDelay`, the tooltip shows next to the mouse (`StackTooltipOffset`, flipped to stay on screen by `KeepOnScreen`) and refreshes every frame. It's hidden while the spellbook is open and ignores the mouse. It shows `Unit.Name` and `Description`, quantity, top health, mana (if the unit has any), Strength/Defense/Speed as the unit's base value plus or minus what effects and the hero add together (`7 + 3`), whether it can melee and shoot, and how many spells and active effects it has. Abilities and effects have no icons or descriptions yet, so they're counts.
- `SpellTooltip` (`scenes/ui/spell_tooltip.tscn`): Godot's built-in tooltip on the spellbook's `SpellButton`s, so it uses Godot's tooltip delay. `SpellButton._MakeCustomTooltip` returns `SpellTooltip.Create(spell, IsHeroSpell, CantCastReason)`: icon, name, mana cost, Knowledge needed (hero spells only), targets, `SpellAbility.Description`, and in red why it can't be cast now (`ArenaUserInterface.GetCantCastReason`). `TooltipText` is set to the spell's name only because Godot shows no tooltip for empty text.

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
- `UnitNode` (`Area3D`, `IInteractable`): root of each unit's model scene, on collision layer 4 so the camera ray hits it. It duplicates its materials in `_Ready`, so the outline (`StandardMaterial3D` stencil outline) and ghost effects apply per unit. Being designed by the user; plan is that it exposes signals the abilities await. Subscribe to a signal *before* triggering the action, and make sure freed nodes don't leave an await hanging.

**Abilities and effects** (`abilities/`, `effects/`), all abstract `[GlobalClass]` Resources; concrete ones are subclasses:

- `IAbility.Use(user, target, arena)` returns a `Task`. Bases: `MeleeAttackAbility`, `LongRangeAttackAbility`, `SpellAbility` (`ManaCost`, `MinKnowledge`; used for both unit and hero spells), `PassiveAbility` → `RoundPassiveAbility` / `TurnPassiveAbility`. Change game state first, then await the animation.
- `Projectile` (`projectiles/`, `Node3D`): `Projectile.Spawn(parent, scene, start, end, angle)` instantiates a scene whose root must be a `Projectile` and flies it along a parabola from `start` to `end` (global positions), leaving at `angle` degrees above the XZ plane (clamped to 0..85), at the exported `HorizontalSpeed`. It emits `Landed` on arrival (it doesn't free itself). `HeadDirection` is the current flight direction.
- `AbilityVisualEffect` (`visual_effects/`, `[GlobalClass]` `Node3D`): `AbilityVisualEffect.Spawn(parent, scene, position)` instantiates a scene whose root must be one, moves it to `position` (global), and emits `Finished` after the exported `Duration`. It doesn't free itself.
- Spells (`abilities/specific/`): `FireBallSpell` deals `Damage` and flies its exported `Projectile` scene from the caster to the target; `HealSpell` heals `Amount` (`Stack.Heal`: top unit only, never revives) and plays its exported `VisualEffect` scene on the target. Both export a `PackedScene` because Resources can't export nodes. Every `SpellAbility` has `Targets` (`TargetSide`: `Enemies`, `Allies` or `Any`, from the caster's side), and `Battle.GetSpellTargets(stack, spell)` gives the living stacks it can be cast on.
- `IEffect.Apply(target, arena)`: `RoundEffect` / `TurnEffect` with stat modifiers and `Duration`. `Stack.AddEffect` duplicates the Resource, because runtime state must never live on a shared `.tres`.

**Board:**

- Grid of `Arena.AreaWidth` x `Arena.AreaHeight` (static, 20x12), tiles `Arena.TileSize` (0.5 m), centered on the Arena's origin. Convert grid to Arena-local positions only with `Arena.GridToWorld` (tile center, any grid point, or a stack's footprint center); the tile generation uses it too, so tiles and units stay aligned. `Stack.Anchor` is the top-left tile of the stack's footprint; the other tiles come from `Size`. Occupancy maps every covered tile to the same `Stack`, so `GetStackAt` works on any of them, and turns stay one per stack.
- `Battle.GetReachability(stack)` is a breadth-first search over anchors: 4 directions (no diagonal steps), up to `SpeedTier` steps, and a step is allowed only if the whole footprint `Fits` (in bounds, not on another stack; own tiles count as free). That stops 2x2 stacks squeezing through 1-wide gaps. It returns a `Reachability`: reachable anchors with where each was first reached from, so `GetPath(anchor)` gives a shortest path without searching again. `GetTiles()` gives tiles to highlight, `GetAnchorsCovering(tile)` the valid 2x2 squares under the mouse.
- Search once per turn: `Arena` stores it as `CurrentReachability` and passes it to the queries' optional `reachability` parameter. It's stale once the board changes.
- Adjacency compares whole footprints (`AreAdjacent(Stack, Stack)`).

**Turn flow** (`Arena.RunBattle`):

1. Before the battle: `SceneTransitions.EnterArena(tree, player, computer)` (`src/helpers/`) creates the arena and calls `SetArmies`. `BeginBattle()` creates the `Battle` after the placement phase (below), with `keepAnchors` so the stacks stay where they were placed. Without it (`AutoBeginBattle`, which `_Ready` currently doesn't start), the `Battle` constructor auto-places stacks (`PlaceArmy`) at the anchors from the static `Battle.GetStartingAnchors(stacks, leftSide, width, height)`, whose result is index-aligned with `stacks`.
2. Round queue order: `SpeedTier` descending, then Defense ascending (effects included, hero excluded), then `Quantity` ascending. Rebuilt every round.
3. Each turn waits one frame first (otherwise instantly finished turns freeze the game). Player stacks wait on a `TaskCompletionSource` completed by `SubmitPlayerTurn(TurnOutput)`; computer stacks use the AI stub.
4. `TurnOutput`: `MoveTo` (an anchor) runs first, then `HeroSpell`, then `Entries` grouped by wave. Entries in one wave run in parallel (`Task.WhenAll`); waves run in order. `TurnOutput.Add(ability, target)` sets the wave to the number of earlier entries on the same target.
5. After a melee hit, the target retaliates in the next wave if `Battle.CanRetaliate` (`Unit.CanRetaliate`, not `Stack.Retaliated` this round, alive, has melee). Retaliations don't trigger retaliations.
6. Wait (the `Wait` button, `Arena.TryWait`): a `TurnOutput` with `Wait` set. `Battle.Wait` puts the stack at the end of this round's queue and sets `Stack.Waited` (once per round, reset with `Retaliated`); the turn doesn't end, so step 7 runs only after the turn it really takes. Waited stacks act in the order they waited (HoMM3 reverses it; TBD). `ArenaUserInterface.SetWaitEnabled` enables the button only on a player turn whose stack can wait.
7. End of turn: turn passives, turn effects, expired effects removed, dead stacks removed (`Arena.RemoveDeadStacks`: `Battle.RemoveDeadStacks` takes them out of the armies, round queue and board, then their `UnitNode`s are freed). End of round: round passives and effects, then `StartNewRound` resets `Retaliated` and the hero's cast.

**Placement phase** (`Arena.SetupPlacementPhase`, used when `AutoBeginBattle` is off): spawns the player's `UnitNode`s at `GetStartingAnchors` and enables the first `Arena.PlacementColumns` (2) columns of tiles. The player drags stacks: pressing on a unit locks the interaction, the stack follows the focused tile, and releasing drops it. `Battle` doesn't exist yet, so `Arena.CanPlaceDuringSetup` checks placement instead of `Battle.Fits`, and `Stack.Anchor` follows the model. `FindSetupAnchorCovering` moves a 2x2 stack as little as needed to cover the hovered tile. A 2x2 stack stands on a `DoubleTile` that is a child of its `UnitNode` (`GroundTile`), and stands in for the 4 small tiles under it: `Arena.ShowTiles`/`HideTiles`/`HideAllTiles` keep those small tiles disabled and show or hide the double tile in their place (by the stacks' current anchors), in the battle too. So a double tile shows only when the tiles it covers would. The Start Battle button (`ArenaUserInterface`) emits `SetupPlacementPhaseCompleted`, then `TransitionToBattle` spawns the computer's units, calls `UnitNode.SwitchToBattleInteractions` on every unit (swaps the setup drag callbacks for the battle ones of its side, picked by `IsPlayerUnit`, all still empty; focus/outline is shared), hides the tiles and moves the camera to `BoardOverviewCameraPosition` (`ArenaCamera.MoveTo`). Then `BeginBattle`. `SpawnUnitNode` sets `Stack.NodeRepresentation` for both sides.

**Melee input** (player turns, all in `Arena`): at turn start the enemies in `Battle.GetMeleeTargets` get their tiles shown red (`SetTilesHighlight`; `IsMeleeAttackTile` swaps in the shared red materials in `assets/battle_system/tiles/`). Every frame, `UpdateTurnHover` takes the focused melee target (its unit or the tile under it) and `PickMeleeAttackAnchor` picks, from `GetMeleeAttackAnchors`, the anchor whose direction from the target's footprint center is closest to the direction to `ArenaCamera.GetBoardHoverPoint()` (the mouse ray on the board plane, so tall models don't skew it); that footprint is red and `Raised`. Clicking the unit or its tile calls `TrySubmitMeleeAttack` (move to the picked anchor, then melee). While a pick exists, `UpdateMeleeCursor` hides the mouse in place and shows the sword (`ArenaUserInterface.ShowMeleeCursor`): the picked footprint's center and a point 1 m further toward the target are projected with `PlayerCamera.UnprojectPosition` and mapped to the screen by `ViewportRect.ViewportToCanvas` (the camera renders into a SubViewport); the sword sits at the first point, rotated from screen-up (`Vector2.Up`, Y points down) to the second. On a 2x2 stack's turn, `Arena.MovePreview` (a second `DoubleTile`, Arena's own) shows the destination: hovering a reachable small tile moves it to the closest anchor covering that tile from where it is (`ClosestAnchor`, shared with the setup drag), hovering the stack's own double tile resets it, and a melee pick moves it to the attack anchor. It's hidden while on the stack's own position, and stands in for the small tiles under it via `GetDoubleTilesByTile`. Clicking it or a small tile calls `TrySubmitMoveTo`, which uses the preview's anchor for 2x2 stacks. `UnitNode.MoveAlong` tweens `position` to footprint centers, and `FaceTowards` keeps the double tile square after turning the unit. Melee comes before ranged: an enemy the stack can reach in melee isn't in `GetRangedTargets`, as in HoMM 5. The enemies in `GetRangedTargets` get red tiles too; hovering one (unit or tile) shows the arrow cursor (`ArenaUserInterface.ShowRangedCursor`: a `Sprite2D` that follows the mouse with the arrowhead's tip, `RangedCursorHotspot`, on it, while the mouse is hidden. Not a custom OS cursor: `Input.SetCustomMouseCursor` fails in the editor's embedded game view on macOS in 4.7.2 with "Parameter imgrep is null"), and clicking calls `TrySubmitRangedAttack` (shoot from where the stack stands). `BasicLongRangeAttackAbility` flies a `Projectile` (`scenes/projectiles/basic_projectile.tscn`) to the target and awaits `Landed`. Everything is cleared after the turn.

**Testing:** run `scenes/battle_test.tscn` (F6). `src/test/BattleTest.cs` builds the two armies from its exported `StackResource` arrays and enters the arena.

**Not built yet:** player input (cursors, clicks, spellbook; `UnitNode`'s battle interaction callbacks are empty), AI, animations (`Arena.AnimateMove` is a stub), concrete abilities and effects, the damage formula.

## Conventions

- **Keep this file current.** When a change makes anything in this file out of date (architecture, battle system, design decisions), update this file in the same change.

- Godot C# scripts are `public partial class` with the same name as the file. Each `.cs` file has a committed `.cs.uid` sidecar. Scenes reference scripts and resources by `uid://`, so keep uid files when moving or renaming files.
- `Battle`, `Arena` and `UnitNode` are split into `#region`s; put new members in the matching region (or add one).
- Tunables are exposed via `[Export]` (grouped with `[ExportGroup]`/`[ExportSubgroup]`) rather than hardcoded.
- Prefer editing `.tscn` and `project.godot` through the Godot editor. If you edit them by hand, preserve `uid`/`unique_id` values.
- Claude: change files with the Edit/Write tools, never with Python, `sed` or other scripts. The user reviews every change as a diff in the IDE, and script edits don't show up there.
