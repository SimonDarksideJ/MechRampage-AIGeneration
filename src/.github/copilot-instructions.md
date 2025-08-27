# AI Coding Agent Instructions for Mech Rampage

Purpose: Enable an AI agent to quickly contribute to the Mech Rampage 3D isometric (MonoGame/XNA-style architecture) by understanding project structure, build/content pipeline quirks, runtime architecture, and editor/gameplay conventions.

## Overview

Mech Rampage is a 3D isometric game where the player will navigate a futuristic city, battling enemies and completing missions.  The player is a commander managing a team of customizable mechs, each with unique abilities and playstyles.

The terrain will be procedurally generated from a set of assets, fielding such arenas as grassy outlands, urban environments, and industrial complexes.  Note, all content to be generated initially by the agent and placed in the content folder for loading.

The play area is an isometric projection, allowing for a unique perspective on the 3D environment.  The player will be able to navigate the space freely, utilizing the verticality and depth of the environment to their advantage.  Control will be through a point and click interface or touch controls:

- When a mech is clicked, it becomes the active unit and the status displays show the stats for that unit
- When a mech is selected, the player can issue commands to move, attack, or use abilities
- When an enemy is clicked, the player can target it for attack or issue special abilities
- When a resource is clicked, the player can collect it or use it to upgrade their mechs

## Implementation plan

1. Create the assets and build the first arena, including terrain, structures, and props.  The Scene will be set up with appropriate lighting and environmental effects to enhance the isometric perspective.  The size of the area will be determined by the gameplay requirements and the visual design. (set initially as defaults in code)
2. Add camera controls to move the camera around the arena, arrow keys will control the isometric movement of the camera on the X and Y axis.  Zooming will be controlled with the mouse wheel or pinch gestures on touch devices.
3. Implement the player controls for selecting and commanding mechs, including point and click or touch controls for issuing commands.
4. Add the first mech, generate a 3D animated model, and implement basic movement and attack animations.
5. Implement the UI for displaying mech stats, selected unit information, and command options.
6. Implement the game logic for handling mech actions, including movement, attacks, and ability usage.
7. Implement the audio system for playing sound effects and music.

## Big Picture
- Solution layout (`MECHRAMPAGE/`):
  - `Core/` shared game logic (entities, managers, screens, editor, content references).
  - `Core/Content/` Where all game assets (textures, models, sounds) are stored and processed.
  - `Dependencies/NormalMappingModelProcessor/` custom MGCB content processor supplying normal/specular/normal/glow map wiring + collision mesh extraction.
  - `Platforms/*` (Android, DesktopGL, iOS, WindowsDX) thin bootstrap projects referencing Core + content.
- Game loop class: `MechRampageGame` creates `GraphicsDeviceManager`, sets window/backbuffer, builds managers (`GameManager`, `ScreenManager`, `FontManager`, `SoundManager`) and orchestrates Update/Draw.
- Rendering pipeline: Scene drawn to HDR color RT (`colorRT`), glow extracted via alpha then dual-pass blur (`glowRT1/2`) in `ScreenManager`, combined (additive blend) then UI drawn.

### 🧠 MCP Initialization Required - CRITICAL STARTUP
**MANDATORY FIRST ACTIONS** - Before any conversation begins:
1. **Memory MCP**: Test with `mcp_memory_read_graph` - if fails, request user to restart Memory server
2. **Context7 MCP**: Verify availability for documentation retrieval 
3. **Sequential Thinking MCP**: Confirm access for complex problem analysis
4. **Startup Validation**: All three MCPs must be running before proceeding with tasks

**Memory Server Startup Issues**: If Memory MCP fails to initialize:
- Request user to manually restart the Memory MCP server
- Do NOT proceed with complex tasks until Memory is available
- Memory contains critical project knowledge and constraints
- Without Memory, you may recreate existing solutions or violate established patterns


## Build & Content Workflow
- Requires .NET 8 SDK. Local MonoGame CI preview NuGets: configure local package source (see README steps). Keep `PackageReference` versions and `.config/dotnet-tools.json` in sync with downloaded preview packages.
- When adding model assets:
  - Name meshes with prefixes to drive processor behavior:
    - `cNN_name` => triangle collision mesh bucket `name`.
    - `chull_name` => convex hull collision.
    - `lNN_name` => layer tag appended to `ModelData.Layers`.
  - Texture naming convention: base color map `_c.` triggers inferred normal `_n.`, specular `_s.`, glow `_g.`; falls back to `null_*.tga` placeholders if absent.

