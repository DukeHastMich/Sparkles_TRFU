using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SparklesReborn;

/// <summary>SplitMix64. Small, fast, reproducible and deliberately independent of System.Random implementation changes.</summary>
public struct DeterministicRng
{
    private ulong _state;
    public DeterministicRng(ulong seed) => _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
    public ulong NextU64()
    {
        ulong z = (_state += 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
    public uint NextU32() => (uint)(NextU64() >> 32);
    public int Next(int min, int maxExclusive) => min + (int)(NextU32() % (uint)Math.Max(1, maxExclusive - min));
    public float NextFloat() => (NextU32() & 0xFFFFFF) / 16777216f;
    public float Range(float min, float max) => min + (max - min) * NextFloat();
    public bool Chance(float p) => NextFloat() < p;
    public T Pick<T>(IReadOnlyList<T> items) => items[Next(0, items.Count)];
}

public static class StableHash
{
    // v2 is the first generation contract with independent RNG streams and gameplay fingerprints.
    // Never silently change this value for an existing save. Bump it only when generation itself changes.
    public const int GeneratorVersion = 2;

    public static ulong Of(params object?[] parts)
    {
        ulong h = 14695981039346656037UL;
        foreach (object? part in parts)
        {
            string s = part?.ToString() ?? "null";
            foreach (char c in s)
            {
                h ^= c;
                h *= 1099511628211UL;
            }
            h ^= 0xFF;
            h *= 1099511628211UL;
        }
        return h;
    }
}

/// <summary>
/// Changes to flowers must not move enemies. Changes to enemy composition must not move secret codes.
/// Each gameplay domain therefore gets a named stream derived from the immutable level seed.
/// </summary>
public sealed class LevelRngStreams
{
    public DeterministicRng Geometry;
    public DeterministicRng Enemies;
    public DeterministicRng Rewards;
    public DeterministicRng Secrets;
    public DeterministicRng Decor;
    public DeterministicRng Jokes;
    public DeterministicRng SetPieces;

    public LevelRngStreams(ulong seed)
    {
        Geometry = new(StableHash.Of(seed, "GEOMETRY"));
        Enemies = new(StableHash.Of(seed, "ENEMIES"));
        Rewards = new(StableHash.Of(seed, "REWARDS"));
        Secrets = new(StableHash.Of(seed, "SECRETS"));
        Decor = new(StableHash.Of(seed, "DECOR"));
        Jokes = new(StableHash.Of(seed, "JOKES"));
        SetPieces = new(StableHash.Of(seed, "SETPIECES"));
    }
}

public static class TraversalEnvelope
{
    // Conservative design budgets, intentionally below theoretical maxima from the current player physics.
    public const float StandingJumpX = 390f;
    public const float RunningJumpX = 560f;
    public const float NormalBoostX = 1120f;
    public const float ExtendedBoostX = 2500f;
    public const float JumpUp = 245f;
    public const float BoostUp = 390f;
    public const float SafeDrop = 560f;
    public const float StompBounceX = 650f;
    public const float JoustRunway = 720f;

    public static float MaxGap(RouteRequirement requirement) => requirement switch
    {
        RouteRequirement.Ground => 0,
        RouteRequirement.Jump => RunningJumpX,
        RouteRequirement.Boost => NormalBoostX,
        RouteRequirement.ExtendedBoost => ExtendedBoostX,
        RouteRequirement.StompBounce => StompBounceX,
        RouteRequirement.JoustSpeed => NormalBoostX,
        RouteRequirement.Optional => ExtendedBoostX,
        _ => RunningJumpX
    };
}

public sealed record ValidationIssue(string Code, string Message);

public static class LevelValidator
{
    public static List<ValidationIssue> Validate(Level level)
    {
        List<ValidationIssue> issues = new();
        if (level.Width < 1800) issues.Add(new("WIDTH", "Level is suspiciously short."));
        if (level.Exit.Width <= 0 || level.Exit.Left < 0 || level.Exit.Right > level.Width + 100)
            issues.Add(new("EXIT", "Exit is outside the declared level bounds."));
        if (level.Route.Count < 2) issues.Add(new("ROUTE", "No mandatory traversal spine was recorded."));

        RoutePoint? prev = null;
        foreach (RoutePoint p in level.Route.Where(r => r.Mandatory).OrderBy(r => r.X))
        {
            if (prev is not null)
            {
                float dx = p.X - prev.X;
                float dy = p.Y - prev.Y;
                float max = TraversalEnvelope.MaxGap(p.Requirement);
                if (max > 0 && dx > max + 80)
                    issues.Add(new("GAP", $"Mandatory {p.Requirement} span {dx:0} exceeds conservative budget {max:0}."));
                if (-dy > TraversalEnvelope.BoostUp + 90 && p.Requirement is not RouteRequirement.Optional)
                    issues.Add(new("UP", $"Mandatory rise {-dy:0} is above the boost envelope."));
                if (dy > TraversalEnvelope.SafeDrop + 120 && p.Requirement is not RouteRequirement.Optional)
                    issues.Add(new("DROP", $"Mandatory drop {dy:0} is above the safe route envelope."));
            }
            prev = p;
        }

        foreach (Platform p in level.Platforms)
        {
            if (p.Rect.Width <= 0 || p.Rect.Height <= 0)
                issues.Add(new("PLATFORM", "A platform has a non-positive dimension."));
            if (float.IsNaN(p.Rect.X) || float.IsNaN(p.Rect.Y))
                issues.Add(new("NAN", "A platform contains NaN coordinates."));
        }
        foreach (Enemy e in level.Enemies)
            if (e.Pos.X < -100 || e.Pos.X > level.Width + 100)
                issues.Add(new("ENEMY_BOUNDS", $"{e.Kind} is outside the level bounds."));

        return issues;
    }
}

public static class LevelFingerprint
{
    private static string Q(float f) => MathF.Round(f * 10f).ToString("0", CultureInfo.InvariantCulture);

    public static string Gameplay(Level level)
    {
        StringBuilder sb = new();
        sb.Append("V=").Append(level.GeneratorVersion).Append("|S=").Append(level.Seed.ToString("X16"));
        foreach (Platform p in level.Platforms)
            sb.Append("|P:").Append((int)p.Kind).Append(',').Append(Q(p.Rect.X)).Append(',').Append(Q(p.Rect.Y)).Append(',').Append(Q(p.Rect.Width)).Append(',').Append(Q(p.Rect.Height)).Append(',').Append(Q(p.MotionAmplitude)).Append(',').Append(Q(p.ConveyorSpeed));
        foreach (Hazard h in level.Hazards)
            sb.Append("|H:").Append((int)h.Kind).Append(',').Append(Q(h.Rect.X)).Append(',').Append(Q(h.Rect.Y)).Append(',').Append(Q(h.Rect.Width)).Append(',').Append(Q(h.Rect.Height));
        foreach (Enemy e in level.Enemies)
            sb.Append("|E:").Append((int)e.Kind).Append(',').Append(Q(e.Spawn.X)).Append(',').Append(Q(e.Spawn.Y)).Append(',').Append(e.Variant);
        foreach (Pickup p in level.Pickups.Where(p => p.Required || p.Kind is PickupKind.BoostExtender or PickupKind.SecretCode))
            sb.Append("|U:").Append((int)p.Kind).Append(',').Append(Q(p.Pos.X)).Append(',').Append(Q(p.Pos.Y)).Append(',').Append(p.Code ?? "");
        foreach (Checkpoint c in level.Checkpoints)
            sb.Append("|C:").Append(Q(c.X));
        sb.Append("|B:").Append(level.BossKind?.ToString() ?? "NONE");
        sb.Append("|X:").Append(Q(level.Exit.X)).Append(',').Append(Q(level.Exit.Y));
        return Digest(sb.ToString());
    }

    public static string Decoration(Level level)
    {
        StringBuilder sb = new();
        foreach (SignPost s in level.Signs)
            sb.Append(Q(s.Pos.X)).Append(':').Append(Q(s.Pos.Y)).Append(':').Append(s.Text).Append('|');
        return Digest(sb.ToString());
    }

    private static string Digest(string text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash.AsSpan(0, 12)); // 96 bits is plenty for a human-readable regression fingerprint.
    }
}

public sealed record DeterminismReport(int LevelsChecked, int Failures, IReadOnlyList<string> Messages);

public static class DeterminismVerifier
{
    public static DeterminismReport RunCampaignRegression(int repeats = 3)
    {
        List<string> messages = new();
        int failures = 0;
        foreach (CampaignLevel desc in Campaign.Levels)
        {
            Level first = LevelGenerator.Generate(desc);
            string fp = first.GameplayFingerprint;
            List<ValidationIssue> initialIssues = LevelValidator.Validate(first);
            foreach (ValidationIssue issue in initialIssues)
            {
                failures++;
                messages.Add($"{desc.Index:00} {desc.Name}: {issue.Code} {issue.Message}");
            }
            for (int r = 1; r < Math.Max(2, repeats); r++)
            {
                Level again = LevelGenerator.Generate(desc);
                if (!string.Equals(fp, again.GameplayFingerprint, StringComparison.Ordinal))
                {
                    failures++;
                    messages.Add($"{desc.Index:00} {desc.Name}: fingerprint changed between generation passes.");
                    break;
                }
            }
        }
        return new(Campaign.Levels.Count, failures, messages);
    }
}
