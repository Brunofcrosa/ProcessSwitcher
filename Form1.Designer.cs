using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ProcessSwitcher
{
    partial class Form1 : Form
    {
        private FlowLayoutPanel processPanel;
        private Button btnUpdateHotkeys;
        private Button btnRefreshProcesses;
        private Button btnSendKeys;
        private TextBox txtSendKeysHotkey;
        private Label lblSendKeysHotkey;

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

            this.btnRefreshProcesses = CreateButton("", 10, 400,
    () => btnRefreshProcesses_Click(null, null), "Resources/refresh.ico");

            this.btnUpdateHotkeys = CreateButton("", 140, 400,
                () => btnUpdateHotkeys_Click(null, null), "Resources/check.ico");

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
                this.Icon = new Icon("Resources/pwicon.ico");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar o ícone: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
processPanel.AllowDrop = true;
            processPanel.DragEnter += ProcessPanel_DragEnter;
            processPanel.DragDrop += ProcessPanel_DragDrop;
            this.ClientSize = new Size(270, 470);
            this.Controls.Add(this.processPanel);
            this.Controls.Add(this.btnRefreshProcesses);
            this.Controls.Add(this.btnUpdateHotkeys);
            this.Controls.Add(this.btnSendKeys);
            this.Controls.Add(lblBelowButtons); 
            this.Controls.Add(lblBelowButtons2); 
            this.Controls.Add(this.lblSendKeysHotkey);
            this.Controls.Add(this.txtSendKeysHotkey);
            this.Text = "Process Switcher";
            this.FormClosing += new FormClosingEventHandler(this.Form1_FormClosing);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private Button CreateButton(string text, int x, int y, Action clickHandler, string iconPath = null)
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
                ImageAlign = ContentAlignment.TopCenter,
                TextImageRelation = TextImageRelation.ImageAboveText,
                Padding = new Padding(5) 
            };

            if (!string.IsNullOrEmpty(iconPath) && System.IO.File.Exists(iconPath))
            {
                Image originalImage = Image.FromFile(iconPath);
                Image resizedImage = new Bitmap(originalImage, new Size(35, 35));
                button.Image = resizedImage;
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

                // Centraliza o texto dentro do TextBox
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