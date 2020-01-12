using Finance;
using Finance.Data;
using Finance.Models;
using Finance.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Calendar;
using static Finance.Helpers;

namespace Finance
{
    /*
     *  PortfolioManager is the top-level class for implementing all features in this library, acting as a virtual
     *  'manager' who conducts ordered operations each day.  It coordinates input/output from all associated objects
     *  and conducts error cross-checking at a high level.
     *  
     *  PortfolioManager operates in a generally linear fashion, conducting operations as if working throughout the trading day.
     *  A 'day' begins upon market open and ends after market close, when end-of-day values are computed and used for determining
     *  next-day trading activity.  Care must be taken to ensure that calculations do not inadvertantly use next-day data which 
     *  would not otherwise be available when making calculations in real time.
     *  
     *  Major subroutines are conducted generally as follows:
     *  
     *  (MARKET OPEN)
     *  
     *  -PortfolioManager updates the database for stock price data for all securities in it's universe (realtime)
     *     NOTE: Care must be taken to make sure no methods executed under the MARKET OPEN timeframe use anything but opening values.  High/Low/Close
     *     are not known and executed upon until MARKET CLOSE
     *  
     *  -PortfolioManager directs the Portfolio to execute pre-existing stoploss trades which are pending in the Pending Trades
     *   queue, if the stops are triggered by the Security's opening price.  This is done before any other pending trades are executed.
     *   
     *  -PortfolioManager executes Position Management rules to ensure the portfolio equity and margin requirements are within 
     *   acceptable parameters upon open.  If not, the Portfolio Management rules will generate priority trades to correct, which are executed immediately
     *   
     *  -PortfolioManager begins executing queued Pending trades from the Pending trades queue in order of priority.  Each trade is first 
     *   executed in a What-If portfolio which is then analyzed by position management rules to ensure the trade does not put the
     *   portfolio out of compliance.  If the trade fails, it is marked as rejected and the next trade is executed until the Pending Trade
     *   queue is empty
     *   
     *  
     *  (MARKET CLOSE)
     *  
     *  
     *  -Portfolio executes all resting stop loss orders based on the day's price range.  If the stop loss would have been triggered at any point
     *   during the day based on high/low, the trade is executed at the stop price.
     *   
     *  // TODO: Management rules for daily adverse scenario range?
     *  
     *  -Position Management updates stop prices for all open positions
     *  
     *  -Strategy rules engine generates additional trades for open positions (increasing size) (Prioritize above new trades) through Position Management
     *  
     *  -Strategy rules engine generates new position trades
     *  
     *  -Pre-approval rules engine screens all new trades to determine trade size and portfolio limitations.  Trades are prioritized and rejected in
     *   order of priority to ensure that the resulting portfolio would not be out of compliance or assume outsized risk.  Approved trades are formatted
     *   and placed in the pending queue for next-day execution.
     *  
     */
    public partial class PortfolioManager
    {

        public Portfolio _Portfolio { get; set; }
        public DataManager _DataManager { get; set; }
        public PortfolioSetup _PortfolioSetup { get; set; }
        public IEnvironment _Environment { get; set; }
        public Strategy _Strategy { get; set; }

        public PortfolioManager(DataManager DataManager, PortfolioSetup PortfolioSetup, IEnvironment Environment, Strategy Strategy)
        {
            _DataManager = DataManager ?? throw new ArgumentNullException(nameof(DataManager));
            _PortfolioSetup = PortfolioSetup ?? throw new ArgumentNullException(nameof(PortfolioSetup));
            _Environment = Environment ?? throw new ArgumentNullException(nameof(Environment));
            _Strategy = Strategy ?? throw new ArgumentNullException(nameof(Strategy));

            _Portfolio = new Portfolio(_Environment, _PortfolioSetup, _Strategy);

            CurrentDate = _PortfolioSetup.InceptionDate;

            PopulateSecurityUniverse();
        }
    }

    /// <summary>
    /// Portfolio chronology and simulation control
    /// </summary>
    public partial class PortfolioManager
    {
        public DateTime CurrentDate { get; set; }

        private List<Security> SecurityUniverse { get; set; }

