# Sparkles TRFU — Determinism and Seed Compatibility

## Level identity

Campaign:

`(generatorVersion, campaignMasterSeed, stageIndex, canonicalStageName)`

Secret code:

`(generatorVersion, "SECRET", normalizedSecretCode)`

A save records the generator version. Generator behavior must never be silently changed while keeping the same version number.

## Required RNG streams before content lock

Derive independent sub-seeds from the level seed:

- `GEOMETRY`
- `ENEMIES`
- `REWARDS`
- `SECRETS`
- `DECOR`
- `JOKES`
- `SETPIECES`

This prevents a harmless decoration change from reshuffling mandatory pits/enemies.

## Physics envelope validation

The generator must measure/encode safe traversal envelopes from the actual player controller:

- standing jump reach
- running jump reach
- normal Ramjet reach
- extended Ramjet reach
- vertical boost ceiling
- controllable fall distance/time
- stomp-bounce reach
- required runway for redline joust speed

Mandatory geometry must stay inside validated envelopes with safety margin. Optional challenge routes may approach the limits but still must be mathematically achievable.

## Fingerprinting

For regression tests, generated gameplay-critical data is normalized and hashed:

- platform geometry/type
- enemy kind + spawn position
- required pickups/set-piece items
- checkpoints
- boss identity/arena
- exit position

Decoration and joke text can have separate fingerprints.

A CI/local verification pass should regenerate all 28 campaign stages multiple times and compare fingerprints before release.
