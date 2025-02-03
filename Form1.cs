using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace ProcessSwitcher
{
    public partial class Form1 : Form
    {
        private const int SW_RESTORE = 9;
        private const string ConfigFile = "hotkeys.config";

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern int UnregisterHotKey(IntPtr hWnd, int id);

        private List<Process> processes = new List<Process>();
        private Dictionary<Keys, int> hotkeyMapping = new Dictionary<Keys, int>();
        private Dictionary<int, TextBox> hotkeySelectors = new Dictionary<int, TextBox>();

        public Form1()
        {
            InitializeComponent();
            LoadHotkeys();
            LoadProcesses();
            RegisterGlobalHotkeys();
        }

        private void LoadProcesses()
        {
            var allProcesses = Process.GetProcessesByName("elementclient_64");
            processPanel.Controls.Clear();
            hotkeySelectors.Clear();

            var processHotkeys = hotkeyMapping
                .Where(kvp => kvp.Value < 10)
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            int index = 0;

            foreach (var process in allProcesses)
            {
                processes.Add(process);

                Label processLabel = new Label
                {
                    Text = $"{process.MainWindowTitle} ",
                    AutoSize = true,
                    Width = 180
                };

                TextBox hotkeyTextBox = new TextBox
                {
                    Width = 80,
                    ReadOnly = true
                };


                if (processHotkeys.ContainsKey(index))
                {
                    hotkeyTextBox.Text = processHotkeys[index].ToString();
                }

                hotkeyTextBox.Click += (sender, e) =>
                {
                    hotkeyTextBox.ReadOnly = false;
                };

                hotkeyTextBox.KeyDown += (sender, e) =>
                {

                    if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu)
                        return;

                    hotkeyTextBox.Text = e.KeyCode.ToString();
                    hotkeyMapping[e.KeyCode] = index;
                    SaveHotkeys();
                    RegisterGlobalHotkeys();


                    hotkeyTextBox.ReadOnly = true;
                };

                hotkeySelectors[index] = hotkeyTextBox;

                FlowLayoutPanel row = new FlowLayoutPanel { Width = 270, Height = 30 };
                row.Controls.Add(processLabel);
                row.Controls.Add(hotkeyTextBox);
                processPanel.Controls.Add(row);

                index++;
            }
        }


        private void RegisterGlobalHotkeys()
        {
            UnregisterGlobalHotkeys();
            foreach (var kvp in hotkeyMapping)
            {
                RegisterHotKey(this.Handle, kvp.Value + 1, 0, (int)kvp.Key);
            }
        }

        private void UnregisterGlobalHotkeys()
        {
            for (int i = 0; i < 10; i++)
            {
                UnregisterHotKey(this.Handle, i + 1);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32() - 1;
                if (id >= 0 && id < processes.Count)
                {
                    SwitchToProcess(processes[id]);
                }
            }
            base.WndProc(ref m);
        }

        private void SwitchToProcess(Process process)
        {
            if (process != null && !process.HasExited && process.MainWindowHandle != IntPtr.Zero)
            {
                ShowWindow(process.MainWindowHandle, SW_RESTORE);
                SetForegroundWindow(process.MainWindowHandle);
            }
        }

        private void SaveHotkeys()
        {
            using (StreamWriter writer = new StreamWriter(ConfigFile))
            {
                foreach (var kvp in hotkeyMapping)
                {
                    writer.WriteLine($"{kvp.Key}:{kvp.Value}");
                }
            }
        }

        private void LoadHotkeys()
        {
            if (File.Exists(ConfigFile))
            {
                foreach (var line in File.ReadAllLines(ConfigFile))
                {
                    var parts = line.Split(':');
                    if (Enum.TryParse(parts[0], out Keys key) && int.TryParse(parts[1], out int index))
                    {
                        hotkeyMapping[key] = index;
                    }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterGlobalHotkeys();
        }

        private void btnUpdateHotkeys_Click(object sender, EventArgs e)
        {
            hotkeyMapping.Clear();

            foreach (var kvp in hotkeySelectors)
            {
                if (Enum.TryParse(kvp.Value.Text, out Keys selectedKey))
                {
                    hotkeyMapping[selectedKey] = kvp.Key;
                }
            }

            SaveHotkeys();
            RegisterGlobalHotkeys();
            MessageBox.Show("Atalhos atualizados com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnRefreshProcesses_Click(object sender, EventArgs e)
        {
            LoadProcesses();
            MessageBox.Show("Lista de processos atualizada.", "Atualização", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
