using System.Numerics;

namespace SparklesReborn;

public sealed class GameApp : IDisposable
{
    private readonly InputState _input;
    private readonly Action _quit;
    private readonly Action _toggleFullscreen;
    private readonly AssetStore _assets = new();
    private readonly SaveManager _save = new();
    private readonly SettingsManager _settings = new();
    private readonly RetroAudio _audio = new();
    private readonly GameRenderer _renderer;
    private readonly List<Particle> _particles = new();
    private readonly List<string> _recentCodes = new();

    private GameMode _mode = GameMode.Title;
    private Level? _level;
    private readonly PlayerActor _player = new();
    private int _mapIndex;
    private int _titleChoice;
    private int _pauseChoice;
    private int _optionsChoice;
    private int _secretChoice;
    private bool _waitingForBinding;
    private ControlAction _bindingAction;
    private GameMode _optionsReturnMode = GameMode.Title;
    private IReadOnlyList<StorySlide> _storySlides = Array.Empty<StorySlide>();
    private int _storyIndex;
    private GameMode _storyReturnMode = GameMode.Title;
    private Action? _storyCompletion;
    private float _mapMoveCooldown;
    private string _codeEntry = "";
    private string _banner = "";
    private float _bannerTimer;
    private float _cameraX, _cameraY, _shake, _boostSfxTimer;
    private float _levelClearTimer;
    private bool _levelClearTriggered;
    private long _runScore;
    private float _elapsed;

    internal GameMode Mode => _mode;
    internal Level? CurrentLevel => _level;
    internal PlayerActor Player => _player;
    internal IReadOnlyList<Particle> Particles => _particles;
    internal SaveData Save => _save.Data;
    internal int MapIndex => _mapIndex;
    internal int TitleChoice => _titleChoice;
    internal int PauseChoice => _pauseChoice;
    internal int OptionsChoice => _optionsChoice;
    internal int SecretChoice => _secretChoice;
    internal bool WaitingForBinding => _waitingForBinding;
    internal ControlAction BindingAction => _bindingAction;
    internal GameSettings Settings => _settings.Data;
    internal IReadOnlyList<StorySlide> StorySlides => _storySlides;
    internal int StoryIndex => _storyIndex;
    internal string CodeEntry => _codeEntry;
    internal string Banner => _banner;
    internal float BannerTimer => _bannerTimer;
    internal float CameraX => _cameraX;
    internal float CameraY => _cameraY;
    internal float Elapsed => _elapsed;
    internal float Shake => _settings.Data.ScreenShake ? _shake : 0f;
    internal IReadOnlyList<string> RecentCodes => _recentCodes;
    internal IReadOnlyList<string> DiscoveredCodes => _save.Data.DiscoveredCodes.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    internal InputState Input => _input;

    public GameApp(InputState input, Action quit, Action toggleFullscreen)
    {
        _input = input;
        _quit = quit;
        _toggleFullscreen = toggleFullscreen;
        _renderer = new GameRenderer(_assets);
        _mapIndex = Math.Clamp(_save.Data.CurrentLevel, 0, 27);
        _player.ArmorCapacity = _save.Data.ArmorCapacity;
        _settings.Data.EnsureBindings();
        _input.ApplyBindings(_settings.Data.KeyBindings);
        ApplyAudioSettings();
    }

    public void Update(float dt, Size clientSize)
    {
        _elapsed += dt;
        _bannerTimer = Math.Max(0, _bannerTimer - dt);
        _mapMoveCooldown = Math.Max(0, _mapMoveCooldown - dt);

        switch (_mode)
        {
            case GameMode.Title: UpdateTitle(); break;
            case GameMode.WorldMap: UpdateMap(dt); break;
            case GameMode.Playing: UpdatePlaying(dt, clientSize); break;
            case GameMode.Paused: UpdatePause(); break;
            case GameMode.CodeEntry: UpdateCodeEntry(); break;
            case GameMode.Credits:
                if (_input.CancelPressed || _input.ConfirmPressed) _mode = GameMode.Title;
                break;
            case GameMode.Options: UpdateOptions(); break;
            case GameMode.SecretBrowser: UpdateSecretBrowser(); break;
            case GameMode.Story: UpdateStory(); break;
            case GameMode.Victory:
                UpdateParticles(dt);
                if (_input.ConfirmPressed || _input.CancelPressed) { _mode = GameMode.WorldMap; _mapIndex = 27; }
                break;
        }
    }

    public void Render(Graphics g, Rectangle client) => _renderer.Render(this, g, client);

    private void UpdateTitle()
    {
        const int count = 7;
        if (_input.Pressed(Keys.Up) || _input.Pressed(Keys.W)) _titleChoice = (_titleChoice + count - 1) % count;
        if (_input.Pressed(Keys.Down) || _input.Pressed(Keys.S)) _titleChoice = (_titleChoice + 1) % count;
        if (!_input.ConfirmPressed) return;
        switch (_titleChoice)
        {
            case 0:
                _save.NewGame();
                _mapIndex = 0;
                _player.Score = 0; _player.Lives = 3;
                StartStory(StoryBook.Intro, GameMode.WorldMap, () =>
                {
                    _mapIndex = 0;
                    ShowBanner("A NEW NIGHTMARE BEGINS. WITH GLITTER.", 2.2f);
                });
                break;
            case 1:
                _mapIndex = Math.Clamp(_save.Data.CurrentLevel, 0, 27);
                _player.Score = _save.Data.HighScore > 0 ? Math.Min(_save.Data.HighScore / 10, 25000) : 0;
                _player.Lives = 3;
                _mode = GameMode.WorldMap;
                break;
            case 2:
                _secretChoice = 0;
                _mode = GameMode.SecretBrowser;
                break;
            case 3: BeginCodeEntry(GameMode.Title); break;
            case 4: BeginOptions(GameMode.Title); break;
            case 5: _mode = GameMode.Credits; break;
            case 6: _quit(); break;
        }
    }

    private void UpdateMap(float dt)
    {
        if (_input.CancelPressed) { _mode = GameMode.Title; return; }
        if (_input.Pressed(Keys.F2)) { BeginCodeEntry(GameMode.WorldMap); return; }
        if (_input.Pressed(Keys.F3)) { _secretChoice = 0; _mode = GameMode.SecretBrowser; return; }
        if (_input.Pressed(Keys.F9))
        {
            DeterminismReport report = DeterminismVerifier.RunCampaignRegression(3);
            ShowBanner(report.Failures == 0 ? $"DETERMINISM CHECK: {report.LevelsChecked} LEVELS CLEAN." : $"DETERMINISM CHECK: {report.Failures} PROBLEM(S). SEE DEBUGGER/ROADMAP.", 4f);
            return;
        }
        if (_input.Pressed(Keys.F11)) _toggleFullscreen();

        if (_mapMoveCooldown <= 0)
        {
            int dx = 0, dy = 0;
            float mx = _input.MoveX, my = _input.MoveY;
            if (Math.Abs(mx) > Math.Abs(my) && Math.Abs(mx) > .6f) dx = Math.Sign(mx);
            else if (Math.Abs(my) > .6f) dy = Math.Sign(my);
            if (dx != 0 || dy != 0)
            {
                int next = Campaign.Neighbor(_mapIndex, dx, dy);
                if (next >= 0)
                {
                    if (_save.Data.Unlocked[next]) _mapIndex = next;
                    else ShowBanner("THAT ROAD IS STILL EXTREMELY CLOSED.", 1.2f);
                }
                _mapMoveCooldown = .20f;
            }
        }

        if (_input.ConfirmPressed && _save.Data.Unlocked[_mapIndex])
            EnterCampaignLevel(_mapIndex);
    }

    private GameMode _codeReturnMode = GameMode.WorldMap;
    private void BeginCodeEntry(GameMode returnMode)
    {
        _codeReturnMode = returnMode;
        _codeEntry = "";
        _mode = GameMode.CodeEntry;
    }

