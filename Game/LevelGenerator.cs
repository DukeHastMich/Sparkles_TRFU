using System.Numerics;

namespace SparklesReborn;

/// <summary>
/// Deterministic authored-procedural level builder.  Geometry is generated as a guaranteed traversal spine first;
/// enemies/rewards/secrets/jokes are then layered from independent RNG streams so cosmetic edits cannot reshuffle combat.
/// </summary>
public static class LevelGenerator
{
    private static readonly string[] JokeSigns =
    {
        "CAUTION: GRAVITY IS ON.",
        "AUTHORIZED UNICORNS ONLY.",
        "THIS PIT PASSED INSPECTION. PROBABLY.",
        "NO GALLOPING.  — MANAGEMENT",
        "RAINBOW EXHAUST MAY VOID WARRANTY.",
        "HORNS ARE NOT LOAD-BEARING EQUIPMENT.",
        "FREE HUGS →  (THIS IS A TRAP)",
        "THE SQUIRRELS HAVE UNIONIZED.",
        "ABSOLUTELY NO HEROICS PAST THIS POINT.",
        "CAKE STORAGE  /  FAIRY INTAKE",
        "IF FOUND FACE-DOWN, TURN UNICORN OVER.",
        "BRIDGE OUT.  BRIDGE WAS NEVER IN.",
        "PLEASE DO NOT FEED THE NIGHTMARE.",
        "SAFETY THIRD.",
        "YOU ARE NOW ENTERING A LOW-DIGNITY ZONE.",
        "WARNING: TEDDY BEARS MAY CONTAIN MALICE.",
        "THE FLOOR IS LAVA. LEGALLY, THIS IS A METAPHOR.",
        "UNICORN JOUSTING LANE — MERGE WITH CONFIDENCE.",
        "THIS WAY TO SOMETHING QUESTIONABLE →",
        "WE TRIED NORMAL. NOBODY LIKED IT.",
        "MANAGEMENT IS NOT RESPONSIBLE FOR FAIRY INGESTION.",
        "DO NOT TAUNT THE PORCELAIN CHILDREN.",
        "THE GOBLIN OSHA REPRESENTATIVE HAS RESIGNED.",
        "EMERGENCY EXIT: KEEP GOING UNTIL THE MUSIC CHANGES.",
        "THIS SIGN IS A CHECKPOINT FOR THE SIGN.",
        "WARNING: CHROME MAY BE SHARPER THAN IT LOOKS.",
        "BEWARE OF FALLING PRODUCT SYNERGY.",
        "NO REFUNDS AFTER THE THIRD EXPLOSION.",
        "CAUTION: LOCAL SQUIRRELS HAVE A COLLECTIVE BARGAINING AGREEMENT.",
        "YOUR WARRANTY DOES NOT COVER HORSE-BASED BALLISTICS."
    };

    private static readonly string[] SecretSyllables =
    {
        "FART", "HOOF", "GLIT", "PIX", "MOON", "BORK", "ZAP", "HONK",
        "WOB", "RAIN", "CAKE", "VOID", "SPRK", "GNAR", "BONG", "MEEP"
    };

