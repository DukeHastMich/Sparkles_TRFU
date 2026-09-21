# Sparkles TRFU — Staged Production Roadmap

This file is the guardrail against losing ideas while the game grows. A stage does not erase or replace earlier Sparkles material unless it is demonstrably unfinished/buggy implementation detail. The original VB project remains the canon source for world names, characters and intent.

## Non-negotiable game identity

Sparkles is a side-scrolling platformer with a Super-Mario-World-like overworld structure, but its movement/combat identity is its own:

1. **Rainbow Ramjet** — rocket-like rainbow fart thrust, useful for speed, gaps and aerial control.
2. **Sparkly Horn Joust** — forward horn attack whose impact scales with velocity.
3. **Four-Hoof Stomp** — downward strike, enemy bounce, breakables and switch interaction.
4. **Fairy Belch** — eat/collect fairy ammunition and fire it back at unreasonable muzzle velocity.
5. **Movement is combat** — the best play combines movement mechanics rather than stopping to spam an attack button.

Campaign stages are deterministic. Secret code levels are deterministic but are not tied to the overworld. The tone is 1990s maximalist toy-commercial / Saturday-morning-cartoon insanity with environmental jokes, not generic procedural filler.

\---

## Stage 0 — Archaeology and preservation — DONE

* preserve original VB.NET project unchanged
* recover 28 overworld nodes and level names
* recover original world map image and Sparkles animation sheets
* recover original story/design notes
* recover enemy list and eight named bosses
* recover old power-up concepts and scoring/progression ideas

**Gate:** we can point from every resurrected legacy feature back to a piece of the old project or an explicit new design decision.

\---

## Stage 1 — Modern playable foundation — IN PROGRESS / MOSTLY IMPLEMENTED

* C#/.NET Windows executable
* fixed 120 Hz simulation independent of render rate
* keyboard + XInput controller
* proper velocity/gravity movement rather than tile-step movement
* collisions, pits, checkpoints, deaths and respawns
* original overworld navigation/progression
* save system
* title/pause/code-entry/credits/victory states
* base sound feedback

### Core combat/movement already represented

* Rainbow Ramjet
* Sparkly Horn Joust with velocity-scaled damage
* Four-Hoof Stomp
* normal enemy head-bounce
* Fairy Belch
* armor capacity

**Gate:** all four signature mechanics feel responsive in a plain test level before content volume becomes a distraction.

\---

## Stage 2 — Deterministic generation contract — FOUNDATION IMPLEMENTED, HARDEN NEXT

Every campaign stage is reproduced from:

`generatorVersion + campaignMasterSeed + stage index + stage name`

Every secret stage is reproduced from:

`generatorVersion + "SECRET" + normalized code`

Work remaining:

* split generation into independent RNG streams for geometry, enemies, rewards, secrets, decoration and jokes
* add stage fingerprints and a deterministic regression test tool
* enforce reachability budgets using measured Sparkles physics envelopes
* guarantee checkpoint placement around dangerous long-form sections
* generate optional high-risk routes independently of required route
* serialize generator version in save data forever

**Gate:** same seed/version produces the same gameplay-critical geometry/enemy placement across repeated runs and machines.

\---

## Stage 3 — Level grammar expansion

Current prototype grammar already includes flat runs, rolling terrain, short gaps, boost gaps, stairs, platform hops, joust halls, stomp galleries and chaos lanes.

Expand to a richer authored grammar library:

* acceleration runways
* vertical boost shafts
* crumbling bridge sequences
* collapsing floors
* enemy-bounce stairways
* chained stomp rooms
* jousting slalom lanes
* ambush courtyards
* moving-platform timing chambers
* crusher corridors
* rising hazard escapes
* haunted fake-platform rooms
* ice momentum puzzles
* cave ceiling squeezes
* fortress gate assaults
* secret ceiling routes
* breakable-wall detours
* miniboss arenas
* puzzle-lite switch rooms
* comedy dead ends / fake treasure closets
* multi-route sections with short-dangerous vs long-safe paths

Difficulty rises by **combining learned mechanics**, not merely adding enemy HP.

**Gate:** a 20-minute generated stage feels composed from intentional encounters instead of one long random strip.

\---

## Stage 4 — Signature set pieces

The first mandatory authored procedural macro is already present conceptually and in prototype form:

### Ramjet Runway

A long midgame runway invites the player to hold dash.

* staggered boost extenders stretch Ramjet duration to roughly five times normal
* approximately thirty pixies are positioned along the high-speed intake path
* rainbow contrail progresses through visual instability tiers
* speed enters a redline/overdrive state
* pixies get vacuumed into Sparkles at speed
* at threshold: fairy-pressure catastrophe
* Sparkles belches a screen-filling fairy explosion
* nearby non-boss enemies are obliterated / bosses take heavy damage
* Sparkles loses composure, tumbles/skids and faceplants briefly
* recovery is intentionally undignified

Future macro set pieces:

* Turbo Joust Freeway
* Squirrel Union Picket Line
* Haunted Toy Conveyor
* Count Spatula's Non-Stick Kitchen of Doom
* Ice Rink of Poor Decisions
* Wolfen Moon Chase
* Night Mare's Reality Breakdown

**Gate:** each world contains at least one moment players describe to someone else afterward.

\---

## Stage 5 — Enemy ecosystem: dozens, not palette swaps

The original eleven minion families remain canon:

* evil teddy bears
* zombies
* porcelain dolls
* rabid squirrels
* dire mice
* ghosts
* evil trees
* goblins
* imps
* unicorn hunters
* werewolves

They become the core of a **36+ minion roster**. New enemies must have a mechanical reason to exist and a readable silhouette. Planned expansion is in `ART\\\\\\\_AND\\\\\\\_CONTENT\\\\\\\_BIBLE.md`.