    private void UpdateCodeEntry()
    {
        char? c;
        while ((c = _input.ReadChar()) is not null)
        {
            char ch = c.Value;
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
            {
                if (_codeEntry.Length < 18) _codeEntry += char.ToUpperInvariant(ch);
            }
        }
        if (_input.Pressed(Keys.Back) && _codeEntry.Length > 0) _codeEntry = _codeEntry[..^1];
        if (_input.CancelPressed) { _mode = _codeReturnMode; return; }
        if (_input.Pressed(Keys.Enter) || (_input.ConfirmPressed && _codeEntry.Length > 0))
        {
            string code = LevelGenerator.NormalizeCode(_codeEntry);
            if (code.Length < 2) { ShowBanner("THE SECRET CODE NEEDS MORE SECRET IN IT.", 1.5f); return; }
            EnterSecretLevel(code);
        }
    }

    private void UpdatePause()
    {
        const int count = 6;
        if (_input.PausePressed && !_input.Pressed(Keys.Down) && !_input.Pressed(Keys.Up)) { _mode = GameMode.Playing; return; }
        if (_input.Pressed(Keys.Up) || _input.Pressed(Keys.W)) _pauseChoice = (_pauseChoice + count - 1) % count;
        if (_input.Pressed(Keys.Down) || _input.Pressed(Keys.S)) _pauseChoice = (_pauseChoice + 1) % count;
        if (!_input.ConfirmPressed) return;
        switch (_pauseChoice)
        {
            case 0: _mode = GameMode.Playing; break;
            case 1: RestartLevel(); break;
            case 2: BeginOptions(GameMode.Paused); break;
            case 3: _toggleFullscreen(); break;
            case 4: _mode = GameMode.WorldMap; _level = null; break;
            case 5: _mode = GameMode.Title; _level = null; break;
        }
    }

    private void BeginOptions(GameMode returnMode)
    {
        _optionsReturnMode = returnMode;
        _optionsChoice = 0;
        _waitingForBinding = false;
        _mode = GameMode.Options;
    }

    private static readonly ControlAction[] OptionActions = Enum.GetValues<ControlAction>();
    internal int OptionsItemCount => 7 + OptionActions.Length;
    internal string OptionsItemText(int index)
    {
        if (index == 0) return $"MUSIC: {(_settings.Data.MusicEnabled ? "ON" : "OFF")}";
        if (index == 1) return $"MASTER VOLUME: {(int)(_settings.Data.MasterVolume * 100):000}%";
        if (index == 2) return $"SCREEN SHAKE: {(_settings.Data.ScreenShake ? "ON" : "OFF")}";
        if (index == 3) return $"REDUCE FLASHES: {(_settings.Data.ReduceFlashes ? "ON" : "OFF")}";
        if (index == 4) return $"SHOW SEED/FINGERPRINT: {(_settings.Data.ShowSeed ? "ON" : "OFF")}";
        if (index == 5) return "RESET KEYBOARD CONTROLS";
        if (index >= 6 && index < 6 + OptionActions.Length)
        {
            ControlAction a = OptionActions[index - 6];
            return $"{a.ToString().ToUpperInvariant()}: {_input.BindingLabel(a)}";
        }
        return "BACK";
    }

    private void UpdateOptions()
    {
        int count = OptionsItemCount;
        if (_waitingForBinding)
        {
            if (_input.Pressed(Keys.Escape)) { _waitingForBinding = false; return; }
            Keys? key = _input.FirstPressedKey();
            if (key is not null && key != Keys.Enter)
            {
                _input.Rebind(_bindingAction, key.Value);
                _settings.Data.KeyBindings[_bindingAction] = key.Value;
                _settings.Save();
                _waitingForBinding = false;
                ShowBanner($"{_bindingAction.ToString().ToUpperInvariant()} = {_input.BindingLabel(_bindingAction)}", 1.2f);
            }
            return;
        }

        if (_input.CancelPressed) { ApplyAudioSettings(); _settings.Save(); _mode = _optionsReturnMode; return; }
        if (_input.Pressed(Keys.Up) || _input.Pressed(Keys.W)) _optionsChoice = (_optionsChoice + count - 1) % count;
        if (_input.Pressed(Keys.Down) || _input.Pressed(Keys.S)) _optionsChoice = (_optionsChoice + 1) % count;

        bool left = _input.Pressed(Keys.Left) || _input.Pressed(Keys.A);
        bool right = _input.Pressed(Keys.Right) || _input.Pressed(Keys.D);
        if (_optionsChoice == 1 && (left || right))
        {
            _settings.Data.MasterVolume = Math.Clamp(_settings.Data.MasterVolume + (right ? .05f : -.05f), 0f, 1f);
            ApplyAudioSettings(); _settings.Save();
        }

        if (!_input.ConfirmPressed) return;
        switch (_optionsChoice)
        {
            case 0: _settings.Data.MusicEnabled = !_settings.Data.MusicEnabled; break;
            case 1: break;
            case 2: _settings.Data.ScreenShake = !_settings.Data.ScreenShake; break;
            case 3: _settings.Data.ReduceFlashes = !_settings.Data.ReduceFlashes; break;
            case 4: _settings.Data.ShowSeed = !_settings.Data.ShowSeed; break;
            case 5:
                _settings.ResetControls();
                _input.ApplyBindings(_settings.Data.KeyBindings);
                ShowBanner("KEYBOARD CONTROLS RETURNED TO FACTORY NONSENSE.", 1.5f);
                return;
            default:
                if (_optionsChoice >= 6 && _optionsChoice < 6 + OptionActions.Length)
                {
                    _bindingAction = OptionActions[_optionsChoice - 6];
                    _waitingForBinding = true;
                    return;
                }
                _settings.Save();
                ApplyAudioSettings();
                _mode = _optionsReturnMode;
                return;
        }
        _settings.Save();
        ApplyAudioSettings();
    }

    private void ApplyAudioSettings()
    {
        _audio.MusicEnabled = _settings.Data.MusicEnabled;
        // RetroAudio already has intentionally conservative per-voice gains. Treat this as user-facing master trim.
        _audio.MasterVolume = _settings.Data.MasterVolume * .55f;
    }

    private void UpdateSecretBrowser()
    {
        string[] codes = _save.Data.DiscoveredCodes.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        int count = Math.Max(1, codes.Length + 1); // +1 ENTER NEW CODE
        if (_input.CancelPressed) { _mode = GameMode.Title; return; }
        if (_input.Pressed(Keys.Up) || _input.Pressed(Keys.W)) _secretChoice = (_secretChoice + count - 1) % count;
        if (_input.Pressed(Keys.Down) || _input.Pressed(Keys.S)) _secretChoice = (_secretChoice + 1) % count;
        _secretChoice = Math.Clamp(_secretChoice, 0, count - 1);
        if (!_input.ConfirmPressed) return;
        if (_secretChoice < codes.Length) EnterSecretLevel(codes[_secretChoice]);
        else BeginCodeEntry(GameMode.SecretBrowser);
    }

    private void StartStory(IReadOnlyList<StorySlide> slides, GameMode returnMode, Action? completion = null)
    {
        _storySlides = slides;
        _storyIndex = 0;
        _storyReturnMode = returnMode;
        _storyCompletion = completion;
        _mode = slides.Count == 0 ? returnMode : GameMode.Story;
        if (slides.Count == 0) completion?.Invoke();
    }

    private void UpdateStory()
    {
        if (_input.CancelPressed) _storyIndex = _storySlides.Count - 1;
        if (!_input.ConfirmPressed && !_input.CancelPressed) return;
        _storyIndex++;
        if (_storyIndex < _storySlides.Count) return;
        Action? done = _storyCompletion;
        _storyCompletion = null;
        _mode = _storyReturnMode;
        done?.Invoke();
    }

    private void EnterCampaignLevel(int index)
    {
        CampaignLevel desc = Campaign.Levels[index];
        string storyId = desc.Boss is null ? "" : $"BOSS-{desc.Boss}";
        if (desc.Boss is not null && !_save.Data.SeenStoryIds.Contains(storyId))
        {
            _save.Data.SeenStoryIds.Add(storyId);
            _save.Save();
            StartStory(StoryBook.BossIntro(desc.Boss.Value), GameMode.WorldMap, () => EnterCampaignLevelNow(index));
            return;
        }
        EnterCampaignLevelNow(index);
    }

    private void EnterCampaignLevelNow(int index)
    {
        CampaignLevel desc = Campaign.Levels[index];
        _level = LevelGenerator.Generate(desc);
        _level.Secret = false;
        StartLevel();
    }

