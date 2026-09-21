# Sparkles TRFU — Reborn

A modern Windows resurrection of the original **Sparkles** VB.NET prototype.

The old project is treated as archaeological source material: its world map, 28 named campaign locations, story, Sparkles animation art, collectible ideas, enemies, bosses and general tone are preserved. The unfinished engine code is being replaced with a deterministic modern platformer core.

## Current foundation

The current checkpoint is a dependency-free C# / .NET 8 Windows prototype. It is intentionally easy to build while the gameplay model is being stabilized.

Implemented in the foundation:

- fixed-timestep 120 Hz simulation
- keyboard and XInput controller input
- original 28-node overworld and level names
- fixed campaign seed + deterministic procedural stage generation
- secret-code levels in a separate deterministic seed namespace
- persistent completion/unlock/code discovery save data
- continuous platformer movement, jumping and collision
- Ramjet-coupled boosted jumping: active dash speed increases takeoff impulse while preserving horizontal momentum
- Rainbow Ramjet dash / fart-thrust
- Rainbow Ramjet contact rule: dash alone punts normal minions into the air without direct damage; combine dash + Sparkly Horn Joust for the high-speed kill. Spiky enemies, projectiles and spike hazards still hurt Sparkles.
- speed-scaled Sparkly Horn jousting with a small discrete sparkle corkscrew around the horn while armed; Ramjet + Joust drops Sparkles into a forward lance posture
- Four-Hoof Stomp
- Fairy Belch projectiles using pixie/cake ammunition
- Sparkle Armor capacity persistence
- checkpoints, pits, platforms, pickups and breakables
- biome-aware procedural stage grammar
- jokes/signage seeded into level construction
- canonical original enemy roster
- all eight original bosses represented with prototype behaviors
- mid-game **Ramjet Runway** set piece with boost extenders, thirty pixies, overdrive escalation, fairy explosion and faceplant recovery
- synthetic retro sound/music layer so the prototype has feedback without external audio dependencies

The renderer is still a **prototype renderer**. Existing Sparkles/world art is carried forward, while enemies, bosses, environments and UI are currently partly procedural/placeholder art pending the dedicated asset passes described in `ART_AND_CONTENT_BIBLE.md`.

## Build

Requires the .NET 8 SDK or newer on Windows.

```powershell
.\Build-Windows.ps1
```

Run directly:

```powershell
dotnet run
```

Create a self-contained Windows x64 build:

```powershell
.\Publish-Windows.ps1
```

## Controls

- Move: Arrow keys / A,D / left stick
- Jump: Space / W / Up / gamepad A
- Rainbow Ramjet: Shift / Z / right trigger
- Sparkly Horn Joust: X / gamepad X (keyboard binding is configurable)
- Four-Hoof Stomp: S / C / Down / gamepad B while airborne
- Fairy Belch: F / V / gamepad Y
- Pause: Esc / Start
- Fullscreen: F11

## Project direction

Do **not** interpret the current GDI+ rendering as the final visual ceiling. It is the gameplay/content laboratory. Once movement, deterministic generation and encounter grammar are locked, the renderer is scheduled for a dedicated high-throughput 2D GPU backend while preserving the C# game simulation and content model.

See:

- `TRFU_ROADMAP.md` — staged production plan and acceptance gates
- `ART_AND_CONTENT_BIBLE.md` — 90s visual target, asset standards, enemy/boss expansion plan
- `DETERMINISM_SPEC.md` — seed rules and save compatibility contract


## ALPHA2.5 notes

Expansion enemies that do not yet have bespoke final art now render through a guaranteed visible placeholder path. This fixes the alpha bug where a mechanically active ranged minion could be invisible while its projectile remained visible. Placeholder family-name tags are intentional playtest instrumentation and will be removed/replaced when final Stage 7 art lands. The MIDI/synth soundtrack is intentionally awful and is not to be improved.

## ALPHA2.6 notes

The horn charge tell is intentionally **not a beam**. Continuous glow/core lines were removed in favor of small orbiting sparkle motes. When Sparkles is simultaneously Ramjetting and holding Joust at speed, the sprite pitches forward into a committed lance posture and the horn collision region follows the lowered horn axis.
