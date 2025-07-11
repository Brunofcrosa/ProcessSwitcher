﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Reflection;

namespace ProcessSwitcher
{
    partial class Form1 : Form
    {
        // Declarações de TODOS os controles UI.
        // É CRUCIAL que cada variável seja declarada APENAS UMA VEZ AQUI.
        private FlowLayoutPanel processPanel;
        private Button btnUpdateHotkeys;
        private Button btnRefreshProcesses;
        // CORREÇÃO: Declarar txtSendKeysHotkey como RoundedTextBox
        private RoundedTextBox txtSendKeysHotkey;
        private Label lblSendKeysHotkey;
        private Label copyright;

        private ListBox comboListBox;
        private Button btnMoveUp;
        private Button btnMoveDown;
        private Label lblComboProcesses;
        private CheckBox chkSwitchWindow;

        private CheckedListBox chkListBoxComboKeys;
        private Label lblComboKeys;

        private void InitializeComponent()
        {
            this.processPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Location = new Point(10, 10),
                Size = new Size(250, 350),
                BackColor = Color.LightGray,
                Padding = new Padding(5)
            };

            this.btnRefreshProcesses = CreateButton("", 10, 400, () => btnRefreshProcesses_Click(null, null), "refresh.ico");
            this.btnUpdateHotkeys = CreateButton("", 140, 400, () => btnUpdateHotkeys_Click(null, null), "check.ico");

            Label lblBelowButtons = new Label
            {
                Text = "Atualizar processos",
                Location = new Point(16, 450),
                AutoSize = true
            };

            Label lblBelowButtons2 = new Label
            {
                Text = "Confirmar teclas",
                Location = new Point(155, 450),
                AutoSize = true
            };
            this.lblSendKeysHotkey = new Label
            {
                Text = "Acionar Combo (F1 até F8):",
                Location = new Point(10, 370),
                AutoSize = true
            };

            this.copyright = new Label
            {
                Text = "Brunu",
                Location = new Point(221, 345),
                AutoSize = true,
                BackColor = Color.LightGray,
            };

            // Inicialização do txtSendKeysHotkey como RoundedTextBox
            this.txtSendKeysHotkey = new RoundedTextBox
            {
                Location = new Point(160, 370),
                Size = new Size(100, 30), 
                ReadOnly = true,
                TextAlign = HorizontalAlignment.Center,
                Cursor = Cursors.Hand,
                BorderRadius = 15,
                BorderColor = Color.Black 
            };

            this.txtSendKeysHotkey.KeyDown += (sender, e) =>
            {
                this.txtSendKeysHotkey.Text = e.KeyCode.ToString();
                this.ActiveControl = null;
            };

            this.txtSendKeysHotkey.KeyDown += new KeyEventHandler(this.txtSendKeysHotkey_KeyDown);

