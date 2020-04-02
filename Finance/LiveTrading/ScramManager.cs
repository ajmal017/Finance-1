using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance.Data;
using Finance.LiveTrading;
using Finance;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance.LiveTrading
{
    public class ScramManager
    {
        private static ScramManager _Instance { get; set; }
        public static ScramManager Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new ScramManager();
                return _Instance;
            }
        }

        #region Events

        public event ScramEventHandler ScramActivated;

        #endregion

        private ScramManager()
        {

        }

        public void InitializeShutdown(ScramEventArgs e)
        {
            ScramAlertIndicatorForm.ShowAlert();

            Log(new LogMessage("SCRAM Manager", $"{(e.UserInitiated ? "USER" : "SYSTEM")} ACTIVATED EMERGECY SHUTDOWN AT {e.PressedTime:hh:mm:ss.fff}", LogMessageType.SCRAM));

            ScramActivated?.Invoke(this, e);
        }
    }
}