    private void EnterSecretLevel(string code)
    {
        _level = LevelGenerator.GenerateSecret(code);
        if (!_save.Data.DiscoveredCodes.Contains(code))
        {
            _save.Data.DiscoveredCodes.Add(code);
            _save.Save();
        }
        StartLevel();
        ShowBanner("SECRET LEVEL: " + _level.Name, 2f);
    }

    private void StartLevel()
    {
        if (_level is null) return;
        if (_player.Lives <= 0) _player.Lives = 3;
        _player.ResetForLevel(260, 552, _save.Data.ArmorCapacity);
        _player.Score = _runScore;
        _cameraX = _cameraY = 0;
        _particles.Clear();
        _levelClearTimer = 0;
        _levelClearTriggered = false;
        _pauseChoice = 0;
        _mode = GameMode.Playing;
        _audio.SetBiome(_level.Biome);
    }

    private void RestartLevel()
    {
        if (_level is null) return;
        if (_level.Secret && _level.SecretCode is not null) _level = LevelGenerator.GenerateSecret(_level.SecretCode);
        else if (_level.CampaignIndex >= 0) _level = LevelGenerator.Generate(Campaign.Levels[_level.CampaignIndex]);
        StartLevel();
    }

    private void UpdatePlaying(float dt, Size clientSize)
    {
        if (_level is null) { _mode = GameMode.WorldMap; return; }
        if (_input.PausePressed) { _pauseChoice = 0; _mode = GameMode.Paused; return; }

        if (_levelClearTriggered)
        {
            _levelClearTimer -= dt;
            UpdateParticles(dt);
            UpdateCamera(dt, clientSize);
            if (_levelClearTimer <= 0) FinishLevelTransition();
            return;
        }

        UpdateDynamicPlatforms(dt);
        UpdateHazards(dt);

        bool wasGrounded = _player.Grounded;
        if (_input.JumpPressed && wasGrounded) _audio.SfxJump();
        if (_input.JoustPressed) _audio.SfxJoust();
        if (_input.StompPressed && !wasGrounded) _audio.SfxStomp();

        _player.Update(dt, _input, _level, Emit);
        ResolveHazards();
        _audio.SetIntensity(_player.Overdrive ? Math.Clamp(_player.Speed / 1080f, 0f, 1f) : 0f);

        if (_player.Boosting)
        {
            _boostSfxTimer -= dt;
            if (_boostSfxTimer <= 0) { _audio.SfxBoost(); _boostSfxTimer = _player.Overdrive ? .13f : .24f; }
        }
        else _boostSfxTimer = 0;

        if (_input.BelchPressed) FireFairy();

        UpdateCheckpoints();
        UpdatePickups(dt);
        UpdateEnemies(dt);
        UpdateProjectiles(dt);
        ResolveCombat();
        UpdateParticles(dt);
        UpdateCamera(dt, clientSize);

        if (_player.Dead && _player.Lives <= 0 && _player.DeathTimer <= 0)
        {
            _player.Lives = 3;
            _mode = GameMode.WorldMap;
            ShowBanner("SPARKLES HAS TEMPORARILY EXHAUSTED THE HORSE SUPPLY.", 2.4f);
            return;
        }

        if (_player.Bounds.IntersectsWith(_level.Exit) && _level.BossDefeated)
            TriggerLevelClear();
    }

    private void UpdateDynamicPlatforms(float dt)
    {
        if (_level is null) return;
        foreach (Platform p in _level.Platforms)
        {
            p.LastDelta = Vector2.Zero;
            if (p.Kind == PlatformKind.Moving)
            {
                Vector2 old = new(p.Rect.X, p.Rect.Y);
                Vector2 axis = p.MotionAxis.LengthSquared() < .001f ? Vector2.UnitY : Vector2.Normalize(p.MotionAxis);
                Vector2 next = p.MotionOrigin + axis * (MathF.Sin(_elapsed * p.MotionSpeed + p.MotionPhase) * p.MotionAmplitude);
                p.Rect = new RectangleF(next.X, next.Y, p.Rect.Width, p.Rect.Height);
                p.LastDelta = next - old;
            }
            else if (p.Kind == PlatformKind.Crumbling)
            {
                if (p.Destroyed)
                {
                    p.CrumbleTimer += dt;
                    if (p.CrumbleTimer >= 0)
                    {
                        p.Destroyed = false;
                        p.CrumbleTimer = 0;
                        p.Triggered = false;
                    }
                }
                else if (p.Triggered)
                {
                    p.CrumbleTimer += dt;
                    if (p.CrumbleTimer >= p.CrumbleDelay)
                    {
                        p.Destroyed = true;
                        p.Triggered = false;
                        p.CrumbleTimer = -2.5f;
                        Burst(new Vector2(p.Rect.Left + p.Rect.Width / 2, p.Rect.Top), Color.SandyBrown, 12);
                    }
                }
            }
        }
    }

    private void UpdateHazards(float dt)
    {
        if (_level is null) return;
        foreach (Hazard h in _level.Hazards)
        {
            switch (h.Kind)
            {
                case HazardKind.FireJet:
                    h.Active = MathF.Sin(_elapsed * 3.1f + h.Phase) > -.15f;
                    break;
                case HazardKind.Crusher:
                case HazardKind.SawBlade:
                    if (h.Amplitude > 0)
                    {
                        Vector2 axis = h.Axis.LengthSquared() < .001f ? Vector2.UnitY : Vector2.Normalize(h.Axis);
                        Vector2 next = h.Origin + axis * (MathF.Sin(_elapsed * Math.Max(.1f, h.Speed) + h.Phase) * h.Amplitude);
                        h.Rect = new RectangleF(next.X, next.Y, h.Rect.Width, h.Rect.Height);
                    }
                    h.Active = true;
                    break;
                case HazardKind.NightmareStatic:
                    h.Active = MathF.Sin(_elapsed * 7.7f + h.Phase) > -.45f;
                    break;
                default:
                    h.Active = true;
                    break;
            }
        }
    }

    private void ResolveHazards()
    {
        if (_level is null || _player.Dead) return;
        foreach (Hazard h in _level.Hazards)
        {
            if (!h.Active || !h.Rect.IntersectsWith(_player.Bounds)) continue;
            if (h.InstantKill)
            {
                _player.Kill();
                _audio.SfxHit();
                _shake = Math.Max(_shake, 13);
                return;
            }
            _player.TakeDamage(Math.Max(1, h.Damage), h.Rect.Left + h.Rect.Width / 2);
            _audio.SfxHit();
            _shake = Math.Max(_shake, 7);
        }
    }

    private void UpdateCheckpoints()
    {
        if (_level is null) return;
        foreach (var cp in _level.Checkpoints)
        {
            if (!cp.Activated && _player.Pos.X >= cp.X)
            {
                cp.Activated = true;
                float gy = FindGroundY(cp.X, _player.Pos.Y + 120);
                if (!float.IsNaN(gy)) _player.ActivateCheckpoint(cp.X + 40, gy);
                ShowBanner("CHECKPOINT — ALL PARTS STILL MOSTLY ATTACHED", 1.2f);
            }
        }
    }

