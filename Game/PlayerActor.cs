using System.Numerics;

namespace SparklesReborn;

public sealed class PlayerActor
{
    public Vector2 Pos;
    public Vector2 Vel;
    public int Facing = 1;
    public bool Grounded;
    public PlatformKind SurfaceKind = PlatformKind.Ground;
    public Platform? GroundPlatform;
    public bool Boosting;
    public bool Jousting;
    public bool Stomping;
    public float BoostFuel = NormalBoostFuel;
    public const float NormalBoostFuel = 1.55f;
    public float InvulnTimer;
    public float FaceplantTimer;
    public float AnimationTime;
    public int Health = 3;
    public int ArmorCapacity;
    public int Armor;
    public int FairyCakes;
    public int Pixies;
    public int PixiesThisBoost;
    public int SparklePower = 100;
    public long Score;
    public int Lives = 3;
    public Vector2 RespawnPoint;
    public bool Dead;
    public float DeathTimer;
    public bool OverchargeTriggered;
    public float AfterExplosionTimer;

    public RectangleF Bounds => new(Pos.X - 33, Pos.Y - 92, 66, 92);
    public float Speed => Math.Abs(Vel.X);
    public bool Overdrive => Boosting && (Speed > 690 || BoostFuel > NormalBoostFuel + .35f);

    // When Sparkles is both Ramjetting and holding the horn attack, she commits to
    // a proper lance posture instead of keeping the same upright trot pose.
    // This flag is intentionally derived from deterministic gameplay state only.
    public bool LancePose => Jousting && Boosting && Speed > 260f;

    public (Vector2 Root, Vector2 Tip) HornAxis
    {
        get
        {
            float rootX = Pos.X + Facing * 20f;
            float rootY = Pos.Y - (LancePose ? 57f : 62f);
            float tipX = Pos.X + Facing * (LancePose ? 112f : 102f);
            float tipY = Pos.Y - (LancePose ? 42f : 62f);
            return (new Vector2(rootX, rootY), new Vector2(tipX, tipY));
        }
    }

    public RectangleF HornBounds
    {
        get
        {
            (Vector2 root, Vector2 tip) = HornAxis;
            const float pad = 15f;
            float left = Math.Min(root.X, tip.X) - pad;
            float top = Math.Min(root.Y, tip.Y) - pad;
            float right = Math.Max(root.X, tip.X) + pad;
            float bottom = Math.Max(root.Y, tip.Y) + pad;
            return new RectangleF(left, top, right - left, bottom - top);
        }
    }

    public RectangleF StompBounds => new(Pos.X - 38, Pos.Y - 18, 76, 34);

    public void ResetForLevel(float x, float y, int armorCapacity)
    {
        Pos = RespawnPoint = new Vector2(x, y);
        Vel = Vector2.Zero;
        Facing = 1;
        Grounded = false;
        SurfaceKind = PlatformKind.Ground;
        GroundPlatform = null;
        BoostFuel = NormalBoostFuel;
        Health = 3;
        ArmorCapacity = armorCapacity;
        Armor = armorCapacity;
        FairyCakes = 0;
        Pixies = 0;
        PixiesThisBoost = 0;
        Dead = false;
        DeathTimer = 0;
        FaceplantTimer = 0;
        InvulnTimer = 0;
        OverchargeTriggered = false;
        AfterExplosionTimer = 0;
        Jousting = Stomping = Boosting = false;
    }

