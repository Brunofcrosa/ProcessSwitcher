using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProcessSwitcher
{
    public partial class Form1 : Form
    {
        private const int MOD_ALT = 0x0001; // Tecla ALT
        private const int MOD_CONTROL = 0x0002; // Tecla CTRL
        private const int MOD_SHIFT = 0x0004; // Tecla SHIFT
        private const int MOD_WIN = 0x0008; // Tecla WINDOWS

        [DllImport("user32.dll")]
        public static extern int RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern int UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        const int SW_RESTORE = 9;

        private Process? process1; // Agora é anulável
        private Process? process2; // Agora é anulável

        private const int HotkeyF1 = 1;
        private const int HotkeyF2 = 2;

        public Form1()
        {
            InitializeComponent();
            LoadProcesses(); // Carrega os processos
            RegisterGlobalHotkeys(); // Registra os atalhos globais
        }

        private void RegisterGlobalHotkeys()
        {
            RegisterHotKey(this.Handle, HotkeyF1, 0, (int)Keys.F1); // F1
            RegisterHotKey(this.Handle, HotkeyF2, 0, (int)Keys.F2); // F2
        }

        private void UnregisterGlobalHotkeys()
        {
            UnregisterHotKey(this.Handle, HotkeyF1); // F1
            UnregisterHotKey(this.Handle, HotkeyF2); // F2
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                if (hotkeyId == HotkeyF1) // F1
                {
                    SwitchToProcess(process1);
                }
                else if (hotkeyId == HotkeyF2) // F2
                {
                    SwitchToProcess(process2);
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
            var allProcesses = Process.GetProcesses();
            foreach (var process in allProcesses)
            {
                if (process.ProcessName.Contains("elementclient")) // Ajuste para o nome do processo desejado
                {
                    if (process1 == null)
                    {
                        process1 = process; // Primeiro processo para F1
                    }
                    else if (process2 == null)
                    {
                        process2 = process; // Segundo processo para F2
                    }

                    if (process1 != null && process2 != null)
                    {
                        break; // Já encontrou dois processos
                    }
                }
            }

            if (process1 == null || process2 == null)
            {
                MessageBox.Show("Não foi possível encontrar dois processos válidos.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterGlobalHotkeys();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Lógica para quando uma tecla é pressionada
        }

        private void comboBoxProcesses_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Lógica para quando o processo for selecionado no ComboBox
        }

        private void btnSwitchProcess_Click(object sender, EventArgs e)
        {
            // Lógica para alternar entre os processos
            SwitchToProcess(process1); // Ou process2, conforme a lógica desejada.
        }

        private void btnSetHotkeys_Click(object sender, EventArgs e)
        {
            // Lógica para configurar os atalhos de teclado
        }
    }
}