    private void UpdatePickups(float dt)
    {
        if (_level is null) return;
        foreach (Pickup p in _level.Pickups)
        {
            if (p.Collected) continue;
            p.BobPhase += dt * 3.2f;
            Vector2 to = _player.Pos + new Vector2(0, -48) - p.Pos;
            float dist = to.Length();
            if (p.Kind == PickupKind.Pixie && _player.Boosting && _player.Speed > 420 && dist < 310)
            {
                float force = 360 + (_player.Speed - 420) * .65f;
                if (dist > 1) p.Pos += Vector2.Normalize(to) * force * dt;
                dist = Vector2.Distance(_player.Pos + new Vector2(0, -48), p.Pos);
            }
            if (dist > 58) continue;

            p.Collected = true;
            switch (p.Kind)
            {
                case PickupKind.Sparkly:
                    _player.Score += 100; _audio.SfxPickup(); Burst(p.Pos, Color.Cyan, 10); break;
                case PickupKind.RainbowDrop:
                    _player.Score += 250; _player.BoostFuel = Math.Min(7.5f, _player.BoostFuel + .2f); _audio.SfxPickup(); Burst(p.Pos, Color.HotPink, 12); break;
                case PickupKind.Pixie:
                    _player.Score += 175; _player.Pixies++; if (_player.Boosting) _player.PixiesThisBoost++; _audio.SfxPixie(_player.PixiesThisBoost); Burst(p.Pos, Color.Gold, 8);
                    if (_player.Boosting && _player.PixiesThisBoost >= 30)
                    {
                        _player.TriggerFairyExplosion(Emit);
                        _audio.SfxExplosion();
                        _shake = 28;
                        foreach (Enemy e in _level.Enemies.Where(e => !e.Dead && Vector2.Distance(e.Pos, _player.Pos) < 950)) DamageEnemy(e, e.Boss ? 5 : 99, _player.Facing * 900);
                        ShowBanner("FAIRY CRITICAL MASS: SCIENCE HAS LEFT THE BUILDING", 2.2f);
                    }
                    break;
                case PickupKind.BoostExtender:
                    _player.AddBoostExtender(); _player.Score += 300; _audio.SfxPickup(); Burst(p.Pos, Color.Lime, 14); break;
                case PickupKind.FairyCake:
                    _player.FairyCakes++; _player.Score += 200; _audio.SfxPickup(); Burst(p.Pos, Color.Violet, 10); break;
                case PickupKind.Armor:
                    _save.Data.ArmorCapacity++; _save.Save(); _player.ArmorCapacity = _player.Armor = _save.Data.ArmorCapacity; _audio.SfxSecret(); ShowBanner("SPARKLE ARMOR +1. LOOKS BADASS.", 1.8f); break;
                case PickupKind.SecretCode:
                    if (!string.IsNullOrWhiteSpace(p.Code))
                    {
                        _save.Data.DiscoveredCodes.Add(p.Code); _save.Save();
                        _recentCodes.Remove(p.Code); _recentCodes.Insert(0, p.Code); if (_recentCodes.Count > 5) _recentCodes.RemoveAt(5);
                        _audio.SfxSecret(); ShowBanner("SECRET LEVEL CODE: " + p.Code + "   WRITE THAT DOWN.", 4f);
                    }
                    break;
                case PickupKind.OneUp:
                    _player.Lives++; _player.Score += 1000; _audio.SfxSecret();
                    ShowBanner("ONE EXTRA UNICORN HAS BEEN ALLOCATED.", 1.4f); break;
                case PickupKind.SparklePower:
                    _player.SparklePower = Math.Min(100, _player.SparklePower + 20); _player.Score += 150;
                    _audio.SfxPickup(); Burst(p.Pos, Color.Cyan, 10); break;
            }
        }
    }

    private void FireFairy()
    {
        if (_level is null) return;
        if (_player.FairyCakes <= 0 && _player.Pixies <= 0) { ShowBanner("FAIRY BELCH: AMMUNITION IS CURRENTLY NOT IN STOMACH.", 1f); return; }
        if (_player.FairyCakes > 0) _player.FairyCakes--; else _player.Pixies--;
        _level.Projectiles.Add(new Projectile
        {
            Pos = _player.Pos + new Vector2(_player.Facing * 45, -58),
            Vel = new Vector2(_player.Facing * 1150, -35),
            Kind = ProjectileKind.Fairy,
            Friendly = true,
            Life = 2.4f,
            Radius = 14,
            Damage = 3
        });
        _audio.SfxPixie();
    }

    private void UpdateEnemies(float dt)
    {
        if (_level is null) return;
        // Summoners and splitters are allowed to append children. Snapshot the count so new children begin next tick.
        int countAtStart = _level.Enemies.Count;
        for (int i = 0; i < countAtStart; i++)
        {
            Enemy e = _level.Enemies[i];
            if (e.Dead) continue;
            e.Timer += dt;
            e.HitCooldown = Math.Max(0, e.HitCooldown - dt);

            // ALPHA2.2: a minion hit by Rainbow Ramjet without the horn is thrown,
            // not killed. While airborne it temporarily stops running its family AI
            // so ground walkers, hoverers and scripted enemies cannot immediately
            // snap back onto their usual rails.
            if (e.RamjetLaunched && !e.Boss)
            {
                UpdateRamjetLaunchedEnemy(e, dt);
                continue;
            }

            if (e.Boss) UpdateBoss(e, dt);
            else UpdateRegularEnemy(e, dt);
        }
    }

    private void UpdateRamjetLaunchedEnemy(Enemy e, float dt)
    {
        if (_level is null) return;

        e.RamjetStunTimer = Math.Max(0, e.RamjetStunTimer - dt);
        e.Vel.Y += 2050f * dt;
        e.Pos += e.Vel * dt;

        // Air drag keeps a punted enemy dramatic without turning it into a railgun
        // projectile capable of leaving the entire level forever.
        e.Vel.X *= MathF.Pow(.9875f, dt * 120f);

        float ground = FindGroundY(e.Pos.X, e.Pos.Y + 150);
        if (!float.IsNaN(ground) && e.Pos.Y >= ground && e.Vel.Y >= 0)
        {
            e.Pos.Y = ground;
            e.Vel.Y = 0;
            e.Vel.X *= .28f;

            if (e.RamjetStunTimer <= 0)
            {
                e.RamjetLaunched = false;
                e.Vel = Vector2.Zero;

                // Several enemy families use Spawn.Y as their local home rail. After
                // being physically punted somewhere else, adopt the landing point so
                // recovery does not visually teleport them back to the old altitude.
                e.Spawn = e.Pos;
            }
        }

        // Falling out of the stage after being punted is still a valid environmental
        // kill. The dash itself did not award a clean hit; gravity/the pit did.
        if (e.Pos.Y > _level.DeathY + 300)
            DamageEnemy(e, 99, 0);
    }

