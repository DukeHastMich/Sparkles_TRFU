# Checkpoint status

This checkpoint is intentionally split into **implemented foundation** and **production backlog**.

## Implemented foundation

- [x] modern C# Windows project
- [x] 28 legacy campaign nodes/names
- [x] deterministic campaign seeds
- [x] deterministic secret-code levels
- [x] save/unlock/code discovery
- [x] continuous movement/jump/collision
- [x] Rainbow Ramjet
- [x] Ramjet-coupled jump impulse (ALPHA2.1 bugfix)
- [x] Ramjet punt-vs-horn combat rule (ALPHA2.2): dash body-contact flings normal minions; dash+horn earns the clean kill; spiky/projectile/spike hazards still punish reckless boosting
- [x] Sparkly Horn Joust
- [x] ALPHA2.3 horn-bayonet visual charge tell: wicked corkscrew sparkle swirl while Joust is held
- [x] ALPHA2.6 lance-pose/readability repair: remove laser-like horn core, use discrete orbiting sparkles, pitch Sparkles forward during Ramjet + Joust, and align horn collision with the lowered lance axis
- [x] Four-Hoof Stomp
- [x] Fairy Belch
- [x] Sparkle Armor persistence
- [x] procedural stage grammar
- [x] biome assignment
- [x] original eleven minion families represented
- [x] eight original bosses represented with prototype AI
- [x] Ramjet Runway prototype macro
- [x] thirty-pixie intake threshold + fairy explosion + faceplant behavior
- [x] joke signs / secret code pickups
- [x] keyboard + XInput
- [x] prototype audio system

## Production backlog

- [ ] independent deterministic RNG streams
- [ ] reachability/fingerprint validation tool
- [ ] GPU renderer transition
- [ ] complete Sparkles final animation atlas
- [ ] 36+ final minion roster and art
- [ ] full eight-boss animation/phase production
- [ ] biome background/prop/hazard art passes
- [ ] music/audio content pass
- [ ] extended level grammar and moving hazards
- [ ] final UI and accessibility/options
- [ ] seed fuzz/QA campaign
- [ ] self-contained release validation on Windows

## ALPHA2.4 compile repair

* Fixed C# switch-expression precedence in `LevelGenerator.cs` by parenthesizing modulo selectors.
* Added the renderer implementations for Options, Secret Browser and Story storyboard modes that ALPHA2.3 referenced but did not define.
* No gameplay seed/generation contract changed in this repair.

## ALPHA2.5 visibility repair

* [x] Expansion-roster enemies can no longer exist as invisible combat entities.
* [x] Generic capability-coded alpha silhouettes cover every not-yet-hand-drawn minion family.
* [x] Alpha labels identify placeholder families during playtest.
* [x] DarkBolt has a deliberate hostile-magic renderer instead of the generic purple ball.
* [x] MIDI/synth soundtrack explicitly locked as intentionally awful canon.

## ALPHA2.6 joust visual/pose repair

* [x] Removed the straight glowing horn core that made the sparkle corkscrew read as a laser beam.
* [x] Rebuilt the charge tell as discrete orbiting sparkle motes/glints around the horn axis.
* [x] Ramjet + Joust now enters a forward-pitched lance pose instead of the upright trot pose.
* [x] Horn collision bounds are derived from the same lowered lance axis used by the renderer.
* [x] No seed/generator behavior changed.