        /// <summary>
        /// Loads the universe of securities along with associated data with which to conduct the simulation
        /// </summary>
        public void PopulateSecurityUniverse()
        {
            // Get security
            SecurityUniverse = new List<Security>();
            SecurityUniverse.Add(_DataManager.GetSecurity("TWTR", true));
            SecurityUniverse.Add(_DataManager.GetSecurity("FB", true));

            //// Update
            //SecurityUniverse.ForEach(s => _DataManager.UpdateSecurity(s, DateTime.Today, true));

            //// Repopulate universe with updated data
            //SecurityUniverse = new List<Security>();
            //SecurityUniverse.Add(_DataManager.GetSecurity("TWTR", true));
            //SecurityUniverse.Add(_DataManager.GetSecurity("FB", true));


            // TODO: Security filter here?
        }

        /// <summary>
        /// Increments CurrentDate to the next valid trading day and executes all daily actions
        /// </summary>
        public void StepDate()
        {

            CurrentDate = Calendar.NextTradingDay(CurrentDate);

            UpdateMarketData();

            ExecuteOpeningStoplosses();

            ExecuteStartOfDayPositionManagement();

            ExecutePendingTrades();

            ExecuteClosingStoplosses();

            ExecuteEndOfDayPositionManagement();

            ExecuteNewPositionTradeGeneration();

            ExecuteEndOfDayPretradeApproval();

            Logger.Log(CurrentDate, _Portfolio.SpecialMemorandumAccountBalance(CurrentDate).ToString("$0.00"));

        }

    }

    /// <summary>
    /// Portfolio daily actions
    /// </summary>
    public partial class PortfolioManager
    {

        #region Market Open Actions

        /// <summary>
        /// Updates all market data to the current date
        /// </summary>
        /// <returns></returns>
        [DailyAction(1)]
        private void UpdateMarketData()
        {
            // Not really needed until we are working in real time
        }

        /// <summary>
        /// Direct the portfolio to execute all stoplosses which will be triggered by this day's opening prices
        /// </summary>
        [DailyAction(2)]
        private void ExecuteOpeningStoplosses()
        {
            _Portfolio.ExecuteStoplossTrades(CurrentDate, true);
        }

        /// <summary>
        /// Direct the portfolio to conduct a margin and liquidity check to ensure that the portfolio is not in a liquidation status before conducting daily business.
        /// </summary>
        [DailyAction(3)]
        private void ExecuteStartOfDayPositionManagement()
        {
            _Portfolio.ExecutePositionManagement(CurrentDate, true);
        }

        /// <summary>
        /// Direct the portfolio to execute all pending trades, which have been pre-approved, in order of priority
        /// </summary>
        [DailyAction(4)]
        private void ExecutePendingTrades()
        {
            _Portfolio.ProcessPendingTrades(CurrentDate);
        }

        #endregion

        #region Market Close Actions

        /// <summary>
        /// Direct the portfolio to execute all stoploss trades using end of day price values
        /// </summary>
        [DailyAction(5)]
        private void ExecuteClosingStoplosses()
        {
            _Portfolio.ExecuteStoplossTrades(CurrentDate, false);
        }

        /// <summary>
        /// Direct the portfolio to execute all end of day position management rules (stoploss, position scaling, etc)
        /// </summary>
        [DailyAction(6)]
        private void ExecuteEndOfDayPositionManagement()
        {
            _Portfolio.ExecutePositionManagement(CurrentDate, false);
        }

        /// <summary>
        /// Direct the portfolio to generate new trades based on end of day pricese
        /// </summary>
        [DailyAction(7)]
        private void ExecuteNewPositionTradeGeneration()
        {
            _Portfolio.ExecuteTradeGenerationStrategy(SecurityUniverse, CurrentDate);
        }

        /// <summary>
        /// Direct the portfolio to pre-approve all trades int he pending queue
        /// </summary>
        [DailyAction(8)]
        private void ExecuteEndOfDayPretradeApproval()
        {
            _Portfolio.ExecuteTradePreApproval(CurrentDate);
        }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DailyActionAttribute : Attribute
    {
        public int order;

        public DailyActionAttribute(int order)
        {
            this.order = order;
        }
    }

}
