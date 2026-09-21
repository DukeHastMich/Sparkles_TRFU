# Sparkles TRFU — Art and Content Bible

## Visual thesis

Sparkles should look like a 1990s toy commercial, arcade cabinet side art and Saturday-morning fantasy cartoon were forced to share one graphics accelerator.

The visual language is deliberately excessive:

- airbrushed gradients
- glossy highlights
- chrome/gold/plastic trim
- glitter and star glints
- deep saturated skies
- strong rim light on important characters
- goofy but readable facial acting
- oversize anticipation / impact poses
- environmental jokes placed as scenery, not walls of text

Do not turn the game into generic 8/16-bit pixel art. The original assets are old because the project is old; the final game is allowed to be sharper, smoother and richer while retaining its 90s DNA.

## Asset pipeline

All final characters use a consistent atlas contract rather than one-off files scattered through code.

### Standard minion atlas

Required states:

- idle: 4–8 frames
- move: 6–12 frames
- primary attack: 6–12 frames
- secondary/special if applicable: 6–12 frames
- hit: 2–4 frames
- defeat: 6–12 frames
- optional spawn/taunt: 4–10 frames

Every sheet includes frame pivot, collision body, damage hitboxes and optional effect sockets in adjacent metadata.

### Boss atlas

Bosses get:

- entrance
- idle
- movement
- every attack tell
- every attack release/recovery
- hit/react
- phase transition
- defeat
- unique effect layers

Boss art is not a scaled-up minion sheet.

### Sparkles atlas

Sparkles needs dedicated final art for:

- idle/bored/looking-around
- walk/trot/run
- jump rise/apex/fall
- landing compression
- Ramjet start/hold/redline/release
- horn joust low/medium/redline impact
- dedicated Ramjet + horn lance posture: head lowered, horn committed forward, body leaning into the charge; sparkle effect remains a tight orbit around the horn rather than a beam
- four-hoof stomp windup/drop/impact/bounce
- fairy swallow/intake overload/belch
- armor variations
- damage/stun
- faceplant/skid/get-up
- victory and map poses

## 36+ minion target roster

The first eleven are original canon. The remainder are expansion candidates designed to provide new mechanics while fitting the game's tone. Names can evolve during art production.

### Pasture / Orchard

1. **Evil Teddy Bear** — basic readable walker; sometimes tries an overconfident charge.
2. **Rabid Squirrel** — quick ground runner; punishes hesitation.
3. **Dire Mouse** — tiny fast ankle threat; easy to joust at speed, awkward slowly.
4. **Cupcake Mimic** — pretends to be food until approached.
5. **Chainmail Chicken** — front armor, vulnerable to stomp or rear attack.
6. **Gnome Lawn Commando** — hides in scenery then throws tiny garden implements.

### Forest / Weald

7. **Evil Tree** — stationary ranged acorn artillery; original canon.
8. **Goblin** — versatile walker/trickster; original canon.
9. **Beehive Knight** — shielded walker carrying an increasingly angry hive.
10. **Mushroom Bouncer** — enemy/traversal hybrid; stomp launches Sparkles upward.
11. **Vine Snatcher** — ceiling/branch grabber with an obvious attack tell.
12. **Squirrel Union Steward** — buffs nearby squirrels until its clipboard is violently revoked.

### Marsh / Coast

13. **Zombie** — durable shambling body-blocker; original canon.
14. **Bog Goblin** — pops out of muck and retreats.
15. **Slime Intern** — weak cousin of Ed; splits once and files no paperwork.
16. **Possessed Rubber Boot** — hopping hazard with excellent waterproofing.
17. **Crab Knight** — side-to-side armored patrol, vulnerable from above.
18. **Beach Imp** — projectile harassment with ridiculous vacation accessories.

### Haunted / Crypt