    private void UpdateRegularEnemy(Enemy e, float dt)
    {
        if (_level is null) return;
        EnemySpec spec = EnemyCatalog.Get(e.Kind);
        float dx = _player.Pos.X - e.Pos.X;
        float d = Vector2.Distance(e.Pos, _player.Pos);

        switch (e.Kind)
        {
            case EnemyKind.Ghost:
                HoverToward(e, dt, 42, 95, 62);
                break;
            case EnemyKind.Imp:
            case EnemyKind.BeachImp:
                HoverToward(e, dt, spec.Speed, 100, 72);
                TryShoot(e, spec.Projectile, 390 + _level.Difficulty * 100, spec.AttackPeriod, 880);
                break;
            case EnemyKind.CorporateSafetyFairy:
                HoverToward(e, dt, 82, 135, 48);
                TryShoot(e, ProjectileKind.Citation, 360, spec.AttackPeriod, 950);
                break;
            case EnemyKind.EvilTree:
                TryShoot(e, ProjectileKind.Acorn, 370 + _level.Difficulty * 130, spec.AttackPeriod, 900);
                break;
            case EnemyKind.PorcelainDoll:
                // Weeping-doll rule: it advances only when Sparkles is facing away from it.
                bool watched = Math.Sign(e.Pos.X - _player.Pos.X) == _player.Facing && Math.Abs(dx) < 620;
                if (!watched) WalkGrounded(e, dt, 92); else e.State = 1;
                break;
            case EnemyKind.CupcakeMimic:
                if (e.State == 0 && d < 240) { e.State = 1; e.Timer = 0; }
                if (e.State != 0) WalkGrounded(e, dt, 175);
                break;
            case EnemyKind.ChainmailChicken:
                WalkGrounded(e, dt, 128);
                break;
            case EnemyKind.GnomeLawnCommando:
                WalkGrounded(e, dt, 54);
                TryShoot(e, ProjectileKind.Spit, 430, 1.85f, 780);
                break;
            case EnemyKind.BeehiveKnight:
                WalkGrounded(e, dt, 58);
                TryShoot(e, ProjectileKind.Spit, 330, 2.25f, 700);
                break;
            case EnemyKind.MushroomBouncer:
                // Deliberately stationary: stomping it is both combat and traversal.
                break;
            case EnemyKind.VineSnatcher:
                if (e.State == 0)
                {
                    e.Pos.Y = e.Spawn.Y - 190 + MathF.Sin(_elapsed * 2f + e.Spawn.X * .01f) * 32;
                    if (Math.Abs(dx) < 260) { e.State = 1; e.Vel.Y = 520; }
                }
                else
                {
                    e.Pos.Y += e.Vel.Y * dt;
                    if (e.Pos.Y >= e.Spawn.Y - 20) { e.State = 0; e.Vel.Y = 0; }
                }
                break;
            case EnemyKind.SquirrelUnionSteward:
                WalkGrounded(e, dt, 76);
                if (e.Timer > 3.2f && d < 900)
                {
                    e.Timer = 0;
                    SummonNear(e, EnemyKind.RabidSquirrel, 2, 95);
                    Shoot(e, ProjectileKind.Acorn, 390);
                }
                break;
            case EnemyKind.BogGoblin:
                if (e.Timer > 2.8f)
                {
                    e.Timer = 0;
                    e.State = 1 - e.State;
                    e.Invulnerable = e.State == 1;
                }
                e.Pos.Y = e.Spawn.Y + (e.State == 1 ? 34 : 0);
                if (e.State == 0) WalkGrounded(e, dt, 84);
                break;
            case EnemyKind.SlimeIntern:
                WalkGrounded(e, dt, 54);
                break;
            case EnemyKind.PossessedRubberBoot:
                HopEnemy(e, dt, 140, 520, 1.15f);
                break;
            case EnemyKind.CrabKnight:
                WalkGrounded(e, dt, 82);
                break;
            case EnemyKind.DiscoSkeleton:
                WalkGrounded(e, dt, 72 + MathF.Sin(_elapsed * 7) * 24);
                TryShoot(e, ProjectileKind.Bone, 470, 1.35f, 820);
                break;
            case EnemyKind.HauntedTrapperKeeper:
                e.Pos.Y = e.Spawn.Y - 115 + MathF.Sin(_elapsed * 2.8f + e.Spawn.X * .02f) * 44;
                TryShoot(e, ProjectileKind.Stationery, 520, 1.55f, 980);
                break;
            case EnemyKind.SockPuppetNecromancer:
                WalkGrounded(e, dt, 46);
                if (e.Timer > 3.1f && d < 1000)
                {
                    e.Timer = 0;
                    SummonNear(e, EnemyKind.DireMouse, 2, 110);
                    Shoot(e, ProjectileKind.DarkBolt, 410);
                }
                break;
            case EnemyKind.VampireBatling:
                DiveBomber(e, dt, 185, 185, 1.4f);
                break;
            case EnemyKind.CaveSpiderling:
                WalkGrounded(e, dt, 132);
                break;
            case EnemyKind.StalactiteGremlin:
                DiveBomber(e, dt, 0, 240, 1.8f);
                break;
            case EnemyKind.SkiGoblin:
                WalkGrounded(e, dt, 250);
                break;
            case EnemyKind.AngrySnowman:
                TryShoot(e, ProjectileKind.Snowball, 430, 1.8f, 950);
                break;
            case EnemyKind.FrostWerepup:
                WalkGrounded(e, dt, d < 600 ? 245 : 105);
                break;
            case EnemyKind.ClockworkHunter:
                WalkGrounded(e, dt, 90);
                TryShoot(e, ProjectileKind.Spark, 560, 1.7f, 980);
                break;
            case EnemyKind.GargoyleIntern:
                DiveBomber(e, dt, 105, 290, 1.65f);
                break;
            case EnemyKind.CursedKnight:
                WalkGrounded(e, dt, 78);
                break;
            case EnemyKind.NightmareColt:
                WalkGrounded(e, dt, Math.Clamp(150 + _player.Speed * .35f, 170, 360));
                if (_player.Boosting && d < 900) e.Direction = Math.Sign(dx);
                break;
            case EnemyKind.PossessedToaster:
                WalkGrounded(e, dt, 72);
                TryShoot(e, ProjectileKind.Toast, 520, 1.7f, 900);
                break;
            case EnemyKind.MallNinjaWizard:
                if (e.Timer > 2.35f && d < 1100)
                {
                    e.Timer = 0;
                    float tx = Math.Clamp(_player.Pos.X + (dx > 0 ? -420 : 420), 160, _level.Width - 160);
                    e.Pos.X = tx;
                    float gy = FindGroundY(tx, _player.Pos.Y + 400); if (!float.IsNaN(gy)) e.Pos.Y = gy;
                    ShootSpread(e, ProjectileKind.DarkBolt, 470);
                }
                break;
            case EnemyKind.RollerbladeOrc:
                WalkGrounded(e, dt, 295);
                break;
            case EnemyKind.BoomBoxMimic:
                if (e.State == 0 && d < 330) e.State = 1;
                if (e.State != 0) WalkGrounded(e, dt, 145 + MathF.Sin(_elapsed * 6) * 35);
                break;
            case EnemyKind.MoonCheeseHomunculus:
                HopEnemy(e, dt, 110, 610, 1.45f);
                TryShoot(e, ProjectileKind.Spit, 390, 2.3f, 780);
                break;
            default:
                WalkGrounded(e, dt, spec.Speed);
                if ((spec.Capabilities & EnemyCapability.Ranged) != 0)
                    TryShoot(e, spec.Projectile, 420, spec.AttackPeriod, 900);
                break;
        }
    }

    private void UpdateBoss(Enemy e, float dt)
    {
        if (_level is null) return;
        float dx = _player.Pos.X - e.Pos.X;
        float hp = e.MaxHealth <= 0 ? 1 : e.Health / (float)e.MaxHealth;
        int desiredPhase = hp <= .33f ? 2 : hp <= .66f ? 1 : 0;
        if (desiredPhase != e.Phase)
        {
            e.Phase = desiredPhase;
            e.Timer = 0;
            e.State = 0;
            _audio.SfxBoss();
            _shake = Math.Max(_shake, 12);
            Burst(new Vector2(e.Pos.X, e.Pos.Y - e.Height * .55f), Color.Gold, 26);
            ShowBanner(BossPhaseLine(e.Kind, e.Phase), 1.8f);
        }
        float aggression = 1f + e.Phase * .28f;

        switch (e.Kind)
        {
            case EnemyKind.Ed:
                e.Vel.Y += 1800 * dt;
                if (Math.Abs(e.Vel.Y) < 1 && e.Timer > 1.65f / aggression)
                {
                    e.Timer = 0; e.Vel.Y = -620 - e.Phase * 90; e.Vel.X = Math.Sign(dx) * (180 + e.Phase * 70);
                    if (e.Phase >= 1) Shoot(e, ProjectileKind.Spit, 340 + e.Phase * 70);
                }
                BossGroundPhysics(e, dt);
                break;
            case EnemyKind.OldGnarley:
                if (e.Timer > 1.4f / aggression)
                {
                    e.Timer = 0; Shoot(e, ProjectileKind.Acorn, 490 + e.Phase * 45); ShootSpread(e, ProjectileKind.Acorn, 390 + e.Phase * 35);
                    if (e.Phase == 2 && _level.Enemies.Count(x => !x.Dead && x.Kind == EnemyKind.VineSnatcher) < 3)
                        SummonNear(e, EnemyKind.VineSnatcher, 1, 260);
                }
                break;
            case EnemyKind.Spookers:
                e.Pos.X = e.Spawn.X + MathF.Sin(_elapsed * (.9f + e.Phase * .18f)) * (480 + e.Phase * 80);
                e.Pos.Y = e.Spawn.Y - 190 + MathF.Sin(_elapsed * (2.1f + e.Phase * .3f)) * 105;
                if (e.Timer > 1.05f / aggression)
                {
                    e.Timer = 0;
                    if (e.Phase >= 1) ShootSpread(e, ProjectileKind.DarkBolt, 470 + e.Phase * 70); else Shoot(e, ProjectileKind.DarkBolt, 520);
                }
                break;
            case EnemyKind.Webbey:
                if (e.Timer > 1.2f / aggression)
                {
                    e.Timer = 0; e.Vel.Y = -700 - e.Phase * 80; e.Vel.X = Math.Sign(dx) * (260 + e.Phase * 80);
                    if (Math.Abs(dx) < 850) Shoot(e, ProjectileKind.Web, 410 + e.Phase * 45);
                }
                e.Vel.Y += 1900 * dt; BossGroundPhysics(e, dt);
                break;
            case EnemyKind.Mina:
                e.Pos.X += Math.Sign(dx) * (150 + e.Phase * 70) * dt;
                e.Pos.Y = e.Spawn.Y - (250 + e.Phase * 40) + MathF.Sin(_elapsed * (2.4f + e.Phase * .4f)) * (130 + e.Phase * 20);
                if (e.Timer > 1.3f / aggression)
                {
                    e.Timer = 0;
                    if (e.Phase == 2) ShootSpread(e, ProjectileKind.DarkBolt, 610); else Shoot(e, ProjectileKind.DarkBolt, 560 + e.Phase * 55);
                }
                break;
            case EnemyKind.CountSpatula:
                if (e.Timer > 2.1f / aggression)
                {
                    e.Timer = 0;
                    e.Pos.X = Math.Clamp(_player.Pos.X + (dx > 0 ? -520 : 520), 200, _level.Width - 500);
                    float gy = FindGroundY(e.Pos.X, _player.Pos.Y + 500); if (!float.IsNaN(gy)) e.Pos.Y = gy;
                    ShootSpread(e, ProjectileKind.Bat, 440 + e.Phase * 45);
                    if (e.Phase >= 1 && _level.Enemies.Count(x => !x.Dead && x.Kind == EnemyKind.VampireBatling) < 6)
                        SummonNear(e, EnemyKind.VampireBatling, 1 + e.Phase, 240);
                }
                break;
            case EnemyKind.Rip:
                WalkGrounded(e, dt, Math.Abs(dx) < 1150 ? 320 + e.Phase * 85 : 110);
                if (e.Timer > 1.8f / aggression && Math.Abs(dx) < 900)
                {
                    e.Timer = 0; e.Vel.X = Math.Sign(dx) * (620 + e.Phase * 130); e.Vel.Y = -420 - e.Phase * 60;
                }
                e.Vel.Y += 1800 * dt; BossGroundPhysics(e, dt, keepHorizontal: true);
                break;
            case EnemyKind.NightMare:
                if (e.Phase == 0)
                {
                    WalkGrounded(e, dt, Math.Abs(dx) < 1300 ? 430 : 180);
                    if (e.Timer > 2.2f) { e.Timer = 0; ShootSpread(e, ProjectileKind.DarkBolt, 590); }
                }
                else if (e.Phase == 1)
                {
                    WalkGrounded(e, dt, Math.Abs(dx) < 1500 ? 520 : 220);
                    if (e.Timer > 1.35f) { e.Timer = 0; ShootSpread(e, ProjectileKind.DarkBolt, 650); Shoot(e, ProjectileKind.DarkBolt, 800); }
                }
                else
                {
                    if (e.Timer > 1.05f)
                    {
                        e.Timer = 0;
                        float tx = Math.Clamp(_player.Pos.X + (_player.Facing > 0 ? 620 : -620), 300, _level.Width - 350);
                        e.Pos.X = tx;
                        float gy = FindGroundY(tx, _player.Pos.Y + 450); if (!float.IsNaN(gy)) e.Pos.Y = gy;
                        ShootSpread(e, ProjectileKind.DarkBolt, 730);
                    }
                    else WalkGrounded(e, dt, 570);
                }
                break;
        }
    }

