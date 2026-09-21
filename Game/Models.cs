using System.Numerics;

namespace SparklesReborn;

public enum GameMode
{
    Title, WorldMap, Playing, Paused, CodeEntry, Credits, Victory,
    Options, SecretBrowser, Story
}

public enum Biome
{
    Pasture, Orchard, Forest, Marsh, Mountain, Coast, Haunted, Crypt,
    Cave, Ice, Night, Fortress, Weald, Nightmare
}

public enum PlatformKind
{
    Ground, Stone, Breakable, Cloud, Ice, Crumbling, Moving, Conveyor
}

public enum PickupKind
{
    Sparkly, RainbowDrop, Pixie, BoostExtender, FairyCake, Armor,
    SecretCode, OneUp, SparklePower
}

public enum ProjectileKind
{
    Fairy, Spit, Acorn, Web, Bat, DarkBolt, Toast, Stationery,
    Snowball, Citation, Spark, Bone
}

public enum HazardKind
{
    Spikes, Lava, Crusher, RisingGoop, SawBlade, FireJet, IceShard, NightmareStatic
}

public enum RouteRequirement
{
    Ground, Jump, Boost, ExtendedBoost, StompBounce, JoustSpeed, Optional
}

public enum SpecialSetPiece
{
    None,
    RamjetRunway,
    TurboJoustFreeway,
    SquirrelUnionPicketLine,
    HauntedToyConveyor,
    CountSpatulaKitchen,
    IceRinkPoorDecisions,
    WolfenMoonChase,
    NightmareRealityBreakdown
}

public enum EnemyKind
{
    // Original eleven canonical families.
    Teddy, Zombie, PorcelainDoll, RabidSquirrel, DireMouse, Ghost, EvilTree,
    Goblin, Imp, UnicornHunter, Werewolf,

    // Expanded roster.  These are mechanically distinct families, not palette swaps.
    CupcakeMimic, ChainmailChicken, GnomeLawnCommando,
    BeehiveKnight, MushroomBouncer, VineSnatcher, SquirrelUnionSteward,
    BogGoblin, SlimeIntern, PossessedRubberBoot, CrabKnight, BeachImp,
    DiscoSkeleton, HauntedTrapperKeeper, SockPuppetNecromancer, VampireBatling,
    CaveSpiderling, StalactiteGremlin, SkiGoblin, AngrySnowman, FrostWerepup,
    ClockworkHunter, GargoyleIntern, CursedKnight, NightmareColt,
    PossessedToaster, MallNinjaWizard, RollerbladeOrc, CorporateSafetyFairy,
    BoomBoxMimic, MoonCheeseHomunculus,

    // Canonical bosses.
    Ed, OldGnarley, Spookers, Webbey, Mina, CountSpatula, Rip, NightMare
}

[Flags]
public enum EnemyCapability
{
    None = 0,
    Walker = 1 << 0,
    Runner = 1 << 1,
    Charger = 1 << 2,
    Flyer = 1 << 3,
    Hover = 1 << 4,
    Ranged = 1 << 5,
    Lobber = 1 << 6,
    ShieldFront = 1 << 7,
    StompImmune = 1 << 8,
    JoustImmune = 1 << 9,
    Burrow = 1 << 10,
    Teleport = 1 << 11,
    Mimic = 1 << 12,
    Summoner = 1 << 13,
    Pack = 1 << 14,
    Bouncer = 1 << 15,
    DiveBomber = 1 << 16,
    Splitter = 1 << 17,
    Rhythm = 1 << 18,
    IceSlider = 1 << 19,
    MirrorMovement = 1 << 20,
    Boss = 1 << 30
}

public sealed record CampaignLevel(
    int Index,
    string Name,
    ulong Seed,
    Biome Biome,
    float Difficulty,
    EnemyKind? Boss = null,
    SpecialSetPiece SetPiece = SpecialSetPiece.None);

public sealed class Platform
{
    public RectangleF Rect;
    public PlatformKind Kind;
    public bool Destroyed;

    // Dynamic-platform data. MotionOrigin is top-left in world coordinates.
    public Vector2 MotionOrigin;
    public Vector2 MotionAxis = Vector2.UnitY;
    public float MotionAmplitude;
    public float MotionSpeed = 1f;
    public float MotionPhase;
    public float ConveyorSpeed;
    public float CrumbleDelay = .55f;
    public Vector2 LastDelta;
    public float CrumbleTimer;
    public bool Triggered;

