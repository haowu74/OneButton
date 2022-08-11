
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

        private string configurationFile = "";

        private JsonSerializer serializer = new JsonSerializer();

        private IntPtr arrow;

        private IntPtr beam;

        private JoystickOffset joystickOffset;
        public Form1()
        {
            InitializeComponent();
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            configurationFile = Path.Combine(homePath, "AutomotiveDiag\\config.json");
            Directory.CreateDirectory(Path.Combine(homePath, "AutomotiveDiag"));
            LoadPosition(out int X, out int Y, out joystickOffset);
            chooseJoyStickButton.SelectedIndex = chooseJoyStickButton.Items.IndexOf(joystickOffset.ToString());
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
                    //if (are.WaitOne(100)) break;
                    Thread.Sleep(100);
                    if (MouseButtons == MouseButtons.Left && teaching)
                    {
                        mousePos.X = MousePosition.X;
                        mousePos.Y = MousePosition.Y;
                        Invoke(new MethodInvoker(() => notifyIcon1.Text = $"({mousePos.X}, {mousePos.Y})"));
                        SavePosition(mousePos.X, mousePos.Y);
                        teaching = false;
                        SetSystemCursor(arrow, 32512);
                        SetSystemCursor(beam, 32513);
                        this.Invoke(new MethodInvoker(() => teachToolStripMenuItem1.Checked = false ));
                    }
                    else if (!teaching)
                    {
                        joystick?.Poll();
                        var datas = joystick?.GetBufferedData();
                        if (datas != null && datas.Any(d => d.Offset == joystickOffset && d.Value == 128))
                        {
                            GetCursorPos(out Point point);
                            oldPos = point;
                            SetCursorPos(mousePos.X, mousePos.Y);
                            mouse_event(2, mousePos.X, mousePos.Y, 0, 0);
                        }
                        else if (datas != null && datas.Any(d => d.Offset == joystickOffset && d.Value == 0))
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
            // If Joystick not found, exit the application
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
            serializer.Serialize(writer, new MousePosition { X = X, Y = Y, JoyStickButton = joystickOffset.ToString() });
        }

        private bool LoadPosition(out int X, out int Y, out JoystickOffset joystickOffset)
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
                        joystickOffset = JoystickOffset.X;
                        try
                        {
                            Enum.TryParse(point.JoyStickButton, out joystickOffset);
                        }
                        catch(ArgumentException)
                        {
                            Application.Exit();
                        }
                        return true;
                    }
                    else
                    {
                        X = 0;
                        Y = 0;
                        joystickOffset = JoystickOffset.X;
                        return false;
                    }
                }
            }
            else
            {
                X = 0;
                Y = 0;
                joystickOffset = JoystickOffset.X;
                return false;
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSystemCursor(arrow, 32512);
            SetSystemCursor(beam, 32513);
            Application.Exit();
        }

        private void chooseJoyStickButton_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = ((ToolStripComboBox)sender).SelectedItem.ToString();
            try
            {
                Enum.TryParse(selected, out joystickOffset);
            }
            catch (ArgumentException)
            {
                Application.Exit();
            }
        }
    }

    class MousePosition
    {
        public int X { get; set; }

        public int Y { get; set; }

        public string? JoyStickButton { get; set; }
    }
}