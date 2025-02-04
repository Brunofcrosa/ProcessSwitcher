namespace ProcessSwitcher
{
    partial class Form1
    {
        private System.Windows.Forms.FlowLayoutPanel processPanel;
        private System.Windows.Forms.Button btnUpdateHotkeys;
        private System.Windows.Forms.Button btnRefreshProcesses;
        private System.Windows.Forms.Button btnSendKeys;
        private System.Windows.Forms.TextBox txtSendKeysHotkey;
        private System.Windows.Forms.Label lblSendKeysHotkey;

        private void InitializeComponent()
        {
            
            this.processPanel = new System.Windows.Forms.FlowLayoutPanel
            {
                AutoScroll = true,
                FlowDirection = System.Windows.Forms.FlowDirection.TopDown,
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(280, 350),
                BackColor = Color.LightGray,
                Padding = new Padding(10)
            };

            
            this.btnRefreshProcesses = CreateButton("Atualizar Processos", 10, 400, () => btnRefreshProcesses_Click(null, null));
            this.btnUpdateHotkeys = CreateButton("Atualizar Atalhos", 140, 400, () => btnUpdateHotkeys_Click(null, null));
            

            
            this.lblSendKeysHotkey = new System.Windows.Forms.Label
            {
                Text = "Tecla para enviar F1-F8:",
                Location = new System.Drawing.Point(10, 370),
                AutoSize = true
            };

            this.txtSendKeysHotkey = new System.Windows.Forms.TextBox
            {
                Location = new System.Drawing.Point(160, 370),
                Size = new System.Drawing.Size(120, 20),
                ReadOnly = true
            };
            this.txtSendKeysHotkey.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSendKeysHotkey_KeyDown);

            
            this.ClientSize = new System.Drawing.Size(300, 450);
            this.Controls.Add(this.processPanel);
            this.Controls.Add(this.btnRefreshProcesses);
            this.Controls.Add(this.btnUpdateHotkeys);
            this.Controls.Add(this.btnSendKeys);
            this.Controls.Add(this.lblSendKeysHotkey);
            this.Controls.Add(this.txtSendKeysHotkey);

            
            this.Text = "Process Switcher by Bruno Fetzer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        
        private Button CreateButton(string text, int x, int y, Action clickHandler)
        {
            var button = new Button
            {
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(120, 30),
                Text = text,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };

            button.MouseEnter += (sender, e) => button.BackColor = Color.MediumBlue;
            button.MouseLeave += (sender, e) => button.BackColor = Color.DodgerBlue;
            button.Click += (sender, e) => clickHandler();

            return button;
        }
    }
}