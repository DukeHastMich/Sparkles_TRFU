using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace SparklesReborn;

public sealed class GameForm : Form
{
    private readonly GameApp _game;
    private readonly InputState _input = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 8 };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastTime;
    private double _accumulator;
    private const double FixedStep = 1.0 / 120.0;
    private bool _fullscreen;
    private FormBorderStyle _oldBorder;
    private Rectangle _oldBounds;

    public GameForm()
    {
        Text = "Sparkles — Rainbow Ramjet Edition";
        ClientSize = new Size(1280, 720);
        MinimumSize = new Size(960, 540);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        BackColor = Color.Black;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        _game = new GameApp(_input, RequestQuit, ToggleFullscreen);
        _timer.Tick += OnTick;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F11) { ToggleFullscreen(); e.Handled = true; return; }
            _input.OnKeyDown(e.KeyCode);
            e.Handled = true;
        };
        KeyUp += (_, e) => { _input.OnKeyUp(e.KeyCode); e.Handled = true; };
        KeyPress += (_, e) => _input.OnTextInput(e.KeyChar);
        Deactivate += (_, _) => _input.ClearHeld();
        FormClosing += (_, _) => _game.Dispose();
        Shown += (_, _) => { _lastTime = _clock.Elapsed.TotalSeconds; _timer.Start(); };
    }

    private void OnTick(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        double frame = Math.Clamp(now - _lastTime, 0, 0.1);
        _lastTime = now;
        _accumulator += frame;

        _input.PollGamepad();
        int safety = 0;
        while (_accumulator >= FixedStep && safety++ < 12)
        {
            _game.Update((float)FixedStep, ClientSize);
            _input.EndFixedFrame();
            _accumulator -= FixedStep;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        _game.Render(e.Graphics, ClientRectangle);
    }

    private void RequestQuit() => BeginInvoke(Close);

    private void ToggleFullscreen()
    {
        if (!_fullscreen)
        {
            _oldBorder = FormBorderStyle;
            _oldBounds = Bounds;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            Bounds = Screen.FromControl(this).Bounds;
            TopMost = false;
            _fullscreen = true;
        }
        else
        {
            FormBorderStyle = _oldBorder;
            Bounds = _oldBounds;
            WindowState = FormWindowState.Normal;
            _fullscreen = false;
        }
    }
}