    private static string BossPhaseLine(EnemyKind kind, int phase) => (kind, phase) switch
    {
        (EnemyKind.Ed, 1) => "ED HAS DEVELOPED A SECOND IDEA.",
        (EnemyKind.Ed, 2) => "ED IS NOW OPERATING WELL ABOVE ED SPECIFICATIONS.",
        (EnemyKind.OldGnarley, 1) => "OLD GNARLEY IS BRANCHING OUT.",
        (EnemyKind.OldGnarley, 2) => "THE TREE HAS CHOSEN ARTILLERY.",
        (EnemyKind.Spookers, 1) => "SPOOKERS HAS BECOME NOTICEABLY MORE SPOOKER.",
        (EnemyKind.Spookers, 2) => "BACKGROUND AND FOREGROUND PRIVILEGES REVOKED.",
        (EnemyKind.Webbey, 1) => "WEBBEY HAS ENABLED HIGH-BANDWIDTH WEB.",
        (EnemyKind.Webbey, 2) => "LATENCY IS NOW EIGHT LEGS.",
        (EnemyKind.Mina, 1) => "MINA HAS CHANGED SHAPE AND ATTITUDE.",
        (EnemyKind.Mina, 2) => "MINA HAS RUN OUT OF REASONABLE BIRDS.",
        (EnemyKind.CountSpatula, 1) => "COUNT SPATULA HAS PREHEATED THE ARENA.",
        (EnemyKind.CountSpatula, 2) => "NON-STICK COATING HAS FAILED.",
        (EnemyKind.Rip, 1) => "RIP IS MAKING THIS A PIECE-MAKING EVENT.",
        (EnemyKind.Rip, 2) => "THE PIECES ARE NOW MOSTLY YOURS.",
        (EnemyKind.NightMare, 1) => "NIGHT MARE IS EDITING THE RULES.",
        (EnemyKind.NightMare, 2) => "REALITY HAS LEFT THE BOSS ARENA.",
        _ => "BOSS PHASE SHIFT"
    };

    private void HoverToward(Enemy e, float dt, float speed, float height, float amplitude)
    {
        float dx = _player.Pos.X - e.Pos.X;
        e.Pos.X += Math.Sign(dx) * speed * dt;
        e.Pos.Y = e.Spawn.Y - height + MathF.Sin(_elapsed * 2.3f + e.Spawn.X * .01f) * amplitude;
    }

    private void TryShoot(Enemy e, ProjectileKind kind, float speed, float period, float range)
    {
        if (Vector2.Distance(e.Pos, _player.Pos) > range || e.Timer <= period) return;
        e.Timer = 0;
        Shoot(e, kind, speed);
    }

    private void SummonNear(Enemy source, EnemyKind kind, int count, float spacing)
    {
        if (_level is null) return;
        for (int i = 0; i < count; i++)
        {
            float x = source.Pos.X + (i - (count - 1) / 2f) * spacing;
            float gy = FindGroundY(x, source.Pos.Y + 220);
            if (float.IsNaN(gy)) gy = source.Pos.Y;
            Enemy child = new(kind, new Vector2(x, gy))
            {
                Direction = Math.Sign(_player.Pos.X - x) == 0 ? 1 : Math.Sign(_player.Pos.X - x),
                Variant = (source.Variant + i + 1) & 3
            };
            _level.Enemies.Add(child);
        }
    }

    private void HopEnemy(Enemy e, float dt, float horizontalSpeed, float jumpSpeed, float period)
    {
        float ground = FindGroundY(e.Pos.X, e.Pos.Y + 100);
        bool grounded = !float.IsNaN(ground) && Math.Abs(e.Pos.Y - ground) < 3 && Math.Abs(e.Vel.Y) < 5;
        if (grounded && e.Timer > period)
        {
            e.Timer = 0;
            e.Vel.Y = -jumpSpeed;
            e.Vel.X = Math.Sign(_player.Pos.X - e.Pos.X) * horizontalSpeed;
        }
        e.Vel.Y += 1850 * dt;
        e.Pos += e.Vel * dt;
        ground = FindGroundY(e.Pos.X, e.Pos.Y + 100);
        if (!float.IsNaN(ground) && e.Pos.Y >= ground) { e.Pos.Y = ground; e.Vel.Y = 0; }
    }

    private void DiveBomber(Enemy e, float dt, float horizontalSpeed, float diveSpeed, float period)
    {
        float dx = _player.Pos.X - e.Pos.X;
        if (e.State == 0)
        {
            e.Pos.X += Math.Sign(dx) * horizontalSpeed * dt;
            e.Pos.Y = e.Spawn.Y - 175 + MathF.Sin(_elapsed * 2.1f + e.Spawn.X * .01f) * 42;
            if (Math.Abs(dx) < 300 && e.Timer > period) { e.Timer = 0; e.State = 1; e.Vel = new Vector2(Math.Sign(dx) * horizontalSpeed, diveSpeed); }
        }
        else
        {
            e.Pos += e.Vel * dt;
            if (e.Pos.Y >= e.Spawn.Y - 5) { e.State = 0; e.Vel = Vector2.Zero; }
        }
    }

