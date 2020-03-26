using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using static Finance.Helpers;

namespace Finance
{
    public class LogOutputForm : Form, IPersistLayout
    {

        public bool Sizeable => true;

        private static List<string> ExistingForms = new List<string>();

        private List<LogMessageType> DisplayTypes;
        public LogOutputForm(List<LogMessageType> displayTypes, string name)
        {
            if (ExistingForms.Contains(name))
                throw new UnknownErrorException() { message = "Cannot create forms with same name " };

            Name = name;
            ExistingForms.Add(Name);

            if (displayTypes == null)
            {
                DisplayTypes = new List<LogMessageType>();
                foreach (LogMessageType val in Enum.GetValues(typeof(LogMessageType)))
                {
                    DisplayTypes.Add(val);
                }
            }
            else
                DisplayTypes = displayTypes;

            this.InitializeMe();

            // Subscribe to static log event
            Logger.LogEvent += (s, e) =>
            {
                try { Print(s, e); }
                catch (Exception) { }
            };
            this.FormClosing += (s, e) =>
            {
                Print("Log closed by application.");
                WriteLogToTxt();

                e.Cancel = true;
                this.Hide();
            };
            this.Shown += (s, e) => LoadLayout();
            this.ResizeEnd += (s, e) => SaveLayout();

            Refresh();
        }

        RichTextBox txtOutput;
        MenuStrip menu;

        [Initializer]
        private void InitializeStyles()
        {
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Text = Name;
            StartPosition = FormStartPosition.Manual;
            WindowState = FormWindowState.Normal;
        }
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

        public void Print(object sender, LogEventArgs e)
        {
            if (!DisplayTypes.Contains(e.message.MessageType))
                return;

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
            txtOutput.AppendText($"{e.message.Created:yyyyMMdd HH:mm:ss.fff} >> ");

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
                case LogMessageType.SystemError:
                    txtOutput.SelectionColor = Color.OrangeRed;
                    txtOutput.AppendText("[ERR] ");
                    break;
                case LogMessageType.SecurityError:
                    txtOutput.SelectionColor = Color.Yellow;
                    txtOutput.AppendText("[SEC] ");
                    break;
                case LogMessageType.TradingNotification:
                    txtOutput.SelectionBackColor = Color.ForestGreen;
                    txtOutput.SelectionColor = Color.White;
                    txtOutput.AppendText("[TRD] ");
                    break;
                case LogMessageType.TradingError:
                    txtOutput.SelectionBackColor = Color.DarkRed;
                    txtOutput.SelectionColor = Color.White;
                    txtOutput.AppendText("[TRD] ");
                    break;
                case LogMessageType.TradingSystemMessage:
                    txtOutput.SelectionColor = Color.White;
                    txtOutput.AppendText("[TRD] ");
                    break;
                default:
                    break;
            }

            // Print message
            txtOutput.AppendText(string.Format($"{msgTxt} "));

            // Space the source out to the right
            txtOutput.SelectionBackColor = Color.Black;
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
            var filePath = string.Format($@"{Settings.Instance.LogOutputDirectoryPath}\SystemLog_{DateTime.Now.ToString("yyyyMMdd_HHmm-ss")}.txt");

            Helpers.OutputToTextFile(output, filePath);

            Process.Start(filePath);
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
