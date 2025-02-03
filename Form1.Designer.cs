namespace ProcessSwitcher
{
    partial class Form1
    {
        private System.Windows.Forms.FlowLayoutPanel processPanel;
        private System.Windows.Forms.Button btnUpdateHotkeys;
        private System.Windows.Forms.Button btnRefreshProcesses;

        private void InitializeComponent()
        {
            this.processPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnUpdateHotkeys = new System.Windows.Forms.Button();
            this.btnRefreshProcesses = new System.Windows.Forms.Button(); 

           
            this.processPanel.AutoScroll = true;
            this.processPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.processPanel.Location = new System.Drawing.Point(10, 10);
            this.processPanel.Size = new System.Drawing.Size(280, 350);

           
            this.btnRefreshProcesses.Location = new System.Drawing.Point(80, 380);
            this.btnRefreshProcesses.Size = new System.Drawing.Size(120, 30);
            this.btnRefreshProcesses.Text = "Atualizar Processos";
            this.btnRefreshProcesses.Click += new System.EventHandler(this.btnRefreshProcesses_Click);

            
            this.btnUpdateHotkeys.Location = new System.Drawing.Point(80, 420);
            this.btnUpdateHotkeys.Size = new System.Drawing.Size(120, 30);
            this.btnUpdateHotkeys.Text = "Atualizar Atalhos";
            this.btnUpdateHotkeys.Click += new System.EventHandler(this.btnUpdateHotkeys_Click);

            
            this.ClientSize = new System.Drawing.Size(300, 460);
            this.Controls.Add(this.processPanel);
            this.Controls.Add(this.btnRefreshProcesses); 
            this.Controls.Add(this.btnUpdateHotkeys);
            this.Text = "Process Switcher";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        }
    }
}
