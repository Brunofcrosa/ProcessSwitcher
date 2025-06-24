using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Threading;

namespace ProcessSwitcher
{
    public partial class Form1 : Form
    {
        private const int SW_RESTORE = 9;
        private const int SW_MINIMIZE = 6;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_SHOWNA = 8;

        // DllImports para manipulação de janela e hotkeys
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern int UnregisterHotKey(IntPtr hWnd, int id);

        // DllImports e estruturas para SendInput
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        // DllImports para controle de foco avançado
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        // Constantes para SendInput
        private const int INPUT_KEYBOARD = 1;
        private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const int KEYEVENTF_UNICODE = 0x0004;
        private const int KEYEVENTF_SCANCODE = 0x0008;

        // Códigos de teclas virtuais para F1-F8
        private static readonly Dictionary<Keys, ushort> VKeyCodesMap = new Dictionary<Keys, ushort>
        {
            { Keys.F1, 0x70 }, { Keys.F2, 0x71 }, { Keys.F3, 0x72 }, { Keys.F4, 0x73 },
            { Keys.F5, 0x74 }, { Keys.F6, 0x75 }, { Keys.F7, 0x76 }, { Keys.F8, 0x77 },
            { Keys.F9, 0x78 }, { Keys.F10, 0x79 }, { Keys.F11, 0x7A }, { Keys.F12, 0x7B }
        };

        // Estruturas para SendInput
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public int type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private List<Process> processes = new List<Process>();
        
        private Dictionary<Keys, int> processHotkeyMapping = new Dictionary<Keys, int>();
        private Dictionary<int, Keys> registeredHotkeys = new Dictionary<int, Keys>();
        
        private Dictionary<int, TextBox> hotkeySelectors = new Dictionary<int, TextBox>();
        private Keys sendKeysHotkey = Keys.None;
        private int sendKeysHotkeyId = -1;
        private int nextHotkeyId = 1000;
        
        private const string TitlesFile = "titles.config";
        private Dictionary<int, string> customTitles = new Dictionary<int, string>();
        private HashSet<int> hiddenProcesses = new HashSet<int>();
        private int finalizerProcessId = -1;
        private const string FinalizerFile = "finalizer.config";

        private List<int> comboProcessOrder = new List<int>();
        private const string ComboFile = "combo.config";

        private bool shouldSwitchWindowForCombo = true;
        private const string SettingsFile = "settings.config";

        private DateTime lastComboExecutionTime = DateTime.MinValue;
        private readonly TimeSpan comboDebounceInterval = TimeSpan.FromMilliseconds(500);
        private CancellationTokenSource? comboCancellationTokenSource;

        private List<Keys> comboKeysToSend = new List<Keys>();
        private const string ComboKeysFile = "comboKeys.config";

        private bool isComboActive = false;

        public Form1()
        {
            InitializeComponent();
            LoadHotkeys();
            LoadCustomTitles();
            LoadFinalizerProcess();
            LoadComboProcesses();
            LoadSettings();
            LoadComboKeys();

            LoadProcesses();
            RegisterGlobalHotkeys();
            UpdateComboListBox();
            UpdateSettingsUI();
            InitializeComboKeysUI();
            UpdateComboHotkeyText();
        }

        private void LoadProcesses()
        {
            processes = Process.GetProcessesByName("elementClient").ToList();
            processPanel.Controls.Clear();
            hotkeySelectors.Clear();

            var currentProcessHotkeys = processHotkeyMapping
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
                    UpdateComboListBox();
                };