    public static Level Generate(CampaignLevel desc)
    {
        LevelRngStreams streams = new(desc.Seed);
        Level level = new()
        {
            GeneratorVersion = StableHash.GeneratorVersion,
            CampaignIndex = desc.Index - 1,
            Name = desc.Name,
            Seed = desc.Seed,
            Biome = desc.Biome,
            Difficulty = desc.Difficulty,
            BossKind = desc.Boss,
            SetPiece = desc.SetPiece
        };

        float x = 0;
        float y = 560;
        AddGround(level, 0, y, 1250, SurfaceForBiome(level.Biome));
        level.Route.Add(new RoutePoint(260, y, RouteRequirement.Ground));
        level.Signs.Add(new SignPost(260, y - 8,
            desc.Index == 1 ? "WELCOME TO SPARKLE'S PASTURE. TRY NOT TO WEAPONIZE ANYTHING." : desc.Name.ToUpperInvariant()));
        level.Checkpoints.Add(new Checkpoint(260));
        x = 1150;

        // Later worlds are genuinely longer, not just more HP.  A late stage contains 45-50 composed macros.
        int sections = 12 + (int)MathF.Round(Math.Max(1, desc.Index) * 1.30f);
        bool setPiecePlaced = false;

        for (int s = 0; s < sections; s++)
        {
            if (!setPiecePlaced && desc.SetPiece != SpecialSetPiece.None && s >= sections / 2)
            {
                level.Checkpoints.Add(new Checkpoint(Math.Max(260, x - 150)));
                x = AddSetPiece(level, streams, desc.SetPiece, x, ref y);
                level.Checkpoints.Add(new Checkpoint(Math.Max(260, x - 220)));
                setPiecePlaced = true;
                continue;
            }

            int grammarCount = desc.Difficulty switch
            {
                < .12f => 4,
                < .25f => 6,
                < .42f => 10,
                < .62f => 14,
                < .82f => 17,
                _ => 18
            };
            int pattern = streams.Geometry.Next(0, grammarCount);
            x = pattern switch
            {
                0 => Flat(level, streams, x, ref y, 700, 1250, desc.Difficulty),
                1 => Rolling(level, streams, x, ref y, desc.Difficulty),
                2 => ShortGap(level, streams, x, ref y, desc.Difficulty),
                3 => Stairs(level, streams, x, ref y, desc.Difficulty),
                4 => PlatformHop(level, streams, x, ref y, desc.Difficulty),
                5 => BoostGap(level, streams, x, ref y, desc.Difficulty),
                6 => JoustHall(level, streams, x, ref y, desc.Difficulty),
                7 => StompGallery(level, streams, x, ref y, desc.Difficulty),
                8 => ChaosLane(level, streams, x, ref y, desc.Difficulty),
                9 => CrumblingBridge(level, streams, x, ref y, desc.Difficulty),
                10 => VerticalBoostShaft(level, streams, x, ref y, desc.Difficulty),
                11 => JoustSlalom(level, streams, x, ref y, desc.Difficulty),
                12 => AmbushCourtyard(level, streams, x, ref y, desc.Difficulty),
                13 => IceMomentum(level, streams, x, ref y, desc.Difficulty),
                14 => HazardCorridor(level, streams, x, ref y, desc.Difficulty),
                15 => CaveSqueeze(level, streams, x, ref y, desc.Difficulty),
                16 => FortressGate(level, streams, x, ref y, desc.Difficulty),
                _ => MultiRoute(level, streams, x, ref y, desc.Difficulty)
            };

            if (s > 1 && s % 5 == 0)
            {
                level.Checkpoints.Add(new Checkpoint(Math.Max(260, x - 180)));
                level.Signs.Add(new SignPost(x - 260, y - 8, "CHECKPOINT — DIGNITY NOT RESTORED"));
            }

            if (streams.Jokes.Chance(.20f))
                level.Signs.Add(new SignPost(x - streams.Jokes.Range(220, 520), y - 8, streams.Jokes.Pick(JokeSigns)));

            // Secret codes live on optional routes. They never change mandatory geometry.
            if (desc.Index >= 4 && s > 2 && streams.Secrets.Chance(.06f))
            {
                string code = MakeSecretCode(desc.Seed, s);
                float sx = x - streams.Secrets.Range(300, 620);
                float sy = y - streams.Secrets.Range(125, 210);
                level.Platforms.Add(new Platform(sx - 90, sy, 220, 22, PlatformKind.Cloud));
                level.Route.Add(new RoutePoint(sx + 20, sy, RouteRequirement.Optional, false));
                level.Pickups.Add(new Pickup(new Vector2(sx + 20, sy - 42), PickupKind.SecretCode) { Code = code });
            }
        }

        // Final approach and arena.
        x += 80;
        AddGround(level, x, y, 980, SurfaceForBiome(level.Biome));
        Populate(level, streams, x + 140, y, 790, desc.Difficulty, 1.25f);
        level.Route.Add(new RoutePoint(x + 900, y, RouteRequirement.Ground));
        x += 880;
        level.Checkpoints.Add(new Checkpoint(x - 100));

        if (desc.Boss is not null)
        {
            PlatformKind arenaKind = desc.Biome == Biome.Ice ? PlatformKind.Ice : PlatformKind.Stone;
            AddGround(level, x, y, 2450, arenaKind);
            level.Signs.Add(new SignPost(x + 180, y - 8, BossIntro(desc.Boss.Value)));
            Enemy boss = MakeEnemy(desc.Boss.Value, new Vector2(x + 1600, y), ref streams.Enemies);
            level.Enemies.Add(boss);
            level.Route.Add(new RoutePoint(x + 2200, y, RouteRequirement.Ground));
            x += 2350;
        }
        else
        {
            AddGround(level, x, y, 1250, SurfaceForBiome(level.Biome));
            level.Route.Add(new RoutePoint(x + 1100, y, RouteRequirement.Ground));
            x += 1150;
        }

        level.Exit = new RectangleF(x - 140, y - 150, 90, 150);
        level.Width = x + 300;
        level.GameplayFingerprint = LevelFingerprint.Gameplay(level);
        level.DecorationFingerprint = LevelFingerprint.Decoration(level);
        return level;
    }

    public static Level GenerateSecret(string rawCode)
    {
        string code = NormalizeCode(rawCode);
        ulong seed = StableHash.Of(StableHash.GeneratorVersion, "SECRET", code);
        DeterministicRng selector = new(seed);
        Biome biome = (Biome)selector.Next(0, Enum.GetValues<Biome>().Length);
        if (biome == Biome.Nightmare && code.Length < 5) biome = Biome.Pasture;
        float difficulty = .25f + selector.NextFloat() * .72f;

        SpecialSetPiece set = code switch
        {
            "RAINBOWRAMJET" or "FAIRYRAMJET" => SpecialSetPiece.RamjetRunway,
            "GNOMESNACKS" => SpecialSetPiece.TurboJoustFreeway,
            "1994" => SpecialSetPiece.HauntedToyConveyor,
            "VOIDDUCK" => SpecialSetPiece.NightmareRealityBreakdown,
            _ => SpecialSetPiece.None
        };

        string name = code switch
        {
            "VOIDDUCK" => "The Honkening",
            "ED" => "Ed's Big Day",
            "1994" => "EXTREME! PRODUCT SYNERGY ZONE",
            "RAINBOWRAMJET" or "FAIRYRAMJET" => "Unlicensed Fairy Propulsion Lab",
            "GNOMESNACKS" => "The Developer Ate The Map",
            _ => "Uncharted Glitter: " + code
        };

        CampaignLevel pseudo = new(0, name, seed, biome, difficulty, code == "ED" ? EnemyKind.Ed : null, set);
        Level level = Generate(pseudo);
        level.CampaignIndex = -1;
        level.Secret = true;
        level.SecretCode = code;
        level.Name = name;
        level.Seed = seed;

        // Hand-authored Easter-egg overlays use deterministic coordinates and remain outside campaign topology.
        if (code == "VOIDDUCK")
        {
            level.Biome = Biome.Night;
            level.Signs.Insert(0, new SignPost(360, 552, "THE VOID DUCK DOES NOT ATTACK. IT JUDGES TRAJECTORY."));
            for (int i = 0; i < 18; i++)
                level.Pickups.Add(new Pickup(new Vector2(900 + i * 390, 390 + (i % 3) * 45), PickupKind.Pixie));
        }
        if (code == "1994")
        {
            level.Signs.Insert(0, new SignPost(300, 552, "NOW WITH 32-BIT ATTITUDE AND 16-BIT ACCOUNTING!"));
            level.Signs.Insert(1, new SignPost(800, 552, "FREE CD-ROM INSIDE! MODEM NOT INCLUDED."));
        }
        if (code == "GNOMESNACKS")
            level.Signs.Insert(0, new SignPost(300, 552, "DEVELOPER AREA — ASSETS MAY BE HALF-EATEN."));

        level.GameplayFingerprint = LevelFingerprint.Gameplay(level);
        level.DecorationFingerprint = LevelFingerprint.Decoration(level);
        return level;
    }