19. **Porcelain Doll** — original canon; freezes/changes behavior depending on approach.
20. **Ghost** — original canon; hovering pursuit.
21. **Disco Skeleton** — telegraphed rhythm attacks and shameless dance frames.
22. **Haunted Trapper Keeper** — opens to fire cursed 90s stationery projectiles.
23. **Sock-Puppet Necromancer** — summons disposable nuisances while arguing with its own hand.
24. **Vampire Batling** — short swooping aerial attack.

### Cave / Mountain / Ice

25. **Imp** — original canon; aerial pressure.
26. **Cave Spiderling** — wall/ceiling movement and drop attack.
27. **Stalactite Gremlin** — disguises as scenery before falling.
28. **Ski Goblin** — fast ice momentum enemy that cannot steer well.
29. **Angry Snowman** — rolls growing snowballs downhill.
30. **Frost Werepup** — fast pack hunter; teaches late-game werewolf patterns cheaply.

### Fortress / Night

31. **Unicorn Hunter** — original canon; ranged anti-unicorn specialist.
32. **Werewolf** — original canon; aggressive chase predator.
33. **Clockwork Hunter** — armored patrol with predictable mechanical attack cycles.
34. **Gargoyle Intern** — awakens from background architecture and dive-bombs.
35. **Cursed Knight** — proper jousting opponent; challenges speed/timing.
36. **Nightmare Colt** — late-game mirror of Sparkles movement, but hostile and less fabulous.

### Rare / secret-level wildcards

37. **Possessed Toaster** — launches toast at unreasonable temperature.
38. **Mall Ninja Wizard** — has too many weapons and no idea which one is appropriate.
39. **Rollerblade Orc** — extremely 1990s, extremely committed, limited braking.
40. **Corporate Safety Fairy** — attempts to issue citations while flying directly into danger.
41. **Boom Box Mimic** — music source until it sprouts legs and becomes everybody's problem.
42. **Moon Cheese Homunculus** — secret-code nonsense enemy for lunar/space-themed seeds.

The target is at least 36 shipping minions, with secret-code oddities allowed above that count.

## Canonical bosses and final encounter goals

### Ed — a rather pathetic slime

Purpose: first joke boss / teaches boss health and arena rules. Starts pathetic, develops delusions of grandeur mid-fight, remains Ed.

### Old Gnarley — evil tree

Root/branch control, acorn artillery, breakable weak limbs, stomp opportunities from branch platforms.

### Spookers — ghost

Arena possession, fake platforms, telegraphed phasing, attacks that deliberately abuse background/foreground presentation.

### Webbey — giant spider

Web geometry changes traversal. Sparkles can use web tension / stomp breaks to create openings.

### Mina — shape-shifting bird

Cycles through distinct aerial shapes with different tells; jousting becomes an aerial interception mechanic.

### Count Spatula — vampire lord

Ridiculous gothic-kitchen fortress. Teleports, bat swarms, reflective/metallic props and one attack involving a weaponized spatula because we have standards.

### Rip "The Piece Maker" — werewolf

High-speed chase/joust duel. Aggression, wall rebounds and baited charge attacks.

### Night Mare — final boss

Multi-phase corruption of the game's own rules. Starts as a physical rival, escalates through nightmare set-piece remix, ends with Sparkles using everything learned without invalidating the player's normal mechanics.

## Miniboss candidates

- Big Ted, Regional Manager of Malice
- The Dollmother
- General Nibbles, Squirrel Logistics
- Sir Pewterhorn, Licensed Unicorn Hunter
- The Bog Standard
- DJ Bonez (no relation to competence)
- OSHA Fairy Prime
- The Rollerblade Orc Champion

Minibosses may be attached to particular campaign seeds and later allowed as rare secret-level encounters.

## Environment asset requirements by biome

Each biome gets:

- 3+ parallax sky/background layers
- 6+ ground/platform material variants
- 12+ prop/decor families
- 4+ animated ambience elements
- unique hazard family
- unique collectible presentation variants
- signage/joke prop pool
- at least one giant visual landmark

The generator may vary decoration, but a biome must have visual authorship. Procedural does not mean bland.