                RoundedTextBox hotkeyTextBox = new RoundedTextBox
                {
                    Width = 50,
                    BorderRadius = 15,
                    BorderColor = Color.Gray,
                    BackgroundColor = Color.White,
                    ReadOnly = true,
                    Text = currentProcessHotkeys.ContainsKey(process.Id) ? currentProcessHotkeys[process.Id].ToString() : string.Empty
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

                    if (hotkeySelectors.ContainsValue(hotkeyTextBox))
                    {
                        var existingEntry = processHotkeyMapping.FirstOrDefault(x => x.Value == process.Id);
                        if (existingEntry.Key != Keys.None)
                        {
                            processHotkeyMapping.Remove(existingEntry.Key);
                        }
                    }
                    if (processHotkeyMapping.ContainsKey(e.KeyCode))
                    {
                        processHotkeyMapping.Remove(e.KeyCode);
                    }
                    
                    hotkeyTextBox.Text = e.KeyCode.ToString();
                    processHotkeyMapping[e.KeyCode] = process.Id;
                    hotkeyTextBox.ReadOnly = true;
                    SaveHotkeys();
                    RegisterGlobalHotkeys();
                    e.SuppressKeyPress = true;
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
                    var hotkeyToRemove = processHotkeyMapping.FirstOrDefault(kvp => kvp.Value == process.Id);
                    if (hotkeyToRemove.Key != Keys.None)
                    {
                        processHotkeyMapping.Remove(hotkeyToRemove.Key);
                        SaveHotkeys();
                        RegisterGlobalHotkeys();
                    }
                    processPanel.Controls.Remove(row);
                    UpdateComboListBox();
                };

                PictureBox dragIcon = new PictureBox
                {
                    Image = LoadEmbeddedImage("drag_icon.ico"),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width = 20,
                    Height = 20,
                    Cursor = Cursors.Hand
                };

                CheckBox selectedCheckBox = new CheckBox
                {
                    Text = "",
                    AutoSize = true,
                    Checked = comboProcessOrder.Contains(process.Id)
                };

                selectedCheckBox.CheckedChanged += (sender, e) =>
                {
                    if (selectedCheckBox.Checked)
                    {
                        if (!comboProcessOrder.Contains(process.Id))
                        {
                            comboProcessOrder.Add(process.Id);
                        }
                    }
                    else
                    {
                        comboProcessOrder.Remove(process.Id);
                    }
                    SaveComboProcesses();
                    UpdateComboListBox();
                };

                row.Controls.Add(dragIcon);
                row.Controls.Add(titleTextBox);
                row.Controls.Add(hotkeyTextBox);
                row.Controls.Add(trashIcon);
                row.Controls.Add(selectedCheckBox);