    public static string NormalizeCode(string code)
        => new(code.ToUpperInvariant().Where(char.IsLetterOrDigit).Take(18).ToArray());

    private static string MakeSecretCode(ulong seed, int salt)
    {
        DeterministicRng r = new(StableHash.Of(seed, "REVEAL", salt));
        return $"{r.Pick(SecretSyllables)}-{r.Pick(SecretSyllables)}-{r.Next(10, 99)}";
    }

    private static float AddSetPiece(Level l, LevelRngStreams s, SpecialSetPiece set, float x, ref float y)
    {
        return set switch
        {
            SpecialSetPiece.RamjetRunway => AddRamjetRunway(l, s, x, ref y),
            SpecialSetPiece.TurboJoustFreeway => AddTurboJoustFreeway(l, s, x, ref y),
            SpecialSetPiece.SquirrelUnionPicketLine => AddSquirrelUnionPicket(l, s, x, ref y),
            SpecialSetPiece.HauntedToyConveyor => AddHauntedToyConveyor(l, s, x, ref y),
            SpecialSetPiece.CountSpatulaKitchen => AddCountSpatulaKitchen(l, s, x, ref y),
            SpecialSetPiece.IceRinkPoorDecisions => AddIceRink(l, s, x, ref y),
            SpecialSetPiece.WolfenMoonChase => AddWolfenMoonChase(l, s, x, ref y),
            SpecialSetPiece.NightmareRealityBreakdown => AddNightmareBreakdown(l, s, x, ref y),
            _ => x
        };
    }

    private static float Flat(Level l, LevelRngStreams s, float x, ref float y, float min, float max, float d)
    {
        float w = s.Geometry.Range(min, max);
        AddGround(l, x, y, w, SurfaceForBiome(l.Biome));
        Populate(l, s, x, y, w, d);
        l.Route.Add(new RoutePoint(x + w - 40, y, RouteRequirement.Ground));
        return x + w;
    }

    private static float Rolling(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        int count = s.Geometry.Next(3, 6);
        for (int i = 0; i < count; i++)
        {
            y = Math.Clamp(y + s.Geometry.Range(-55, 55), 430, 600);
            float w = s.Geometry.Range(300, 520);
            AddGround(l, x, y, w, SurfaceForBiome(l.Biome));
            Populate(l, s, x, y, w, d, .65f);
            x += w;
            l.Route.Add(new RoutePoint(x - 20, y, RouteRequirement.Ground));
        }
        return x;
    }

    private static float ShortGap(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float pre = s.Geometry.Range(300, 520);
        AddGround(l, x, y, pre, SurfaceForBiome(l.Biome));
        Populate(l, s, x, y, pre, d, .5f);
        x += pre;
        l.Route.Add(new RoutePoint(x - 25, y, RouteRequirement.Ground));
        float gap = s.Geometry.Range(150, Math.Min(500, 280 + d * 170));
        if (s.Rewards.Chance(.35f)) l.Pickups.Add(new Pickup(new Vector2(x + gap / 2, y - 135), PickupKind.RainbowDrop));
        x += gap;
        y = Math.Clamp(y + s.Geometry.Range(-35, 45), 440, 600);
        float landing = s.Geometry.Range(550, 900);
        AddGround(l, x, y, landing, SurfaceForBiome(l.Biome));
        l.Route.Add(new RoutePoint(x + 30, y, RouteRequirement.Jump));
        Populate(l, s, x + 120, y, landing - 150, d, .75f);
        return x + landing;
    }

    private static float BoostGap(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float pre = 560;
        AddGround(l, x, y, pre, SurfaceForBiome(l.Biome));
        l.Pickups.Add(new Pickup(new Vector2(x + 350, y - 95), PickupKind.BoostExtender) { Required = true });
        l.Signs.Add(new SignPost(x + 120, y - 8, "RAINBOW ASSIST RECOMMENDED →"));
        x += pre;
        l.Route.Add(new RoutePoint(x - 20, y, RouteRequirement.Ground));
        float gap = s.Geometry.Range(520, Math.Min(1050, 720 + d * 300));
        for (int i = 1; i <= 4; i++) l.Pickups.Add(new Pickup(new Vector2(x + gap * i / 5, y - 120 - (i % 2) * 35), PickupKind.Sparkly));
        x += gap;
        y = Math.Clamp(y + s.Geometry.Range(-25, 35), 445, 595);
        AddGround(l, x, y, 820, SurfaceForBiome(l.Biome));
        l.Route.Add(new RoutePoint(x + 35, y, RouteRequirement.Boost));
        Populate(l, s, x + 120, y, 650, d, .8f);
        return x + 820;
    }

    private static float Stairs(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        bool up = s.Geometry.Chance(.5f);
        int steps = s.Geometry.Next(4, 7);
        for (int i = 0; i < steps; i++)
        {
            y = Math.Clamp(y + (up ? -34 : 34), 420, 610);
            AddGround(l, x, y, 280, i % 2 == 0 ? PlatformKind.Stone : SurfaceForBiome(l.Biome));
            if (s.Rewards.Chance(.35f)) l.Pickups.Add(new Pickup(new Vector2(x + 140, y - 75), PickupKind.Sparkly));
            x += 280;
            l.Route.Add(new RoutePoint(x - 20, y, RouteRequirement.Ground));
        }
        AddGround(l, x, y, 520, SurfaceForBiome(l.Biome));
        Populate(l, s, x, y, 520, d);
        return x + 520;
    }