Enemy behaviors will be composed from reusable capabilities:

* walker / runner / charger
* flyer / hover / dive-bomber
* shielded front / vulnerable rear
* stomp-immune / joust-immune / armor-break target
* ranged lob / straight shot / spread / trap
* burrow / teleport / disguise / mimic
* pack behavior
* environmental interaction
* mount/rider pairing
* death hazard / death reward

**Gate:** no two adjacent enemy families are solved identically.

\---

All enemies that can't be stomped on to defeat them should have spikes.



\---

Allies:These are the parallel to Mario's Yoshi, a companion with an ability.
Moonlight - The ghost cat with emerald eyes.
If sparkles has moonlight, Moonlight rides sparkles back and uses 'telekenisis' to steal the bonus items often but not every last one, collecting them for sparkles.
Jack - A black cat with Batwings that flies circling above sparkles dropping tootsie rolls on enemies that get too close making them flee if they get hit.
If Sparkles has Jack, he will occasionally get tired and land on her back riding sparkles like  a pony for awhile.
If sparkles has Jack, if sparkles rainbow dashes, jack will swoop down and grab sparkles neck and be pulled along hanging onn for dear life. Jack cannot fling tootsie rolls in this state.

## Stage 6 — Boss production

Canonical bosses from the original design are preserved:

1. **Ed** — a rather pathetic slime
2. **Old Gnarley** — evil tree
3. **Spookers** — ghost
4. **Webbey** — giant spider
5. **Mina** — evil shape-shifting bird
6. **Count Spatula** — vampire lord
7. **Rip "The Piece Maker"** — werewolf
8. **Night Mare** — final boss

Prototype behavior exists for all eight. Final boss work requires authored multi-phase encounters, unique animation sets, introductions, transition jokes and defeat sequences.

Add world minibosses without replacing the canonical eight. Minibosses can later enter procedural secret levels as rare seeded encounters.

**Gate:** every canonical boss tests a different combination of Sparkles mechanics and has at least one memorable visual/comedic beat.

\---

## Stage 7 — 90s insanity art overhaul

Visual goal: **high-detail retro maximalism**, not pixel-art cosplay and not modern minimalist UI.

* oversaturated but deliberately controlled palette
* chrome, glitter, airbrush gradients, toy packaging energy
* chunky readable silhouettes
* parallax skies and absurd scenic props
* exaggerated anticipation/impact frames
* background jokes and billboards
* biome-specific lighting/palette scripts
* explosive particle work for Ramjet and fairy events
* animated environmental clutter
* boss-scale spectacle

Legacy Sparkles art is reference material, not a resolution limit. Sparkles gets a full modern animation set while preserving the recognizable character.

**Gate:** a screenshot with the HUD hidden is still immediately identifiable as Sparkles.

\---

## Stage 8 — Audio / music identity

* biome-specific music families with 90s game/CD-ROM energy
* tempo/intensity layer that reacts to Ramjet overdrive
* unique boss themes/stingers
* crunchy readable combat SFX
* escalating fart-thrust layers rather than one looping sample
* pixie intake notes climb in pitch during Runway
* fairy-pressure detonation earns a deliberately excessive audio event
* menu/UI sounds fit the toy-commercial aesthetic

**Gate:** gameplay remains readable with eyes closed; major mechanics have distinct audio signatures.

\---

## Stage 9 — Campaign progression and secret-code meta

* 28 original overworld stages stay fixed campaign nodes
* completion unlocks graph-connected stages
* fixed campaign seeds make named stages permanent/replayable
* discovered codes are saved in a Secret Levels menu
* the actual code is always visible so players can write/share it
* secret levels never need a map node
* exact hand-authored Easter egg codes coexist with algorithmic code levels
* selected secret codes may activate bespoke rules, bosses or set pieces

Examples currently reserved in prototype design include `VOIDDUCK`, `1994`, `ED`, `GNOMESNACKS`, `RAINBOWRAMJET` and `FAIRYRAMJET`.

**Gate:** entering the same code on another machine creates the same secret stage for the same generator version.

\---

## Stage 10 — Renderer transition and production polish

The current no-dependency GDI+ renderer is a gameplay laboratory. Before final art volume explodes:

* move rendering to a GPU-backed 2D layer while leaving simulation/content C# code intact
* sprite atlases + metadata
* batched particles
* parallax/multilayer backgrounds
* post-process-style palette/flash/distortion effects where appropriate
* resolution-independent camera and UI
* fullscreen/windowed/borderless options
* configurable input and controller glyphs
* robust pause/focus behavior

Candidate renderer libraries must remain an implementation detail, not become an editor/engine dependency. MonoGame/FNA/SDL-style solutions are acceptable; Unity is not required.

**Gate:** 60/120 FPS remains stable under the heaviest planned particle/enemy encounter on ordinary Windows hardware.

\---

## Stage 11 — QA, tuning and shipping

* deterministic seed fuzzing across thousands of generated stages
* reachability validator catches impossible mandatory geometry
* boss regression tests
* save migration tests
* controller hot-plug / focus / alt-tab tests
* ultrawide and common display aspect ratios
* difficulty tuning from clean-save playthrough
* crash logging
* self-contained win-x64 release package
* credits preserve original project history

**Done means:** beginning-to-end campaign completion, all eight bosses, 36+ distinct minions, secret-code replay, reproducible seeded stages, polished art/audio, no Unity dependency, and no known impossible generated mandatory route.
Done also means: exhaustive remarks in source for future enhancement, gameplay documentation, sound, video, control configuration, menu system, save and load mechanics, story line and dialog, minimaly cutscene placeholders with the static storyboard or slideshow for the cutscene in the video format of the eventual cutscenes.

