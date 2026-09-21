using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Numerics;

namespace SparklesReborn;

public sealed class GameRenderer
{
    private readonly AssetStore _assets;
    private readonly Bitmap _worldMap;
    private readonly Bitmap[] _walk = new Bitmap[6];
    private readonly Bitmap[] _jump = new Bitmap[6];
    private readonly Bitmap[] _mapSparkles = new Bitmap[6];
    private readonly Bitmap _compass;
    private readonly Font _titleFont = new("Arial Black", 54, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _hugeFont = new("Arial Black", 86, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _menuFont = new("Arial Black", 25, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _hudFont = new("Consolas", 18, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _smallFont = new("Consolas", 14, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _tinyFont = new("Consolas", 11, FontStyle.Bold, GraphicsUnit.Pixel);

    public GameRenderer(AssetStore assets)
    {
        _assets = assets;
        _worldMap = assets.Bitmap("world_map.png");
        for (int i = 0; i < 6; i++) { _walk[i] = assets.Bitmap($"walk_{i}.png"); _jump[i] = assets.Bitmap($"jump_{i}.png"); }
        string[] map = { "sparkle_sm_1.png", "sparkle_sm_1b.png", "sparkle_sm_1c.png", "sparkle_sm_2.png", "sparkle_sm_2b.png", "sparkle_sm_2c.png" };
        for (int i = 0; i < 6; i++) _mapSparkles[i] = assets.Bitmap(map[i]);
        _compass = assets.Bitmap("compass.png");
    }

    public void Render(GameApp app, Graphics g, Rectangle client)
    {
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        switch (app.Mode)
        {
            case GameMode.Title: DrawTitle(app, g, client); break;
            case GameMode.WorldMap: DrawMap(app, g, client); break;
            case GameMode.Playing: DrawLevel(app, g, client); break;
            case GameMode.Paused: DrawLevel(app, g, client); DrawPause(app, g, client); break;
            case GameMode.CodeEntry: DrawCodeEntry(app, g, client); break;
            case GameMode.Credits: DrawCredits(app, g, client); break;
            case GameMode.Options: DrawOptions(app, g, client); break;
            case GameMode.SecretBrowser: DrawSecretBrowser(app, g, client); break;
            case GameMode.Story: DrawStory(app, g, client); break;
            case GameMode.Victory: DrawVictory(app, g, client); break;
        }
        if (app.BannerTimer > 0) DrawBanner(g, client, app.Banner, app.BannerTimer);
    }

    private void DrawTitle(GameApp app, Graphics g, Rectangle r)
    {
        using var bg = new LinearGradientBrush(r, Color.FromArgb(18, 5, 65), Color.FromArgb(0, 180, 210), 35);
        g.FillRectangle(bg, r);
        DrawRetroGrid(g, r, app.Elapsed);
        DrawStars(g, r, 0x51504152, app.Elapsed);

        int logoY = Math.Max(35, r.Height / 10);
        DrawShadowText(g, "SPARKLES", _hugeFont, new PointF(r.Width / 2f, logoY), StringAlignment.Center, Color.White, Color.HotPink, 5);
        using Font sub = new("Arial Black", Math.Max(18, r.Width / 65f), FontStyle.Bold, GraphicsUnit.Pixel);
        DrawCentered(g, "RAINBOW RAMJET EDITION", sub, r.Width / 2f, logoY + 96, Color.Gold);
        DrawCentered(g, "A PROCEDURALLY UNREASONABLE UNICORN ADVENTURE", _smallFont, r.Width / 2f, logoY + 137, Color.WhiteSmoke);

        Bitmap sparkle = _mapSparkles[(int)(app.Elapsed * 6) % 3];
        RectangleF horse = new(r.Width / 2f - 225, logoY + 158, 190, 133);
        g.DrawImage(sparkle, horse);
        DrawRainbowArc(g, new PointF(r.Width / 2f - 120, logoY + 250), 250, 85, -5);

        string[] menu = { "NEW GAME", "CONTINUE", "SECRET LEVELS", "ENTER SECRET CODE", "OPTIONS", "CREDITS / CONTROLS", "QUIT" };
        float menuY = Math.Min(r.Height - 365, logoY + 300);
        for (int i = 0; i < menu.Length; i++)
        {
            bool selected = i == app.TitleChoice;
            RectangleF box = new(r.Width / 2f - 250, menuY + i * 45, 500, 38);
            if (selected)
            {
                using var br = new LinearGradientBrush(box, Color.FromArgb(220, 255, 30, 180), Color.FromArgb(220, 80, 255, 255), 0f);
                g.FillRoundedRectangle(br, box, 14);
                using Pen p = new(Color.White, 2); g.DrawRoundedRectangle(p, box, 14);
            }
            using Font mf = new("Arial Black", 20, FontStyle.Bold, GraphicsUnit.Pixel);
            DrawCentered(g, (selected ? "▶ " : "") + menu[i], mf, r.Width / 2f, box.Top + 5, selected ? Color.Black : Color.White);
        }
        DrawCentered(g, "ARROWS / WASD  •  ENTER  •  GAMEPAD  •  F11 FULLSCREEN", _tinyFont, r.Width / 2f, r.Bottom - 28, Color.FromArgb(220, 255, 255, 255));
    }

    private void DrawMap(GameApp app, Graphics g, Rectangle r)
    {
        g.Clear(Color.FromArgb(13, 12, 31));
        RectangleF mapRect = FitRect(_worldMap.Size, new RectangleF(20, 60, r.Width - 40, r.Height - 190));
        g.DrawImage(_worldMap, mapRect);

        float sx = mapRect.Width / _worldMap.Width, sy = mapRect.Height / _worldMap.Height;
        // faint route graph so the old topology becomes readable without repainting the original map.
        using Pen route = new(Color.FromArgb(80, 255, 255, 255), Math.Max(1, 2 * sx));
        for (int i = 0; i < Campaign.MapPoints.Length; i++)
        {
            foreach (int n in Campaign.AllNeighbors(i).Where(n => n > i))
            {
                Point a = Campaign.MapPoints[i], b = Campaign.MapPoints[n];
                g.DrawLine(route, mapRect.Left + a.X * sx, mapRect.Top + a.Y * sy, mapRect.Left + b.X * sx, mapRect.Top + b.Y * sy);
            }
        }

        for (int i = 0; i < Campaign.MapPoints.Length; i++)
        {
            Point p = Campaign.MapPoints[i];
            float x = mapRect.Left + p.X * sx, y = mapRect.Top + p.Y * sy;
            bool unlocked = app.Save.Unlocked[i], complete = app.Save.Completed[i];
            float rr = i == app.MapIndex ? 15 : 9;
            Color c = !unlocked ? Color.FromArgb(120, 40, 40, 40) : complete ? Color.LimeGreen : Color.Gold;
            using Brush b = new SolidBrush(Color.FromArgb(220, c));
            g.FillEllipse(b, x - rr, y - rr, rr * 2, rr * 2);
            using Pen pen = new(i == app.MapIndex ? Color.White : Color.FromArgb(150, Color.Black), i == app.MapIndex ? 4 : 2);
            g.DrawEllipse(pen, x - rr, y - rr, rr * 2, rr * 2);
        }

        Point cp = Campaign.MapPoints[app.MapIndex];
        float cx = mapRect.Left + cp.X * sx, cy = mapRect.Top + cp.Y * sy;
        Bitmap marker = _mapSparkles[((int)(app.Elapsed * 7) % 3) + (app.MapIndex is <= 1 or 7 or 8 or 9 or 10 or 11 or 12 or 25 or 26 ? 0 : 3)];
        float mw = Math.Max(48, 83 * sx * 1.5f), mh = mw * marker.Height / marker.Width;
        g.DrawImage(marker, cx - mw / 2, cy - mh - 10, mw, mh);

        CampaignLevel level = Campaign.Levels[app.MapIndex];
        RectangleF info = new(24, r.Bottom - 118, r.Width - 48, 92);
        using Brush ib = new SolidBrush(Color.FromArgb(225, 8, 8, 22)); g.FillRoundedRectangle(ib, info, 15);
        using Pen ip = new(Color.FromArgb(200, 255, 255, 255), 2); g.DrawRoundedRectangle(ip, info, 15);
        DrawShadowText(g, $"{level.Index:00}. {level.Name}", _menuFont, new PointF(info.Left + 18, info.Top + 10), StringAlignment.Near, Color.White, Color.DeepPink, 2);
        string status = app.Save.Completed[app.MapIndex] ? "COMPLETE" : app.Save.Unlocked[app.MapIndex] ? "READY" : "LOCKED";
        g.DrawString($"{status}   •   BIOME {level.Biome.ToString().ToUpperInvariant()}   •   SEED {level.Seed:X16}", _smallFont, Brushes.Gold, info.Left + 20, info.Top + 50);
        g.DrawString("Travel: arrows/WASD   Enter: play   F2: code   F3: discovered codes   F9: seed regression", _tinyFont, Brushes.WhiteSmoke, info.Right - 720, info.Top + 53);

        // The old compass survives as a decorative nod.
        float cw = Math.Min(95, r.Width * .075f);
        g.DrawImage(_compass, r.Right - cw - 30, 68, cw, cw);
    }

    private void DrawLevel(GameApp app, Graphics g, Rectangle r)
    {
        Level? level = app.CurrentLevel;
        if (level is null) { g.Clear(Color.Black); return; }
        DrawWorldBackground(g, r, level, app.CameraX, app.Elapsed);

        float shakeAmount = app.Shake + (app.Player.AfterExplosionTimer > 0 ? 8 : 0);
        float shakeX = shakeAmount > 0 ? MathF.Sin(app.Elapsed * 75) * Math.Min(18, shakeAmount) : 0;
        float shakeY = shakeAmount > 0 ? MathF.Cos(app.Elapsed * 66) * Math.Min(12, shakeAmount * .65f) : 0;
        GraphicsState state = g.Save();
        g.TranslateTransform(-app.CameraX + shakeX, -app.CameraY + shakeY);

        float viewL = app.CameraX - 200, viewR = app.CameraX + r.Width + 250;
        foreach (Platform p in level.Platforms)
        {
            if (p.Destroyed || p.Rect.Right < viewL || p.Rect.Left > viewR) continue;
            DrawPlatform(g, p, level.Biome);
        }
        foreach (Hazard h in level.Hazards)
            if (h.Rect.Right > viewL && h.Rect.Left < viewR) DrawHazard(g, h, app.Elapsed, app.Settings.ReduceFlashes);
        foreach (SignPost s in level.Signs)
            if (s.Pos.X > viewL && s.Pos.X < viewR) DrawSign(g, s);
        foreach (Checkpoint cp in level.Checkpoints)
            if (cp.X > viewL && cp.X < viewR) DrawCheckpoint(g, cp, level);

        DrawExit(g, level.Exit, level.BossDefeated, app.Elapsed);

        foreach (Pickup p in level.Pickups)
            if (!p.Collected && p.Pos.X > viewL && p.Pos.X < viewR) DrawPickup(g, p, app.Elapsed);
        foreach (Projectile p in level.Projectiles)
            if (p.Pos.X > viewL - 100 && p.Pos.X < viewR + 100) DrawProjectile(g, p, app.Elapsed);
        foreach (Enemy e in level.Enemies)
            if (!e.Dead && e.Pos.X > viewL - 250 && e.Pos.X < viewR + 250) DrawEnemy(g, e, app.Elapsed);
        foreach (Particle p in app.Particles)
            if (p.Pos.X > viewL - 200 && p.Pos.X < viewR + 200) DrawParticle(g, p);

        DrawPlayer(g, app.Player, app.Elapsed);
        g.Restore(state);

        DrawHud(app, g, r, level);
        DrawScanlines(g, r);
    }

    private void DrawWorldBackground(Graphics g, Rectangle r, Level level, float camX, float t)
    {
        var (skyTop, skyBottom, far, near, accent) = Palette(level.Biome);
        using var bg = new LinearGradientBrush(r, skyTop, skyBottom, 90);
        g.FillRectangle(bg, r);

        if (level.Biome is Biome.Night or Biome.Nightmare or Biome.Haunted or Biome.Crypt or Biome.Cave)
            DrawStars(g, r, (int)(level.Seed & 0x7FFFFFFF), t);

        if (level.Biome is Biome.Pasture or Biome.Orchard or Biome.Coast or Biome.Mountain)
        {
            float sunX = r.Width * .78f - (camX * .015f) % 140;
            float sunY = r.Height * .20f;
            using Brush sb = new SolidBrush(Color.FromArgb(185, accent));
            g.FillEllipse(sb, sunX - 65, sunY - 65, 130, 130);
            using Pen stripe = new(skyTop, 5);
            for (int y = -35; y < 55; y += 13) g.DrawLine(stripe, sunX - 70, sunY + y, sunX + 70, sunY + y);
        }

        DrawParallaxHills(g, r, camX * .10f, r.Height * .58f, far, 70, level.Seed ^ 0x10);
        DrawParallaxHills(g, r, camX * .20f, r.Height * .70f, near, 95, level.Seed ^ 0x20);

        if (level.Biome == Biome.Fortress)
        {
            using Brush b = new SolidBrush(Color.FromArgb(85, 220, 220, 220));
            for (int x = -(int)(camX * .3f) % 180; x < r.Width; x += 180) g.FillRectangle(b, x, r.Height * .25f, 85, r.Height * .47f);
        }
        if (level.Biome == Biome.Coast)
        {
            using Brush water = new SolidBrush(Color.FromArgb(85, 30, 140, 235));
            g.FillRectangle(water, 0, r.Height * .72f, r.Width, r.Height * .28f);
        }
    }

    private static void DrawParallaxHills(Graphics g, Rectangle r, float offset, float baseY, Color color, float amp, ulong seed)
    {
        using GraphicsPath path = new();
        path.StartFigure();
        path.AddLine(0, r.Bottom, 0, baseY);
        for (int x = 0; x <= r.Width + 80; x += 80)
        {
            float wx = x + offset;
            float y = baseY - MathF.Sin((wx + (seed & 0xFFFF)) * .0062f) * amp - MathF.Sin(wx * .014f + 2.1f) * amp * .35f;
            path.AddLine(path.GetLastPoint(), new PointF(x, y));
        }
        path.AddLine(r.Right, r.Bottom, r.Left, r.Bottom);
        path.CloseFigure();
        using Brush b = new SolidBrush(color); g.FillPath(b, path);
    }

    private void DrawPlatform(Graphics g, Platform p, Biome biome)
    {
        RectangleF q = p.Rect;
        Color dirt = biome switch
        {
            Biome.Marsh => Color.FromArgb(82, 72, 45),
            Biome.Cave => Color.FromArgb(58, 54, 68),
            Biome.Ice => Color.FromArgb(115, 175, 210),
            Biome.Nightmare => Color.FromArgb(50, 16, 55),
            Biome.Fortress => Color.FromArgb(75, 78, 90),
            _ => Color.FromArgb(150, 94, 58)
        };
        Color top = p.Kind switch
        {
            PlatformKind.Stone => Color.FromArgb(118, 125, 138),
            PlatformKind.Cloud => Color.FromArgb(225, 245, 255),
            PlatformKind.Ice => Color.FromArgb(155, 235, 255),
            PlatformKind.Breakable => Color.FromArgb(215, 145, 70),
            PlatformKind.Crumbling => Color.FromArgb(205, 154, 92),
            PlatformKind.Moving => Color.FromArgb(70, 210, 225),
            PlatformKind.Conveyor => Color.FromArgb(155, 155, 175),
            _ => biome is Biome.Haunted or Biome.Night or Biome.Nightmare ? Color.FromArgb(65, 115, 68) : Color.FromArgb(35, 190, 82)
        };
        using Brush db = new SolidBrush(dirt), tb = new SolidBrush(top);
        g.FillRectangle(db, q);
        g.FillRectangle(tb, q.X, q.Y, q.Width, Math.Min(22, q.Height));
        using Pen edge = new(Color.FromArgb(160, Color.Black), 2); g.DrawLine(edge, q.X, q.Y + 22, q.Right, q.Y + 22);
        if (p.Kind == PlatformKind.Breakable)
        {
            using Pen crack = new(Color.FromArgb(130, 80, 40, 20), 3);
            for (float x = q.Left + 35; x < q.Right; x += 55) g.DrawLine(crack, x, q.Top + 3, x + 12, q.Top + 21);
        }
        if (p.Kind != PlatformKind.Cloud)
        {
            using Pen fleck = new(Color.FromArgb(32, Color.White), 1);
            for (float x = q.Left + 25; x < q.Right; x += 62) g.DrawLine(fleck, x, q.Top + 33, x + 19, q.Top + 44);
        }
        if (p.Kind == PlatformKind.Crumbling)
        {
            using Pen crack = new(Color.FromArgb(180, 90, 50, 25), 3);
            for (float x = q.Left + 22; x < q.Right - 20; x += 52)
            { g.DrawLine(crack, x, q.Top + 3, x + 14, q.Top + 16); g.DrawLine(crack, x + 14, q.Top + 16, x + 5, q.Top + 27); }
        }
        if (p.Kind == PlatformKind.Moving)
        {
            using Pen neon = new(Color.Cyan, 3); g.DrawRectangle(neon, q.X + 5, q.Y + 5, q.Width - 10, Math.Min(17, q.Height - 8));
        }
        if (p.Kind == PlatformKind.Conveyor)
        {
            using Pen belt = new(Color.FromArgb(220, 35, 35, 45), 3);
            for (float x = q.Left + 14; x < q.Right - 10; x += 36)
            {
                float dir = Math.Sign(p.ConveyorSpeed) == 0 ? 1 : Math.Sign(p.ConveyorSpeed);
                g.DrawLine(belt, x, q.Top + 11, x + 13 * dir, q.Top + 11);
                g.DrawLine(belt, x + 13 * dir, q.Top + 11, x + 7 * dir, q.Top + 5);
            }
        }
    }

    private void DrawHazard(Graphics g, Hazard h, float t, bool reduceFlashes)
    {
        RectangleF q = h.Rect;
        float alpha = h.Active ? 1f : .28f;
        if (reduceFlashes) alpha = Math.Max(.55f, alpha);
        switch (h.Kind)
        {
            case HazardKind.Spikes:
            case HazardKind.IceShard:
                using (Brush b = new SolidBrush(Color.FromArgb((int)(230 * alpha), h.Kind == HazardKind.IceShard ? Color.Cyan : Color.Silver)))
                {
                    int n = Math.Max(1, (int)(q.Width / 24));
                    for (int i = 0; i < n; i++)
                    {
                        float x = q.Left + i * q.Width / n;
                        g.FillPolygon(b, new[] { new PointF(x, q.Bottom), new PointF(x + q.Width / n / 2, q.Top), new PointF(x + q.Width / n, q.Bottom) });
                    }
                }
                break;
            case HazardKind.FireJet:
                using (Brush outer = new SolidBrush(Color.FromArgb((int)(190 * alpha), Color.OrangeRed)))
                    g.FillPolygon(outer, new[] { new PointF(q.Left, q.Bottom), new PointF(q.Left + q.Width * .18f, q.Top + q.Height * .35f), new PointF(q.Left + q.Width * .5f, q.Top), new PointF(q.Right - q.Width * .15f, q.Top + q.Height * .38f), new PointF(q.Right, q.Bottom) });
                using (Brush inner = new SolidBrush(Color.FromArgb((int)(220 * alpha), Color.Gold)))
                    g.FillEllipse(inner, q.Left + q.Width * .25f, q.Top + q.Height * .35f, q.Width * .5f, q.Height * .58f);
                break;
            case HazardKind.SawBlade:
                using (Pen p = new(Color.FromArgb((int)(235 * alpha), Color.Silver), 7)) g.DrawEllipse(p, q);
                using (Pen p = new(Color.FromArgb((int)(220 * alpha), Color.Red), 3))
                {
                    float cx = q.Left + q.Width / 2, cy = q.Top + q.Height / 2;
                    for (int i = 0; i < 8; i++)
                    { float a = t * 5 + i * MathF.Tau / 8; g.DrawLine(p, cx, cy, cx + MathF.Cos(a) * q.Width * .62f, cy + MathF.Sin(a) * q.Height * .62f); }
                }
                break;
            case HazardKind.Crusher:
                using (Brush b = new SolidBrush(Color.FromArgb((int)(220 * alpha), 75, 75, 85))) g.FillRectangle(b, q);
                using (Pen p = new(Color.Gold, 4)) g.DrawRectangle(p, q.X + 4, q.Y + 4, q.Width - 8, q.Height - 8);
                break;
            case HazardKind.Lava:
            case HazardKind.RisingGoop:
                using (Brush b = new SolidBrush(Color.FromArgb((int)(210 * alpha), h.Kind == HazardKind.Lava ? Color.OrangeRed : Color.YellowGreen))) g.FillRectangle(b, q);
                break;
            case HazardKind.NightmareStatic:
                using (Brush b = new SolidBrush(Color.FromArgb((int)(120 * alpha), 255, 30, 210))) g.FillRectangle(b, q);
                using (Pen p = new(Color.FromArgb((int)(210 * alpha), Color.Cyan), 2))
                    for (int i = 0; i < 5; i++) { float yy = q.Top + ((i * 19 + (int)(t * 85)) % Math.Max(1, (int)q.Height)); g.DrawLine(p, q.Left, yy, q.Right, yy); }
                break;
        }
    }

    private void DrawSign(Graphics g, SignPost s)
    {
        const float w = 310, h = 68;
        RectangleF box = new(s.Pos.X - 4, s.Pos.Y - h - 54, w, h);
        using Brush post = new SolidBrush(Color.FromArgb(100, 64, 34)); g.FillRectangle(post, s.Pos.X + 18, s.Pos.Y - 56, 10, 56);
        using Brush wood = new SolidBrush(Color.FromArgb(232, 203, 126)); g.FillRoundedRectangle(wood, box, 8);
        using Pen pen = new(Color.FromArgb(110, 65, 35), 3); g.DrawRoundedRectangle(pen, box, 8);
        RectangleF text = RectangleF.Inflate(box, -8, -6);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(s.Text, _tinyFont, Brushes.Black, text, sf);
    }

    private static void DrawCheckpoint(Graphics g, Checkpoint cp, Level level)
    {
        float y = 530;
        foreach (Platform p in level.Platforms)
            if (!p.Destroyed && cp.X >= p.Rect.Left && cp.X <= p.Rect.Right) { y = Math.Min(y, p.Rect.Top); }
        using Pen pole = new(Color.White, 5); g.DrawLine(pole, cp.X, y - 120, cp.X, y);
        Color c = cp.Activated ? Color.Lime : Color.Gold;
        using Brush flag = new SolidBrush(c); g.FillPolygon(flag, new[] { new PointF(cp.X, y - 120), new PointF(cp.X + 58, y - 102), new PointF(cp.X, y - 82) });
    }

    private void DrawExit(Graphics g, RectangleF exit, bool active, float t)
    {
        Color glow = active ? Color.FromArgb(200, 60, 255, 190) : Color.FromArgb(150, 140, 40, 60);
        using Pen p = new(glow, 7); g.DrawEllipse(p, exit.X - 18, exit.Y, exit.Width + 36, exit.Height);
        using Pen p2 = new(Color.FromArgb(120 + (int)(MathF.Sin(t * 5) * 50), Color.White), 3); g.DrawEllipse(p2, exit.X - 8, exit.Y + 8, exit.Width + 16, exit.Height - 16);
        using Font f = new("Arial Black", 14, FontStyle.Bold, GraphicsUnit.Pixel);
        g.DrawString(active ? "EXIT" : "BOSS FIRST", f, active ? Brushes.White : Brushes.IndianRed, exit.X - 14, exit.Bottom + 6);
    }

    private void DrawPickup(Graphics g, Pickup p, float t)
    {
        float y = p.Pos.Y + MathF.Sin(p.BobPhase + t * 2f) * 6;
        switch (p.Kind)
        {
            case PickupKind.Sparkly:
                DrawStar(g, p.Pos.X, y, 11, Color.Cyan, Color.White); break;
            case PickupKind.RainbowDrop:
                using (var b = new LinearGradientBrush(new RectangleF(p.Pos.X - 12, y - 16, 24, 32), Color.HotPink, Color.Cyan, 90))
                    g.FillEllipse(b, p.Pos.X - 12, y - 16, 24, 32);
                break;
            case PickupKind.Pixie:
                DrawFairy(g, p.Pos.X, y, .7f); break;
            case PickupKind.BoostExtender:
                using (Brush b = new SolidBrush(Color.Lime)) g.FillEllipse(b, p.Pos.X - 15, y - 15, 30, 30);
                using (Pen pe = new(Color.White, 4)) { g.DrawLine(pe, p.Pos.X - 8, y + 2, p.Pos.X + 8, y - 8); g.DrawLine(pe, p.Pos.X + 2, y - 8, p.Pos.X + 8, y - 8); }
                break;
            case PickupKind.FairyCake:
                using (Brush cake = new SolidBrush(Color.SandyBrown)) g.FillRectangle(cake, p.Pos.X - 15, y - 8, 30, 18);
                using (Brush icing = new SolidBrush(Color.HotPink)) g.FillEllipse(icing, p.Pos.X - 16, y - 16, 32, 18);
                break;
            case PickupKind.Armor:
                using (Pen ar = new(Color.Silver, 5)) g.DrawRectangle(ar, p.Pos.X - 14, y - 18, 28, 36);
                break;
            case PickupKind.SecretCode:
                using (Brush q = new SolidBrush(Color.FromArgb(235, 255, 230, 40))) g.FillRoundedRectangle(q, new RectangleF(p.Pos.X - 20, y - 20, 40, 40), 8);
                DrawCentered(g, "?", _menuFont, p.Pos.X, y - 14, Color.Black);
                break;
            case PickupKind.OneUp:
                using (Brush one = new SolidBrush(Color.LimeGreen)) g.FillEllipse(one, p.Pos.X - 18, y - 18, 36, 36);
                DrawCentered(g, "1U", _smallFont, p.Pos.X, y - 9, Color.White);
                break;
            case PickupKind.SparklePower:
                DrawStar(g, p.Pos.X, y, 16, Color.DeepSkyBlue, Color.White);
                DrawStar(g, p.Pos.X, y, 8, Color.White, Color.HotPink);
                break;
        }
    }

    private void DrawProjectile(Graphics g, Projectile p, float t)
    {
        switch (p.Kind)
        {
            case ProjectileKind.Fairy: DrawFairy(g, p.Pos.X, p.Pos.Y, .55f); break;
            case ProjectileKind.Web:
                using (Pen w = new(Color.WhiteSmoke, 2))
                { g.DrawEllipse(w, p.Pos.X - 14, p.Pos.Y - 14, 28, 28); g.DrawLine(w, p.Pos.X - 12, p.Pos.Y, p.Pos.X + 12, p.Pos.Y); g.DrawLine(w, p.Pos.X, p.Pos.Y - 12, p.Pos.X, p.Pos.Y + 12); }
                break;
            case ProjectileKind.Acorn:
                using (Brush b = new SolidBrush(Color.SaddleBrown)) g.FillEllipse(b, p.Pos.X - 8, p.Pos.Y - 10, 16, 20); break;
            case ProjectileKind.Bat:
                using (Brush b = new SolidBrush(Color.FromArgb(220, 40, 20, 55))) g.FillPolygon(b, new[] { new PointF(p.Pos.X - 15, p.Pos.Y), new PointF(p.Pos.X, p.Pos.Y - 8), new PointF(p.Pos.X + 15, p.Pos.Y), new PointF(p.Pos.X, p.Pos.Y + 6) }); break;
            case ProjectileKind.Toast:
                using (Brush b = new SolidBrush(Color.Peru)) g.FillRoundedRectangle(b, new RectangleF(p.Pos.X - 11, p.Pos.Y - 9, 22, 18), 5); break;
            case ProjectileKind.Stationery:
                using (Brush b = new SolidBrush(Color.HotPink)) g.FillRectangle(b, p.Pos.X - 12, p.Pos.Y - 8, 24, 16);
                using (Pen pe = new(Color.Cyan, 2)) g.DrawLine(pe, p.Pos.X - 8, p.Pos.Y, p.Pos.X + 8, p.Pos.Y); break;
            case ProjectileKind.Snowball:
                using (Brush b = new SolidBrush(Color.White)) g.FillEllipse(b, p.Pos.X - 12, p.Pos.Y - 12, 24, 24);
                using (Pen pe = new(Color.LightBlue, 2)) g.DrawEllipse(pe, p.Pos.X - 12, p.Pos.Y - 12, 24, 24); break;
            case ProjectileKind.Citation:
                using (Brush b = new SolidBrush(Color.Gold)) g.FillRectangle(b, p.Pos.X - 15, p.Pos.Y - 10, 30, 20);
                using (Font f = new("Consolas", 7, FontStyle.Bold, GraphicsUnit.Pixel)) DrawCentered(g, "OSHA", f, p.Pos.X, p.Pos.Y - 4, Color.Black); break;
            case ProjectileKind.Bone:
                using (Pen pe = new(Color.WhiteSmoke, 5)) g.DrawLine(pe, p.Pos.X - 11, p.Pos.Y - 7, p.Pos.X + 11, p.Pos.Y + 7); break;
            case ProjectileKind.Spark:
                DrawStar(g, p.Pos.X, p.Pos.Y, 10 + MathF.Sin(t * 20) * 2, Color.Cyan, Color.White); break;
            case ProjectileKind.DarkBolt:
                // Make hostile dark magic unmistakable instead of a context-free purple ball.
                using (Brush aura = new SolidBrush(Color.FromArgb(95, 225, 40, 255))) g.FillEllipse(aura, p.Pos.X - 16, p.Pos.Y - 16, 32, 32);
                DrawStar(g, p.Pos.X, p.Pos.Y, 11 + MathF.Sin(t * 24) * 2, Color.DarkViolet, Color.HotPink);
                using (Pen ring = new(Color.White, 2)) g.DrawEllipse(ring, p.Pos.X - 8, p.Pos.Y - 8, 16, 16);
                break;
            default:
                using (Brush b = new SolidBrush(Color.FromArgb(220, 180, 40, 255))) g.FillEllipse(b, p.Pos.X - 9, p.Pos.Y - 9, 18, 18); break;
        }
    }

    private void DrawPlayer(Graphics g, PlayerActor p, float t)
    {
        bool left = p.Facing < 0;
        Bitmap sprite;
        if (!p.Grounded || Math.Abs(p.Vel.Y) > 40)
        {
            int phase = Math.Clamp((int)((Math.Clamp(-p.Vel.Y, -900, 900) + 900) / 600), 0, 2);
            sprite = _jump[(left ? 3 : 0) + phase];
        }
        else
        {
            int frame = Math.Abs(p.Vel.X) > 30 ? (int)(t * (p.Boosting ? 14 : 8)) % 3 : 0;
            sprite = _walk[(left ? 3 : 0) + frame];
        }
        float w = 100, h = 120;
        RectangleF dst = new(p.Pos.X - w / 2, p.Pos.Y - h, w, h);
        if (p.InvulnTimer > 0 && ((int)(t * 18) & 1) == 0) return;

        if (p.FaceplantTimer > 0)
        {
            GraphicsState st = g.Save();
            g.TranslateTransform(p.Pos.X, p.Pos.Y - 30);
            g.RotateTransform(p.Facing > 0 ? 82 : -82);
            g.DrawImage(sprite, -w / 2, -h + 30, w, h);
            g.Restore(st);
        }
        else if (p.LancePose)
        {
            // ALPHA2.6: Ramjet + Joust is a committed lance posture.  The legacy
            // sprite is one-piece art, so pitch the whole silhouette slightly around
            // a low body pivot.  That makes Sparkles visibly drop her head/horn into
            // the charge instead of trotting upright while the collision box attacks.
            GraphicsState st = g.Save();
            float pivotX = p.Pos.X - p.Facing * 12f;
            float pivotY = p.Pos.Y - 28f;
            g.TranslateTransform(pivotX, pivotY);
            g.RotateTransform(p.Facing > 0 ? 8.5f : -8.5f);
            g.DrawImage(sprite, dst.X - pivotX, dst.Y - pivotY, dst.Width, dst.Height);
            g.Restore(st);
        }
        else g.DrawImage(sprite, dst);

        if (p.Jousting && !p.Dead)
        {
            (Vector2 root, Vector2 tip) = p.HornAxis;

            // No glowing straight line here.  ALPHA2.3 accidentally made the
            // "cute wicked spiral" read like a laser beam by drawing a fat core
            // through the horn.  The armed tell is now sparkle motion only.
            DrawHornJoustSwirl(g, p, t, root, tip);
            DrawStar(g, tip.X, tip.Y, p.Overdrive ? 6.5f : 4.5f, Color.White, p.Overdrive ? Color.HotPink : Color.Cyan);
        }
        if (p.Stomping && !p.Grounded)
        {
            using Pen ring = new(Color.FromArgb(150, 255, 255, 255), 4); g.DrawEllipse(ring, p.Pos.X - 48, p.Pos.Y - 18, 96, 26);
        }
    }

    private void DrawEnemy(Graphics g, Enemy e, float t)
    {
        RectangleF b = e.Bounds;
        float cx = e.Pos.X, bottom = e.Pos.Y;
        if (e.HitCooldown > 0 && ((int)(t * 28) & 1) == 0) return;
        switch (e.Kind)
        {
            case EnemyKind.Teddy: DrawTeddy(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.Zombie: DrawZombie(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.PorcelainDoll: DrawDoll(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.RabidSquirrel: DrawSquirrel(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.DireMouse: DrawMouse(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.Ghost: DrawGhost(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.EvilTree: DrawTree(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.Goblin: DrawGoblin(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.Imp: DrawImp(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.UnicornHunter: DrawHunter(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.Werewolf: DrawWerewolf(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.Ed: DrawEd(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.OldGnarley: DrawTree(g, cx, bottom, e.Width, e.Height, true); break;
            case EnemyKind.Spookers: DrawGhost(g, cx, bottom, e.Width, e.Height, true); break;
            case EnemyKind.Webbey: DrawSpider(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.Mina: DrawBird(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.CountSpatula: DrawVampire(g, cx, bottom, e.Width, e.Height); break;
            case EnemyKind.Rip: DrawWerewolf(g, cx, bottom, e.Width, e.Height, true); break;
            case EnemyKind.NightMare: DrawNightMare(g, cx, bottom, e.Width, e.Height); break;
            default:
                // Expansion-roster safety net.  During ALPHA2.4 the simulation/catalog
                // had grown far beyond the hand-drawn switch above, which meant those
                // enemies could attack and collide while rendering literally nothing.
                // Every catalog enemy must remain guest-visible even before its final
                // production sprite exists.  This readable 90s-style proxy encodes
                // capability cues (wings, shield, spikes, ranged weapon) and labels the
                // family so playtest bug reports can identify the culprit.
                DrawCatalogMinion(g, e, t);
                break;
        }
        if (e.Boss)
        {
            RectangleF hp = new(cx - 95, b.Top - 26, 190, 12);
            using Brush back = new SolidBrush(Color.FromArgb(180, 0, 0, 0)); g.FillRectangle(back, hp);
            using Brush fill = new SolidBrush(Color.HotPink); g.FillRectangle(fill, hp.X + 2, hp.Y + 2, (hp.Width - 4) * Math.Max(0, e.Health) / e.MaxHealth, hp.Height - 4);
            DrawCentered(g, BossName(e.Kind), _tinyFont, cx, b.Top - 46, Color.White);
        }
    }

    private void DrawCatalogMinion(Graphics g, Enemy e, float t)
    {
        EnemySpec spec = EnemyCatalog.Get(e.Kind);
        EnemyCapability caps = spec.Capabilities;
        float cx = e.Pos.X;
        float bottom = e.Pos.Y;
        float w = Math.Max(34, e.Width);
        float h = Math.Max(32, e.Height);
        float top = bottom - h;

        // Give every placeholder family a deterministic silhouette variation so a
        // crowd is readable before the final sprite/atlas pass replaces these proxies.
        int variant = ((int)e.Kind * 17 + 11) % 5;
        float bob = (caps & (EnemyCapability.Flyer | EnemyCapability.Hover)) != 0
            ? MathF.Sin(t * 5.2f + (int)e.Kind) * 3.5f : 0;
        top += bob;
        bottom += bob;

        using Brush shadow = new SolidBrush(Color.FromArgb(75, 0, 0, 0));
        g.FillEllipse(shadow, cx - w * .42f, bottom - 6, w * .84f, 13);

        using Brush primary = new SolidBrush(spec.Primary);
        using Brush secondary = new SolidBrush(spec.Secondary);
        using Pen outline = new(Color.FromArgb(235, 20, 15, 30), Math.Max(2, w * .035f));

        RectangleF body = variant switch
        {
            0 => new RectangleF(cx - w * .36f, top + h * .28f, w * .72f, h * .62f),
            1 => new RectangleF(cx - w * .42f, top + h * .34f, w * .84f, h * .52f),
            2 => new RectangleF(cx - w * .31f, top + h * .22f, w * .62f, h * .68f),
            3 => new RectangleF(cx - w * .45f, top + h * .40f, w * .90f, h * .45f),
            _ => new RectangleF(cx - w * .38f, top + h * .30f, w * .76f, h * .58f)
        };

        if (variant is 1 or 3)
        {
            g.FillEllipse(primary, body);
            g.DrawEllipse(outline, body);
        }
        else
        {
            g.FillRoundedRectangle(primary, body, Math.Max(5, w * .12f));
            g.DrawRoundedRectangle(outline, body, Math.Max(5, w * .12f));
        }

        // Head / face.  The expression is intentionally obnoxious and readable at speed.
        float headR = Math.Clamp(Math.Min(w, h) * .23f, 10, 23);
        float headY = top + headR + h * .06f;
        g.FillEllipse(secondary, cx - headR, headY - headR, headR * 2, headR * 2);
        g.DrawEllipse(outline, cx - headR, headY - headR, headR * 2, headR * 2);
        using Brush eye = new SolidBrush(Color.White);
        using Brush pupil = new SolidBrush(Color.FromArgb(245, 15, 5, 25));
        float eyeDx = headR * .38f;
        float eyeY = headY - headR * .12f;
        for (int side = -1; side <= 1; side += 2)
        {
            float ex = cx + side * eyeDx;
            g.FillEllipse(eye, ex - 4, eyeY - 4, 8, 8);
            g.FillEllipse(pupil, ex - 1.7f + e.Direction * 1.5f, eyeY - 1.7f, 3.4f, 3.4f);
        }

        // Wings immediately identify airborne families.
        if ((caps & (EnemyCapability.Flyer | EnemyCapability.Hover | EnemyCapability.DiveBomber)) != 0)
        {
            using Brush wing = new SolidBrush(Color.FromArgb(150, spec.Secondary));
            PointF[] leftWing = { new(cx - w * .28f, top + h * .45f), new(cx - w * .72f, top + h * .25f), new(cx - w * .55f, top + h * .62f) };
            PointF[] rightWing = { new(cx + w * .28f, top + h * .45f), new(cx + w * .72f, top + h * .25f), new(cx + w * .55f, top + h * .62f) };
            g.FillPolygon(wing, leftWing); g.FillPolygon(wing, rightWing);
            g.DrawPolygon(outline, leftWing); g.DrawPolygon(outline, rightWing);
        }

        // The design rule says non-stompable enemies must advertise it with spikes.
        if ((caps & EnemyCapability.StompImmune) != 0)
        {
            using Brush spike = new SolidBrush(Color.Gold);
            int count = Math.Max(3, (int)(w / 18));
            for (int i = 0; i < count; i++)
            {
                float sx = cx - w * .34f + i * (w * .68f / Math.Max(1, count - 1));
                PointF[] tri = { new(sx - 6, top + h * .30f), new(sx, top + h * .08f), new(sx + 6, top + h * .30f) };
                g.FillPolygon(spike, tri); g.DrawPolygon(outline, tri);
            }
        }

        // Shielded fronts get a giant obvious plate on the facing side.
        if ((caps & EnemyCapability.ShieldFront) != 0)
        {
            float shieldX = cx + e.Direction * (w * .38f);
            RectangleF shield = new(shieldX - 8 - (e.Direction < 0 ? 16 : 0), top + h * .37f, 24, h * .42f);
            using Brush metal = new SolidBrush(Color.FromArgb(220, 205, 215, 230));
            using Pen rim = new(Color.Gold, 3);
            g.FillRoundedRectangle(metal, shield, 7); g.DrawRoundedRectangle(rim, shield, 7);
        }

        // Ranged enemies visibly carry the thing that has been ruining the player's day.
        if ((caps & EnemyCapability.Ranged) != 0)
        {
            float gunY = top + h * .58f;
            float muzzleX = cx + e.Direction * w * .52f;
            using Pen gun = new(Color.FromArgb(235, 35, 25, 45), 6);
            g.DrawLine(gun, cx + e.Direction * w * .12f, gunY, muzzleX, gunY - 3);
            DrawStar(g, muzzleX, gunY - 3, 5 + MathF.Sin(t * 13 + (int)e.Kind) * 1.2f, Color.MediumPurple, Color.White);
        }

        // Tiny wheels/feet help runners read as movement threats.
        if ((caps & (EnemyCapability.Runner | EnemyCapability.Charger | EnemyCapability.IceSlider)) != 0)
        {
            using Brush wheel = new SolidBrush(Color.FromArgb(235, 25, 25, 32));
            g.FillEllipse(wheel, cx - w * .30f - 6, bottom - 12, 12, 12);
            g.FillEllipse(wheel, cx + w * .30f - 6, bottom - 12, 12, 12);
        }

        // Alpha-build identification tag.  Final art replaces the need for this, but it
        // is invaluable while dozens of families are mechanically live before art lock.
        string tag = spec.Name.ToUpperInvariant();
        SizeF ts = g.MeasureString(tag, _tinyFont);
        RectangleF tagBg = new(cx - ts.Width / 2 - 4, top - 18, ts.Width + 8, 15);
        using Brush tb = new SolidBrush(Color.FromArgb(155, 0, 0, 0));
        g.FillRoundedRectangle(tb, tagBg, 4);
        DrawCentered(g, tag, _tinyFont, cx, top - 17, Color.White);
    }

    private void DrawParticle(Graphics g, Particle p)
    {
        float a = p.MaxLife <= 0 ? 1 : Math.Clamp(p.Life / p.MaxLife, 0, 1);
        Color c = Color.FromArgb((int)(a * p.Color.A), p.Color);
        using Brush b = new SolidBrush(c);
        if (p.Shape == 1) DrawStar(g, p.Pos.X, p.Pos.Y, p.Size, c, Color.White);
        else if (p.Shape == 2) g.FillRectangle(b, p.Pos.X - p.Size / 2, p.Pos.Y - p.Size / 2, p.Size, p.Size);
        else g.FillEllipse(b, p.Pos.X - p.Size / 2, p.Pos.Y - p.Size / 2, p.Size, p.Size);
    }

    private void DrawHud(GameApp app, Graphics g, Rectangle r, Level level)
    {
        PlayerActor p = app.Player;
        RectangleF panel = new(15, 15, Math.Min(720, r.Width - 30), 92);
        using Brush back = new SolidBrush(Color.FromArgb(205, 8, 10, 26)); g.FillRoundedRectangle(back, panel, 14);
        using Pen border = new(Color.FromArgb(210, 90, 245, 255), 2); g.DrawRoundedRectangle(border, panel, 14);

        g.DrawString($"SCORE {p.Score:00000000}   LIVES {p.Lives}", _hudFont, Brushes.White, panel.Left + 16, panel.Top + 11);
        g.DrawString($"PIXIES {p.Pixies:00}   CAKES {p.FairyCakes:00}", _smallFont, Brushes.Gold, panel.Left + 16, panel.Top + 42);
        for (int i = 0; i < 3; i++) DrawHeart(g, panel.Left + 330 + i * 30, panel.Top + 18, i < p.Health ? Color.HotPink : Color.FromArgb(70, 120, 80, 100));
        for (int i = 0; i < p.ArmorCapacity; i++)
        {
            using Pen ap = new(i < p.Armor ? Color.Silver : Color.FromArgb(70, Color.Silver), 3);
            g.DrawRectangle(ap, panel.Left + 330 + i * 19, panel.Top + 54, 13, 17);
        }
        RectangleF fuel = new(panel.Left + 475, panel.Top + 21, 220, 18);
        using Brush fb = new SolidBrush(Color.FromArgb(90, Color.White)); g.FillRoundedRectangle(fb, fuel, 6);
        float normalFrac = Math.Min(1, p.BoostFuel / PlayerActor.NormalBoostFuel);
        using Brush ff = new SolidBrush(p.Overdrive ? Color.HotPink : Color.Cyan); g.FillRoundedRectangle(ff, new RectangleF(fuel.X + 2, fuel.Y + 2, (fuel.Width - 4) * normalFrac, fuel.Height - 4), 5);
        if (p.BoostFuel > PlayerActor.NormalBoostFuel)
        {
            float extra = Math.Min(1, (p.BoostFuel - PlayerActor.NormalBoostFuel) / 5.8f);
            using Brush ex = new SolidBrush(Color.Gold); g.FillRectangle(ex, fuel.X + fuel.Width - 4, fuel.Y + 2, Math.Min(90, extra * 90), fuel.Height - 4);
        }
        g.DrawString(p.Overdrive ? "FART DRIVE: UNSTABLE" : "FART DRIVE", _tinyFont, p.Overdrive ? Brushes.HotPink : Brushes.WhiteSmoke, fuel.X, fuel.Bottom + 4);

        float progress = Math.Clamp(p.Pos.X / Math.Max(1, level.Width), 0, 1);
        RectangleF prog = new(15, r.Bottom - 27, r.Width - 30, 10);
        using Brush pb = new SolidBrush(Color.FromArgb(90, Color.White)); g.FillRectangle(pb, prog);
        using Brush pf = new SolidBrush(Color.Gold); g.FillRectangle(pf, prog.X, prog.Y, prog.Width * progress, prog.Height);
        DrawCentered(g, $"{level.Name}   •   SEED {level.Seed:X16}", _tinyFont, r.Width / 2f, r.Bottom - 48, Color.WhiteSmoke);

        using Font controls = new("Consolas", 10, FontStyle.Bold, GraphicsUnit.Pixel);
        g.DrawString("A/D MOVE  SPACE JUMP  SHIFT/Z RAINBOW RAMJET  X/J HORN JOUST  S/C STOMP  F/V FAIRY BELCH  ESC PAUSE", controls, Brushes.WhiteSmoke, 18, r.Bottom - 67);
    }

    private void DrawPause(GameApp app, Graphics g, Rectangle r)
    {
        using Brush shade = new SolidBrush(Color.FromArgb(175, 0, 0, 0)); g.FillRectangle(shade, r);
        RectangleF box = new(r.Width / 2f - 260, r.Height / 2f - 220, 520, 440);
        using Brush back = new SolidBrush(Color.FromArgb(235, 23, 16, 50)); g.FillRoundedRectangle(back, box, 25);
        using Pen pen = new(Color.Cyan, 3); g.DrawRoundedRectangle(pen, box, 25);
        DrawCentered(g, "PAUSED", _titleFont, r.Width / 2f, box.Top + 25, Color.White);
        string[] items = { "RESUME", "RESTART LEVEL", "RETURN TO MAP", "TOGGLE SYNTH MUSIC", "TOGGLE FULLSCREEN", "TITLE SCREEN" };
        for (int i = 0; i < items.Length; i++)
        {
            Color c = i == app.PauseChoice ? Color.Gold : Color.White;
            DrawCentered(g, (i == app.PauseChoice ? "▶ " : "") + items[i], _menuFont, r.Width / 2f, box.Top + 105 + i * 45, c);
        }
    }

    private void DrawCodeEntry(GameApp app, Graphics g, Rectangle r)
    {
        using var bg = new LinearGradientBrush(r, Color.FromArgb(10, 10, 35), Color.FromArgb(90, 5, 100), 90); g.FillRectangle(bg, r);
        DrawStars(g, r, 0xC0DE, app.Elapsed);
        DrawCentered(g, "SECRET LEVEL CODE", _titleFont, r.Width / 2f, r.Height / 2f - 150, Color.Gold);
        RectangleF input = new(r.Width / 2f - 320, r.Height / 2f - 45, 640, 85);
        using Brush b = new SolidBrush(Color.FromArgb(225, 0, 0, 0)); g.FillRoundedRectangle(b, input, 15);
        using Pen p = new(Color.Cyan, 3); g.DrawRoundedRectangle(p, input, 15);
        string shown = app.CodeEntry + (((int)(app.Elapsed * 2) & 1) == 0 ? "_" : "");
        DrawCentered(g, shown, _menuFont, r.Width / 2f, input.Top + 23, Color.White);
        DrawCentered(g, "Any code becomes a deterministic off-map level. Some codes are... less accidental.", _smallFont, r.Width / 2f, input.Bottom + 35, Color.WhiteSmoke);
        DrawCentered(g, "ENTER: launch   ESC: back", _smallFont, r.Width / 2f, input.Bottom + 72, Color.Gold);
    }

    private void DrawOptions(GameApp app, Graphics g, Rectangle r)
    {
        using var bg = new LinearGradientBrush(r, Color.FromArgb(12, 8, 38), Color.FromArgb(16, 92, 118), 90f);
        g.FillRectangle(bg, r);
        DrawStars(g, r, 0x0B710A5, app.Elapsed * .35f);
        DrawCentered(g, "OPTIONS / CONTROL DEPARTMENT", _titleFont, r.Width / 2f, 38, Color.Gold);
        DrawCentered(g, "Because apparently a rainbow-farting unicorn still needs paperwork.", _smallFont, r.Width / 2f, 103, Color.WhiteSmoke);

        int count = app.OptionsItemCount;
        int visible = Math.Max(8, Math.Min(14, (r.Height - 220) / 38));
        int start = Math.Clamp(app.OptionsChoice - visible / 2, 0, Math.Max(0, count - visible));
        int end = Math.Min(count, start + visible);
        float top = 145;
        float rowH = 38;
        float width = Math.Min(760, r.Width - 80);

        for (int i = start; i < end; i++)
        {
            float y = top + (i - start) * rowH;
            RectangleF row = new(r.Width / 2f - width / 2f, y, width, rowH - 4);
            bool selected = i == app.OptionsChoice;
            if (selected)
            {
                using Brush hi = new SolidBrush(Color.FromArgb(220, 255, 218, 40));
                g.FillRoundedRectangle(hi, row, 10);
                using Pen edge = new(Color.White, 2);
                g.DrawRoundedRectangle(edge, row, 10);
            }
            using var font = new Font("Consolas", 17, FontStyle.Bold, GraphicsUnit.Pixel);
            DrawCentered(g, (selected ? "▶ " : "") + app.OptionsItemText(i), font, r.Width / 2f, y + 6,
                selected ? Color.Black : Color.White);
        }

        if (count > visible)
        {
            string range = $"{start + 1}-{end} / {count}";
            g.DrawString(range, _tinyFont, Brushes.WhiteSmoke, r.Right - 105, r.Bottom - 34);
        }

        if (app.WaitingForBinding)
        {
            RectangleF prompt = new(r.Width / 2f - 330, r.Bottom - 122, 660, 72);
            using Brush pb = new SolidBrush(Color.FromArgb(235, 5, 5, 15));
            g.FillRoundedRectangle(pb, prompt, 14);
            using Pen pp = new(Color.HotPink, 3);
            g.DrawRoundedRectangle(pp, prompt, 14);
            DrawCentered(g, $"PRESS A KEY FOR {app.BindingAction.ToString().ToUpperInvariant()}", _menuFont, r.Width / 2f, prompt.Top + 9, Color.White);
            DrawCentered(g, "ESC cancels rebinding", _tinyFont, r.Width / 2f, prompt.Top + 46, Color.Gold);
        }
        else
        {
            DrawCentered(g, "UP/DOWN: SELECT   LEFT/RIGHT: VOLUME   ENTER: CHANGE   ESC: BACK", _tinyFont,
                r.Width / 2f, r.Bottom - 36, Color.Gold);
        }
    }

    private void DrawSecretBrowser(GameApp app, Graphics g, Rectangle r)
    {
        using var bg = new LinearGradientBrush(r, Color.FromArgb(8, 5, 26), Color.FromArgb(88, 8, 96), 35f);
        g.FillRectangle(bg, r);
        DrawStars(g, r, 0x5EC8E7, app.Elapsed * .45f);
        DrawCentered(g, "SECRET LEVELS THEY TOLD YOU NOT TO WRITE DOWN", _titleFont, r.Width / 2f, 48, Color.HotPink);
        DrawCentered(g, "Same code + same generator version = same ridiculous level.", _smallFont, r.Width / 2f, 112, Color.WhiteSmoke);

        IReadOnlyList<string> codes = app.DiscoveredCodes;
        int count = codes.Count + 1; // final pseudo-row is ENTER NEW CODE
        int visible = Math.Max(7, Math.Min(13, (r.Height - 250) / 43));
        int start = Math.Clamp(app.SecretChoice - visible / 2, 0, Math.Max(0, count - visible));
        int end = Math.Min(count, start + visible);
        float top = 160;
        float rowH = 43;
        float width = Math.Min(720, r.Width - 90);

        for (int i = start; i < end; i++)
        {
            string label = i < codes.Count ? codes[i] : "+ ENTER A NEW SECRET CODE";
            bool selected = i == app.SecretChoice;
            RectangleF row = new(r.Width / 2f - width / 2f, top + (i - start) * rowH, width, 36);
            if (selected)
            {
                using Brush hi = new SolidBrush(Color.FromArgb(225, 35, 225, 255));
                g.FillRoundedRectangle(hi, row, 10);
                using Pen edge = new(Color.White, 2);
                g.DrawRoundedRectangle(edge, row, 10);
            }
            DrawCentered(g, (selected ? "▶ " : "") + label, _smallFont, r.Width / 2f, row.Top + 8,
                selected ? Color.Black : Color.White);
        }

        if (codes.Count == 0)
            DrawCentered(g, "NO DISCOVERED CODES YET. THIS IS EITHER DISCIPLINE OR FAILURE.", _smallFont, r.Width / 2f, top + 58, Color.Gold);

        DrawCentered(g, "ENTER: PLAY / ENTER CODE   ESC: TITLE", _tinyFont, r.Width / 2f, r.Bottom - 42, Color.Gold);
    }

    private void DrawStory(GameApp app, Graphics g, Rectangle r)
    {
        if (app.StorySlides.Count == 0)
        {
            g.Clear(Color.Black);
            return;
        }

        int index = Math.Clamp(app.StoryIndex, 0, app.StorySlides.Count - 1);
        StorySlide slide = app.StorySlides[index];
        var palette = Palette(slide.Backdrop);
        using var bg = new LinearGradientBrush(r, palette.top, palette.bottom, 90f);
        g.FillRectangle(bg, r);
        DrawStars(g, r, unchecked((int)(0x57000000u + (uint)index * 7919u)), app.Elapsed * .22f);

        // A deliberately simple storyboard frame now; this rectangle is the contract a later
        // video cutscene player can replace without changing story progression.
        RectangleF frame = new(70, 74, r.Width - 140, r.Height - 170);
        using Brush veil = new SolidBrush(Color.FromArgb(185, 8, 8, 18));
        g.FillRoundedRectangle(veil, frame, 24);
        using Pen edge = new(palette.accent, 3);
        g.DrawRoundedRectangle(edge, frame, 24);

        DrawShadowText(g, slide.Title, _titleFont, new PointF(r.Width / 2f, frame.Top + 38), StringAlignment.Center,
            Color.White, Color.FromArgb(150, 20, 0, 40), 3);

        RectangleF body = new(frame.Left + 70, frame.Top + 135, frame.Width - 140, Math.Max(150, frame.Height - 290));
        using Brush bodyBrush = new SolidBrush(Color.WhiteSmoke);
        using var bodyFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.Word
        };
        using Font bodyFont = new("Arial", Math.Max(20, Math.Min(30, r.Width / 52f)), FontStyle.Bold, GraphicsUnit.Pixel);
        g.DrawString(slide.Body, bodyFont, bodyBrush, body, bodyFormat);

        RectangleF captionBox = new(frame.Left + 65, frame.Bottom - 118, frame.Width - 130, 58);
        using Brush cb = new SolidBrush(Color.FromArgb(170, 0, 0, 0));
        g.FillRoundedRectangle(cb, captionBox, 12);
        using Brush capBrush = new SolidBrush(Color.Gold);
        using var capFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(slide.Caption, _smallFont, capBrush, captionBox, capFormat);

        DrawCentered(g, $"STORYBOARD {index + 1}/{app.StorySlides.Count}   •   ENTER: NEXT   •   ESC: SKIP",
            _tinyFont, r.Width / 2f, r.Bottom - 52, Color.White);
    }

    private void DrawCredits(GameApp app, Graphics g, Rectangle r)
    {
        g.Clear(Color.FromArgb(11, 9, 30));
        DrawStars(g, r, 0xC0FFEE, app.Elapsed);
        DrawCentered(g, "SPARKLES", _titleFont, r.Width / 2f, 60, Color.HotPink);
        string[] lines =
        {
            "Rainbow-farting unicorn platformer, resurrected from the supplied 2015 VB.NET prototype.",
            "The original 28-node world map, stage names, character art and design archaeology are preserved.",
            "",
            "REBUILT AS:  C# / .NET 8 / WinForms / GDI+ / native WinMM procedural synth",
            "NO UNITY.  NO THIRD-PARTY GAME ENGINE.  NO NUGET RUNTIME DEPENDENCIES.",
            "",
            "CORE COMBAT",
            "Rainbow Ramjet — hold SHIFT/Z or right trigger",
            "Sparkly Horn Joust — hold X/J or gamepad X; speed makes it nastier",
            "Four-Hoof Stomp — S/C or gamepad B while airborne",
            "Fairy Belch — F/V or gamepad Y; consumes cake/pixie ammunition",
            "",
            "DETERMINISTIC LEVEL IDENTITY",
            $"Generator v{StableHash.GeneratorVersion} + fixed campaign seed + stage index = same stage every time.",
            "Secret codes use a separate SECRET namespace and never occupy a map node.",
            "",
            "F11 toggles fullscreen.  Esc returns.  XInput-compatible controller supported.",
            "",
            "Press ENTER or ESC to return to the title screen."
        };
        float y = 145;
        foreach (string line in lines)
        {
            Font f = line is "CORE COMBAT" or "DETERMINISTIC LEVEL IDENTITY" ? _menuFont : _smallFont;
            Color c = line is "CORE COMBAT" or "DETERMINISTIC LEVEL IDENTITY" ? Color.Gold : Color.WhiteSmoke;
            DrawCentered(g, line, f, r.Width / 2f, y, c);
            y += line.Length == 0 ? 20 : (f == _menuFont ? 39 : 26);
        }
    }

    private void DrawVictory(GameApp app, Graphics g, Rectangle r)
    {
        using var bg = new LinearGradientBrush(r, Color.FromArgb(25, 0, 65), Color.FromArgb(255, 70, 170), 35); g.FillRectangle(bg, r);
        DrawRetroGrid(g, r, app.Elapsed * 1.5f);
        for (int i = 0; i < 45; i++)
        {
            float x = (i * 137 + app.Elapsed * (35 + i % 5 * 12)) % (r.Width + 100) - 50;
            float y = (i * 89) % r.Height;
            DrawStar(g, x, y, 5 + i % 9, i % 2 == 0 ? Color.Gold : Color.Cyan, Color.White);
        }
        DrawShadowText(g, "NIGHT MARE DEFEATED", _titleFont, new PointF(r.Width / 2f, r.Height * .20f), StringAlignment.Center, Color.White, Color.DeepPink, 4);
        DrawCentered(g, "THE CHARMING FOREST IS CHARMING AGAIN.", _menuFont, r.Width / 2f, r.Height * .38f, Color.Gold);
        DrawCentered(g, "UNFORTUNATELY, SPARKLES IS STILL SPARKLES.", _menuFont, r.Width / 2f, r.Height * .45f, Color.White);
        Bitmap sparkle = _mapSparkles[(int)(app.Elapsed * 6) % 3];
        g.DrawImage(sparkle, r.Width / 2f - 90, r.Height * .53f, 180, 126);
        DrawRainbowArc(g, new PointF(r.Width / 2f - 50, r.Height * .70f), 300, 100, 0);
        DrawCentered(g, "PRESS ENTER", _menuFont, r.Width / 2f, r.Height * .86f, Color.White);
    }

    private void DrawBanner(Graphics g, Rectangle r, string text, float timer)
    {
        float alpha = Math.Clamp(timer * 2f, 0, 1);
        RectangleF box = new(30, r.Height * .17f, r.Width - 60, 58);
        using Brush b = new SolidBrush(Color.FromArgb((int)(210 * alpha), 5, 5, 15)); g.FillRoundedRectangle(b, box, 12);
        using Pen p = new(Color.FromArgb((int)(230 * alpha), Color.Gold), 2); g.DrawRoundedRectangle(p, box, 12);
        using Brush tb = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.White));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, _smallFont, tb, box, sf);
    }

    private static (Color top, Color bottom, Color far, Color near, Color accent) Palette(Biome b) => b switch
    {
        Biome.Pasture => (Color.FromArgb(80, 205, 255), Color.FromArgb(200, 245, 255), Color.FromArgb(80, 180, 160), Color.FromArgb(50, 145, 90), Color.Gold),
        Biome.Orchard => (Color.FromArgb(110, 215, 255), Color.FromArgb(245, 210, 185), Color.FromArgb(110, 175, 145), Color.FromArgb(55, 125, 75), Color.Orange),
        Biome.Forest => (Color.FromArgb(68, 155, 205), Color.FromArgb(180, 225, 190), Color.FromArgb(40, 115, 90), Color.FromArgb(24, 82, 55), Color.Gold),
        Biome.Marsh => (Color.FromArgb(82, 110, 120), Color.FromArgb(160, 180, 115), Color.FromArgb(80, 93, 66), Color.FromArgb(58, 72, 50), Color.YellowGreen),
        Biome.Mountain => (Color.FromArgb(85, 180, 240), Color.FromArgb(210, 225, 245), Color.FromArgb(135, 145, 160), Color.FromArgb(90, 100, 125), Color.White),
        Biome.Coast => (Color.FromArgb(70, 205, 245), Color.FromArgb(245, 210, 155), Color.FromArgb(78, 155, 125), Color.FromArgb(45, 110, 92), Color.Gold),
        Biome.Haunted => (Color.FromArgb(50, 40, 90), Color.FromArgb(100, 75, 118), Color.FromArgb(55, 42, 75), Color.FromArgb(30, 28, 45), Color.MediumPurple),
        Biome.Crypt => (Color.FromArgb(24, 24, 48), Color.FromArgb(78, 56, 83), Color.FromArgb(60, 55, 68), Color.FromArgb(35, 32, 42), Color.Violet),
        Biome.Cave => (Color.FromArgb(18, 17, 28), Color.FromArgb(62, 65, 79), Color.FromArgb(50, 48, 65), Color.FromArgb(28, 27, 38), Color.Cyan),
        Biome.Ice => (Color.FromArgb(65, 120, 190), Color.FromArgb(220, 245, 255), Color.FromArgb(130, 175, 210), Color.FromArgb(90, 135, 175), Color.Cyan),
        Biome.Fortress => (Color.FromArgb(75, 35, 65), Color.FromArgb(145, 70, 70), Color.FromArgb(85, 65, 75), Color.FromArgb(50, 45, 55), Color.Gold),
        Biome.Weald => (Color.FromArgb(38, 70, 78), Color.FromArgb(90, 105, 85), Color.FromArgb(45, 72, 55), Color.FromArgb(28, 48, 38), Color.OrangeRed),
        Biome.Nightmare => (Color.FromArgb(12, 5, 26), Color.FromArgb(72, 14, 70), Color.FromArgb(45, 18, 55), Color.FromArgb(25, 8, 35), Color.HotPink),
        _ => (Color.FromArgb(24, 30, 68), Color.FromArgb(80, 50, 90), Color.FromArgb(45, 50, 85), Color.FromArgb(25, 30, 55), Color.MediumPurple)
    };


    private static void DrawHornJoustSwirl(Graphics g, PlayerActor p, float t, Vector2 root, Vector2 tip)
    {
        // ALPHA2.6: the bayonet tell is a *sparkle spiral*, not a beam.  The first
        // implementation drew two continuous sine ribbons plus a bright horn core;
        // at gameplay scale those lines merged into a tiny laser.  This version uses
        // only discrete orbiting motes/glints around the actual horn axis.
        Vector2 axis = tip - root;
        float length = axis.Length();
        if (length < 1f) return;
        Vector2 along = axis / length;
        Vector2 normal = new(-along.Y, along.X);

        int motes = p.Overdrive ? 13 : 10;
        float turns = p.Overdrive ? 3.0f : 2.15f;
        float spin = t * (p.Overdrive ? 17.0f : 10.5f);
        float maxRadius = p.Overdrive ? 6.5f : 4.8f;
        Color[] colors = p.Overdrive
            ? new[] { Color.White, Color.HotPink, Color.Cyan, Color.Gold }
            : new[] { Color.White, Color.Cyan, Color.Gold, Color.HotPink };

        for (int i = 0; i < motes; i++)
        {
            float u = i / (float)Math.Max(1, motes - 1);
            float hornU = .24f + .72f * u;
            float phase = spin + u * MathF.PI * 2f * turns;
            float radius = maxRadius * (.38f + .62f * u);
            float orbit = MathF.Sin(phase) * radius;
            Vector2 point = root + along * (length * hornU) + normal * orbit;

            // Cosine is the fake depth component: motes on the "front" of the
            // corkscrew are a little larger/brighter than those behind the horn.
            float depth = (MathF.Cos(phase) + 1f) * .5f;
            float size = (p.Overdrive ? 2.8f : 2.2f) + depth * (p.Overdrive ? 2.8f : 1.9f);
            Color c = colors[i % colors.Length];
            int alpha = (int)(105 + depth * 145);
            using Brush mote = new SolidBrush(Color.FromArgb(Math.Clamp(alpha, 0, 255), c));
            g.FillEllipse(mote, point.X - size / 2f, point.Y - size / 2f, size, size);

            if ((i + (int)(t * 12f)) % 4 == 0)
                DrawStar(g, point.X, point.Y, 2.2f + depth * 1.8f, Color.White, c);
        }
    }

    private static void DrawStars(Graphics g, Rectangle r, int seed, float t)
    {
        var rng = new DeterministicRng((ulong)(uint)seed);
        for (int i = 0; i < 95; i++)
        {
            float x = rng.NextFloat() * r.Width, y = rng.NextFloat() * r.Height * .70f;
            float s = 1 + rng.NextFloat() * 3 + MathF.Sin(t * (1 + (i % 4)) + i) * .7f;
            using Brush b = new SolidBrush(Color.FromArgb(120 + i % 120, 255, 255, 255)); g.FillEllipse(b, x, y, s, s);
        }
    }

    private static void DrawRetroGrid(Graphics g, Rectangle r, float t)
    {
        int horizon = (int)(r.Height * .66f);
        using Brush ground = new SolidBrush(Color.FromArgb(70, 8, 5, 30)); g.FillRectangle(ground, 0, horizon, r.Width, r.Height - horizon);
        using Pen p = new(Color.FromArgb(100, 255, 50, 190), 1);
        for (int y = horizon; y < r.Bottom; y += 26) g.DrawLine(p, 0, y, r.Width, y);
        float vanish = r.Width / 2f;
        for (int x = -r.Width; x <= r.Width * 2; x += 90) g.DrawLine(p, vanish, horizon, x + (t * 35) % 90, r.Bottom);
    }

    private static void DrawScanlines(Graphics g, Rectangle r)
    {
        using Pen p = new(Color.FromArgb(12, 0, 0, 0), 1);
        for (int y = 1; y < r.Height; y += 4) g.DrawLine(p, 0, y, r.Width, y);
    }

    private static void DrawRainbowArc(Graphics g, PointF origin, float length, float spread, float tilt)
    {
        Color[] c = { Color.Red, Color.Orange, Color.Gold, Color.LimeGreen, Color.DeepSkyBlue, Color.MediumPurple };
        GraphicsState st = g.Save(); g.TranslateTransform(origin.X, origin.Y); g.RotateTransform(tilt);
        for (int i = 0; i < c.Length; i++)
        {
            using Pen p = new(Color.FromArgb(190, c[i]), 11);
            g.DrawBezier(p, 0, i * 7, length * .35f, -spread + i * 8, length * .70f, spread + i * 5, length, i * 7);
        }
        g.Restore(st);
    }

    private static void DrawStar(Graphics g, float cx, float cy, float radius, Color fill, Color edge)
    {
        PointF[] pts = new PointF[10];
        for (int i = 0; i < 10; i++)
        {
            float a = -MathF.PI / 2 + i * MathF.PI / 5;
            float rr = i % 2 == 0 ? radius : radius * .42f;
            pts[i] = new(cx + MathF.Cos(a) * rr, cy + MathF.Sin(a) * rr);
        }
        using Brush b = new SolidBrush(fill); g.FillPolygon(b, pts);
        using Pen p = new(edge, Math.Max(1, radius * .11f)); g.DrawPolygon(p, pts);
    }

    private static void DrawFairy(Graphics g, float x, float y, float scale)
    {
        using Brush wing = new SolidBrush(Color.FromArgb(150, 150, 245, 255));
        g.FillEllipse(wing, x - 15 * scale, y - 10 * scale, 16 * scale, 12 * scale);
        g.FillEllipse(wing, x + 1 * scale, y - 10 * scale, 16 * scale, 12 * scale);
        using Brush body = new SolidBrush(Color.Gold); g.FillEllipse(body, x - 4 * scale, y - 8 * scale, 8 * scale, 16 * scale);
        DrawStar(g, x, y - 12 * scale, 5 * scale, Color.HotPink, Color.White);
    }

    private static void DrawHeart(Graphics g, float x, float y, Color c)
    {
        using GraphicsPath path = new();
        path.AddBezier(x, y + 8, x - 8, y - 2, x - 18, y + 6, x, y + 25);
        path.AddBezier(x, y + 25, x + 18, y + 6, x + 8, y - 2, x, y + 8);
        using Brush b = new SolidBrush(c); g.FillPath(b, path);
    }

    private static RectangleF FitRect(Size image, RectangleF bounds)
    {
        float s = Math.Min(bounds.Width / image.Width, bounds.Height / image.Height);
        float w = image.Width * s, h = image.Height * s;
        return new(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
    }

    private static void DrawCentered(Graphics g, string text, Font font, float x, float y, Color color)
    {
        using Brush b = new SolidBrush(color);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
        g.DrawString(text, font, b, new PointF(x, y), sf);
    }

    private static void DrawShadowText(Graphics g, string text, Font font, PointF p, StringAlignment align, Color fore, Color shadow, int depth)
    {
        using var sf = new StringFormat { Alignment = align, LineAlignment = StringAlignment.Near };
        using Brush sb = new SolidBrush(shadow), fb = new SolidBrush(fore);
        for (int i = depth; i >= 1; i--) g.DrawString(text, font, sb, new PointF(p.X + i, p.Y + i), sf);
        g.DrawString(text, font, fb, p, sf);
    }

    private static string BossName(EnemyKind kind) => kind switch
    {
        EnemyKind.Ed => "ED",
        EnemyKind.OldGnarley => "OLD GNARLEY",
        EnemyKind.Spookers => "SPOOKERS",
        EnemyKind.Webbey => "WEBBEY",
        EnemyKind.Mina => "MINA",
        EnemyKind.CountSpatula => "COUNT SPATULA",
        EnemyKind.Rip => "RIP 'THE PIECE MAKER'",
        EnemyKind.NightMare => "NIGHT MARE",
        _ => kind.ToString().ToUpperInvariant()
    };

    // Enemy caricatures intentionally use primitives so the revived game has a coherent 90s-cartoon look
    // without inventing external copyrighted sprite sheets.
    private static void DrawTeddy(Graphics g, float x, float y, float w, float h)
    {
        using Brush fur = new SolidBrush(Color.FromArgb(150, 92, 55)), dark = new SolidBrush(Color.FromArgb(75, 42, 28));
        g.FillEllipse(fur, x - w * .34f, y - h * .68f, w * .68f, h * .62f);
        g.FillEllipse(fur, x - w * .28f, y - h, w * .56f, h * .48f);
        g.FillEllipse(dark, x - w * .34f, y - h, w * .20f, h * .22f); g.FillEllipse(dark, x + w * .14f, y - h, w * .20f, h * .22f);
        DrawAngryEyes(g, x, y - h * .78f, w * .22f);
    }
    private static void DrawZombie(Graphics g, float x, float y, float w, float h)
    {
        using Brush body = new SolidBrush(Color.FromArgb(92, 130, 88)), shirt = new SolidBrush(Color.FromArgb(95, 70, 120));
        g.FillRectangle(shirt, x - w * .28f, y - h * .58f, w * .56f, h * .55f);
        g.FillEllipse(body, x - w * .27f, y - h, w * .54f, h * .48f);
        using Pen arm = new(body is SolidBrush sb ? sb.Color : Color.Green, 8); g.DrawLine(arm, x - w * .25f, y - h * .48f, x - w * .48f, y - h * .35f); g.DrawLine(arm, x + w * .25f, y - h * .48f, x + w * .48f, y - h * .35f);
        DrawAngryEyes(g, x, y - h * .78f, w * .20f);
    }
    private static void DrawDoll(Graphics g, float x, float y, float w, float h)
    {
        using Brush dress = new SolidBrush(Color.FromArgb(235, 105, 175)), skin = new SolidBrush(Color.FromArgb(245, 225, 210));
        g.FillPolygon(dress, new[] { new PointF(x, y - h * .62f), new PointF(x - w * .44f, y), new PointF(x + w * .44f, y) });
        g.FillEllipse(skin, x - w * .27f, y - h, w * .54f, h * .48f); DrawAngryEyes(g, x, y - h * .77f, w * .18f);
    }
    private static void DrawSquirrel(Graphics g, float x, float y, float w, float h)
    {
        using Brush fur = new SolidBrush(Color.FromArgb(105, 88, 75));
        g.FillEllipse(fur, x - w * .16f, y - h * .62f, w * .45f, h * .56f);
        g.FillEllipse(fur, x + w * .08f, y - h * .85f, w * .32f, h * .33f);
        g.FillEllipse(fur, x - w * .55f, y - h * .78f, w * .48f, h * .70f); DrawAngryEyes(g, x + w * .24f, y - h * .72f, w * .12f);
    }
    private static void DrawMouse(Graphics g, float x, float y, float w, float h)
    {
        using Brush b = new SolidBrush(Color.FromArgb(58, 58, 70)); g.FillEllipse(b, x - w * .42f, y - h * .62f, w * .78f, h * .58f); g.FillEllipse(b, x + w * .16f, y - h * .72f, w * .30f, h * .31f); DrawAngryEyes(g, x + w * .28f, y - h * .57f, w * .10f);
    }
    private static void DrawGhost(Graphics g, float x, float y, float w, float h, bool boss = false)
    {
        using Brush b = new SolidBrush(Color.FromArgb(boss ? 205 : 165, 225, 235, 255)); g.FillEllipse(b, x - w / 2, y - h, w, h * .72f); g.FillPolygon(b, new[] { new PointF(x - w / 2, y - h * .45f), new PointF(x - w * .28f, y), new PointF(x, y - h * .18f), new PointF(x + w * .28f, y), new PointF(x + w / 2, y - h * .45f) }); DrawAngryEyes(g, x, y - h * .62f, w * .24f);
    }
    private static void DrawTree(Graphics g, float x, float y, float w, float h, bool boss = false)
    {
        using Brush trunk = new SolidBrush(Color.FromArgb(95, 58, 38)), leaf = new SolidBrush(boss ? Color.FromArgb(62, 92, 50) : Color.FromArgb(55, 115, 58));
        g.FillRectangle(trunk, x - w * .18f, y - h * .62f, w * .36f, h * .62f); g.FillEllipse(leaf, x - w * .48f, y - h, w * .96f, h * .58f); DrawAngryEyes(g, x, y - h * .50f, w * .20f);
    }
    private static void DrawGoblin(Graphics g, float x, float y, float w, float h)
    {
        using Brush b = new SolidBrush(Color.FromArgb(72, 155, 76)); g.FillEllipse(b, x - w * .34f, y - h * .82f, w * .68f, h * .68f); g.FillPolygon(b, new[] { new PointF(x - w * .25f, y - h * .66f), new PointF(x - w * .62f, y - h * .55f), new PointF(x - w * .30f, y - h * .45f) }); g.FillPolygon(b, new[] { new PointF(x + w * .25f, y - h * .66f), new PointF(x + w * .62f, y - h * .55f), new PointF(x + w * .30f, y - h * .45f) }); DrawAngryEyes(g, x, y - h * .58f, w * .20f);
    }
    private static void DrawImp(Graphics g, float x, float y, float w, float h)
    {
        using Brush b = new SolidBrush(Color.FromArgb(180, 45, 65)); g.FillEllipse(b, x - w * .32f, y - h * .78f, w * .64f, h * .65f); g.FillPolygon(b, new[] { new PointF(x - w * .24f, y - h * .72f), new PointF(x - w * .14f, y - h), new PointF(x, y - h * .70f) }); g.FillPolygon(b, new[] { new PointF(x + w * .24f, y - h * .72f), new PointF(x + w * .14f, y - h), new PointF(x, y - h * .70f) }); DrawAngryEyes(g, x, y - h * .55f, w * .19f);
    }
    private static void DrawHunter(Graphics g, float x, float y, float w, float h)
    {
        using Brush armor = new SolidBrush(Color.FromArgb(90, 90, 105)), skin = new SolidBrush(Color.FromArgb(220, 180, 150)); g.FillRectangle(armor, x - w * .28f, y - h * .62f, w * .56f, h * .60f); g.FillEllipse(skin, x - w * .23f, y - h, w * .46f, h * .40f); using Pen spear = new(Color.Silver, 4); g.DrawLine(spear, x + w * .22f, y - h * .62f, x + w * .68f, y - h * .88f); DrawAngryEyes(g, x, y - h * .79f, w * .15f);
    }
    private static void DrawWerewolf(Graphics g, float x, float y, float w, float h, bool boss = false)
    {
        using Brush fur = new SolidBrush(boss ? Color.FromArgb(55, 45, 48) : Color.FromArgb(75, 68, 70)); g.FillEllipse(fur, x - w * .38f, y - h * .72f, w * .76f, h * .70f); g.FillEllipse(fur, x - w * .28f, y - h, w * .56f, h * .45f); g.FillPolygon(fur, new[] { new PointF(x - w * .22f, y - h * .86f), new PointF(x - w * .10f, y - h * 1.12f), new PointF(x, y - h * .86f) }); g.FillPolygon(fur, new[] { new PointF(x + w * .22f, y - h * .86f), new PointF(x + w * .10f, y - h * 1.12f), new PointF(x, y - h * .86f) }); DrawAngryEyes(g, x, y - h * .76f, w * .22f);
    }
    private static void DrawEd(Graphics g, float x, float y, float w, float h)
    {
        using Brush slime = new SolidBrush(Color.FromArgb(110, 220, 95)); g.FillEllipse(slime, x - w / 2, y - h, w, h); using Brush eye = new SolidBrush(Color.White); g.FillEllipse(eye, x - 25, y - h * .72f, 18, 18); g.FillEllipse(eye, x + 7, y - h * .70f, 18, 18); using Brush pupil = new SolidBrush(Color.Black); g.FillEllipse(pupil, x - 18, y - h * .66f, 7, 7); g.FillEllipse(pupil, x + 13, y - h * .64f, 7, 7); using Pen mouth = new(Color.FromArgb(70, 120, 50), 3); g.DrawArc(mouth, x - 20, y - h * .44f, 40, 20, 10, 160);
    }
    private static void DrawSpider(Graphics g, float x, float y, float w, float h)
    {
        using Pen leg = new(Color.FromArgb(60, 45, 60), 8); for (int i = -1; i <= 1; i += 2) for (int k = 0; k < 4; k++) { float yy = y - h * (.55f - k * .08f); g.DrawLine(leg, x + i * w * .18f, yy, x + i * w * (.55f + k * .05f), yy + (k - 1.5f) * 20); } using Brush body = new SolidBrush(Color.FromArgb(55, 42, 55)); g.FillEllipse(body, x - w * .34f, y - h * .78f, w * .68f, h * .66f); DrawAngryEyes(g, x, y - h * .52f, w * .20f);
    }
    private static void DrawBird(Graphics g, float x, float y, float w, float h)
    {
        using Brush b = new SolidBrush(Color.FromArgb(40, 45, 78)); g.FillEllipse(b, x - w * .22f, y - h * .65f, w * .44f, h * .50f); g.FillPolygon(b, new[] { new PointF(x - w * .15f, y - h * .55f), new PointF(x - w * .55f, y - h * .85f), new PointF(x - w * .30f, y - h * .30f) }); g.FillPolygon(b, new[] { new PointF(x + w * .15f, y - h * .55f), new PointF(x + w * .55f, y - h * .85f), new PointF(x + w * .30f, y - h * .30f) }); DrawAngryEyes(g, x, y - h * .56f, w * .13f);
    }
    private static void DrawVampire(Graphics g, float x, float y, float w, float h)
    {
        using Brush cape = new SolidBrush(Color.FromArgb(80, 10, 40)), face = new SolidBrush(Color.FromArgb(230, 225, 215)); g.FillPolygon(cape, new[] { new PointF(x, y - h * .78f), new PointF(x - w * .55f, y), new PointF(x + w * .55f, y) }); g.FillEllipse(face, x - w * .24f, y - h, w * .48f, h * .35f); DrawAngryEyes(g, x, y - h * .83f, w * .17f); using Pen spat = new(Color.Silver, 7); g.DrawLine(spat, x + w * .38f, y - h * .15f, x + w * .70f, y - h * .82f); g.DrawRectangle(spat, x + w * .61f, y - h * .95f, w * .22f, h * .20f);
    }
    private static void DrawNightMare(Graphics g, float x, float y, float w, float h)
    {
        using Brush body = new SolidBrush(Color.FromArgb(28, 18, 42)); g.FillEllipse(body, x - w * .40f, y - h * .62f, w * .72f, h * .50f); g.FillEllipse(body, x + w * .15f, y - h * .82f, w * .30f, h * .38f); using Pen leg = new(Color.FromArgb(28, 18, 42), 18); for (int i = -1; i <= 1; i += 2) { g.DrawLine(leg, x + i * w * .22f, y - h * .35f, x + i * w * .24f, y); g.DrawLine(leg, x + i * w * .05f, y - h * .35f, x + i * w * .04f, y); } using Pen horn = new(Color.Magenta, 8); g.DrawLine(horn, x + w * .32f, y - h * .78f, x + w * .63f, y - h * .98f); DrawAngryEyes(g, x + w * .27f, y - h * .68f, w * .10f);
    }
    private static void DrawAngryEyes(Graphics g, float x, float y, float span)
    {
        using Pen p = new(Color.Red, 4); g.DrawLine(p, x - span, y - 4, x - 4, y + 2); g.DrawLine(p, x + span, y - 4, x + 4, y + 2);
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using GraphicsPath path = Rounded(rect, radius); g.FillPath(brush, path);
    }
    public static void DrawRoundedRectangle(this Graphics g, Pen pen, RectangleF rect, float radius)
    {
        using GraphicsPath path = Rounded(rect, radius); g.DrawPath(pen, path);
    }
    private static GraphicsPath Rounded(RectangleF r, float d)
    {
        float dd = Math.Min(d * 2, Math.Min(r.Width, r.Height));
        GraphicsPath p = new();
        p.AddArc(r.X, r.Y, dd, dd, 180, 90); p.AddArc(r.Right - dd, r.Y, dd, dd, 270, 90); p.AddArc(r.Right - dd, r.Bottom - dd, dd, dd, 0, 90); p.AddArc(r.X, r.Bottom - dd, dd, dd, 90, 90); p.CloseFigure(); return p;
    }
}