    private static float PlatformHop(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        AddGround(l, x, y, 300, SurfaceForBiome(l.Biome));
        x += 300;
        l.Route.Add(new RoutePoint(x - 20, y, RouteRequirement.Ground));
        float gapTotal = s.Geometry.Range(650, Math.Min(1050, 820 + d * 260));
        int islands = s.Geometry.Next(2, 4);
        float lastX = x;
        for (int i = 0; i < islands; i++)
        {
            float px = x + gapTotal * (i + 1) / (islands + 1) - 90;
            float py = y - s.Geometry.Range(80, 180);
            l.Platforms.Add(new Platform(px, py, 180, 24, PlatformKind.Cloud));
            l.Pickups.Add(new Pickup(new Vector2(px + 90, py - 45), i == islands - 1 && s.Rewards.Chance(.25f) ? PickupKind.FairyCake : PickupKind.Sparkly));
            l.Route.Add(new RoutePoint(px + 90, py, RouteRequirement.Jump));
            lastX = px + 180;
        }
        x += gapTotal;
        AddGround(l, x, y, 760, SurfaceForBiome(l.Biome));
        l.Route.Add(new RoutePoint(x + 35, y, RouteRequirement.Jump));
        Populate(l, s, x + 100, y, 620, d, .65f);
        return x + 760;
    }