            try
            {
                this.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("ProcessSwitcher.Resources.pwicon.ico"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar o ícone: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            processPanel.AllowDrop = true;
            processPanel.DragEnter += ProcessPanel_DragEnter;
            processPanel.DragDrop += ProcessPanel_DragDrop;
            
            // Inicialização dos componentes do combo de processos
            this.lblComboProcesses = new Label
            {
                Text = "Processos no Combo (ordem de execução):",
                Location = new Point(270, 10),
                AutoSize = true
            };

            this.comboListBox = new ListBox
            {
                Location = new Point(270, 30),
                Size = new Size(180, 200),
                SelectionMode = SelectionMode.One
            };

            this.btnMoveUp = CreateButton("Mover Acima", 270, 240, MoveComboProcessUp);
            this.btnMoveUp.Size = new Size(85, 30);

            this.btnMoveDown = CreateButton("Mover Abaixo", 365, 240, MoveComboProcessDown);
            this.btnMoveDown.Size = new Size(85, 30);

            this.chkSwitchWindow = new CheckBox
            {
                Text = "Trocar para a janela ao Combar",
                Location = new Point(270, 280),
                AutoSize = true,
                Checked = true
            };
            this.chkSwitchWindow.CheckedChanged += new EventHandler(this.chkSwitchWindow_CheckedChanged);

            // Inicialização dos NOVOS componentes para seleção de teclas do combo
            this.lblComboKeys = new Label
            {
                Text = "Teclas do Combo:",
                Location = new Point(460, 10),
                AutoSize = true
            };

            this.chkListBoxComboKeys = new CheckedListBox
            {
                Location = new Point(460, 30),
                Size = new Size(100, 200),
                CheckOnClick = true
            };

            this.Controls.Add(this.copyright);
            this.Controls.Add(this.processPanel);
            this.Controls.Add(this.btnRefreshProcesses);
            this.Controls.Add(this.btnUpdateHotkeys);
            this.Controls.Add(lblBelowButtons); 
            this.Controls.Add(lblBelowButtons2); 
            this.Controls.Add(this.lblSendKeysHotkey);
            this.Controls.Add(this.txtSendKeysHotkey);
            this.Controls.Add(this.lblComboProcesses);
            this.Controls.Add(this.comboListBox);
            this.Controls.Add(this.btnMoveUp);
            this.Controls.Add(this.btnMoveDown);
            this.Controls.Add(this.chkSwitchWindow);
            this.Controls.Add(this.lblComboKeys);
            this.Controls.Add(this.chkListBoxComboKeys);
           
            this.ClientSize = new Size(570, 470); 
            this.Text = "Process Switcher";
            this.FormClosing += new FormClosingEventHandler(this.Form1_FormClosing);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen; 
        }

        private Button CreateButton(string text, int x, int y, Action clickHandler, string iconName = null)
        {
            var button = new RoundedButton
            {
                Location = new Point(x, y),
                Size = new Size(120, 50),
                Text = text,
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.BottomCenter,
                ImageAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Padding = new Padding(5)
            };

            if (!string.IsNullOrEmpty(iconName))
            {
                Image originalImage = LoadEmbeddedImage(iconName);
                if (originalImage != null)
                {
                    Image resizedImage = new Bitmap(originalImage, new Size(35, 35));
                    button.Image = resizedImage;
                }
            }

            button.MouseEnter += (sender, e) => button.BackColor = Color.LightGray;
            button.MouseLeave += (sender, e) => button.BackColor = Color.White;
            button.Click += (sender, e) => clickHandler?.Invoke();

            return button;
        }

        class RoundedButton : Button
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                GraphicsPath path = new GraphicsPath();
                int radius = 20;

                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(Width - radius, 0, radius, radius, 270, 90);
                path.AddArc(Width - radius, Height - radius, radius, radius, 0, 90);
                path.AddArc(0, Height - radius, radius, radius, 90, 90);
                path.CloseFigure();

                this.Region = new Region(path);

                using (Pen pen = new Pen(Color.Black, 2))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }


        class RoundedTextBox : TextBox
        {
            private int borderRadius = 15;
            private Color borderColor = Color.Black;
            private Color backgroundColor = Color.White;
            private Color textColor = Color.Black;

            public int BorderRadius
            {
                get { return borderRadius; }
                set { borderRadius = value; this.Invalidate(); }
            }

            public Color BorderColor
            {
                get { return borderColor; }
                set { borderColor = value; this.Invalidate(); } 
            }

            public Color BackgroundColor
            {
                get { return backgroundColor; }
                set { backgroundColor = value; this.Invalidate(); }
            }

            public Color TextColor
            {
                get { return textColor; }
                set { textColor = value; this.ForeColor = value; this.Invalidate(); }
            }

            public RoundedTextBox()
            {
                this.SetStyle(ControlStyles.UserPaint, true);
                this.BorderStyle = BorderStyle.None;
                this.ForeColor = textColor;
                this.BackColor = backgroundColor;
                this.TextAlign = HorizontalAlignment.Center; 
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(0, 0, borderRadius, borderRadius, 180, 90);
                    path.AddArc(Width - borderRadius, 0, borderRadius, borderRadius, 270, 90);
                    path.AddArc(Width - borderRadius, Height - borderRadius, borderRadius, borderRadius, 0, 90);
                    path.AddArc(0, Height - borderRadius, borderRadius, borderRadius, 90, 90);
                    path.CloseFigure();

                    this.Region = new Region(path);

                    using (SolidBrush brush = new SolidBrush(backgroundColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    using (Pen pen = new Pen(borderColor, 2))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }

                
                using (SolidBrush textBrush = new SolidBrush(textColor))
                {
                    SizeF textSize = g.MeasureString(this.Text, this.Font);
                    float textX = (Width - textSize.Width) / 2; 
                    float textY = (Height - textSize.Height) / 2; 
                    g.DrawString(this.Text, this.Font, textBrush, new PointF(textX, textY));
                }
            }

            protected override void OnTextChanged(EventArgs e)
            {
                base.OnTextChanged(e);
                this.Invalidate(); 
            }

            protected override void OnEnter(EventArgs e)
            {
                base.OnEnter(e);
                this.BorderColor = Color.Blue; 
                this.Invalidate(); 
            }

            protected override void OnLeave(EventArgs e)
            {
                base.OnLeave(e);
                this.BorderColor = Color.Black; 
                this.Invalidate(); 
            }
        }
    }
}