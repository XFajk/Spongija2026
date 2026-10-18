# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Spongija2026 is a 3D turn-based strategy game in the style of Heroes of Might and Magic V (free mouse cursor, clickable 3D objects), built with **Godot 4.7.2 (.NET / C#)**. The current FPS player controller and test scene are placeholder code reused to test the CI/CD pipeline, not the intended gameplay. It targets .NET 8 (`Godot.NET.Sdk/4.7.2`, pinned in `global.json`). All gameplay code is C# in `src/`; scenes live in `scenes/`; art/media in `assets/`. There are no tests and no linter configured.

## Commands

`godot` is not on PATH locally; use the Godot .NET (mono) editor binary.

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

## Conventions

- Godot C# scripts are `public partial class` with the same name as the file. Each `.cs` file has a committed `.cs.uid` sidecar. Scenes reference scripts and resources by `uid://`, so keep uid files when moving or renaming files.
- Tunables are exposed via `[Export]` (grouped with `[ExportGroup]`/`[ExportSubgroup]`) rather than hardcoded.
- Prefer editing `.tscn` and `project.godot` through the Godot editor. If you edit them by hand, preserve `uid`/`unique_id` values.