    public Platform(float x, float y, float w, float h, PlatformKind kind = PlatformKind.Ground)
    {
        Rect = new RectangleF(x, y, w, h);
        Kind = kind;
        MotionOrigin = new Vector2(x, y);
    }
}

public sealed class Pickup
{
    public Vector2 Pos;
    public PickupKind Kind;
    public bool Collected;
    public string? Code;
    public float BobPhase;
    public bool Required;
    public Pickup(Vector2 pos, PickupKind kind) { Pos = pos; Kind = kind; }
}

public sealed class SignPost
{
    public Vector2 Pos;
    public string Text;
    public SignPost(float x, float y, string text) { Pos = new(x, y); Text = text; }
}

public sealed class Enemy
{
    public EnemyKind Kind;
    public Vector2 Pos;
    public Vector2 Vel;
    public Vector2 Spawn;
    public float Width;
    public float Height;
    public int Health;
    public int MaxHealth;
    public int Direction = -1;
    public bool Dead;
    public float Timer;
    public float Aux;
    public float HitCooldown;
    public bool Invulnerable;
    public bool OnGround;
    public int State;
    public int Phase;
    public int Variant;
    public bool SplitOnce;

    // ALPHA2.2: Rainbow Ramjet body-contact is crowd control, not an automatic kill.
    // A launched minion temporarily leaves its normal AI state and follows simple
    // ballistic physics until it lands and recovers. Bosses are never put into this
    // state; they still require an actual attack mechanic.
    public bool RamjetLaunched;
    public float RamjetStunTimer;

    public EnemyCapability Capabilities;
    public bool Boss => (Capabilities & EnemyCapability.Boss) != 0;

    public Enemy(EnemyKind kind, Vector2 pos)
    {
        Kind = kind;
        Pos = Spawn = pos;
        EnemySpec spec = EnemyCatalog.Get(kind);
        Width = spec.Width;
        Height = spec.Height;
        Health = MaxHealth = spec.Health;
        Capabilities = spec.Capabilities;
    }

    public RectangleF Bounds => new(Pos.X - Width / 2, Pos.Y - Height, Width, Height);
}

public sealed class Projectile
{
    public Vector2 Pos, Vel;
    public ProjectileKind Kind;
    public bool Friendly;
    public float Life = 4;
    public float Radius = 10;
    public int Damage = 1;
}

public sealed class Hazard
{
    public HazardKind Kind;
    public RectangleF Rect;
    public bool Active = true;
    public int Damage = 1;
    public bool InstantKill;
    public Vector2 Origin;
    public Vector2 Axis = Vector2.UnitY;
    public float Amplitude;
    public float Speed = 1f;
    public float Phase;

    public Hazard(HazardKind kind, RectangleF rect)
    {
        Kind = kind;
        Rect = rect;
        Origin = new Vector2(rect.X, rect.Y);
    }
}

public sealed class Checkpoint
{
    public float X;
    public bool Activated;
    public Checkpoint(float x) => X = x;
}

public sealed class Particle
{
    public Vector2 Pos, Vel;
    public float Life, MaxLife, Size;
    public Color Color;
    public int Shape;
}

public sealed record RoutePoint(float X, float Y, RouteRequirement Requirement, bool Mandatory = true);

public sealed class Level
{
    public int GeneratorVersion = StableHash.GeneratorVersion;
    public int CampaignIndex;
    public string Name = "";
    public ulong Seed;
    public Biome Biome;
    public float Difficulty;
    public float Width;
    public float DeathY = 1050;
    public readonly List<Platform> Platforms = new();
    public readonly List<Pickup> Pickups = new();
    public readonly List<SignPost> Signs = new();
    public readonly List<Enemy> Enemies = new();
    public readonly List<Projectile> Projectiles = new();
    public readonly List<Hazard> Hazards = new();
    public readonly List<Checkpoint> Checkpoints = new();
    public readonly List<RoutePoint> Route = new();
    public RectangleF Exit;
    public EnemyKind? BossKind;
    public bool Secret;
    public string? SecretCode;
    public SpecialSetPiece SetPiece;
    public string GameplayFingerprint = "";
    public string DecorationFingerprint = "";
    public bool BossDefeated => BossKind is null || Enemies.All(e => !e.Boss || e.Dead);
}

public sealed record StorySlide(string Title, string Body, string Caption, Biome Backdrop = Biome.Night);
