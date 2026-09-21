namespace SparklesReborn;

public sealed record EnemySpec(
    EnemyKind Kind,
    string Name,
    float Width,
    float Height,
    int Health,
    float Speed,
    EnemyCapability Capabilities,
    Color Primary,
    Color Secondary,
    ProjectileKind Projectile = ProjectileKind.Spit,
    float AttackPeriod = 2.2f,
    int ContactDamage = 1);

/// <summary>
/// Canonical gameplay definition for every enemy family. Rendering and behavior both consume this table,
/// so a new minion cannot silently exist as an enum value with no readable dimensions or combat identity.
/// </summary>
public static class EnemyCatalog
{
    private static readonly Dictionary<EnemyKind, EnemySpec> Specs = Build();

    public static EnemySpec Get(EnemyKind kind) => Specs[kind];
    public static IEnumerable<EnemySpec> All => Specs.Values;
    public static IEnumerable<EnemySpec> Minions => Specs.Values.Where(s => (s.Capabilities & EnemyCapability.Boss) == 0);

    private static Dictionary<EnemyKind, EnemySpec> Build()
    {
        var d = new Dictionary<EnemyKind, EnemySpec>();
        void Add(EnemyKind k, string n, float w, float h, int hp, float spd, EnemyCapability c,
            Color a, Color b, ProjectileKind p = ProjectileKind.Spit, float period = 2.2f, int contact = 1)
            => d[k] = new(k, n, w, h, hp, spd, c, a, b, p, period, contact);

        // Original families.
        Add(EnemyKind.Teddy, "Evil Teddy Bear", 58, 58, 1, 72, EnemyCapability.Walker, Color.SaddleBrown, Color.Bisque);
        Add(EnemyKind.Zombie, "Zombie", 60, 82, 1, 58, EnemyCapability.Walker, Color.OliveDrab, Color.MediumPurple);
        Add(EnemyKind.PorcelainDoll, "Porcelain Doll", 52, 76, 2, 92, EnemyCapability.Walker | EnemyCapability.MirrorMovement, Color.MistyRose, Color.HotPink);
        Add(EnemyKind.RabidSquirrel, "Rabid Squirrel", 54, 48, 1, 165, EnemyCapability.Runner | EnemyCapability.Pack, Color.DimGray, Color.Chocolate);
        Add(EnemyKind.DireMouse, "Dire Mouse", 48, 34, 1, 145, EnemyCapability.Runner | EnemyCapability.Pack, Color.DarkSlateGray, Color.LightGray);
        Add(EnemyKind.Ghost, "Ghost", 62, 72, 2, 74, EnemyCapability.Flyer | EnemyCapability.Hover, Color.GhostWhite, Color.MediumPurple);
        Add(EnemyKind.EvilTree, "Evil Tree", 82, 116, 3, 0, EnemyCapability.Ranged | EnemyCapability.Lobber | EnemyCapability.StompImmune, Color.SaddleBrown, Color.ForestGreen, ProjectileKind.Acorn, 2.35f);
        Add(EnemyKind.Goblin, "Goblin", 56, 64, 1, 105, EnemyCapability.Walker, Color.YellowGreen, Color.DarkOliveGreen);
        Add(EnemyKind.Imp, "Imp", 52, 55, 2, 92, EnemyCapability.Flyer | EnemyCapability.Hover | EnemyCapability.Ranged, Color.IndianRed, Color.Gold, ProjectileKind.DarkBolt, 2.0f);
        Add(EnemyKind.UnicornHunter, "Unicorn Hunter", 56, 84, 2, 88, EnemyCapability.Walker | EnemyCapability.ShieldFront | EnemyCapability.Ranged, Color.SlateGray, Color.Firebrick, ProjectileKind.Spit, 2.1f);
        Add(EnemyKind.Werewolf, "Werewolf", 74, 82, 3, 210, EnemyCapability.Runner | EnemyCapability.Charger, Color.DimGray, Color.Silver, contact: 2);

        // Expansion roster. Every family exists because it asks the player to solve a different little problem.
        Add(EnemyKind.CupcakeMimic, "Cupcake Mimic", 58, 48, 2, 175, EnemyCapability.Mimic | EnemyCapability.Runner, Color.HotPink, Color.SandyBrown);
        Add(EnemyKind.ChainmailChicken, "Chainmail Chicken", 60, 56, 2, 128, EnemyCapability.Walker | EnemyCapability.ShieldFront, Color.Silver, Color.Gold);
        Add(EnemyKind.GnomeLawnCommando, "Gnome Lawn Commando", 54, 66, 2, 54, EnemyCapability.Walker | EnemyCapability.Ranged, Color.Red, Color.ForestGreen, ProjectileKind.Spit, 1.85f);
        Add(EnemyKind.BeehiveKnight, "Beehive Knight", 68, 78, 3, 58, EnemyCapability.Walker | EnemyCapability.ShieldFront | EnemyCapability.Ranged, Color.Goldenrod, Color.SaddleBrown, ProjectileKind.Spit, 2.25f);
        Add(EnemyKind.MushroomBouncer, "Mushroom Bouncer", 66, 48, 2, 0, EnemyCapability.Bouncer | EnemyCapability.JoustImmune, Color.Crimson, Color.White);
        Add(EnemyKind.VineSnatcher, "Vine Snatcher", 48, 116, 2, 0, EnemyCapability.DiveBomber | EnemyCapability.StompImmune, Color.ForestGreen, Color.GreenYellow);
        Add(EnemyKind.SquirrelUnionSteward, "Squirrel Union Steward", 64, 60, 3, 76, EnemyCapability.Walker | EnemyCapability.Summoner | EnemyCapability.Ranged, Color.Brown, Color.Gold, ProjectileKind.Acorn, 3.2f);
        Add(EnemyKind.BogGoblin, "Bog Goblin", 62, 64, 2, 84, EnemyCapability.Walker | EnemyCapability.Burrow, Color.Olive, Color.YellowGreen);
        Add(EnemyKind.SlimeIntern, "Slime Intern", 56, 40, 2, 54, EnemyCapability.Walker | EnemyCapability.Splitter, Color.LimeGreen, Color.Aqua);
        Add(EnemyKind.PossessedRubberBoot, "Possessed Rubber Boot", 46, 52, 2, 140, EnemyCapability.Bouncer | EnemyCapability.Runner, Color.DarkSlateBlue, Color.HotPink);
        Add(EnemyKind.CrabKnight, "Crab Knight", 72, 50, 3, 82, EnemyCapability.Walker | EnemyCapability.ShieldFront | EnemyCapability.StompImmune, Color.OrangeRed, Color.Silver);
        Add(EnemyKind.BeachImp, "Beach Imp", 54, 58, 2, 98, EnemyCapability.Flyer | EnemyCapability.Hover | EnemyCapability.Ranged, Color.Coral, Color.Aqua, ProjectileKind.Spit, 1.9f);
        Add(EnemyKind.DiscoSkeleton, "Disco Skeleton", 58, 82, 2, 95, EnemyCapability.Walker | EnemyCapability.Ranged | EnemyCapability.Rhythm, Color.WhiteSmoke, Color.Magenta, ProjectileKind.Bone, 1.35f);
        Add(EnemyKind.HauntedTrapperKeeper, "Haunted Trapper Keeper", 70, 58, 2, 70, EnemyCapability.Flyer | EnemyCapability.Hover | EnemyCapability.Ranged, Color.MediumPurple, Color.HotPink, ProjectileKind.Stationery, 1.55f);
        Add(EnemyKind.SockPuppetNecromancer, "Sock Puppet Necromancer", 58, 78, 3, 46, EnemyCapability.Walker | EnemyCapability.Ranged | EnemyCapability.Summoner, Color.DarkViolet, Color.Gold, ProjectileKind.DarkBolt, 3.1f);
        Add(EnemyKind.VampireBatling, "Vampire Batling", 52, 36, 1, 175, EnemyCapability.Flyer | EnemyCapability.DiveBomber | EnemyCapability.Pack, Color.MidnightBlue, Color.Crimson);
        Add(EnemyKind.CaveSpiderling, "Cave Spiderling", 62, 38, 1, 132, EnemyCapability.Runner | EnemyCapability.Pack, Color.Black, Color.MediumPurple);
        Add(EnemyKind.StalactiteGremlin, "Stalactite Gremlin", 48, 62, 2, 0, EnemyCapability.DiveBomber | EnemyCapability.StompImmune, Color.SlateGray, Color.OrangeRed);
        Add(EnemyKind.SkiGoblin, "Ski Goblin", 58, 66, 2, 250, EnemyCapability.Runner | EnemyCapability.IceSlider, Color.YellowGreen, Color.Cyan);
        Add(EnemyKind.AngrySnowman, "Angry Snowman", 72, 92, 3, 36, EnemyCapability.Walker | EnemyCapability.Ranged | EnemyCapability.StompImmune, Color.White, Color.Orange, ProjectileKind.Snowball, 1.8f);
        Add(EnemyKind.FrostWerepup, "Frost Werepup", 66, 62, 2, 230, EnemyCapability.Runner | EnemyCapability.Charger, Color.LightSteelBlue, Color.White);
        Add(EnemyKind.ClockworkHunter, "Clockwork Hunter", 62, 86, 4, 90, EnemyCapability.Walker | EnemyCapability.ShieldFront | EnemyCapability.Ranged, Color.Goldenrod, Color.SlateGray, ProjectileKind.Spark, 1.7f);
        Add(EnemyKind.GargoyleIntern, "Gargoyle Intern", 64, 66, 3, 110, EnemyCapability.Flyer | EnemyCapability.Hover | EnemyCapability.DiveBomber, Color.DimGray, Color.MediumPurple);
        Add(EnemyKind.CursedKnight, "Cursed Knight", 68, 92, 4, 78, EnemyCapability.Walker | EnemyCapability.ShieldFront | EnemyCapability.StompImmune, Color.DarkSlateGray, Color.Crimson, contact: 2);
        Add(EnemyKind.NightmareColt, "Nightmare Colt", 82, 72, 4, 285, EnemyCapability.Runner | EnemyCapability.Charger | EnemyCapability.JoustImmune, Color.Black, Color.HotPink, contact: 2);
        Add(EnemyKind.PossessedToaster, "Possessed Toaster", 62, 50, 3, 72, EnemyCapability.Walker | EnemyCapability.Ranged, Color.Silver, Color.OrangeRed, ProjectileKind.Toast, 1.7f);
        Add(EnemyKind.MallNinjaWizard, "Mall Ninja Wizard", 64, 82, 4, 68, EnemyCapability.Teleport | EnemyCapability.Ranged, Color.Black, Color.DeepSkyBlue, ProjectileKind.DarkBolt, 2.35f);
        Add(EnemyKind.RollerbladeOrc, "Rollerblade Orc", 70, 76, 3, 295, EnemyCapability.Runner | EnemyCapability.Charger, Color.OliveDrab, Color.Magenta, contact: 2);
        Add(EnemyKind.CorporateSafetyFairy, "Corporate Safety Fairy", 58, 58, 3, 82, EnemyCapability.Flyer | EnemyCapability.Hover | EnemyCapability.Ranged, Color.Gold, Color.DeepSkyBlue, ProjectileKind.Citation, 2.0f);
        Add(EnemyKind.BoomBoxMimic, "Boom Box Mimic", 78, 58, 3, 145, EnemyCapability.Mimic | EnemyCapability.Runner | EnemyCapability.Rhythm, Color.DimGray, Color.HotPink);
        Add(EnemyKind.MoonCheeseHomunculus, "Moon Cheese Homunculus", 64, 58, 3, 110, EnemyCapability.Bouncer | EnemyCapability.Ranged, Color.Khaki, Color.Gold, ProjectileKind.Spit, 2.3f);

        // Canonical bosses.
        EnemyCapability boss = EnemyCapability.Boss;
        Add(EnemyKind.Ed, "Ed — A Rather Pathetic Slime", 150, 80, 6, 120, boss | EnemyCapability.Bouncer, Color.LimeGreen, Color.HotPink, ProjectileKind.Spit, 1.7f);
        Add(EnemyKind.OldGnarley, "Old Gnarley", 210, 260, 20, 0, boss | EnemyCapability.Ranged | EnemyCapability.Summoner | EnemyCapability.StompImmune, Color.SaddleBrown, Color.ForestGreen, ProjectileKind.Acorn, 1.4f, 2);
        Add(EnemyKind.Spookers, "Spookers", 170, 170, 18, 120, boss | EnemyCapability.Flyer | EnemyCapability.Teleport | EnemyCapability.Ranged, Color.GhostWhite, Color.MediumPurple, ProjectileKind.DarkBolt, 1.1f, 2);
        Add(EnemyKind.Webbey, "Webbey", 210, 150, 22, 170, boss | EnemyCapability.Bouncer | EnemyCapability.Ranged, Color.Black, Color.MediumPurple, ProjectileKind.Web, 1.2f, 2);
        Add(EnemyKind.Mina, "Mina", 190, 130, 21, 180, boss | EnemyCapability.Flyer | EnemyCapability.Ranged, Color.MidnightBlue, Color.Cyan, ProjectileKind.DarkBolt, 1.3f, 2);
        Add(EnemyKind.CountSpatula, "Count Spatula", 110, 190, 25, 95, boss | EnemyCapability.Teleport | EnemyCapability.Ranged | EnemyCapability.Summoner, Color.Black, Color.Crimson, ProjectileKind.Bat, 2.1f, 2);
        Add(EnemyKind.Rip, "Rip 'The Piece Maker'", 170, 180, 28, 340, boss | EnemyCapability.Runner | EnemyCapability.Charger, Color.DimGray, Color.Crimson, contact: 2);
        Add(EnemyKind.NightMare, "Night Mare", 220, 170, 48, 450, boss | EnemyCapability.Runner | EnemyCapability.Charger | EnemyCapability.Teleport | EnemyCapability.Ranged, Color.Black, Color.HotPink, ProjectileKind.DarkBolt, 1.2f, 3);

        // Fail loudly during development if an enum was added without a spec.
        foreach (EnemyKind k in Enum.GetValues<EnemyKind>())
            if (!d.ContainsKey(k)) throw new InvalidOperationException($"EnemyCatalog missing {k}");
        return d;
    }
}