    public void Update(float dt, InputState input, Level level, Action<Particle> emit)
    {
        AnimationTime += dt;
        InvulnTimer = Math.Max(0, InvulnTimer - dt);
        AfterExplosionTimer = Math.Max(0, AfterExplosionTimer - dt);

        if (Dead)
        {
            DeathTimer -= dt;
            Vel.Y += 2100 * dt;
            Pos += Vel * dt;
            if (DeathTimer <= 0) Respawn();
            return;
        }

        if (FaceplantTimer > 0)
        {
            FaceplantTimer -= dt;
            Jousting = Stomping = Boosting = false;
            Vel.X = MoveTowards(Vel.X, 0, 1600 * dt);
            Vel.Y += 2300 * dt;
            MoveAndCollide(dt, level);
            return;
        }

        // A platform moved before the player update: carry Sparkles with it at the guest-visible contact boundary.
        if (Grounded && GroundPlatform is not null && !GroundPlatform.Destroyed)
        {
            Pos += GroundPlatform.LastDelta;
            if (GroundPlatform.Kind == PlatformKind.Conveyor)
                Pos.X += GroundPlatform.ConveyorSpeed * dt;
        }

        float move = input.MoveX;
        if (Math.Abs(move) > .05f) Facing = Math.Sign(move);

        Boosting = input.DashDown && BoostFuel > 0.01f;
        Jousting = input.JoustDown;

        bool onIce = Grounded && SurfaceKind == PlatformKind.Ice;
        float accel = Grounded ? (onIce ? 1100 : 2400) : 1300;
        float maxRun = onIce ? 455 : 390;
        if (Boosting)
        {
            accel = Grounded ? 3600 : 2500;
            maxRun = Overdrive ? 1080 : 850;
            float direction = Math.Abs(move) > .05f ? Math.Sign(move) : Facing;
            Vel.X += direction * accel * dt;
            BoostFuel = Math.Max(0, BoostFuel - dt);
            if (PixiesThisBoost == 0 && !Overdrive) PixiesThisBoost = 0;
            EmitRainbow(dt, emit);
        }
        else
        {
            PixiesThisBoost = 0;
            OverchargeTriggered = false;
            if (Grounded) BoostFuel = Math.Min(NormalBoostFuel, BoostFuel + .72f * dt);
            Vel.X += move * accel * dt;
        }

        if (Math.Abs(move) < .05f && !Boosting)
            Vel.X = MoveTowards(Vel.X, 0, (Grounded ? (onIce ? 210 : 2100) : 260) * dt);
        Vel.X = Math.Clamp(Vel.X, -maxRun, maxRun);

        if (input.JumpPressed && Grounded)
        {
            // BUGFIX ALPHA2.1: jump takeoff used to be a hard-coded -820 Y impulse, so
            // Rainbow Ramjet speed changed the horizontal part of the trajectory but did
            // absolutely nothing to the launch impulse. That made a boosted jump feel
            // weirdly disconnected from the rocket strapped to Sparkles' ass.
            //
            // Keep the ordinary jump stable, but couple the vertical launch impulse to
            // active Ramjet speed. Pressing Dash+Jump together gets an immediate boost;
            // entering the jump already at Ramjet/Overdrive speed gets progressively more.
            // Horizontal velocity is deliberately NOT reset here, so the full Ramjet
            // momentum carries through the jump as well.
            float jumpImpulse = 820f;
            if (Boosting)
            {
                float ramjetSpeed = Math.Clamp(Speed / 850f, 0f, 1.25f);
                jumpImpulse += 120f + 120f * ramjetSpeed;
                if (Overdrive) jumpImpulse += 80f;
            }

            Vel.Y = -jumpImpulse;
            Grounded = false;
            emit(new Particle { Pos = Pos + new Vector2(-18, -6), Vel = new Vector2(-80, -80), Life = .35f, MaxLife = .35f, Size = 10, Color = Color.White });
        }

        if (input.StompPressed && !Grounded)
        {
            Stomping = true;
            Vel.Y = Math.Max(Vel.Y, 1250);
            Vel.X *= .72f;
        }
        if (Grounded) Stomping = false;

        // Mild vertical thrust makes fart boost useful in the air without turning it into free flight.
        if (Boosting && !Grounded && input.MoveY < -.25f)
            Vel.Y -= 730 * dt;

        Vel.Y += (Stomping ? 3000 : 2150) * dt;
        Vel.Y = Math.Min(Vel.Y, 1550);
        MoveAndCollide(dt, level);

        if (Pos.Y > level.DeathY)
            Kill();
    }

    private void EmitRainbow(float dt, Action<Particle> emit)
    {
        int amount = Speed > 800 ? 3 : 2;
        Color[] rainbow = { Color.Red, Color.Orange, Color.Gold, Color.LimeGreen, Color.DeepSkyBlue, Color.MediumPurple };
        for (int i = 0; i < amount; i++)
        {
            float jitter = (float)(Random.Shared.NextDouble() * 22 - 11);
            Color c = rainbow[(int)(AnimationTime * 18 + i * 2) % rainbow.Length];
            emit(new Particle
            {
                Pos = new Vector2(Pos.X - Facing * 42, Pos.Y - 30 + jitter),
                Vel = new Vector2(-Facing * (130 + Speed * .18f) + jitter * 2, jitter * 3),
                Life = Overdrive ? .9f : .58f,
                MaxLife = Overdrive ? .9f : .58f,
                Size = Overdrive ? 16 : 10,
                Color = c,
                Shape = Overdrive ? 1 : 0
            });
        }
    }

