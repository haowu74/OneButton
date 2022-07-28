
using SharpDX.DirectInput;
using System.Runtime.InteropServices;

namespace MousePos
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern int GetKeyState(int nVirtKey); 

        private Point mousePos;

        private bool teaching;

        private Joystick joystick;

        public Form1()
        {
            InitializeComponent();

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Q:
                    SetCursorPos(mousePos.X, mousePos.Y);
                    break;
                default:
                    break;
            }

            return base.ProcessCmdKey(ref msg, keyData);
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
                    if (MouseButtons == MouseButtons.Middle && teaching)
                    {
                        mousePos.X = MousePosition.X;
                        mousePos.Y = MousePosition.Y;
                        notifyIcon1.BalloonTipText = $"{mousePos.X}, {mousePos.Y}";
                        notifyIcon1.Text = $"Teach mode: {mousePos.X}, {mousePos.Y}";
                        notifyIcon1.ShowBalloonTip(10000);
                    }
                    else if (MouseButtons == MouseButtons.Middle && !teaching)
                    {
                        SetCursorPos(mousePos.X, mousePos.Y);
                        mouse_event(2, mousePos.X, mousePos.Y, 0, 0);
                        mouse_event(4, mousePos.X, mousePos.Y, 0, 0);
                    }
                    //else if ((GetKeyState(0x12) & 0x8000) != 0)
                    //{
                    //    SetCursorPos(mousePos.X, mousePos.Y);
                    //    mouse_event(2, mousePos.X, mousePos.Y, 0, 0);
                    //    mouse_event(4, mousePos.X, mousePos.Y, 0, 0);
                    //}
                    else if (!teaching)
                    {
                        joystick?.Poll();
                        var datas = joystick?.GetBufferedData();
                        if (datas != null && datas.Any(d => d.Offset == JoystickOffset.Buttons0 && d.Value == 128))
                        {
                            SetCursorPos(mousePos.X, mousePos.Y);
                            mouse_event(2, mousePos.X, mousePos.Y, 0, 0);
                            mouse_event(4, mousePos.X, mousePos.Y, 0, 0);
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
                Console.WriteLine("No joystick/Gamepad found.");
                Console.ReadKey();
                Environment.Exit(1);
            }
            joystick = new Joystick(directInput, joystickGuid);
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();
            StartWaitingForClickFromOutside();
        }

        private void teachToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            playToolStripMenuItem1.Checked = false;
            Cursor.Current = Cursors.Hand;
            notifyIcon1.Text = "Teach mode";
            teaching = true;
        }

        private void playToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            teachToolStripMenuItem1.Checked = false;
            Cursor.Current = Cursors.Default;
            notifyIcon1.Text = "Play mode";
            teaching = false;
            //StartWaitingForKeyboardHitFromOutside();
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}