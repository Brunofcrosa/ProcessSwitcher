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
        private Keys sendKeysHotkey = Keys.None;

        public Form1()
        {
            InitializeComponent();
            LoadHotkeys();
            LoadProcesses();
            RegisterGlobalHotkeys();
        }

        private void LoadProcesses()
        {
            processes = Process.GetProcessesByName("elementclient_64").ToList();
            processPanel.Controls.Clear();
            hotkeySelectors.Clear();

            var processHotkeys = hotkeyMapping
                .Where(kvp => kvp.Value < processes.Count)
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            for (int index = 0; index < processes.Count; index++)
            {
                var process = processes[index];

                Label processLabel = new Label
                {
                    Text = $"{process.MainWindowTitle} ",
                    AutoSize = true,
                    Width = 180
                };

                TextBox hotkeyTextBox = new TextBox
                {
                    Width = 80,
                    ReadOnly = true,
                    Text = processHotkeys.ContainsKey(index) ? processHotkeys[index].ToString() : string.Empty
                };

                hotkeyTextBox.Click += (sender, e) => hotkeyTextBox.ReadOnly = false;

                hotkeyTextBox.KeyDown += (sender, e) =>
                {
                    if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu)
                        return;

                    
                    var existingKey = hotkeyMapping.FirstOrDefault(kvp => kvp.Value == index).Key;
                    if (existingKey != Keys.None)
                    {
                        hotkeyMapping.Remove(existingKey);
                    }

                    
                    hotkeyMapping[e.KeyCode] = index;
                    hotkeyTextBox.Text = e.KeyCode.ToString();
                    SaveHotkeys();
                    RegisterGlobalHotkeys();
                    hotkeyTextBox.ReadOnly = true;
                };

                hotkeySelectors[index] = hotkeyTextBox;

                FlowLayoutPanel row = new FlowLayoutPanel { Width = 270, Height = 30 };
                row.Controls.Add(processLabel);
                row.Controls.Add(hotkeyTextBox);
                processPanel.Controls.Add(row);
            }
        }

        private void RegisterGlobalHotkeys()
        {
            UnregisterGlobalHotkeys();

            
            foreach (var kvp in hotkeyMapping)
            {
                
                int id = kvp.Key.GetHashCode() % 1000; 
                RegisterHotKey(this.Handle, id, 0, (int)kvp.Key);
            }

            
            if (sendKeysHotkey != Keys.None)
            {
                int id = sendKeysHotkey.GetHashCode() % 1000;
                RegisterHotKey(this.Handle, id, 0, (int)sendKeysHotkey);
            }
        }

        private void UnregisterGlobalHotkeys()
        {
            
            for (int i = 0; i <= 1000; i++)
            {
                UnregisterHotKey(this.Handle, i);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();

                if (id == sendKeysHotkey.GetHashCode() % 1000)
                {
                    SendKeysToAllProcesses();
                }
                else
                {
                    
                    var key = hotkeyMapping.FirstOrDefault(kvp => kvp.Key.GetHashCode() % 1000 == id).Key;
                    if (hotkeyMapping.ContainsKey(key))
                    {
                        int index = hotkeyMapping[key];
                        if (index >= 0 && index < processes.Count)
                        {
                            SwitchToProcess(processes[index]);
                        }
                    }
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
            File.WriteAllLines(ConfigFile, hotkeyMapping.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
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

        private void txtSendKeysHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu)
                return;

            sendKeysHotkey = e.KeyCode;
            txtSendKeysHotkey.Text = e.KeyCode.ToString();
            RegisterGlobalHotkeys();
        }

        private void SendKeysToAllProcesses()
{
    if (processes.Count == 0)
    {
        MessageBox.Show("Nenhum processo encontrado.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

    foreach (var process in processes)
    {
        if (process != null && !process.HasExited && process.MainWindowHandle != IntPtr.Zero)
        {
            
            ShowWindow(process.MainWindowHandle, SW_RESTORE);

            
            SwitchToProcess(process);

            
            System.Threading.Thread.Sleep(100);

            
            SendKeysToProcess();

            
            System.Threading.Thread.Sleep(200);
        }
    }
}

        private void SendKeysToProcess()
        {
            for (int i = 1; i <= 8; i++)
            {
                SendKeys.SendWait($"{{F{i}}}");
            }
        }
    }
} 