                processPanel.Controls.Add(row);
            }
        }

        private async Task ExecuteComboAsync(CancellationToken cancellationToken)
        {
            isComboActive = true;
            UpdateComboHotkeyText();

            try
            {
                if (comboProcessOrder.Count == 0)
                {
                    MessageBox.Show("Nenhum processo selecionado para o combo.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboKeysToSend.Count == 0)
                {
                    MessageBox.Show("Nenhuma tecla configurada para o combo. Por favor, selecione as teclas.", "Erro de Configuração", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var currentProcessesSnapshot = Process.GetProcessesByName("elementClient").ToList();

                List<Process> orderedComboProcesses = new List<Process>();

                foreach (var processId in comboProcessOrder)
                {
                    var process = currentProcessesSnapshot.FirstOrDefault(p => p.Id == processId);
                    if (process != null && !process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                    {
                        orderedComboProcesses.Add(process);
                    }
                    else
                    {
                        Debug.WriteLine($"Processo com ID {processId} não encontrado, encerrado ou sem janela principal. Pulando no combo.");
                    }
                }

                if (!orderedComboProcesses.Any())
                {
                    MessageBox.Show("Nenhum processo ativo e válido encontrado para o combo.", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Process? firstProcessInCombo = orderedComboProcesses.FirstOrDefault();
                uint currentThreadId = GetCurrentThreadId();
                IntPtr originalForegroundWindow = GetForegroundWindow();

                foreach (var process in orderedComboProcesses)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine("Combo cancelado antes de terminar a iteração.");
                        return;
                    }

                    if (process == null || process.HasExited || process.MainWindowHandle == IntPtr.Zero)
                    {
                        Debug.WriteLine($"Processo {process?.Id} - {process?.ProcessName} tornou-se inválido durante o combo. Pulando.");
                        continue;
                    }

                    uint targetThreadId = 0; 
                    bool attachedThreadInput = false;

                    try
                    {
                        targetThreadId = GetWindowThreadProcessId(process.MainWindowHandle, out _);

                        if (currentThreadId != targetThreadId)
                        {
                            AttachThreadInput(currentThreadId, targetThreadId, true);
                            attachedThreadInput = true; 
                        }

                        if (shouldSwitchWindowForCombo)
                        {
                            // OPÇÃO: Trocar para a janela (comportamento padrão)
                            ShowWindow(process.MainWindowHandle, SW_RESTORE); 
                            SetForegroundWindow(process.MainWindowHandle);
                            await Task.Delay(300, cancellationToken); 
                        }
                        else 
                        {
                            // OPÇÃO: Tentar combo em segundo plano (sem piscar/roubar foco)
                            // Para Perfect World, que permite input em segundo plano, não fazemos ShowWindow/SetForegroundWindow.
                            // Apenas um pequeno delay para estabilidade antes de SendInput.
                            await Task.Delay(100, cancellationToken); 
                            
                            // Se a janela estiver minimizada, ShowWindow(SW_RESTORE) seria necessário para torná-la visível.
                            // Mas se a intenção é que não roube foco, não podemos usar SetForegroundWindow.
                            // Para PW, que permite, confiamos que AttachThreadInput + SendInput é suficiente.
                        }

                        SendKeysToProcess(comboKeysToSend); 
                        
                        await Task.Delay(150, cancellationToken); 
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("Combo cancelado durante a interação com o processo.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erro ao interagir com o processo {process.Id} - {process.ProcessName}: {ex.Message}");
                    }
                    finally
                    {
                        if (attachedThreadInput && currentThreadId != targetThreadId)
                        {
                            AttachThreadInput(currentThreadId, targetThreadId, false);
                        }
                    }
                }
                
                Process? targetProcessAfterCombo = null;
                if (finalizerProcessId != -1)
                {
                    targetProcessAfterCombo = currentProcessesSnapshot.FirstOrDefault(p => p.Id == finalizerProcessId);
                }
                else if (firstProcessInCombo != null)
                {
                    targetProcessAfterCombo = firstProcessInCombo;
                }

                if (targetProcessAfterCombo != null && !targetProcessAfterCombo.HasExited && targetProcessAfterCombo.MainWindowHandle != IntPtr.Zero)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine("Combo cancelado antes de retornar ao finalizador/primeiro processo.");
                        return;
                    }

                    uint targetThreadId = 0; 
                    bool attachedThreadInput = false;

                    try
                    {
                        targetThreadId = GetWindowThreadProcessId(targetProcessAfterCombo.MainWindowHandle, out _);
                        if (currentThreadId != targetThreadId)
                        {
                            AttachThreadInput(currentThreadId, targetThreadId, true);
                            attachedThreadInput = true;
                        }

                        if (shouldSwitchWindowForCombo)
                        {
                            ShowWindow(targetProcessAfterCombo.MainWindowHandle, SW_RESTORE);
                            SetForegroundWindow(targetProcessAfterCombo.MainWindowHandle);
                            await Task.Delay(300, cancellationToken);
                        }
                        else
                        {
                            // Comportamento em segundo plano para o finalizador/retorno
                            await Task.Delay(100, cancellationToken);
                        }
                        SendKeysToProcess(comboKeysToSend);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("Combo cancelado durante a interação com o finalizador/primeiro processo.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erro ao interagir com o processo finalizador/primeiro processo ({targetProcessAfterCombo.Id}): {ex.Message}");
                    }
                    finally
                    {
                        if (attachedThreadInput && currentThreadId != targetThreadId)
                        {
                            AttachThreadInput(currentThreadId, targetThreadId, false);
                        }
                    }
                }

                // Ação final: tenta retornar à janela original que estava em foco,
                // SOMENTE se a opção de NÃO trocar de janela foi selecionada
                // e o foco ainda não retornou naturalmente.
                if (!shouldSwitchWindowForCombo && originalForegroundWindow != IntPtr.Zero && GetForegroundWindow() != originalForegroundWindow)
                {
                     try
                    {
                        uint originalThreadId = GetWindowThreadProcessId(originalForegroundWindow, out _);
                        uint currentThread = GetCurrentThreadId();

                        if (currentThread != originalThreadId)
                        {
                            AttachThreadInput(currentThread, originalThreadId, true);
                        }

                        SetForegroundWindow(originalForegroundWindow);
                        await Task.Delay(100, cancellationToken);

                        if (currentThread != originalThreadId)
                        {
                            AttachThreadInput(currentThread, originalThreadId, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erro ao tentar restaurar foco original após combo: {ex.Message}");
                    }
                }
            }
            finally
            {
                // Garante que o estado é atualizado mesmo que a Task seja cancelada
                isComboActive = false;
                UpdateComboHotkeyText();
            }
        }

        private void SendKeysToProcess(List<Keys> keysToSend)
        {
            if (keysToSend == null || keysToSend.Count == 0)
            {
                Debug.WriteLine("Nenhuma tecla para enviar no combo.");
                return;
            }

            foreach (Keys key in keysToSend)
            {
                if (VKeyCodesMap.TryGetValue(key, out ushort virtualKey))
                {
                    INPUT[] inputsPress = new INPUT[1];
                    inputsPress[0].type = INPUT_KEYBOARD;
                    inputsPress[0].U.ki.wVk = virtualKey;
                    inputsPress[0].U.ki.dwFlags = 0; // Key down
                    SendInput(1, inputsPress, Marshal.SizeOf(typeof(INPUT)));

                    System.Threading.Thread.Sleep(50); // Delay between key down and key up

                    INPUT[] inputsRelease = new INPUT[1];
                    inputsRelease[0].type = INPUT_KEYBOARD;
                    inputsRelease[0].U.ki.wVk = virtualKey;
                    inputsRelease[0].U.ki.dwFlags = KEYEVENTF_KEYUP; // Key up
                    SendInput(1, inputsRelease, Marshal.SizeOf(typeof(INPUT)));

                    System.Threading.Thread.Sleep(50); // Delay between keys
                }
                else
                {
                    Debug.WriteLine($"Tecla não mapeada para SendInput: {key}.");
                }
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
                    if (int.TryParse(parts[0], out int id) && parts.Length > 1)
                    {
                        customTitles[id] = parts[1];
                    }
                }
            }
        }

        private void SaveComboProcesses()
        {
            File.WriteAllLines(ComboFile, comboProcessOrder.Select(id => id.ToString()));
        }

        private void LoadComboProcesses()
        {
            comboProcessOrder.Clear();
            if (File.Exists(ComboFile))
            {
                foreach (var line in File.ReadAllLines(ComboFile))
                {
                    if (int.TryParse(line, out int processId))
                    {
                        comboProcessOrder.Add(processId);
                    }
                }
            }
        }

        private void SaveComboKeys()
        {
            try
            {
                File.WriteAllLines(ComboKeysFile, comboKeysToSend.Select(k => k.ToString()));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar teclas do combo: {ex.Message}", "Erro de Configuração", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadComboKeys()
        {
            comboKeysToSend.Clear();
            if (File.Exists(ComboKeysFile))
            {
                try
                {
                    foreach (var line in File.ReadAllLines(ComboKeysFile))
                    {
                        if (Enum.TryParse(line, out Keys key))
                        {
                            comboKeysToSend.Add(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao carregar teclas do combo: {ex.Message}. Usando padrão.", "Erro de Configuração", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void SaveSettings()
        {
            try
            {
                File.WriteAllText(SettingsFile, shouldSwitchWindowForCombo.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar configurações: {ex.Message}", "Erro de Configuração", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string settingValue = File.ReadAllText(SettingsFile);
                    if (bool.TryParse(settingValue, out bool loadedValue))
                    {
                        shouldSwitchWindowForCombo = loadedValue;
                    }
                    else
                    {
                        shouldSwitchWindowForCombo = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao carregar configurações: {ex.Message}. Usando padrão.", "Erro de Configuração", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    shouldSwitchWindowForCombo = true;
                }
            }
            else
            {
                shouldSwitchWindowForCombo = true;
            }
        }

        private void UpdateSettingsUI()
        {
            if (chkSwitchWindow != null)
            {
                chkSwitchWindow.Checked = shouldSwitchWindowForCombo;
            }
        }

        private void RegisterGlobalHotkeys()
        {
            UnregisterGlobalHotkeys();

            registeredHotkeys.Clear();
            nextHotkeyId = 1000;

            foreach (var kvp in processHotkeyMapping)
            {
                if (!processes.Any(p => p.Id == kvp.Value))
                {
                    continue;
                }

                int id = nextHotkeyId++;
                if (RegisterHotKey(this.Handle, id, 0, (int)kvp.Key) == 0)
                {
                    Debug.WriteLine($"Falha ao registrar hotkey: {kvp.Key} para o ProcessId: {kvp.Value}. ID: {id}");
                    MessageBox.Show($"Falha ao registrar hotkey para {kvp.Key}. Pode estar em uso por outro programa.", "Erro de Hotkey", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    registeredHotkeys[id] = kvp.Key;
                }
            }

            if (sendKeysHotkey != Keys.None)
            {
                sendKeysHotkeyId = nextHotkeyId++;
                if (RegisterHotKey(this.Handle, sendKeysHotkeyId, 0, (int)sendKeysHotkey) == 0)
                {
                    Debug.WriteLine($"Falha ao registrar hotkey de combo: {sendKeysHotkey}. ID: {sendKeysHotkeyId}");
                    MessageBox.Show($"Falha ao registrar hotkey de combo para {sendKeysHotkey}. Pode estar em uso por outro programa.", "Erro de Hotkey de Combo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void UnregisterGlobalHotkeys()
        {
            foreach (var id in registeredHotkeys.Keys)
            {
                UnregisterHotKey(this.Handle, id);
            }
            if (sendKeysHotkeyId != -1)
            {
                UnregisterHotKey(this.Handle, sendKeysHotkeyId);
            }
            registeredHotkeys.Clear();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();

                if (hotkeyId == sendKeysHotkeyId)
                {
                    if (isComboActive)
                    {
                        comboCancellationTokenSource?.Cancel();
                        Debug.WriteLine("Combo cancelado manualmente.");
                    }
                    else
                    {
                        if ((DateTime.Now - lastComboExecutionTime) < comboDebounceInterval)
                        {
                            Debug.WriteLine("Combo hotkey acionado muito rapidamente. Ignorando.");
                            return;
                        }

                        comboCancellationTokenSource?.Dispose();
                        comboCancellationTokenSource = new CancellationTokenSource();
                        CancellationToken cancellationToken = comboCancellationTokenSource.Token;

                        lastComboExecutionTime = DateTime.Now;

                        Task.Run(async () => await ExecuteComboAsync(cancellationToken), cancellationToken);
                    }
                }
                else if (registeredHotkeys.TryGetValue(hotkeyId, out Keys hotkey))
                {
                    if (processHotkeyMapping.TryGetValue(hotkey, out int processId))
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

        // Atualiza o texto do Hotkey (TextBox) para refletir o estado de ativo/inativo
        private void UpdateComboHotkeyText()
        {
            if (txtSendKeysHotkey.InvokeRequired)
            {
                txtSendKeysHotkey.Invoke(new Action(UpdateComboHotkeyText));
                return;
            }

            if (isComboActive)
            {
                txtSendKeysHotkey.Text = "PARAR COMBO";
                if (txtSendKeysHotkey is RoundedTextBox rtb)
                {
                    rtb.BorderColor = Color.Red;
                }
            }
            else
            {
                txtSendKeysHotkey.Text = sendKeysHotkey != Keys.None ? sendKeysHotkey.ToString() : "Configurar Hotkey";
                if (txtSendKeysHotkey is RoundedTextBox rtb)
                {
                    rtb.BorderColor = Color.Black;
                }
            }
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
            File.WriteAllLines("hotkeys.config", processHotkeyMapping.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }

        private void LoadHotkeys()
        {
            processHotkeyMapping.Clear();

            if (File.Exists("hotkeys.config"))
            {
                foreach (var line in File.ReadAllLines("hotkeys.config"))
                {
                    var parts = line.Split(':');
                    if (Enum.TryParse(parts[0], out Keys key) && int.TryParse(parts[1], out int processId))
                    {
                        processHotkeyMapping[key] = processId;
                    }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterGlobalHotkeys();
            SaveSettings();
            SaveComboKeys();
            comboCancellationTokenSource?.Dispose();
        }

        private void btnUpdateHotkeys_Click(object sender, EventArgs e)
        {
            SaveHotkeys();
            RegisterGlobalHotkeys();
            MessageBox.Show("Atalhos atualizados com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnRefreshProcesses_Click(object sender, EventArgs e)
        {
            LoadProcesses();
            UpdateComboListBox();
            InitializeComboKeysUI();
            MessageBox.Show("Lista de processos atualizada.", "Atualização", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void txtSendKeysHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu)
                return;

            sendKeysHotkey = e.KeyCode;
            txtSendKeysHotkey.Text = e.KeyCode.ToString();
            RegisterGlobalHotkeys();
            e.SuppressKeyPress = true;
            UpdateComboHotkeyText();
        }

        private void chkSwitchWindow_CheckedChanged(object sender, EventArgs e)
        {
            shouldSwitchWindowForCombo = chkSwitchWindow.Checked;
            SaveSettings();
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
                newIndex = newIndex < 0 ? 0 : newIndex;


                if (newIndex >= processPanel.Controls.Count)
                {
                    newIndex = processPanel.Controls.Count - 1;
                }

                if (oldIndex != newIndex)
                {
                    processPanel.Controls.SetChildIndex(draggedRow, newIndex);
                    ReorderProcesses();
                    UpdateComboListBox();
                }
            }
        }

        private void ReorderProcesses()
        {
            List<Process> reorderedList = new List<Process>();

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

            var newProcessHotkeyMapping = new Dictionary<Keys, int>();
            foreach (var kvp in processHotkeyMapping)
            {
                if (processes.Any(p => p.Id == kvp.Value))
                {
                    newProcessHotkeyMapping[kvp.Key] = kvp.Value;
                }
            }
            processHotkeyMapping = newProcessHotkeyMapping;

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
        
        private void UpdateComboListBox()
        {
            if (comboListBox != null)
            {
                comboListBox.Items.Clear();
                foreach (var processId in comboProcessOrder)
                {
                    var process = processes.FirstOrDefault(p => p.Id == processId);
                    if (process != null)
                    {
                        string title = customTitles.ContainsKey(process.Id) ? customTitles[process.Id] : process.MainWindowTitle;
                        comboListBox.Items.Add(new ProcessItem(process.Id, title));
                    }
                }
            }
        }

        private void InitializeComboKeysUI()
        {
            if (chkListBoxComboKeys == null) return;

            chkListBoxComboKeys.ItemCheck -= ChkListBoxComboKeys_ItemCheck;
            
            chkListBoxComboKeys.Items.Clear();
            for (int i = 1; i <= 12; i++)
            {
                Keys fKey = (Keys)(Keys.F1 + i - 1);
                bool isChecked = comboKeysToSend.Contains(fKey);
                chkListBoxComboKeys.Items.Add(fKey.ToString(), isChecked);
            }

            chkListBoxComboKeys.ItemCheck += ChkListBoxComboKeys_ItemCheck;
        }

        private void ChkListBoxComboKeys_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            Keys changedKey = (Keys)Enum.Parse(typeof(Keys), chkListBoxComboKeys.Items[e.Index].ToString()!);
            if (e.NewValue == CheckState.Checked)
            {
                if (!comboKeysToSend.Contains(changedKey))
                {
                    comboKeysToSend.Add(changedKey);
                }
            }
            else
            {
                comboKeysToSend.Remove(changedKey);
            }
            SaveComboKeys();
        }

        private void MoveComboProcessUp()
        {
            if (comboListBox != null && comboListBox.SelectedItem is ProcessItem selectedItem)
            {
                int selectedIndex = comboListBox.SelectedIndex;
                if (selectedIndex > 0)
                {
                    int processIdToMove = selectedItem.ProcessId;
                    comboProcessOrder.RemoveAt(selectedIndex);
                    comboProcessOrder.Insert(selectedIndex - 1, processIdToMove);
                    SaveComboProcesses();
                    UpdateComboListBox();
                    comboListBox.SelectedIndex = selectedIndex - 1;
                }
            }
        }

        private void MoveComboProcessDown()
        {
            if (comboListBox != null && comboListBox.SelectedItem is ProcessItem selectedItem)
            {
                int selectedIndex = comboListBox.SelectedIndex;
                if (selectedIndex < comboListBox.Items.Count - 1)
                {
                    int processIdToMove = selectedItem.ProcessId;
                    comboProcessOrder.RemoveAt(selectedIndex);
                    comboProcessOrder.Insert(selectedIndex + 1, processIdToMove);
                    SaveComboProcesses();
                    UpdateComboListBox();
                    comboListBox.SelectedIndex = selectedIndex + 1;
                }
            }
        }

        private class ProcessItem
        {
            public int ProcessId { get; set; }
            public string DisplayName { get; set; }

            public ProcessItem(int processId, string displayName)
            {
                ProcessId = processId;
                DisplayName = displayName;
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }
    }
}