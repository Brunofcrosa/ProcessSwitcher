using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

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
        private const string TitlesFile = "titles.config";
        private Dictionary<int, string> customTitles = new Dictionary<int, string>();
        private HashSet<int> hiddenProcesses = new HashSet<int>();



        public Form1()
        {
            InitializeComponent();
            LoadHotkeys();
            LoadProcesses();
            RegisterGlobalHotkeys();
            LoadCustomTitles();
        }

        private void LoadProcesses()
        {
            processes = Process.GetProcessesByName("elementclient_64").ToList();
            processPanel.Controls.Clear();
            hotkeySelectors.Clear();

            var processHotkeys = hotkeyMapping
                .Where(kvp => processes.Any(p => p.Id == kvp.Value))
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            for (int index = 0; index < processes.Count; index++)
            {
                var process = processes[index];



                TextBox titleTextBox = new TextBox
                {
                    Width = 90,
                    Text = customTitles.ContainsKey(process.Id) ? customTitles[process.Id] : process.MainWindowTitle,
                    TextAlign = HorizontalAlignment.Center
                };

                titleTextBox.TextChanged += (sender, e) =>
                {
                    customTitles[process.Id] = titleTextBox.Text;
                    SaveCustomTitles();
                };

                RoundedTextBox hotkeyTextBox = new RoundedTextBox
                {
                    Width = 50,
                    BorderRadius = 15,
                    BorderColor = Color.Gray,
                    BackgroundColor = Color.White,
                    ReadOnly = true,
                    Text = processHotkeys.ContainsKey(process.Id) ? processHotkeys[process.Id].ToString() : string.Empty
                };

                hotkeyTextBox.Click += (sender, e) =>
                {
                    hotkeyTextBox.ReadOnly = false;
                    hotkeyTextBox.BorderColor = Color.Blue;
                };

                hotkeyTextBox.Leave += (sender, e) =>
                {
                    hotkeyTextBox.ReadOnly = true;
                    hotkeyTextBox.BorderColor = Color.Gray;
                };

                hotkeyTextBox.KeyDown += (sender, e) =>
                {
                    if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu)
                        return;

                    hotkeyTextBox.Text = e.KeyCode.ToString();
                    hotkeyMapping[e.KeyCode] = process.Id;
                    hotkeyTextBox.ReadOnly = true;
                    SaveHotkeys();
                    RegisterGlobalHotkeys();
                };

                hotkeySelectors[process.Id] = hotkeyTextBox;

                PictureBox trashIcon = new PictureBox
                {
                    Image = LoadEmbeddedImage("trash.png"),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width = 20,
                    Height = 20,
                    Cursor = Cursors.Hand
                };


                FlowLayoutPanel row = new FlowLayoutPanel
                {
                    Width = 450,
                    Height = 30,
                    Tag = process.Id
                };

                trashIcon.Click += (sender, e) =>
                {
                    hiddenProcesses.Add(process.Id);
                    processPanel.Controls.Remove(row);
                };

                PictureBox dragIcon = new PictureBox
                {
                    Image = LoadEmbeddedImage("drag_icon.ico"),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width = 20,
                    Height = 20,
                    Cursor = Cursors.Hand
                };


                row.Controls.Add(titleTextBox);
                row.Controls.Add(hotkeyTextBox);


                processPanel.Controls.Add(row);
                dragIcon.MouseDown += (sender, e) =>
                {
                    processPanel.DoDragDrop(row, DragDropEffects.Move);
                };

                CheckBox selectedCheckBox = new CheckBox
                {
                    Text = "",
                    AutoSize = true,
                    Checked = false // Todos os processos começam selecionados
                };

                selectedCheckBox.CheckedChanged += (sender, e) =>
                {
                    // Atualizar a lista de processos selecionados
                    if (selectedCheckBox.Checked)
                    {
                        // Marque o processo como selecionado
                        selectedProcesses.Add(process.Id);
                    }
                    else
                    {
                        // Remova o processo da lista de selecionados
                        selectedProcesses.Remove(process.Id);
                    }
                };

                row.Controls.Add(dragIcon);
                row.Controls.Add(titleTextBox);
                row.Controls.Add(hotkeyTextBox);
                row.Controls.Add(trashIcon);
                row.Controls.Add(selectedCheckBox);

                processPanel.Controls.Add(row);
            }
        }

        private int finalizerProcessId = -1; // Guarda o ID do processo finalizador
        private const string FinalizerFile = "finalizer.config"; // Arquivo para salvar a escolha
        private HashSet<int> selectedProcesses = new HashSet<int>();

        private void SendKeysToSelectedProcesses()
        {
            if (selectedProcesses.Count == 0)
            {
                MessageBox.Show("Nenhum processo selecionado.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<Process> orderedProcesses = new List<Process>();

            // Filtra os processos selecionados
            foreach (var process in processes)
            {
                if (selectedProcesses.Contains(process.Id) && !process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                {
                    orderedProcesses.Add(process);
                }
            }

            // Enviar teclas para os processos selecionados
            foreach (var process in orderedProcesses)
            {
                ShowWindow(process.MainWindowHandle, SW_RESTORE);
                SwitchToProcess(process);
                System.Threading.Thread.Sleep(100);

                SendKeysToProcess();
                System.Threading.Thread.Sleep(200);
            }
        }

        private void SendKeysToProcess()
        {
            for (int i = 1; i <= 8; i++)
            {
                SendKeys.SendWait($"{{F{i}}}");
            }
        }
        private void SaveFinalizerProcess()
        {
            File.WriteAllText(FinalizerFile, finalizerProcessId.ToString());
        }

        private void LoadFinalizerProcess()
        {
            if (File.Exists(FinalizerFile) && int.TryParse(File.ReadAllText(FinalizerFile), out int id))
            {
                finalizerProcessId = id;
            }
        }
        private void SaveCustomTitles()
        {
            File.WriteAllLines(TitlesFile, customTitles.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }

        private void LoadCustomTitles()
        {
            if (File.Exists(TitlesFile))
            {
                foreach (var line in File.ReadAllLines(TitlesFile))
                {
                    var parts = line.Split(':');
                    if (int.TryParse(parts[0], out int index) && parts.Length > 1)
                    {
                        customTitles[index] = parts[1];
                    }
                }
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
                    if (hotkeyMapping.TryGetValue(hotkeyMapping.FirstOrDefault(kvp => kvp.Key.GetHashCode() % 1000 == id).Key, out int processId))
                    {
                        var process = processes.FirstOrDefault(p => p.Id == processId);
                        if (process != null)
                        {
                            SwitchToProcess(process);
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
                    if (Enum.TryParse(parts[0], out Keys key) && int.TryParse(parts[1], out int processId))
                    {
                        hotkeyMapping[key] = processId;
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
            if (selectedProcesses.Count == 0)
            {
                MessageBox.Show("Nenhum processo selecionado.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<Process> orderedProcesses = new List<Process>();
            Process? firstProcess = null;

            foreach (var process in processes)
            {
                if (selectedProcesses.Contains(process.Id) && process != null && !process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                {
                    if (firstProcess == null)
                        firstProcess = process; // Guarda o primeiro processo da lista

                    orderedProcesses.Add(process);

                    if (process.Id == finalizerProcessId)
                    {

                        break; // Para de adicionar processos ao encontrar o finalizador
                    }
                }
            }

            // Enviar teclas para os processos selecionados na lista ordenada
            foreach (var process in orderedProcesses)
            {
                ShowWindow(process.MainWindowHandle, SW_RESTORE);
                SwitchToProcess(process);
                System.Threading.Thread.Sleep(100);

                SendKeysToProcess();
                System.Threading.Thread.Sleep(200);
            }

            // Voltar para o primeiro processo e enviar as teclas novamente
            if (firstProcess != null)
            {
                ShowWindow(firstProcess.MainWindowHandle, SW_RESTORE);
                SwitchToProcess(firstProcess);
                System.Threading.Thread.Sleep(100);

                SendKeysToProcess();
            }
        }





        private void ProcessPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(FlowLayoutPanel)) == true)
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void ProcessPanel_DragDrop(object sender, DragEventArgs e)
        {
            FlowLayoutPanel? draggedRow = e.Data?.GetData(typeof(FlowLayoutPanel)) as FlowLayoutPanel;
            if (draggedRow == null) return;

            if (draggedRow.Parent == processPanel)
            {
                int oldIndex = processPanel.Controls.GetChildIndex(draggedRow);

                int newIndex = processPanel.PointToClient(new Point(e.X, e.Y)).Y / draggedRow.Height;
                newIndex = newIndex < 0 ? 0 : newIndex;  // Garantir que o índice não seja negativo.


                if (newIndex >= processPanel.Controls.Count)
                {
                    newIndex = processPanel.Controls.Count - 1;
                }

                if (oldIndex != newIndex)
                {
                    processPanel.Controls.SetChildIndex(draggedRow, newIndex);
                    ReorderProcesses();
                }
            }
        }

        private void ReorderProcesses()
        {
            List<Process> reorderedList = new List<Process>();
            Dictionary<Keys, int> newHotkeyMapping = new Dictionary<Keys, int>();

            foreach (FlowLayoutPanel row in processPanel.Controls)
            {
                if (row.Tag is int processId)
                {
                    var process = processes.FirstOrDefault(p => p.Id == processId);
                    if (process != null)
                    {
                        reorderedList.Add(process);
                    }
                }
            }

            processes = reorderedList;


            foreach (var kvp in hotkeyMapping)
            {
                if (processes.Any(p => p.Id == kvp.Value))
                {
                    newHotkeyMapping[kvp.Key] = kvp.Value;
                }
            }

            hotkeyMapping = newHotkeyMapping;
            SaveHotkeys();
            RegisterGlobalHotkeys();
        }

        private Image LoadEmbeddedImage(string resourceName)
{
    var assembly = Assembly.GetExecutingAssembly();
    string resourcePath = $"ProcessSwitcher.Resources.{resourceName}";

    using (var stream = assembly.GetManifestResourceStream(resourcePath))
    {
        return stream != null ? Image.FromStream(stream) : new Bitmap(1, 1);
    }
}




    }
}