## Core Runtime Architecture
- `GameManager` owns gameplay state: players (`PlayerShip` entities), particle/projectile/animated sprite managers, physics, HUD textures, shadow atlas + light clusters, level load.
- Level load flow: `ScreenManager` switches to game screen -> `GameManager.LoadFiles()` loads level XML (`content/levels/<LevelFile>/level.xml`), creates `Scene`, wires per-entity events (`Scene.LevelStart`).
- Entity system: base `Entity` with editable reflection-driven properties (marked `[Editable]`). Transform caching via dirty flag; `YawPitchRoll` stored separately for editor friendliness.
- Scene editing/runtime events: `Entity.OnSceneAdd/OnSceneRemove/OnLevelStart/OnEditorOpen/OnEditorClose` allow custom hook-in when extending types.
- Screen system: Derived `Screen*` classes handle 3D + 2D separation (`Draw3D` then `Draw2D`), transitions managed with fade timing & color via `ScreenManager` (mid-transition swap when `fade < 0.5 * fadeTime`).
- Post-processing: Glow intensity comes from alpha channel of rendered geometry; blur pass differs for multiplayer split-screen horizontal handling.
- Audio: `SoundManager` pre-loads fixed sound list, 3D-ish playback loops through listener ships (distance-capped). Keep new sound keys consistent with `sounds/` folder and add to `soundAssetNames`.

## Editor (In-Game)
- Toggle with F12 (see README). When open: `Entity.EditorMode = true`, `ShipBase.NoHit = true`, icons drawn for each entity type (icons loaded from `editor/` by `CreatableAttribute` mapping).
- Gizmo transforms selected entity; transform ops recorded to undo stack (`UndoStack`, `TransformAction`).
- Clipboard operations use serialized entity XML (via `Scene.Serialize/Deserialize`) and system clipboard (TextCopy).
- Add new entity types:
  - Decorate class with `[Creatable(...)]` to appear in toolbox and supply `IconName` texture.
  - Mark editable fields/properties with `[Editable("Label")]` to expose in property panel.
  - Implement hooks if special init needed (e.g., allocate buffers in `OnSceneAdd`).

## Conventions & Patterns
- Static configuration constants live in `GameOptions`; adjust gameplay balance there (movement forces, cooldowns, camera offsets).
- Draw order constants in `GameManager` (e.g., `ShipDrawOrder = 200`) govern layering; new drawable entities should set `DrawOrder` appropriately.
- Prefer adding sounds through central `soundAssetNames` array to keep load/unload symmetrical.
- Use `PlaySound3D` for positional effects; it iterates current `ShipBase` entities — non-ship listeners aren't modeled.
- Performance considerations: Avoid reallocations inside per-frame loops (e.g., reuse arrays or pre-load textures). Follow existing pattern (e.g., lazily load textures once, keep arrays cached).
- Transform math: Always set `Position/Scale/YawPitchRoll` instead of directly editing `Transform` unless you also update `_transformDirty`; or assign full matrix to `Transform` property which decomposes & re-syncs yaw/pitch/roll.

## Adding Features Safely (Examples)
- New projectile type: extend enum (if any), add model under `projectiles/`, update arrays (`projectileFiles`, load logic). Ensure HUD representation if needed.
- New powerup: extend powerup enum/class; serialize spawn points via XML (see `PowerupManager` pattern) and hook pickup logic into `GameManager` similar to existing weapon boost/shield logic.
- New screen: subclass `Screen`, add instance in `ScreenManager` constructor list, integrate transitions using `SetNextScreen` calls.
- New model with collision: Name collision meshes with `c<number>_logicalName` inside source DCC tool; processor strips them from visual scene and adds to `ModelData.CollisionMesh`.

## Gotchas / Pitfalls
- Changing shader semantics: ensure maintained compatibility with expected vertex channels (`TextureCoordinate0`, `Normal0`, `Binormal0`, `Tangent0`) — processor strips unused channels.
- Editor Save assumes running from source layout and walks up to find `MechRampageGame.sln`; packaged builds may break save path logic.
- Multiplayer glow blur uses split technique; altering blur pipeline must preserve special case or MP artifacts appear.
- Sound cleanup: unmanaged `SoundEffectInstance`s are disposed in `SoundManager.Update`; ensure Update still called when adding long-lived instances.

## Quick Reference
- Toggle full screen: Alt+Enter. Toggle VSync: Ctrl+V. FPS overlay: Ctrl+F.
- Editor shortcuts: F12 toggle, Ctrl+Z/Y undo/redo, Ctrl+D duplicate, Ctrl+X/C/V cut/copy/paste, Delete deletes.
- Key entry points: `MechRampageGame.cs`, `GameManager.cs`, `ScreenManager.cs`, `Editor/Editor.cs`, `GameOptions.cs`.

## When Unsure
- Search for existing pattern in `Core/Game/*Manager.cs` before adding a new manager.
- Favor extending data-driven XML (levels/entities) via existing serialization before hardcoding values.
- Maintain cross-platform graphics constraints: test shader/asset changes under at least WindowsDX and DesktopGL projects.

(End of instructions — please review and suggest clarifications if any project-specific nuance is missing.)
