using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Finance;
using Finance.Data;
using System.Reflection;
using System.Drawing;
using System.Threading;

namespace Finance
{
    public class DatabaseInfoPanelNew : UserControl
    {
        private GroupBox grpMain;

        public DatabaseInfoPanelNew()
        {
            this.InitializeMe();
            ShowInfo();
        }

        [Initializer]
        private void InitializeComponent()
        {
            this.grpMain = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // grpMain
            // 
            this.grpMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpMain.Location = new System.Drawing.Point(3, 3);
            this.grpMain.Name = "grpMain";
            this.grpMain.Size = new System.Drawing.Size(271, 139);
            this.grpMain.TabIndex = 0;
            this.grpMain.TabStop = false;
            this.grpMain.Text = "Database Status";
            // 
            // DatabaseInfoPanelNew
            // 
            this.Controls.Add(this.grpMain);
            this.Name = "DatabaseInfoPanelNew";
            this.Size = new System.Drawing.Size(277, 145);
            this.ResumeLayout(false);

        }
        [Initializer]
        private void InitializeHandlers()
        {
            RefDataManager.Instance.SecurityDataChanged += (s, e) =>
            {
                ShowInfo();
            };
        }

        private void ShowInfo()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { ShowInfo(); }));
                return;
            }

            grpMain.Controls.Clear();

            //
            // Get a list of all UI Output methods in Security (marked with Attribute)
            //
            var methods = (from method in RefDataManager.Instance.GetType().GetMethods()
                           where Attribute.IsDefined(method, typeof(UiDisplayTextAttribute))
                           select method).OrderBy(x => (x.GetCustomAttribute<UiDisplayTextAttribute>()).Order);
            //
            // Add a label for each UI output method
            //
            foreach (var field in methods)
            {
                grpMain.Controls.Add(new Label()
                {
                    Text = field.Invoke(RefDataManager.Instance, null) as string,
                    AutoSize = true,
                    Font = Helpers.SystemFont(8),
                    AutoEllipsis = true
                });
            }

            //
            // Arrange labels
            //
            grpMain.Controls[0].Location = new Point(5, 20);
            for (int i = 1; i < grpMain.Controls.Count; i++)
                grpMain.Controls[i].DockTo(grpMain.Controls[i - 1], ControlEdge.Bottom);
        }
    }
}
