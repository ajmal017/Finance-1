using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance;
using Finance.LiveTrading;
using static Finance.Helpers;
using static Finance.Logger;
using static Finance.Calendar;

namespace Finance.LiveTrading
{
    public class LiveRiskManager
    {
        private static LiveRiskManager _Instance { get; set; }
        public static LiveRiskManager Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new LiveRiskManager();
                return _Instance;
            }
        }

        private List<LiveTradeApprovalRuleBase> TradeApprovalRulePipeline { get; set; }

        private LiveRiskManager()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeApprovalPipeline()
        {
            TradeApprovalRulePipeline = new List<LiveTradeApprovalRuleBase>();

            TradeApprovalRulePipeline.Add(new LiveTradeApprovalRule_0());
        }

        public bool ApproveOrder(LiveOrder order, LiveOrder stopOrder, LiveAccount account)
        {
            List<TradeApprovalMessage> ruleResults = new List<TradeApprovalMessage>();

            foreach (var rule in TradeApprovalRulePipeline)
            {
                ruleResults.Add(rule.Run(order, account));
            }

            if (!ruleResults.TrueForAll(x =>
                x.Result == LiveTradeApprovalMessageType.Passed ||
                x.Result == LiveTradeApprovalMessageType.Warning))
            {
                // Write fails to log
                ruleResults.Where(x => x.Result == LiveTradeApprovalMessageType.Failed)
                    .ToList()
                    .ForEach(x => Log(new LogMessage("Risk Mgr", $"{order} {x}", LogMessageType.TradingError)));

                return false;
            }
                        
            foreach (var result in ruleResults.Where(x => x.Result == LiveTradeApprovalMessageType.Warning))
            {
                Log(new LogMessage("Risk Mgr", $"{order} {result}", LogMessageType.TradeWarning));
            }

            Log(new LogMessage("Risk Mgr", $"{order} APPROVED", LogMessageType.TradingNotification));

            // Check stop order params
            // ...
            if(stopOrder != null)
            {


                // return false if invalid
            }

            return true;
        }


    }
}
