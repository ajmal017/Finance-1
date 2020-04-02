using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using Finance.Data;
using System.ComponentModel;
using static Finance.Helpers;

namespace Finance.LiveTrading
{
    public class SCRAM : Form, IPersistLayout
    {
        private static SCRAM _Instance { get; set; }
        public static SCRAM Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new SCRAM();
                return _Instance;
            }
        }

        private Button btnScram;
        private Button btnCancel;

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private bool Armed { get; set; } = false;
        private bool Fired { get; set; } = false;

        private System.Windows.Forms.Timer tmrArmedFlash;

        public bool Sizeable => false;

        private SCRAM()
        {
            Name = "ScramForm";
            InitializeComponent();
            this.InitializeMe();

            this.Shown += (s, e) => LoadLayout();
            this.ResizeEnd += (s, e) => SaveLayout();
        }

        [Initializer]
        private void InitializeStyles()
        {

        }
        [Initializer]
        private void InitializeTheButton()
        {
            btnScram.FlatStyle = FlatStyle.Popup;

            btnScram.Click += (s, e) =>
            {
                if (Fired)
                    return;

                if (!Armed)
                    ArmScram();
                else
                    FireScram();
            };

            Brush _brush1 = new SolidBrush(Color.Red);
            Brush _brush2 = new SolidBrush(Color.Yellow);

            var w = btnScram.ClientRectangle.Width;
            var h = btnScram.ClientRectangle.Height;

            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            Point[] pts = new[]
            {
                new Point(5,5),
                new Point(w/2, 5),
                new Point(w-5,5),
                new Point(5,h/2),
                new Point(w-5,h/2),
                new Point(5,h-5),
                new Point(w/2, h-5),
                new Point(w-5,h-5)
            };

            btnScram.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                ControlPaint.DrawButton(e.Graphics, btnScram.ClientRectangle, Fired ?
                    ButtonState.Pushed : ButtonState.Normal);

                // Red
                e.Graphics.FillPolygon(_brush1, new[] { pts[0], pts[1], pts[3] });
                // White
                e.Graphics.FillPolygon(_brush2, new[] { pts[3], pts[1], pts[2], pts[5] });
                // Red
                e.Graphics.FillPolygon(_brush1, new[] { pts[5], pts[2], pts[4], pts[6] });
                // White
                e.Graphics.FillPolygon(_brush2, new[] { pts[6], pts[4], pts[7] });

                e.Graphics.DrawString(
                    "A3-5",
                    new Font("Stencil", 32, FontStyle.Bold),
                    new SolidBrush(Color.Black),
                    btnScram.ClientRectangle,
                    stringFormat);
            };
        }
        [Initializer]
        private void InitializeFlashAndCancelButton()
        {
            btnCancel.Visible = false;
            btnCancel.Font = new Font("Stencil", 16, FontStyle.Bold);
            btnCancel.BackColor = Color.Green;

            btnCancel.Click += (s, e) =>
            {
                DisarmScram();
            };

            tmrArmedFlash = new System.Windows.Forms.Timer()
            {
                Interval = 500
            };
            tmrArmedFlash.Tick += (s, e) =>
            {
                this.BackColor = this.BackColor == Color.Orange ? Color.Red : Color.Orange;
                if (this.BackColor == Color.Red)
                    new Thread(() => Console.Beep(500, 750)).Start();
            };
        }

        private void InitializeComponent()
        {
            this.btnScram = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnScram
            // 
            this.btnScram.Location = new System.Drawing.Point(21, 23);
            this.btnScram.Name = "btnScram";
            this.btnScram.Size = new System.Drawing.Size(237, 175);
            this.btnScram.TabIndex = 0;
            this.btnScram.Text = "A3-5";
            this.btnScram.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(21, 205);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(237, 44);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "CANCEL";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // SCRAM
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnScram);
            this.Name = "SCRAM";
            this.Text = "SCRAM";
            this.ResumeLayout(false);

        }

        private void ArmScram()
        {
            this.BackColor = Color.Orange;
            Refresh();

            if (MessageBox.Show("Shutdown Armed - Pressing A3-5 again will initiate portfolio liquidation", "SCRAM",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                Armed = true;
                tmrArmedFlash.Start();
                btnCancel.Visible = true;
            }
            else
            {
                Armed = false;
                tmrArmedFlash.Stop();
                this.BackColor = Form.DefaultBackColor;
                btnCancel.Visible = false;
            }
            Refresh();
        }
        private void DisarmScram()
        {
            tmrArmedFlash.Stop();
            this.BackColor = Form.DefaultBackColor;
            btnCancel.Visible = false;
            Armed = false;
            Refresh();
        }
        private void FireScram()
        {
            tmrArmedFlash.Stop();
            this.BackColor = Color.Red;
            Fired = true;

            System.Media.SoundPlayer player = new System.Media.SoundPlayer(Settings.Instance.SoundFilePath("alarm"));

            new Thread(() =>
            {
                int i = 0;

                while (i++ < 5)
                    player.PlaySync();
            }).Start();

            ScramManager.Instance.InitializeShutdown(new ScramEventArgs(DateTime.Now, true));
        }

        public void SaveLayout()
        {
            Settings.Instance.SaveFormLayout(this);
        }
        public void LoadLayout()
        {
            Settings.Instance.LoadFormLayout(this);
        }
    }
}