    private static float JoustHall(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float w = s.Geometry.Range(1500, 2200);
        AddGround(l, x, y, w, PlatformKind.Stone);
        l.Signs.Add(new SignPost(x + 120, y - 8, "JOUSTING LANE — ACCELERATE RESPONSIBLY (OPTIONAL)"));
        int count = s.Enemies.Next(4, 8 + (int)(d * 5));
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = i % 3 == 2 && d > .35f ? EnemyKind.UnicornHunter : PickEnemy(l.Biome, ref s.Enemies, d);
            l.Enemies.Add(MakeEnemy(kind, new Vector2(x + 480 + i * ((w - 650) / Math.Max(1, count)), y), ref s.Enemies));
        }
        l.Pickups.Add(new Pickup(new Vector2(x + 260, y - 85), PickupKind.BoostExtender));
        l.Route.Add(new RoutePoint(x + w - 30, y, RouteRequirement.JoustSpeed));
        return x + w;
    }

    private static float StompGallery(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float w = s.Geometry.Range(1200, 1700);
        AddGround(l, x, y, w, SurfaceForBiome(l.Biome));
        l.Signs.Add(new SignPost(x + 120, y - 8, "FOUR-HOOF CERTIFICATION AREA"));
        int stacks = s.Geometry.Next(2, 5);
        for (int i = 0; i < stacks; i++)
        {
            float bx = x + 350 + i * 300;
            l.Platforms.Add(new Platform(bx, y - 120, 150, 28, PlatformKind.Breakable));
            EnemyKind target = d > .45f && i % 2 == 1 ? EnemyKind.MushroomBouncer : PickEnemy(l.Biome, ref s.Enemies, d * .8f);
            l.Enemies.Add(MakeEnemy(target, new Vector2(bx + 75, y), ref s.Enemies));
            l.Pickups.Add(new Pickup(new Vector2(bx + 75, y - 180), PickupKind.Sparkly));
        }
        l.Route.Add(new RoutePoint(x + w - 30, y, RouteRequirement.StompBounce));
        return x + w;
    }

    private static float ChaosLane(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float w = s.Geometry.Range(1600, 2500);
        AddGround(l, x, y, w, SurfaceForBiome(l.Biome));
        Populate(l, s, x + 160, y, w - 240, d, 1.8f);
        for (int i = 0; i < 3; i++)
        {
            float px = x + 450 + i * 420;
            l.Platforms.Add(new Platform(px, y - 135 - (i % 2) * 65, 240, 24, i % 2 == 0 ? PlatformKind.Cloud : PlatformKind.Breakable));
        }
        l.Route.Add(new RoutePoint(x + w - 30, y, RouteRequirement.Ground));
        return x + w;
    }

    private static float CrumblingBridge(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        AddGround(l, x, y, 360, PlatformKind.Stone);
        x += 360;
        l.Route.Add(new RoutePoint(x - 20, y, RouteRequirement.Ground));
        int slabs = s.Geometry.Next(6, 10);
        for (int i = 0; i < slabs; i++)
        {
            Platform p = new(x + i * 150, y, 132, 30, PlatformKind.Crumbling)
            {
                CrumbleDelay = .75f - Math.Min(.25f, d * .25f)
            };
            l.Platforms.Add(p);
            if (i % 2 == 0) l.Pickups.Add(new Pickup(new Vector2(p.Rect.X + 66, y - 70), PickupKind.Sparkly));
        }
        x += slabs * 150;
        AddGround(l, x, y, 680, SurfaceForBiome(l.Biome));
        l.Route.Add(new RoutePoint(x + 25, y, RouteRequirement.Jump));
        Populate(l, s, x + 150, y, 460, d, .6f);
        return x + 680;
    }

    private static float VerticalBoostShaft(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float baseY = y;
        AddGround(l, x, y, 500, PlatformKind.Stone);
        l.Signs.Add(new SignPost(x + 80, y - 8, "VERTICAL RAINBOW RESEARCH SHAFT — UP IS OPTIONAL UNTIL IT ISN'T"));
        l.Pickups.Add(new Pickup(new Vector2(x + 300, y - 90), PickupKind.BoostExtender) { Required = true });
        float px = x + 520;
        float topY = Math.Max(300, y - (250 + d * 90));
        for (int i = 0; i < 4; i++)
        {
            float py = y - 80 - i * ((y - topY) / 4f);
            l.Platforms.Add(new Platform(px + i * 170, py, 170, 24, PlatformKind.Cloud));
            l.Pickups.Add(new Pickup(new Vector2(px + i * 170 + 85, py - 44), PickupKind.Sparkly));
        }
        x += 1250;
        y = Math.Clamp(topY + 100, 390, 560);
        AddGround(l, x, y, 750, SurfaceForBiome(l.Biome));
        l.Route.Add(new RoutePoint(x + 25, y, RouteRequirement.Boost));
        if (baseY - y > 260) l.Pickups.Add(new Pickup(new Vector2(x + 120, y - 80), PickupKind.RainbowDrop));
        return x + 750;
    }

    private static float JoustSlalom(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float w = 2100 + d * 650;
        AddGround(l, x, y, w, PlatformKind.Stone);
        l.Signs.Add(new SignPost(x + 100, y - 8, "TURBO JOUST SLALOM — HORNS INSIDE THE VEHICLE AT ALL TIMES"));
        l.Pickups.Add(new Pickup(new Vector2(x + 260, y - 85), PickupKind.BoostExtender));
        for (int i = 0; i < 7; i++)
        {
            float ex = x + 600 + i * 250;
            EnemyKind k = i % 3 == 0 ? EnemyKind.ChainmailChicken : i % 3 == 1 ? EnemyKind.CrabKnight : EnemyKind.UnicornHunter;
            l.Enemies.Add(MakeEnemy(k, new Vector2(ex, y), ref s.Enemies));
            if (i % 2 == 1) l.Platforms.Add(new Platform(ex - 45, y - 120, 100, 22, PlatformKind.Cloud));
        }
        l.Route.Add(new RoutePoint(x + w - 25, y, RouteRequirement.JoustSpeed));
        return x + w;
    }

    private static float AmbushCourtyard(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float w = 1850;
        AddGround(l, x, y, w, PlatformKind.Stone);
        l.Platforms.Add(new Platform(x + 450, y - 175, 280, 28, PlatformKind.Stone));
        l.Platforms.Add(new Platform(x + 1120, y - 215, 320, 28, PlatformKind.Stone));
        int count = 5 + (int)(d * 6);
        for (int i = 0; i < count; i++)
        {
            EnemyKind k = PickEnemy(l.Biome, ref s.Enemies, Math.Min(1, d + .15f));
            l.Enemies.Add(MakeEnemy(k, new Vector2(x + 500 + i * 115, y), ref s.Enemies));
        }
        l.Signs.Add(new SignPost(x + 110, y - 8, "AMBUSH COURTYARD — PLEASE FORM AN ORDERLY MOB"));
        l.Route.Add(new RoutePoint(x + w - 25, y, RouteRequirement.Ground));
        return x + w;
    }

    private static float IceMomentum(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float w = 1800 + d * 500;
        AddGround(l, x, y, w, PlatformKind.Ice);
        l.Signs.Add(new SignPost(x + 120, y - 8, "ICE HAS BEEN INSTALLED. BRAKES HAVE NOT."));
        for (int i = 0; i < 5; i++)
        {
            float hx = x + 430 + i * 290;
            if (i % 2 == 0) l.Hazards.Add(new Hazard(HazardKind.IceShard, new RectangleF(hx, y - 46, 36, 46)));
            else l.Enemies.Add(MakeEnemy(i % 4 == 1 ? EnemyKind.SkiGoblin : EnemyKind.FrostWerepup, new Vector2(hx, y), ref s.Enemies));
        }
        l.Route.Add(new RoutePoint(x + w - 20, y, RouteRequirement.Ground));
        return x + w;
    }

    private static float HazardCorridor(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float w = 1900 + d * 600;
        AddGround(l, x, y, w, PlatformKind.Stone);
        l.Signs.Add(new SignPost(x + 100, y - 8, "HAZARD DEMONSTRATION CORRIDOR — DEMONSTRATION IS MANDATORY"));
        int count = 5 + (int)(d * 4);
        for (int i = 0; i < count; i++)
        {
            float hx = x + 420 + i * ((w - 650) / Math.Max(1, count));
            Hazard h = (i % 3) switch
            {
                0 => new Hazard(HazardKind.Spikes, new RectangleF(hx, y - 34, 86, 34)),
                1 => new Hazard(HazardKind.FireJet, new RectangleF(hx, y - 150, 54, 150)) { Phase = i * .7f },
                _ => new Hazard(HazardKind.SawBlade, new RectangleF(hx, y - 70, 64, 64)) { Axis = Vector2.UnitY, Amplitude = 70, Speed = 2.2f, Phase = i }
            };
            l.Hazards.Add(h);
        }
        l.Route.Add(new RoutePoint(x + w - 25, y, RouteRequirement.Ground));
        return x + w;
    }

    private static float CaveSqueeze(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float w = 1800;
        AddGround(l, x, y, w, PlatformKind.Stone);
        l.Platforms.Add(new Platform(x + 450, y - 240, 1050, 60, PlatformKind.Stone));
        l.Signs.Add(new SignPost(x + 90, y - 8, "LOW CEILING — UNICORN HORNS ARE NOT A HARD HAT"));
        for (int i = 0; i < 5; i++)
        {
            float ex = x + 600 + i * 190;
            l.Enemies.Add(MakeEnemy(i % 2 == 0 ? EnemyKind.CaveSpiderling : EnemyKind.StalactiteGremlin, new Vector2(ex, y), ref s.Enemies));
        }
        l.Route.Add(new RoutePoint(x + w - 25, y, RouteRequirement.Ground));
        return x + w;
    }

    private static float FortressGate(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float w = 2200;
        AddGround(l, x, y, w, PlatformKind.Stone);
        l.Platforms.Add(new Platform(x + 980, y - 220, 90, 220, PlatformKind.Breakable));
        l.Signs.Add(new SignPost(x + 110, y - 8, "FORTRESS GATE — KNOCK POLITELY AT 700 MPH"));
        l.Pickups.Add(new Pickup(new Vector2(x + 350, y - 90), PickupKind.BoostExtender));
        for (int i = 0; i < 5; i++)
        {
            EnemyKind k = i % 2 == 0 ? EnemyKind.ClockworkHunter : EnemyKind.CursedKnight;
            l.Enemies.Add(MakeEnemy(k, new Vector2(x + 1200 + i * 175, y), ref s.Enemies));
        }
        l.Route.Add(new RoutePoint(x + w - 25, y, RouteRequirement.JoustSpeed));
        return x + w;
    }

    private static float MultiRoute(Level l, LevelRngStreams s, float x, ref float y, float d)
    {
        float w = 2300;
        AddGround(l, x, y, w, SurfaceForBiome(l.Biome));
        l.Signs.Add(new SignPost(x + 90, y - 8, "SHORT DANGEROUS ROUTE ↑   LONG QUESTIONABLE ROUTE →"));
        // Required lower route remains safe.
        Populate(l, s, x + 450, y, w - 600, d, .75f);
        l.Route.Add(new RoutePoint(x + w - 20, y, RouteRequirement.Ground));

        // Independent optional high route has better rewards and nastier enemies.
        float py = y - 190;
        for (int i = 0; i < 6; i++)
        {
            float px = x + 450 + i * 270;
            l.Platforms.Add(new Platform(px, py - (i % 2) * 55, 210, 24, PlatformKind.Cloud));
            l.Pickups.Add(new Pickup(new Vector2(px + 105, py - 45 - (i % 2) * 55), i == 5 ? PickupKind.FairyCake : PickupKind.Sparkly));
            if (i is 2 or 4) l.Enemies.Add(MakeEnemy(PickEnemy(l.Biome, ref s.Enemies, Math.Min(1, d + .2f)), new Vector2(px + 105, py - (i % 2) * 55), ref s.Enemies));
            l.Route.Add(new RoutePoint(px + 105, py, RouteRequirement.Optional, false));
        }
        return x + w;
    }

    // ------------------------- SIGNATURE SET PIECES -------------------------

    private static float AddRamjetRunway(Level l, LevelRngStreams s, float x, ref float y)
    {
        const float length = 6000;
        AddGround(l, x, y, length, PlatformKind.Stone);
        l.Signs.Add(new SignPost(x + 120, y - 8, "RAMJET RUNWAY — HOLD DASH. WHAT COULD POSSIBLY GO WRONG?"));
        l.Route.Add(new RoutePoint(x + length - 30, y, RouteRequirement.ExtendedBoost));

        for (int i = 0; i < 6; i++)
            l.Pickups.Add(new Pickup(new Vector2(x + 480 + i * 720, y - 85), PickupKind.BoostExtender) { Required = true });

        // Exactly thirty pixies on the canonical intake path.  At speed they are vacuumed toward Sparkles.
        for (int i = 0; i < 30; i++)
        {
            float px = x + 900 + i * 145;
            float py = y - 95 - (i % 5) * 20;
            l.Pickups.Add(new Pickup(new Vector2(px, py), PickupKind.Pixie) { Required = true });
        }

        // The lane escalates from harmless targets into a rainbow blast-wave casualty report.
        EnemyKind[] victims = { EnemyKind.Teddy, EnemyKind.Goblin, EnemyKind.ChainmailChicken, EnemyKind.UnicornHunter, EnemyKind.RollerbladeOrc };
        for (int i = 0; i < 16; i++)
            l.Enemies.Add(MakeEnemy(victims[i % victims.Length], new Vector2(x + 1350 + i * 250, y), ref s.Enemies));

        l.Signs.Add(new SignPost(x + 4350, y - 8, "FAIRY INTAKE LIMIT: 29. THIS SIGN WAS INSTALLED TOO LATE."));
        l.Signs.Add(new SignPost(x + 5400, y - 8, "END OF RUNWAY — PLEASE RETURN UNICORN IN ORIGINAL SHAPE."));
        return x + length;
    }

    private static float AddTurboJoustFreeway(Level l, LevelRngStreams s, float x, ref float y)
    {
        const float length = 5200;
        AddGround(l, x, y, length, PlatformKind.Stone);
        l.Signs.Add(new SignPost(x + 100, y - 8, "TURBO JOUST FREEWAY — LEFT LANE FOR UNREASONABLE VELOCITY"));
        l.Pickups.Add(new Pickup(new Vector2(x + 330, y - 85), PickupKind.BoostExtender));
        for (int i = 0; i < 18; i++)
        {
            EnemyKind k = (i % 4) switch
            {
                0 => EnemyKind.ChainmailChicken,
                1 => EnemyKind.UnicornHunter,
                2 => EnemyKind.CursedKnight,
                _ => EnemyKind.RollerbladeOrc
            };
            l.Enemies.Add(MakeEnemy(k, new Vector2(x + 900 + i * 220, y), ref s.Enemies));
            if (i % 5 == 0) l.Pickups.Add(new Pickup(new Vector2(x + 760 + i * 220, y - 88), PickupKind.RainbowDrop));
        }
        l.Route.Add(new RoutePoint(x + length - 25, y, RouteRequirement.JoustSpeed));
        return x + length;
    }

    private static float AddSquirrelUnionPicket(Level l, LevelRngStreams s, float x, ref float y)
    {
        const float length = 3600;
        AddGround(l, x, y, length, SurfaceForBiome(l.Biome));
        l.Signs.Add(new SignPost(x + 100, y - 8, "LOCAL 47 — SQUIRRELS DEMAND NUT EQUITY AND DENTAL."));
        for (int i = 0; i < 22; i++)
        {
            EnemyKind k = i is 6 or 14 ? EnemyKind.SquirrelUnionSteward : EnemyKind.RabidSquirrel;
            l.Enemies.Add(MakeEnemy(k, new Vector2(x + 650 + i * 120, y), ref s.Enemies));
        }
        l.Signs.Add(new SignPost(x + 2450, y - 8, "SCAB!  (THEY MEAN YOU. PROBABLY.)"));
        l.Route.Add(new RoutePoint(x + length - 20, y, RouteRequirement.Ground));
        return x + length;
    }

    private static float AddHauntedToyConveyor(Level l, LevelRngStreams s, float x, ref float y)
    {
        const float length = 4400;
        AddGround(l, x, y, 500, PlatformKind.Stone);
        x += 500;
        for (int i = 0; i < 10; i++)
        {
            Platform p = new(x + i * 360, y, 340, 44, PlatformKind.Conveyor) { ConveyorSpeed = (i % 2 == 0 ? 1 : -1) * (110 + i * 7) };
            l.Platforms.Add(p);
            EnemyKind k = i % 3 == 0 ? EnemyKind.PorcelainDoll : i % 3 == 1 ? EnemyKind.HauntedTrapperKeeper : EnemyKind.BoomBoxMimic;
            l.Enemies.Add(MakeEnemy(k, new Vector2(p.Rect.X + p.Rect.Width / 2, y), ref s.Enemies));
        }
        l.Signs.Add(new SignPost(x + 80, y - 8, "HAUNTED TOY CONVEYOR — PRODUCT MAY SCREAM DURING SHIPPING"));
        l.Route.Add(new RoutePoint(x + 3600, y, RouteRequirement.Ground));
        AddGround(l, x + 3600, y, 500, PlatformKind.Stone);
        return x + length - 100;
    }

    private static float AddCountSpatulaKitchen(Level l, LevelRngStreams s, float x, ref float y)
    {
        const float length = 4200;
        AddGround(l, x, y, length, PlatformKind.Stone);
        l.Signs.Add(new SignPost(x + 100, y - 8, "COUNT SPATULA'S NON-STICK KITCHEN OF DOOM — PAM IS NOT A DEFENSE"));
        for (int i = 0; i < 7; i++)
        {
            float hx = x + 700 + i * 470;
            l.Hazards.Add(new Hazard(HazardKind.FireJet, new RectangleF(hx, y - 170, 70, 170)) { Phase = i * .65f });
            l.Enemies.Add(MakeEnemy(i % 2 == 0 ? EnemyKind.PossessedToaster : EnemyKind.VampireBatling, new Vector2(hx + 170, y), ref s.Enemies));
        }
        l.Route.Add(new RoutePoint(x + length - 20, y, RouteRequirement.Ground));
        return x + length;
    }

    private static float AddIceRink(Level l, LevelRngStreams s, float x, ref float y)
    {
        const float length = 4200;
        AddGround(l, x, y, length, PlatformKind.Ice);
        l.Signs.Add(new SignPost(x + 100, y - 8, "THE ICE RINK OF POOR DECISIONS — COMMIT TO THE SLIDE"));
        for (int i = 0; i < 12; i++)
        {
            EnemyKind k = i % 3 == 0 ? EnemyKind.AngrySnowman : i % 3 == 1 ? EnemyKind.SkiGoblin : EnemyKind.FrostWerepup;
            l.Enemies.Add(MakeEnemy(k, new Vector2(x + 650 + i * 260, y), ref s.Enemies));
            if (i % 4 == 2) l.Hazards.Add(new Hazard(HazardKind.IceShard, new RectangleF(x + 700 + i * 260, y - 48, 42, 48)));
        }
        l.Route.Add(new RoutePoint(x + length - 20, y, RouteRequirement.Ground));
        return x + length;
    }

    private static float AddWolfenMoonChase(Level l, LevelRngStreams s, float x, ref float y)
    {
        const float length = 5000;
        AddGround(l, x, y, length, SurfaceForBiome(l.Biome));
        l.Signs.Add(new SignPost(x + 100, y - 8, "WOLFEN MOON CHASE — IF YOU HEAR BREATHING, FART FASTER"));
        l.Pickups.Add(new Pickup(new Vector2(x + 300, y - 85), PickupKind.BoostExtender));
        for (int i = 0; i < 15; i++)
        {
            EnemyKind k = i % 4 == 0 ? EnemyKind.NightmareColt : i % 2 == 0 ? EnemyKind.Werewolf : EnemyKind.FrostWerepup;
            l.Enemies.Add(MakeEnemy(k, new Vector2(x + 1000 + i * 240, y), ref s.Enemies));
        }
        l.Route.Add(new RoutePoint(x + length - 20, y, RouteRequirement.Boost));
        return x + length;
    }

    private static float AddNightmareBreakdown(Level l, LevelRngStreams s, float x, ref float y)
    {
        const float length = 4800;
        AddGround(l, x, y, length, PlatformKind.Stone);
        l.Signs.Add(new SignPost(x + 100, y - 8, "REALITY BREAKDOWN AHEAD — GEOMETRY HAS FILED FOR DIVORCE"));
        for (int i = 0; i < 8; i++)
        {
            float px = x + 500 + i * 500;
            Platform p = new(px, y - 80 - (i % 3) * 70, 270, 30, PlatformKind.Moving)
            {
                MotionOrigin = new Vector2(px, y - 80 - (i % 3) * 70),
                MotionAxis = i % 2 == 0 ? Vector2.UnitY : Vector2.UnitX,
                MotionAmplitude = 75 + (i % 3) * 25,
                MotionSpeed = 1.2f + i * .08f,
                MotionPhase = i * .7f
            };
            l.Platforms.Add(p);
            l.Hazards.Add(new Hazard(HazardKind.NightmareStatic, new RectangleF(px + 300, y - 120, 90, 120)) { Phase = i });
            if (i % 2 == 0) l.Enemies.Add(MakeEnemy(EnemyKind.MallNinjaWizard, new Vector2(px + 160, y), ref s.Enemies));
            else l.Enemies.Add(MakeEnemy(EnemyKind.NightmareColt, new Vector2(px + 160, y), ref s.Enemies));
        }
        l.Route.Add(new RoutePoint(x + length - 20, y, RouteRequirement.Ground));
        return x + length;
    }

    private static void Populate(Level l, LevelRngStreams streams, float x, float y, float w, float d, float density = 1f)
    {
        int enemies = Math.Max(0, (int)(w / 430f * density * (.55f + d * .85f)));
        for (int i = 0; i < enemies; i++)
        {
            float ex = x + 120 + (i + .5f) * Math.Max(120, (w - 240) / Math.Max(1, enemies));
            ex += streams.Enemies.Range(-55, 55);
            EnemyKind kind = PickEnemy(l.Biome, ref streams.Enemies, d);
            l.Enemies.Add(MakeEnemy(kind, new Vector2(ex, y), ref streams.Enemies));
        }

        int rewards = Math.Max(1, (int)(w / 400));
        for (int i = 0; i < rewards; i++)
        {
            float px = x + streams.Rewards.Range(90, Math.Max(100, w - 90));
            float py = y - streams.Rewards.Range(70, 145);
            PickupKind kind = streams.Rewards.NextFloat() switch
            {
                < .08f => PickupKind.FairyCake,
                < .16f => PickupKind.Pixie,
                < .20f => PickupKind.SparklePower,
                < .23f => PickupKind.RainbowDrop,
                _ => PickupKind.Sparkly
            };
            l.Pickups.Add(new Pickup(new Vector2(px, py), kind));
        }
        if (d > .35f && streams.Rewards.Chance(.025f))
            l.Pickups.Add(new Pickup(new Vector2(x + w * .7f, y - 130), PickupKind.OneUp));
    }

    private static Enemy MakeEnemy(EnemyKind kind, Vector2 pos, ref DeterministicRng rng)
    {
        Enemy e = new(kind, pos)
        {
            Direction = rng.Chance(.5f) ? -1 : 1,
            Variant = rng.Next(0, 4)
        };
        return e;
    }

    private static EnemyKind PickEnemy(Biome biome, ref DeterministicRng r, float d)
    {
        EnemyKind[] pool = biome switch
        {
            Biome.Pasture or Biome.Orchard => new[] { EnemyKind.Teddy, EnemyKind.RabidSquirrel, EnemyKind.DireMouse, EnemyKind.CupcakeMimic, EnemyKind.ChainmailChicken, EnemyKind.GnomeLawnCommando, EnemyKind.MushroomBouncer },
            Biome.Forest => new[] { EnemyKind.Teddy, EnemyKind.RabidSquirrel, EnemyKind.EvilTree, EnemyKind.Goblin, EnemyKind.BeehiveKnight, EnemyKind.VineSnatcher, EnemyKind.SquirrelUnionSteward, EnemyKind.MushroomBouncer },
            Biome.Marsh => new[] { EnemyKind.Zombie, EnemyKind.Goblin, EnemyKind.BogGoblin, EnemyKind.SlimeIntern, EnemyKind.PossessedRubberBoot },
            Biome.Mountain => new[] { EnemyKind.Goblin, EnemyKind.DireMouse, EnemyKind.GnomeLawnCommando, EnemyKind.StalactiteGremlin, EnemyKind.GargoyleIntern },
            Biome.Coast => new[] { EnemyKind.CrabKnight, EnemyKind.BeachImp, EnemyKind.Goblin, EnemyKind.PossessedRubberBoot, EnemyKind.DireMouse },
            Biome.Haunted => new[] { EnemyKind.Ghost, EnemyKind.PorcelainDoll, EnemyKind.DiscoSkeleton, EnemyKind.HauntedTrapperKeeper, EnemyKind.SockPuppetNecromancer, EnemyKind.BoomBoxMimic },
            Biome.Crypt => new[] { EnemyKind.Ghost, EnemyKind.Zombie, EnemyKind.VampireBatling, EnemyKind.DiscoSkeleton, EnemyKind.SockPuppetNecromancer },
            Biome.Cave => new[] { EnemyKind.CaveSpiderling, EnemyKind.StalactiteGremlin, EnemyKind.Goblin, EnemyKind.Imp, EnemyKind.DireMouse },
            Biome.Ice => new[] { EnemyKind.SkiGoblin, EnemyKind.AngrySnowman, EnemyKind.FrostWerepup, EnemyKind.Werewolf, EnemyKind.GargoyleIntern },
            Biome.Fortress => new[] { EnemyKind.UnicornHunter, EnemyKind.ClockworkHunter, EnemyKind.CursedKnight, EnemyKind.PossessedToaster, EnemyKind.GargoyleIntern },
            Biome.Weald => new[] { EnemyKind.Werewolf, EnemyKind.NightmareColt, EnemyKind.EvilTree, EnemyKind.UnicornHunter, EnemyKind.FrostWerepup },
            Biome.Nightmare => new[] { EnemyKind.NightmareColt, EnemyKind.MallNinjaWizard, EnemyKind.RollerbladeOrc, EnemyKind.CorporateSafetyFairy, EnemyKind.BoomBoxMimic, EnemyKind.MoonCheeseHomunculus, EnemyKind.Imp, EnemyKind.Werewolf },
            _ => new[] { EnemyKind.Ghost, EnemyKind.Imp, EnemyKind.UnicornHunter, EnemyKind.PossessedToaster, EnemyKind.MallNinjaWizard }
        };
        EnemyKind pick = r.Pick(pool);
        if (d < .22f && EnemyCatalog.Get(pick).Health > 3) pick = EnemyKind.Teddy;
        return pick;
    }

    private static PlatformKind SurfaceForBiome(Biome biome) => biome == Biome.Ice ? PlatformKind.Ice : PlatformKind.Ground;

    private static void AddGround(Level l, float x, float y, float w, PlatformKind kind = PlatformKind.Ground)
        => l.Platforms.Add(new Platform(x, y, w, 500, kind));

    private static string BossIntro(EnemyKind boss) => boss switch
    {
        EnemyKind.Ed => "BOSS: ED — A RATHER PATHETIC SLIME",
        EnemyKind.OldGnarley => "BOSS: OLD GNARLEY — ROOTED IN BAD DECISIONS",
        EnemyKind.Spookers => "BOSS: SPOOKERS — BOO, ET CETERA",
        EnemyKind.Webbey => "BOSS: WEBBEY — EIGHT LEGS, ZERO SOCIAL SKILLS",
        EnemyKind.Mina => "BOSS: MINA — PLEASE KEEP LIMBS INSIDE REALITY",
        EnemyKind.CountSpatula => "BOSS: COUNT SPATULA — LORD OF THE NON-STICK NIGHT",
        EnemyKind.Rip => "BOSS: RIP 'THE PIECE MAKER' — HE MISREAD PEACEMAKER",
        EnemyKind.NightMare => "NIGHT MARE — FINAL WARRANTY-VOIDING EVENT",
        _ => "BOSS"
    };
}