    private void WalkGrounded(Enemy e, float dt, float speed)
    {
        if (_level is null) return;
        EnemyCapability caps = e.Capabilities;
        if ((caps & (EnemyCapability.Charger | EnemyCapability.Runner)) != 0 || e.Kind == EnemyKind.UnicornHunter || e.Boss)
        {
            int toward = Math.Sign(_player.Pos.X - e.Pos.X);
            if (toward != 0) e.Direction = toward;
        }
        if (e.Direction == 0) e.Direction = 1;
        float nx = e.Pos.X + e.Direction * speed * dt;
        float gy = FindGroundY(nx, e.Pos.Y + 80);
        if (float.IsNaN(gy) || Math.Abs(gy - e.Pos.Y) > 120)
        {
            e.Direction *= -1;
            return;
        }
        e.Pos.X = nx; e.Pos.Y = gy;
    }

    private void BossGroundPhysics(Enemy e, float dt, bool keepHorizontal = false)
    {
        if (_level is null) return;
        e.Pos += e.Vel * dt;
        float gy = FindGroundY(e.Pos.X, e.Pos.Y + 110);
        if (!float.IsNaN(gy) && e.Pos.Y >= gy)
        {
            e.Pos.Y = gy;
            e.Vel.Y = 0;
            if (!keepHorizontal) e.Vel.X *= .65f;
        }
    }

    private void Shoot(Enemy e, ProjectileKind kind, float speed)
    {
        if (_level is null) return;
        Vector2 from = e.Pos + new Vector2(0, -e.Height * .6f);
        Vector2 to = _player.Pos + new Vector2(0, -50) - from;
        if (to.LengthSquared() < 1) to = Vector2.UnitX;
        Vector2 vel = Vector2.Normalize(to) * speed;
        (float radius, int damage) = ProjectileStats(kind);
        _level.Projectiles.Add(new Projectile { Pos = from, Vel = vel, Kind = kind, Friendly = false, Radius = radius, Damage = damage, Life = 4 });
    }

    private void ShootSpread(Enemy e, ProjectileKind kind, float speed)
    {
        if (_level is null) return;
        Vector2 from = e.Pos + new Vector2(0, -e.Height * .6f);
        Vector2 to = _player.Pos + new Vector2(0, -50) - from;
        float baseA = MathF.Atan2(to.Y, to.X);
        (float radius, int damage) = ProjectileStats(kind);
        foreach (float da in new[] { -.23f, 0f, .23f })
            _level.Projectiles.Add(new Projectile { Pos = from, Vel = new Vector2(MathF.Cos(baseA + da), MathF.Sin(baseA + da)) * speed, Kind = kind, Friendly = false, Radius = radius, Damage = damage, Life = 4 });
    }

    private static (float radius, int damage) ProjectileStats(ProjectileKind kind) => kind switch
    {
        ProjectileKind.Web => (18, 1),
        ProjectileKind.Toast => (15, 1),
        ProjectileKind.Snowball => (16, 1),
        ProjectileKind.Citation => (15, 1),
        ProjectileKind.DarkBolt => (12, 1),
        ProjectileKind.Bone => (12, 1),
        _ => (11, 1)
    };

    private void UpdateProjectiles(float dt)
    {
        if (_level is null) return;
        for (int i = _level.Projectiles.Count - 1; i >= 0; i--)
        {
            Projectile p = _level.Projectiles[i];
            p.Life -= dt;
            if (p.Kind is ProjectileKind.Acorn or ProjectileKind.Snowball or ProjectileKind.Toast or ProjectileKind.Bone)
                p.Vel.Y += 520 * dt;
            p.Pos += p.Vel * dt;
            if (p.Life <= 0 || p.Pos.Y > _level.DeathY || p.Pos.X < -200 || p.Pos.X > _level.Width + 200)
            { _level.Projectiles.RemoveAt(i); continue; }

            RectangleF b = new(p.Pos.X - p.Radius, p.Pos.Y - p.Radius, p.Radius * 2, p.Radius * 2);
            if (p.Friendly)
            {
                bool hit = false;
                int enemyCount = _level.Enemies.Count;
                for (int ei = 0; ei < enemyCount; ei++)
                {
                    Enemy e = _level.Enemies[ei];
                    if (e.Dead) continue;
                    if (b.IntersectsWith(e.Bounds)) { DamageEnemy(e, p.Damage, p.Vel.X * .3f); hit = true; break; }
                }
                if (hit) _level.Projectiles.RemoveAt(i);
            }
            else if (b.IntersectsWith(_player.Bounds))
            {
                _player.TakeDamage(Math.Max(1, p.Damage), p.Pos.X);
                _audio.SfxHit();
                _level.Projectiles.RemoveAt(i);
            }
        }
    }

    private void ResolveCombat()
    {
        if (_level is null || _player.Dead) return;
        int count = _level.Enemies.Count;
        for (int i = 0; i < count; i++)
        {
            Enemy e = _level.Enemies[i];
            if (e.Dead || e.HitCooldown > 0) continue;
            RectangleF eb = e.Bounds;
            EnemyCapability caps = e.Capabilities;

            if (_player.Jousting && _player.HornBounds.IntersectsWith(eb))
            {
                bool hittingFront = Math.Sign(_player.Pos.X - e.Pos.X) != e.Direction;
                if ((caps & EnemyCapability.ShieldFront) != 0 && hittingFront && _player.Speed < 720)
                {
                    _player.Vel.X *= -.32f;
                    _audio.SfxJoust();
                    Burst(new Vector2(e.Pos.X, e.Pos.Y - e.Height * .55f), Color.Silver, 7);
                    e.HitCooldown = .12f;
                    continue;
                }
                if ((caps & EnemyCapability.JoustImmune) != 0 && _player.Speed < 880)
                {
                    _player.Vel.X *= -.22f;
                    e.HitCooldown = .12f;
                    continue;
                }
                int dmg = _player.Speed > 820 ? 5 : _player.Speed > 620 ? 3 : _player.Speed > 430 ? 2 : 1;
                DamageEnemy(e, dmg, _player.Facing * (250 + _player.Speed));
                _audio.SfxJoust();
                _shake = Math.Max(_shake, dmg * 2.5f);
                continue;
            }

            if (_player.Stomping && _player.StompBounds.IntersectsWith(eb) && _player.Vel.Y > 150)
            {
                if ((caps & EnemyCapability.StompImmune) != 0)
                {
                    _player.TakeDamage(1, e.Pos.X);
                    _player.Bounce(430);
                    _audio.SfxHit();
                }
                else
                {
                    DamageEnemy(e, 3, 0);
                    _player.Bounce((caps & EnemyCapability.Bouncer) != 0 ? 1120 : 810);
                    _audio.SfxStomp();
                    _shake = Math.Max(_shake, 8);
                }
                continue;
            }

            if (_player.Bounds.IntersectsWith(eb))
            {
                bool topHit = _player.Vel.Y > 120 && _player.Bounds.Bottom < eb.Top + eb.Height * .45f;
                if (topHit && (caps & EnemyCapability.StompImmune) == 0)
                {
                    DamageEnemy(e, 1, 0);
                    _player.Bounce((caps & EnemyCapability.Bouncer) != 0 ? 1000 : 690);
                    _audio.SfxStomp();
                }
                else if (_player.Boosting && _player.Speed > 360)
                {
                    // ALPHA2.2 COMBAT RULE:
                    // Rainbow Ramjet by itself is not Sonic's sawblade. It is a punt.
                    // Ordinary minions get flung into the air and survive unless the
                    // environment subsequently kills them. For a clean high-speed kill
                    // the player must also commit to Sparkly Horn Joust; that branch is
                    // resolved above via HornBounds before body contact reaches here.
                    //
                    // The design rule says enemies that cannot be stomped must visibly
                    // communicate that with spikes. StompImmune therefore doubles as
                    // the current gameplay "spiky contact" flag: dash immunity does NOT
                    // protect Sparkles from those enemies, projectiles, or spike hazards.
                    bool spiky = (caps & EnemyCapability.StompImmune) != 0;
                    if (spiky || e.Boss)
                    {
                        int contact = EnemyCatalog.Get(e.Kind).ContactDamage;
                        _player.TakeDamage(contact, e.Pos.X);
                        _audio.SfxHit();
                        _shake = Math.Max(_shake, 8);
                    }
                    else
                    {
                        float launchX = _player.Facing * Math.Clamp(420f + _player.Speed * .72f, 520f, 1150f);
                        float launchY = -Math.Clamp(360f + _player.Speed * .48f, 460f, 920f);
                        e.RamjetLaunched = true;
                        e.RamjetStunTimer = .42f + Math.Min(.55f, _player.Speed / 1800f);
                        e.Vel = new Vector2(launchX, launchY);
                        e.HitCooldown = .22f;
                        e.Pos.X += _player.Facing * 18f;

                        // Sparkles keeps most of the dash so a crowd becomes slapstick,
                        // not a row of invisible brick walls.
                        _player.Vel.X *= .92f;
                        Burst(new Vector2(e.Pos.X, e.Pos.Y - e.Height * .45f), Color.HotPink, 11);
                        _audio.SfxJoust();
                        _shake = Math.Max(_shake, 5.5f);
                    }
                }
                else
                {
                    int contact = EnemyCatalog.Get(e.Kind).ContactDamage;
                    _player.TakeDamage(contact, e.Pos.X); _audio.SfxHit(); _shake = Math.Max(_shake, 7);
                }
            }
        }
    }

