using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProcessSwitcher
{
    public partial class Form1 : Form
    {
        private const int SW_RESTORE = 9;
        private const int MaxProcesses = 10; 

        [DllImport("user32.dll")]
        public static extern int RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern int UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private Process?[] processes = new Process?[MaxProcesses]; 

        public Form1()
        {
            InitializeComponent();
            LoadProcesses(); 
            RegisterGlobalHotkeys(); 
        }

        private void RegisterGlobalHotkeys()
        {
            for (int i = 0; i < MaxProcesses; i++)
            {
                RegisterHotKey(this.Handle, i + 1, 0, (int)Keys.F1 + i);
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
                int hotkeyId = m.WParam.ToInt32();
                if (hotkeyId >= 1 && hotkeyId <= MaxProcesses)
                {
                    SwitchToProcess(processes[hotkeyId - 1]);
                }
            }
            base.WndProc(ref m);
        }

        private void SwitchToProcess(Process? process)
        {
            if (process != null && process.MainWindowHandle != IntPtr.Zero)
            {
                IntPtr hwnd = process.MainWindowHandle;
                ShowWindow(hwnd, SW_RESTORE);
                SetForegroundWindow(hwnd);
            }
            else
            {
                MessageBox.Show("Não foi possível alternar para o processo selecionado.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadProcesses()
        {
            var allProcesses = Process.GetProcessesByName("elementclient_64"); 
            int index = 0;

            foreach (var process in allProcesses)
            {
                if (index < MaxProcesses)
                {
                    processes[index] = process;
                    index++;
                }
                else
                {
                    break; 
                }
            }

            if (index < 2)
            {
                MessageBox.Show("Não foi possível encontrar dois ou mais processos válidos.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            
            comboBoxProcesses.Items.Clear();
            for (int i = 0; i < index; i++)
            {
                comboBoxProcesses.Items.Add($"elementclient {i + 1} - {processes[i]?.Id}");
            }
            if (comboBoxProcesses.Items.Count > 0)
            {
                comboBoxProcesses.SelectedIndex = 0;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterGlobalHotkeys();
        }

        private void btnSwitchProcess_Click(object sender, EventArgs e)
        {
            if (comboBoxProcesses.SelectedIndex >= 0 && comboBoxProcesses.SelectedIndex < MaxProcesses)
            {
                SwitchToProcess(processes[comboBoxProcesses.SelectedIndex]);
            }
        }

        private void btnSetHotkeys_Click(object sender, EventArgs e)
        {
            RegisterGlobalHotkeys();
        }

        private void comboBoxProcesses_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (e.KeyCode >= Keys.F1 && e.KeyCode <= Keys.F10)
            {
                int index = e.KeyCode - Keys.F1; 
                if (index >= 0 && index < processes.Length)
                {
                    SwitchToProcess(processes[index]);
                }
            }
        }

        private void btnRefreshList_Click(object sender, EventArgs e)
        {
            LoadProcesses(); 
            MessageBox.Show("Lista de processos atualizada.", "Atualização", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
