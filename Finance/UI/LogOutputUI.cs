using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Finance.Helpers;

namespace Finance.UI
{
    public partial class LogOutputUI : Form
    {

        public LogOutputUI()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.Manual;
            Size = new Size((Screen.PrimaryScreen.WorkingArea.Width / 3) * 2, 
                Screen.PrimaryScreen.WorkingArea.Height / 2);
            Location = new Point(0, Screen.PrimaryScreen.WorkingArea.Height / 2);
            Text = "System Log";

            this.InitializeMe();

            // Subscribe to static log event
            Logger.LogEvent += (s, e) =>
            {
                try { Print(s, e); }
                catch (Exception) { }
            };

            FormClosing += (s, e) =>
            {
                Print("Log closed by application.");
                WriteLogToTxt();
            };

            Refresh();
        }

        #region Initializers

        RichTextBox txtOutput;
        MenuStrip menu;

        [Initializer]
        private void InitializeMenu()
        {
            menu = new MenuStrip();

            ToolStripMenuItem menuWriteLogToTxt = new ToolStripMenuItem()
            {
                Name = "menu",
                Text = "Write Log To File",
            };
            menuWriteLogToTxt.Click += (s, e) =>
            {
                Print("Wrote output to file at user request.");
                WriteLogToTxt();
            };

            ToolStripButton menuPrintSeperator = new ToolStripButton()
            {
                Name = "printSep",
                Text = "Print Seperator"
            };
            menuPrintSeperator.Click += (s, e) => PrintSeperator();

            menu.Items.Add(menuWriteLogToTxt);
            menu.Items.Add(menuPrintSeperator);

            Controls.Add(menu);
        }

        [Initializer]
        private void InitializeOutputDisplay()
        {
            txtOutput = new RichTextBox
            {
                Name = "txt",
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true,
                Font = Helpers.SystemFont(),
                BackColor = Color.Black,
                ForeColor = Color.White,
                HideSelection = false
            };

            Controls.Add(txtOutput);
            Controls["txt"].BringToFront();
        }

        #endregion

        public void Print(object sender, LogEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { Print(sender, e); }));
                return;
            }

            string msgTxt = e.message.Message;

            if (msgTxt.Length > 100)
                msgTxt = msgTxt.Substring(0, 100);

            // Print timestamp
            txtOutput.SelectionColor = Color.Orange;
            txtOutput.AppendText($"{e.message.Created:yyyyMMdd hh:mm:ss.fff} >> ");

            // Print message type
            switch (e.message.MessageType)
            {
                case LogMessageType.Debug:
                    txtOutput.SelectionColor = Color.LightBlue;
                    txtOutput.AppendText("[DBG] ");
                    break;
                case LogMessageType.Production:
                    txtOutput.SelectionColor = Color.White;
                    txtOutput.AppendText("[SYS] ");
                    break;
                case LogMessageType.Error:
                    txtOutput.SelectionColor = Color.OrangeRed;
                    txtOutput.AppendText("[ERR] ");
                    break;
                default:
                    break;
            }

            // Print message
            txtOutput.AppendText(string.Format($"{msgTxt} "));

            // Space the source out to the right
            txtOutput.SelectionColor = Color.DimGray;
            txtOutput.AppendText(new string('.', 105 - msgTxt.Length));
            txtOutput.AppendText(string.Format($" [From: {e.message.Sender}]{Environment.NewLine}"));
        }
        private void Print(string logMessage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { Print(logMessage); }));
                return;
            }

            txtOutput.SelectionColor = Color.Orange;
            txtOutput.AppendText($"{DateTime.Now.ToLogFormat()} >> ");

            txtOutput.SelectionColor = Color.White;
            txtOutput.AppendText(string.Format($"{logMessage} {Environment.NewLine}"));

        }
        private void PrintSeperator()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { PrintSeperator(); }));
                return;
            }
            txtOutput.SelectionColor = Color.Yellow;
            txtOutput.AppendText($"---------- {DateTime.Now.ToString("yyyyMMdd hh:mm:ss.fff")} ----------{Environment.NewLine}");
        }
        private void WriteLogToTxt()
        {
            var output = txtOutput.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Helpers.OutputToTextFile(output, string.Format($"SystemLog_{DateTime.Now.ToString("yyyyMMdd")}"));
        }


    }
}
