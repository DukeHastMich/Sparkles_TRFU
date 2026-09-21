using System.Runtime.InteropServices;

namespace SparklesReborn;

public sealed class InputState
{
    private readonly HashSet<Keys> _held = new();
    private readonly HashSet<Keys> _pressed = new();
    private readonly Queue<char> _text = new();
    private readonly Dictionary<ControlAction, Keys> _bindings = GameSettings.Defaults();
    private ushort _padButtons;
    private ushort _padPressed;
    private byte _leftTrigger;
    private byte _rightTrigger;
    private short _thumbX;
    private short _thumbY;
    private bool _padAvailable = true;

    public void ApplyBindings(IReadOnlyDictionary<ControlAction, Keys> bindings)
    {
        foreach ((ControlAction action, Keys key) in bindings) _bindings[action] = key;
    }

    public Keys Binding(ControlAction action) => _bindings.TryGetValue(action, out Keys key) ? key : GameSettings.Defaults()[action];
    public string BindingLabel(ControlAction action)
    {
        Keys k = Binding(action);
        return k switch
        {
            Keys.ShiftKey => "SHIFT",
            Keys.Space => "SPACE",
            Keys.Escape => "ESC",
            _ => k.ToString().ToUpperInvariant()
        };
    }
    public void Rebind(ControlAction action, Keys key) => _bindings[action] = key;

    public void OnKeyDown(Keys key)
    {
        if (_held.Add(key)) _pressed.Add(key);
    }

    public void OnKeyUp(Keys key) => _held.Remove(key);
    public void OnTextInput(char c) => _text.Enqueue(c);
    public bool Down(Keys key) => _held.Contains(key);
    public bool Pressed(Keys key) => _pressed.Contains(key);
    public char? ReadChar() => _text.Count > 0 ? _text.Dequeue() : null;
    public Keys? FirstPressedKey() => _pressed.Count == 0 ? null : _pressed.First();

    private bool ActionDown(ControlAction action) => Down(Binding(action));
    private bool ActionPressed(ControlAction action) => Pressed(Binding(action));

    public float MoveX
    {
        get
        {
            float x = 0;
            if (ActionDown(ControlAction.MoveLeft) || Down(Keys.Left)) x -= 1;
            if (ActionDown(ControlAction.MoveRight) || Down(Keys.Right)) x += 1;
            if (Math.Abs(_thumbX) > 9000) x = Math.Clamp(_thumbX / 32767f, -1f, 1f);
            return x;
        }
    }

    public float MoveY
    {
        get
        {
            float y = 0;
            if (ActionDown(ControlAction.MoveUp) || Down(Keys.Up)) y -= 1;
            if (ActionDown(ControlAction.MoveDown) || Down(Keys.Down)) y += 1;
            if (Math.Abs(_thumbY) > 9000) y = -Math.Clamp(_thumbY / 32767f, -1f, 1f);
            return y;
        }
    }

    public bool JumpPressed => ActionPressed(ControlAction.Jump) || Pressed(Keys.Up) || PadPressed(0x1000); // A
    public bool DashDown => ActionDown(ControlAction.Dash) || _rightTrigger > 40;
    public bool DashPressed => ActionPressed(ControlAction.Dash) || PadPressed(0x0200);
    public bool JoustDown => ActionDown(ControlAction.Joust) || PadDown(0x4000); // X
    public bool JoustPressed => ActionPressed(ControlAction.Joust) || PadPressed(0x4000);
    public bool StompDown => ActionDown(ControlAction.Stomp) || PadDown(0x2000); // B
    public bool StompPressed => ActionPressed(ControlAction.Stomp) || PadPressed(0x2000);
    public bool BelchPressed => ActionPressed(ControlAction.Belch) || PadPressed(0x8000); // Y
    public bool ConfirmPressed => Pressed(Keys.Enter) || Pressed(Keys.Space) || PadPressed(0x1000);
    public bool CancelPressed => Pressed(Keys.Escape) || PadPressed(0x2000);
    public bool PausePressed => ActionPressed(ControlAction.Pause) || PadPressed(0x0010); // Start

    private bool PadDown(ushort mask) => (_padButtons & mask) != 0;
    private bool PadPressed(ushort mask) => (_padPressed & mask) != 0;

    public void EndFixedFrame()
    {
        _pressed.Clear();
        _padPressed = 0;
    }

    public void ClearHeld()
    {
        _held.Clear();
        _pressed.Clear();
        _text.Clear();
        _padButtons = _padPressed = 0;
    }

    public void PollGamepad()
    {
        if (!_padAvailable) return;
        try
        {
            if (XInputGetState(0, out XINPUT_STATE state) == 0)
            {
                ushort old = _padButtons;
                _padButtons = state.Gamepad.wButtons;
                _padPressed |= (ushort)(_padButtons & ~old);
                _leftTrigger = state.Gamepad.bLeftTrigger;
                _rightTrigger = state.Gamepad.bRightTrigger;
                _thumbX = state.Gamepad.sThumbLX;
                _thumbY = state.Gamepad.sThumbLY;
            }
            else
            {
                _padButtons = 0;
                _leftTrigger = _rightTrigger = 0;
                _thumbX = _thumbY = 0;
            }
        }
        catch (DllNotFoundException) { _padAvailable = false; }
        catch (EntryPointNotFoundException) { _padAvailable = false; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger, bRightTrigger;
        public short sThumbLX, sThumbLY, sThumbRX, sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState(uint dwUserIndex, out XINPUT_STATE pState);
}
