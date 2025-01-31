namespace ProcessSwitcher
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ComboBox comboBoxProcesses;
        private System.Windows.Forms.Button btnSwitchProcess;
        private System.Windows.Forms.Button btnSetHotkeys;
        private System.Windows.Forms.Button btnRefreshList;
        private System.Windows.Forms.Label lblProcessCount;
        private System.Windows.Forms.Label lblSelectProcess;

        private void InitializeComponent()
        {
            this.comboBoxProcesses = new System.Windows.Forms.ComboBox();
            this.btnSwitchProcess = new System.Windows.Forms.Button();
            this.btnSetHotkeys = new System.Windows.Forms.Button();
            this.btnRefreshList = new System.Windows.Forms.Button();
            this.lblProcessCount = new System.Windows.Forms.Label();
            this.lblSelectProcess = new System.Windows.Forms.Label();

            // ComboBox para selecionar o processo
            this.comboBoxProcesses.FormattingEnabled = true;
            this.comboBoxProcesses.Location = new System.Drawing.Point(50, 30);
            this.comboBoxProcesses.Name = "comboBoxProcesses";
            this.comboBoxProcesses.Size = new System.Drawing.Size(200, 21);
            this.comboBoxProcesses.TabIndex = 0;
            this.comboBoxProcesses.SelectedIndexChanged += new System.EventHandler(this.comboBoxProcesses_SelectedIndexChanged);

            // Botão para alternar processo
            this.btnSwitchProcess.Location = new System.Drawing.Point(100, 100);
            this.btnSwitchProcess.Name = "btnSwitchProcess";
            this.btnSwitchProcess.Size = new System.Drawing.Size(150, 50);
            this.btnSwitchProcess.TabIndex = 1;
            this.btnSwitchProcess.Text = "Alternar Processo";
            this.btnSwitchProcess.UseVisualStyleBackColor = true;
            this.btnSwitchProcess.Click += new System.EventHandler(this.btnSwitchProcess_Click);

            // Botão para definir os atalhos
            this.btnSetHotkeys.Location = new System.Drawing.Point(160, 150);
            this.btnSetHotkeys.Name = "btnSetHotkeys";
            this.btnSetHotkeys.Size = new System.Drawing.Size(100, 23);
            this.btnSetHotkeys.TabIndex = 2;
            this.btnSetHotkeys.Text = "Definir Atalhos";
            this.btnSetHotkeys.UseVisualStyleBackColor = true;
            this.btnSetHotkeys.Click += new System.EventHandler(this.btnSetHotkeys_Click);

            // Botão para atualizar a lista de processos
            this.btnRefreshList.Location = new System.Drawing.Point(50, 70);
            this.btnRefreshList.Name = "btnRefreshList";
            this.btnRefreshList.Size = new System.Drawing.Size(150, 23);
            this.btnRefreshList.TabIndex = 5;
            this.btnRefreshList.Text = "Atualizar Lista";
            this.btnRefreshList.UseVisualStyleBackColor = true;
            this.btnRefreshList.Click += new System.EventHandler(this.btnRefreshList_Click);

            // Rótulo para indicar o número de processos
            this.lblProcessCount.Location = new System.Drawing.Point(50, 180);
            this.lblProcessCount.Name = "lblProcessCount";
            this.lblProcessCount.Size = new System.Drawing.Size(200, 20);
            this.lblProcessCount.TabIndex = 3;
            this.lblProcessCount.Text = "Defina os atalhos abaixo:";

            // Rótulo para o ComboBox
            this.lblSelectProcess.Location = new System.Drawing.Point(50, 10);
            this.lblSelectProcess.Name = "lblSelectProcess";
            this.lblSelectProcess.Size = new System.Drawing.Size(200, 20);
            this.lblSelectProcess.TabIndex = 4;
            this.lblSelectProcess.Text = "Selecione um Processo:";

            // Configuração do formulário
            this.ClientSize = new System.Drawing.Size(300, 500);
            this.Controls.Add(this.comboBoxProcesses);
            this.Controls.Add(this.btnSwitchProcess);
            this.Controls.Add(this.btnSetHotkeys);
            this.Controls.Add(this.btnRefreshList);
            this.Controls.Add(this.lblProcessCount);
            this.Controls.Add(this.lblSelectProcess);
            this.Name = "Form1";
            this.Text = "Process Switcher";
            this.KeyPreview = true;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