    private void DamageEnemy(Enemy e, int damage, float knockback)
    {
        if (_level is null || e.Dead || e.HitCooldown > 0 || e.Invulnerable) return;
        e.Health -= damage;
        e.HitCooldown = .16f;
        if (!e.Boss) e.Pos.X += Math.Clamp(knockback * .035f, -55, 55);
        Burst(new Vector2(e.Pos.X, e.Pos.Y - e.Height * .55f), e.Boss ? Color.Gold : Color.White, e.Boss ? 18 : 7);
        if (e.Health > 0) return;

        e.Dead = true;
        long points = e.Boss ? 5000 + e.MaxHealth * 250 : 400 + e.MaxHealth * 100;
        _player.Score += points;
        Burst(new Vector2(e.Pos.X, e.Pos.Y - e.Height / 2), e.Boss ? Color.HotPink : Color.Orange, e.Boss ? 70 : 22);

        if (e.Kind == EnemyKind.SlimeIntern && !e.SplitOnce)
        {
            e.SplitOnce = true;
            for (int n = -1; n <= 1; n += 2)
            {
                Enemy child = new(EnemyKind.SlimeIntern, new Vector2(e.Pos.X + n * 32, e.Pos.Y))
                {
                    Width = e.Width * .72f,
                    Height = e.Height * .72f,
                    Health = 1,
                    MaxHealth = 1,
                    Direction = n,
                    SplitOnce = true,
                    Variant = (e.Variant + (n > 0 ? 1 : 2)) & 3
                };
                _level.Enemies.Add(child);
            }
        }

        if (e.Boss)
        {
            _audio.SfxBoss(); _shake = 22;
            ShowBanner(BossDefeatLine(e.Kind), 2.8f);
        }
    }

    private static string BossDefeatLine(EnemyKind k) => k switch
    {
        EnemyKind.Ed => "ED HAS BEEN DEFEATED. THIS SURPRISED ED MOST OF ALL.",
        EnemyKind.OldGnarley => "OLD GNARLEY HAS BEEN VIOLENTLY PRUNED.",
        EnemyKind.Spookers => "SPOOKERS HAS BEEN RECLASSIFIED AS ATMOSPHERE.",
        EnemyKind.Webbey => "WEBBEY HAS LOGGED OFF THE WEB.",
        EnemyKind.Mina => "MINA HAS SELECTED A LESS HOSTILE SHAPE.",
        EnemyKind.CountSpatula => "COUNT SPATULA HAS BEEN... FLIPPED.",
        EnemyKind.Rip => "RIP HAS BEEN MADE INTO SEVERAL PIECES. IRONY ACHIEVED.",
        EnemyKind.NightMare => "NIGHT MARE HAS BEEN POWER-WASHED WITH RAINBOWS.",
        _ => "BOSS DEFEATED"
    };

    private float FindGroundY(float x, float aroundY)
    {
        if (_level is null) return float.NaN;
        float best = float.NaN;
        float bestDelta = float.MaxValue;
        foreach (Platform p in _level.Platforms)
        {
            if (p.Destroyed || x < p.Rect.Left + 2 || x > p.Rect.Right - 2) continue;
            float d = Math.Abs(p.Rect.Top - aroundY);
            if (d < bestDelta && p.Rect.Top < aroundY + 170)
            { bestDelta = d; best = p.Rect.Top; }
        }
        return bestDelta < 260 ? best : float.NaN;
    }

    private void TriggerLevelClear()
    {
        if (_level is null || _levelClearTriggered) return;
        _levelClearTriggered = true;
        _levelClearTimer = 2.3f;
        _runScore = _player.Score;
        ShowBanner(_level.Secret ? "SECRET LEVEL COMPLETE — THE MAP PRETENDS THIS NEVER HAPPENED." : "LEVEL COMPLETE — SPARKLES REMAINS A PUBLIC HAZARD.", 2.2f);
        _audio.SfxSecret();
        for (int i = 0; i < 80; i++) EmitCelebrationParticle();
    }

    private void FinishLevelTransition()
    {
        if (_level is null) return;
        if (_level.Secret)
        {
            _save.Data.HighScore = Math.Max(_save.Data.HighScore, _player.Score);
            if (_level.SecretCode is not null) _save.Data.DiscoveredCodes.Add(_level.SecretCode);
            _save.Save();
            _mode = GameMode.WorldMap;
            _level = null;
            return;
        }
        int idx = _level.CampaignIndex;
        _save.CompleteLevel(idx, _player.Score);
        _mapIndex = idx;
        if (idx == 27)
        {
            _level = null;
            StartStory(StoryBook.Ending, GameMode.Victory);
        }
        else
        {
            _mode = GameMode.WorldMap;
            _level = null;
        }
    }

    private void UpdateCamera(float dt, Size size)
    {
        if (_level is null) return;
        float targetX = _player.Pos.X - size.Width * .38f;
        _cameraX += (targetX - _cameraX) * Math.Min(1, dt * 5.5f);
        _cameraX = Math.Clamp(_cameraX, 0, Math.Max(0, _level.Width - size.Width));
        float targetY = Math.Clamp(_player.Pos.Y - size.Height * .70f, -40, 180);
        _cameraY += (targetY - _cameraY) * Math.Min(1, dt * 4f);
        _shake = Math.Max(0, _shake - 34 * dt);
    }

    private void UpdateParticles(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            Particle p = _particles[i];
            p.Life -= dt;
            if (p.Life <= 0) { _particles.RemoveAt(i); continue; }
            p.Pos += p.Vel * dt;
            p.Vel.Y += 220 * dt;
            p.Vel *= MathF.Pow(.25f, dt);
        }
    }

    private void Emit(Particle p) => _particles.Add(p);

    private void Burst(Vector2 pos, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float a = (float)(Random.Shared.NextDouble() * MathF.Tau);
            float sp = 80 + (float)Random.Shared.NextDouble() * 360;
            Emit(new Particle { Pos = pos, Vel = new Vector2(MathF.Cos(a), MathF.Sin(a)) * sp, Life = .35f + (float)Random.Shared.NextDouble() * .65f, MaxLife = 1, Size = 4 + (float)Random.Shared.NextDouble() * 10, Color = color, Shape = i % 3 });
        }
    }

    private void EmitCelebrationParticle()
    {
        Color[] c = { Color.HotPink, Color.Gold, Color.Cyan, Color.Lime, Color.Violet, Color.White };
        Emit(new Particle { Pos = _player.Pos + new Vector2(Random.Shared.Next(-180, 181), Random.Shared.Next(-180, 30)), Vel = new Vector2(Random.Shared.Next(-260, 261), Random.Shared.Next(-520, -120)), Life = 1.5f, MaxLife = 1.5f, Size = Random.Shared.Next(5, 15), Color = c[Random.Shared.Next(c.Length)], Shape = 1 });
    }

    private void ShowBanner(string text, float seconds)
    {
        _banner = text;
        _bannerTimer = seconds;
    }

    public void Dispose()
    {
        _save.Data.HighScore = Math.Max(_save.Data.HighScore, _player.Score);
        _save.Data.GeneratorVersion = StableHash.GeneratorVersion;
        _save.Save();
        _settings.Save();
        _audio.Dispose();
        _assets.Dispose();
    }
}
