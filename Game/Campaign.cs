namespace SparklesReborn;

public static class Campaign
{
    public const ulong MasterSeed = 0x5A17C1E5D00DF00DUL;
    public static readonly Point[] MapPoints =
    {
        new(342,706), new(233,653), new(283,555), new(465,551), new(586,529), new(700,564), new(838,579), new(911,499),
        new(808,423), new(770,468), new(669,429), new(566,389), new(476,283), new(402,195), new(583,179), new(765,136),
        new(735,244), new(779,336), new(905,273), new(954,193), new(1091,180), new(1150,303), new(1295,274), new(1465,345),
        new(1516,490), new(1449,602), new(1235,580), new(1342,479)
    };

    private static readonly string[] Names =
    {
        "Sparkle's Pasture", "Sparkle Wood", "Sparkle's Orchard", "Sparkle's Pond", "The Woody Way", "The Woody Way II",
        "Mucky Marsh", "Old Man's Knee", "The Mountainside", "The Forest's Edge", "No Where To Hide", "Gnarley's Glenn",
        "Near The Beach", "The Haunted Coast", "A Grave Undertaking", "Spooker's Crypt", "To The Caves!", "The Crowded Caves",
        "The Icy Summit", "The Verge Of Night", "Count Spatulas Fortress", "The Path Less Traveled", "The Wolfen Way",
        "The Long Dark Road", "Get On With It!", "The Coil", "Night Mare's Weald", "Night Mare's Pasture"
    };

    public static IReadOnlyList<CampaignLevel> Levels { get; } = BuildLevels();

    private static List<CampaignLevel> BuildLevels()
    {
        var list = new List<CampaignLevel>();
        for (int i = 0; i < Names.Length; i++)
        {
            int n = i + 1;
            Biome biome = n switch
            {
                <= 3 => Biome.Pasture,
                4 => Biome.Orchard,
                <= 6 => Biome.Forest,
                7 => Biome.Marsh,
                8 => Biome.Mountain,
                <= 12 => Biome.Forest,
                13 => Biome.Coast,
                14 => Biome.Haunted,
                15 => Biome.Haunted,
                16 => Biome.Crypt,
                <= 18 => Biome.Cave,
                19 => Biome.Ice,
                20 => Biome.Night,
                21 => Biome.Fortress,
                22 => Biome.Night,
                23 => Biome.Weald,
                <= 26 => Biome.Night,
                27 => Biome.Weald,
                _ => Biome.Nightmare
            };
            EnemyKind? boss = n switch
            {
                7 => EnemyKind.Ed,
                12 => EnemyKind.OldGnarley,
                16 => EnemyKind.Spookers,
                18 => EnemyKind.Webbey,
                19 => EnemyKind.Mina,
                21 => EnemyKind.CountSpatula,
                23 => EnemyKind.Rip,
                28 => EnemyKind.NightMare,
                _ => null
            };
            SpecialSetPiece set = n switch
            {
                6 => SpecialSetPiece.SquirrelUnionPicketLine,
                14 => SpecialSetPiece.RamjetRunway,
                15 => SpecialSetPiece.HauntedToyConveyor,
                19 => SpecialSetPiece.IceRinkPoorDecisions,
                21 => SpecialSetPiece.CountSpatulaKitchen,
                23 => SpecialSetPiece.WolfenMoonChase,
                24 => SpecialSetPiece.TurboJoustFreeway,
                28 => SpecialSetPiece.NightmareRealityBreakdown,
                _ => SpecialSetPiece.None
            };
            float difficulty = Math.Clamp((n - 1) / 27f, 0f, 1f);
            ulong seed = StableHash.Of(StableHash.GeneratorVersion, MasterSeed, n, Names[i]);
            list.Add(new CampaignLevel(n, Names[i], seed, biome, difficulty, boss, set));
        }
        return list;
    }

    // Directional topology recovered from the original VB prototype.
    // Value is 0-based level index, -1 means no road in that direction.
    public static int Neighbor(int current, int dx, int dy)
    {
        int n = current + 1;
        if (dx < 0)
        {
            if (new[] {1,8,9,10,11,12,13,25,26}.Contains(n)) return Math.Min(27, current + 1);
            if (new[] {3,4,5,6,7,15,16,19,20,21,23,24}.Contains(n)) return Math.Max(0, current - 1);
        }
        if (dx > 0)
        {
            if (new[] {3,4,5,6,7,14,15,18,20,22,23}.Contains(n)) return Math.Min(27, current + 1);
            if (new[] {1,2,9,10,11,12,13,27}.Contains(n)) return Math.Max(0, current - 1);
        }
        if (dy < 0)
        {
            if (new[] {1,2,8,13,19,27}.Contains(n)) return Math.Min(27, current + 1);
            if (new[] {10,17,18,22,24,25,26}.Contains(n)) return Math.Max(0, current - 1);
        }
        if (dy > 0)
        {
            if (new[] {9,16,17,21,24,25}.Contains(n)) return Math.Min(27, current + 1);
            if (new[] {2,3,8,13,14,19,20,28}.Contains(n)) return Math.Max(0, current - 1);
        }
        return -1;
    }

    public static IEnumerable<int> AllNeighbors(int current)
    {
        int[] ns = { Neighbor(current,-1,0), Neighbor(current,1,0), Neighbor(current,0,-1), Neighbor(current,0,1) };
        return ns.Where(x => x >= 0).Distinct();
    }
}
