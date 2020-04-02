using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance;
using Finance.Data;
using System.Threading;
using Finance.LiveTrading;
using System.Windows.Forms;
using static Finance.Helpers;

namespace Finance
{
    public partial class SingleSecurityIndicatorForm : Form, IPersistLayout
    {
        private static SingleSecurityIndicatorForm _Instance { get; set; }
        public static SingleSecurityIndicatorForm Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new SingleSecurityIndicatorForm();
                return _Instance;
            }
        }

        public Security Security { get; protected set; }
        public bool Sizeable => false;

        private SingleSecurityIndicatorForm()
        {
            InitializeComponent();

            Name = "SingleSecurityIndicatorForm";

            this.ResizeEnd += (s, e) => SaveLayout();
            this.Shown += (s, e) => LoadLayout();
        }

        public void SetSecurity(Security security)
        {
            if (this.Security == security)
                return;

            this.Security = security;
            ReloadAllIndicators();
        }
        private void ReloadAllIndicators()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    ReloadAllIndicators();
                    return;
                }));
            }

            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is SingleStockIndicatorTile indicatorTile)
                    Invoke(new Action(() => indicatorTile.SetSecurity(this.Security)));
            }            
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