    private void MoveAndCollide(float dt, Level level)
    {
        // Horizontal.
        Pos.X += Vel.X * dt;
        RectangleF b = Bounds;
        foreach (Platform p in level.Platforms)
        {
            if (p.Destroyed || p.Kind == PlatformKind.Cloud) continue;
            if (!b.IntersectsWith(p.Rect)) continue;

            // At high joust velocity a breakable wall is a suggestion, not a wall.
            if (p.Kind == PlatformKind.Breakable && Jousting && Math.Abs(Vel.X) > 610)
            {
                p.Destroyed = true;
                continue;
            }

            if (Vel.X > 0) Pos.X = p.Rect.Left - b.Width / 2;
            else if (Vel.X < 0) Pos.X = p.Rect.Right + b.Width / 2;
            Vel.X = 0;
            b = Bounds;
        }

        // Vertical.
        bool wasGrounded = Grounded;
        Grounded = false;
        GroundPlatform = null;
        float oldBottom = Bounds.Bottom;
        Pos.Y += Vel.Y * dt;
        b = Bounds;
        foreach (Platform p in level.Platforms)
        {
            if (p.Destroyed) continue;
            bool oneWay = p.Kind == PlatformKind.Cloud;
            if (oneWay && (Vel.Y < 0 || oldBottom > p.Rect.Top + 10)) continue;
            if (!b.IntersectsWith(p.Rect)) continue;
            if (p.Kind == PlatformKind.Breakable && Stomping && Vel.Y > 420)
            {
                p.Destroyed = true;
                continue;
            }
            if (Vel.Y >= 0 && oldBottom <= p.Rect.Top + Math.Max(18, Math.Abs(Vel.Y * dt) + 4))
            {
                Pos.Y = p.Rect.Top;
                Vel.Y = 0;
                Grounded = true;
                GroundPlatform = p;
                SurfaceKind = p.Kind;
                if (p.Kind == PlatformKind.Crumbling) p.Triggered = true;
                Stomping = false;
                b = Bounds;
            }
            else if (Vel.Y < 0 && b.Top < p.Rect.Bottom)
            {
                Pos.Y = p.Rect.Bottom + b.Height;
                Vel.Y = 0;
                b = Bounds;
            }
        }

        if (!wasGrounded && Grounded)
        {
            // Landing effects intentionally stay in the renderer/audio layer; physics remains deterministic.
        }
    }

    public void AddBoostExtender()
    {
        BoostFuel = Math.Min(7.5f, BoostFuel + 1.25f);
    }

    public void TakeDamage(int amount, float sourceX)
    {
        if (InvulnTimer > 0 || Dead) return;
        if (Armor > 0) Armor = Math.Max(0, Armor - 1);
        else Health -= amount;
        InvulnTimer = 1.2f;
        Vel.X = sourceX < Pos.X ? 520 : -520;
        Vel.Y = -430;
        if (Health <= 0) Kill();
    }

    public void Bounce(float strength = 700)
    {
        Vel.Y = -strength;
        Stomping = false;
        Grounded = false;
    }

    public void TriggerFairyExplosion(Action<Particle> emit)
    {
        if (OverchargeTriggered) return;
        OverchargeTriggered = true;
        for (int i = 0; i < 130; i++)
        {
            float a = (float)(Random.Shared.NextDouble() * MathF.Tau);
            float sp = 160 + (float)Random.Shared.NextDouble() * 780;
            Color[] colors = { Color.HotPink, Color.Gold, Color.Cyan, Color.Lime, Color.White, Color.Violet };
            emit(new Particle
            {
                Pos = Pos + new Vector2(Facing * 30, -55),
                Vel = new Vector2(MathF.Cos(a), MathF.Sin(a)) * sp,
                Life = .8f + (float)Random.Shared.NextDouble() * 1.4f,
                MaxLife = 2.2f,
                Size = 7 + (float)Random.Shared.NextDouble() * 21,
                Color = colors[Random.Shared.Next(colors.Length)],
                Shape = i % 3
            });
        }
        PixiesThisBoost = 0;
        Pixies = Math.Max(0, Pixies - 30);
        BoostFuel = 0;
        Vel.X = Facing * 250;
        Vel.Y = -240;
        FaceplantTimer = 1.15f;
        AfterExplosionTimer = 1.8f;
    }

    public void ActivateCheckpoint(float x, float groundY)
    {
        RespawnPoint = new Vector2(x, groundY - 4);
    }

    public void Kill()
    {
        if (Dead) return;
        Lives--;
        Dead = true;
        DeathTimer = 1.25f;
        Vel = new Vector2(Facing * -180, -620);
    }

    private void Respawn()
    {
        if (Lives <= 0) { Lives = 0; DeathTimer = 0; return; }
        Pos = RespawnPoint;
        Vel = Vector2.Zero;
        Health = 3;
        Armor = ArmorCapacity;
        BoostFuel = NormalBoostFuel;
        InvulnTimer = 1.5f;
        Dead = false;
        FaceplantTimer = 0;
        Grounded = false;
        GroundPlatform = null;
        SurfaceKind = PlatformKind.Ground;
        PixiesThisBoost = 0;
    }

    private static float MoveTowards(float value, float target, float maxDelta)
    {
        if (Math.Abs(target - value) <= maxDelta) return target;
        return value + Math.Sign(target - value) * maxDelta;
    }
}
