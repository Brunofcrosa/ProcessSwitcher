using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProcessSwitcher
{
    public partial class Form1 : Form
    {
        private const int SW_RESTORE = 9;
        private const int MaxProcesses = 10;
        private const string ConfigFile = "hotkeys.config";

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern int UnregisterHotKey(IntPtr hWnd, int id);

        private Process?[] processes = new Process?[MaxProcesses];
        private Dictionary<Keys, int> hotkeyMapping = new Dictionary<Keys, int>();
        private Dictionary<int, ComboBox> hotkeySelectors = new Dictionary<int, ComboBox>();

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
            int index = 0;

            foreach (var process in allProcesses)
            {
                if (index < MaxProcesses)
                {
                    processes[index] = process;

                    
                    Label processLabel = new Label
                    {
                        Text = $"elementclient {index + 1} - {process.Id}",
                        AutoSize = true,
                        Width = 180
                    };

                    
                    ComboBox hotkeySelector = new ComboBox
                    {
                        Width = 80,
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };

                    foreach (Keys key in Enum.GetValues(typeof(Keys)))
                    {
                        if (key >= Keys.F1 && key <= Keys.F12)
                            hotkeySelector.Items.Add(key);
                    }

                    hotkeySelector.SelectedIndexChanged += (sender, e) =>
                    {
                        if (hotkeySelector.SelectedItem != null)
                        {
                            Keys selectedKey = (Keys)hotkeySelector.SelectedItem;
                            hotkeyMapping[selectedKey] = index;
                            SaveHotkeys();
                            RegisterGlobalHotkeys();
                        }
                    };

                    
                    foreach (var kvp in hotkeyMapping)
                    {
                        if (kvp.Value == index)
                        {
                            hotkeySelector.SelectedItem = kvp.Key;
                            break;
                        }
                    }

                    hotkeySelectors[index] = hotkeySelector;

                    
                    FlowLayoutPanel row = new FlowLayoutPanel { Width = 270, Height = 30 };
                    row.Controls.Add(processLabel);
                    row.Controls.Add(hotkeySelector);
                    processPanel.Controls.Add(row);

                    index++;
                }
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
            for (int i = 0; i < MaxProcesses; i++)
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
                if (id >= 0 && id < MaxProcesses)
                {
                    SwitchToProcess(processes[id]);
                }
            }
            base.WndProc(ref m);
        }

        private void SwitchToProcess(Process? process)
        {
            if (process != null && process.MainWindowHandle != IntPtr.Zero)
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
        if (kvp.Value.SelectedItem != null)
        {
            Keys selectedKey = (Keys)kvp.Value.SelectedItem;
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
