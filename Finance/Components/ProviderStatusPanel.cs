using Finance.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms.DataVisualization.Charting;
using Finance.TradeStrategies;
using Finance.LiveTrading;
using Timer = System.Windows.Forms.Timer;
using static Finance.Helpers;
using System.Data;
using System.ComponentModel;

namespace Finance
{
    public interface IProviderStatus : INotifyPropertyChanged
    {
        string Name { get; }

        ControlStatus Status { get; set; }

        string StatusMessage { get; set; }
        string StatusMessage2 { get; set; }

        bool Connected { get; }
        void Connect();
    }

    public sealed class ProviderStatusPanel : Panel
    {
        Size _defaultSize = new Size(300, 75);
        Size _defaultButtonSize = new Size(60, 20);
        Font _defaultLabelFont = new Font("Calibri", 8);

        private event PropertyChangedEventHandler AttachedControlPropertyChanged;

        public IProviderStatus AttachedControl { get; private set; }

        Rectangle rectConnectionStatusBubble;
        Label lblProviderName;
        Label lblStatusMsg;
        Label lblStatusMsg2;
        Button btnConnect;

        public ProviderStatusPanel(IProviderStatus control)
        {
            this.AttachedControl = control;
            this.InitializeMe();
        }

        [Initializer]
        private void Initialize()
        {
            this.AttachedControl.PropertyChanged += (s, e) =>
            {
                Invoke(new Action(() =>
                {
                    AttachedControlPropertyChanged?.Invoke(s, e);
                    Invalidate();
                }));
            };
        }
        [Initializer]
        private void InitializeStyles()
        {
            Size = _defaultSize;
            MinimumSize = _defaultSize;
            MaximumSize = _defaultSize;
            BackColor = Color.Black;
            BorderStyle = BorderStyle.FixedSingle;

            this.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.Gray, ButtonBorderStyle.Outset);
            };
        }
        [Initializer]
        private void InitializeDisplay()
        {
            lblProviderName = new Label()
            {
                Text = AttachedControl.Name,
                Location = new Point(2, 2),
                Width = 150,
                Height = 12,
                Font = _defaultLabelFont,
                ForeColor = Color.White
            };
            this.Controls.Add(lblProviderName);

            lblStatusMsg = new Label()
            {
                Text = AttachedControl.StatusMessage,
                Location = new Point(2, 20),
                Width = _defaultSize.Width -50,
                Height = 12,
                Font = _defaultLabelFont,
                ForeColor = Color.Goldenrod
            };
            this.Controls.Add(lblStatusMsg);

            lblStatusMsg2 = new Label()
            {
                Text = AttachedControl.StatusMessage2,
                Location = new Point(2, 32),
                Width = _defaultSize.Width - 50,
                Height = 12,
                Font = _defaultLabelFont,
                ForeColor = Color.Goldenrod
            };
            this.Controls.Add(lblStatusMsg2);

            AttachedControlPropertyChanged += (s, e) =>
            {
                lblStatusMsg.Text = AttachedControl.StatusMessage;
                lblStatusMsg2.Text = AttachedControl.StatusMessage2;
                Invalidate();
            };

        }
        [Initializer]
        private void InitializeStatusBubbles()
        {
            rectConnectionStatusBubble = new Rectangle(_defaultSize.Width - 40, 8, 30, 30);
            GraphicsPath bubblePath = new GraphicsPath();
            bubblePath.AddEllipse(rectConnectionStatusBubble);
            PathGradientBrush bubbleBrush = new PathGradientBrush(bubblePath);

            this.Paint += (s, e) =>
            {
                switch (AttachedControl?.Status)
                {
                    case ControlStatus.ErrorState:
                        bubbleBrush.CenterColor = Color.Pink;
                        bubbleBrush.SurroundColors = new[] { Color.Red };
                        break;
                    case ControlStatus.Ready:
                        bubbleBrush.CenterColor = Color.Yellow;
                        bubbleBrush.SurroundColors = new[] { Color.DarkGreen };
                        break;
                    case ControlStatus.Working:
                        bubbleBrush.CenterColor = Color.Yellow;
                        bubbleBrush.SurroundColors = new[] { Color.DarkOrange };
                        break;
                    case ControlStatus.Offline:
                        bubbleBrush.CenterColor = Color.DarkRed;
                        bubbleBrush.SurroundColors = new[] { Color.FromArgb(255, 32, 0, 0) };
                        break;
                    default:
                        break;
                }
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                e.Graphics.FillEllipse(bubbleBrush, rectConnectionStatusBubble);

                e.Graphics.DrawEllipse(new Pen(Color.FromArgb(96, 255, 255, 255), 2), rectConnectionStatusBubble);
            };
        }
        [Initializer]
        private void InitializeConnectButton()
        {
            btnConnect = new Button()
            {
                Text = "Connect",
                Size = _defaultButtonSize,
                Location = new Point(5, 50),
                Font = new Font("Calibri", 8),
                BackColor = Button.DefaultBackColor,
            };
            btnConnect.Click += (s, e) => AttachedControl.Connect();
            AttachedControlPropertyChanged += (s, e) => btnConnect.Visible = !AttachedControl.Connected;
            this.Controls.Add(btnConnect);
        }
    }

}
