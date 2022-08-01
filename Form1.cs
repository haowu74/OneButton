
using Newtonsoft.Json;
using SharpDX.DirectInput;
using System.Runtime.InteropServices;

namespace MousePos
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out Point point);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern int GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern int SetSystemCursor(IntPtr hCursor, uint id);

        [DllImport("user32.dll")]
        static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        public static extern IntPtr CopyIcon(IntPtr pcur);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, String? pvParam, UInt32 fWinIni);

        private Point mousePos;

        private Point oldPos;

        private bool teaching;

        private Joystick joystick;

        private const string configurationFile = "config.json";

        private JsonSerializer serializer = new JsonSerializer();

        private IntPtr arrow;

        private IntPtr beam;
        public Form1()
        {
            InitializeComponent();
            LoadPosition(out int X, out int Y);
            mousePos.X = X;
            mousePos.Y = Y;
        }

        private AutoResetEvent are = new AutoResetEvent(false);
        private void StartWaitingForClickFromOutside()
        {
            are.Reset();
            var ctx = new SynchronizationContext();
            var task = Task.Run(() =>
            {
                while (true)
                {
                    if (are.WaitOne(100)) break;
                    if (MouseButtons == MouseButtons.Left && teaching)
                    {
                        mousePos.X = MousePosition.X;
                        mousePos.Y = MousePosition.Y;
                        Invoke(new MethodInvoker(() => notifyIcon1.Text = $"({mousePos.X}, {mousePos.Y})"));
                        SavePosition(mousePos.X, mousePos.Y);
                        teaching = false;
                        //SystemParametersInfo(0x57, 0, null, 0);
                        SetSystemCursor(arrow, 32512);
                        SetSystemCursor(beam, 32513);
                        this.Invoke(new MethodInvoker(() => teachToolStripMenuItem1.Checked = false ));
                    }
                    else if (!teaching)
                    {
                        joystick?.Poll();
                        var datas = joystick?.GetBufferedData();
                        if (datas != null && datas.Any(d => d.Offset == JoystickOffset.Buttons0 && d.Value == 128))
                        {
                            GetCursorPos(out Point point);
                            oldPos = point;
                            SetCursorPos(mousePos.X, mousePos.Y);
                            mouse_event(2, mousePos.X, mousePos.Y, 0, 0);
                        }
                        else if (datas != null && datas.Any(d => d.Offset == JoystickOffset.Buttons0 && d.Value == 0))
                        {
                            mouse_event(4, mousePos.X, mousePos.Y, 0, 0);
                            SetCursorPos(oldPos.X, oldPos.Y);
                        }
                    }
                }
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            notifyIcon1.Visible = true;
            this.WindowState = FormWindowState.Minimized;
            var directInput = new DirectInput();
            var joystickGuid = Guid.Empty;
            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad,
                DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
                        DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;
            // If Joystick not found, throws an error
            if (joystickGuid == Guid.Empty)
            {
                Application.Exit();
            }
            joystick = new Joystick(directInput, joystickGuid);
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();
            StartWaitingForClickFromOutside();
        }

        private void TeachToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            teaching = true;
            arrow = CopyIcon(LoadCursor(IntPtr.Zero, 32512));
            beam = CopyIcon(LoadCursor(IntPtr.Zero, 32513));
            SetSystemCursor(CopyIcon(LoadCursor(IntPtr.Zero, 32515)), 32512);
            SetSystemCursor(CopyIcon(LoadCursor(IntPtr.Zero, 32515)), 32513);
        }

        private void SavePosition(int X, int Y)
        {
            using StreamWriter sw = new StreamWriter(configurationFile);
            using JsonWriter writer = new JsonTextWriter(sw);
            serializer.Serialize(writer, new MousePosition { X = X, Y = Y });
        }

        private bool LoadPosition(out int X, out int Y)
        {
            if (File.Exists(configurationFile))
            {
                using (StreamReader file = File.OpenText(configurationFile))
                {
                    MousePosition? point = serializer.Deserialize(file, typeof(MousePosition)) as MousePosition;
                    if (point != null)
                    {
                        X = point.X;
                        Y = point.Y;
                        return true;
                    }
                    else
                    {
                        X = 0;
                        Y = 0;
                        return false;
                    }
                }
            }
            else
            {
                X = 0;
                Y = 0;
                return false;
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSystemCursor(arrow, 32512);
            SetSystemCursor(beam, 32513);
            Application.Exit();
        }
    }

    class MousePosition
    {
        public int X { get; set; }

        public int Y { get; set; }
    }
}