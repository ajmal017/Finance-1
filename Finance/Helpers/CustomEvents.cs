using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance;
using Finance.Data;
using Finance.LiveTrading;

namespace Finance
{

    public class LogEventArgs : EventArgs
    {
        public LogMessage message { get; }

        public LogEventArgs(LogMessage message)
        {
            this.message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }

    public delegate void ChartCursorChangedEventHandler(object sender, ChartCursorChangedEventArgs e);
    public class ChartCursorChangedEventArgs : EventArgs
    {
        public double X;
        public double Y;

        public ChartCursorChangedEventArgs(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public delegate void SimulationStatusEventHandler(object sender, SimulationStatusEventArgs e);
    public class SimulationStatusEventArgs : EventArgs
    {
        public Simulation Simulation { get; }
        public SimulationStatus Status => Simulation.SimulationStatus;

        public SimulationStatusEventArgs(Simulation simulation)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        }
    }

    public delegate void SelectedSimulationChangedEventHandler(object sender, SelectedSimulationEventArgs e);
    public class SelectedSimulationEventArgs : EventArgs
    {
        public Simulation SelectedSimulation { get; set; }

        public SelectedSimulationEventArgs(Simulation selectedSimulation)
        {
            SelectedSimulation = selectedSimulation;
        }
    }

    public delegate void SecurityDataUpdateEventHandler(object sender, SecurityDataUpdateEventArgs e);
    public class SecurityDataUpdateEventArgs : EventArgs
    {
        public List<Security> securities;
        public bool IsDatabaseUpdate { get; private set; }

        public bool TryGetSecurity(string ticker, out Security security)
        {
            if (securities == null)
            {
                security = null;
                return false;
            }

            if (securities.Exists(x => x.Ticker == ticker))
            {
                security = securities.Find(x => x.Ticker == ticker);
                return true;
            }

            security = null;
            return false;
        }

        public SecurityDataUpdateEventArgs()
        {
            securities = null;
            IsDatabaseUpdate = true;
        }
        public SecurityDataUpdateEventArgs(Security security)
        {
            securities = new List<Security>
            {
                security
            };
            IsDatabaseUpdate = false;
        }
        public SecurityDataUpdateEventArgs(List<Security> securities)
        {
            if (securities == null)
                this.securities = new List<Security>();
            else
                this.securities = securities;

            IsDatabaseUpdate = false;
        }
    }

    public delegate void DataProviderResponseEventHandler(object sender, DataProviderResponseEventArgs e);
    public class DataProviderResponseEventArgs : EventArgs
    {
        public int RequestId => Request.RequestID;
        public bool HasError => (Request.DataProviderErrorType != DataProviderErrorType.NoError);
        public string ErrorMessage => Request.ErrorMessage;
        public RefDataProviderRequest Request;
    }

    public delegate void DataProviderSymbolsResponseEventHandler(object sender, DataProviderSupportedSymbolsEventArgs e);
    public class DataProviderSupportedSymbolsEventArgs : EventArgs
    {
        public List<RefDataProviderSupportedSymbolsResponse> Symbols { get; }

        public DataProviderSupportedSymbolsEventArgs(List<RefDataProviderSupportedSymbolsResponse> symbols)
        {
            Symbols = symbols ?? throw new ArgumentNullException(nameof(symbols));
        }
    }

    public delegate void DataProviderSectorsResponseEventHandler(object sender, DataProviderSectorsEventArgs e);
    public class DataProviderSectorsEventArgs : EventArgs
    {
        public RefDataProviderSectorListResponse Response { get; set; }

        public DataProviderSectorsEventArgs(RefDataProviderSectorListResponse response)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }
    }

    public delegate void PositionClosedEventHandler(object sender, PositionDataEventArgs e);
    public delegate void StoplossRequestHandler(object sender, PositionDataEventArgs e);
    public class PositionDataEventArgs : EventArgs
    {
        public Position position;
        public DateTime AsOf;
        public PositionDataEventArgs(Position position, DateTime AsOf)
        {
            this.position = position ?? throw new ArgumentNullException(nameof(position));
            this.AsOf = AsOf;
        }
    }

    public delegate void SelectedSecurityChangedEventHandler(object sender, SelectedSecurityEventArgs e);
    public class SelectedSecurityEventArgs
    {
        public Security SelectedSecurity { get; }

        public SelectedSecurityEventArgs(Security selectedSecurity)
        {
            SelectedSecurity = selectedSecurity;
        }
    }

    #region Live Trading

    public delegate void TradingAccountProviderStatusEventHandler(object sender, TradingAccountProviderStatusEventArgs e);
    public class TradingAccountProviderStatusEventArgs
    {
        public TradingAccountProviderStatusFlag StatusFlag { get; private set; }
        public string Message { get; set; }

        public bool IsError => StatusFlag == TradingAccountProviderStatusFlag.Error;

        public TradingAccountProviderStatusEventArgs(TradingAccountProviderStatusFlag statusFlag, string message = "")
        {
            StatusFlag = statusFlag;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }

    public delegate void TradingAccountIdListEventHandler(object sender, TradingAccountListEventArgs e);
    public class TradingAccountListEventArgs
    {
        public List<string> AccountIds { get; }

        public TradingAccountListEventArgs(List<string> accountIds)
        {
            AccountIds = accountIds ?? throw new ArgumentNullException(nameof(accountIds));
        }
    }

    public delegate void ActiveAccountChanged(object sender, AccountUpdateEventArgs e);
    public delegate void AccountUpdateEventHandler(object sender, AccountUpdateEventArgs e);
    public class AccountUpdateEventArgs
    {
        public LiveAccount Account { get; set; }

        public AccountUpdateEventArgs(LiveAccount account)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
        }
    }

    public delegate void TradeStatusUpdateEventHandler(object sender, TradeStatusUpdateEventArgs e);
    public class TradeStatusUpdateEventArgs
    {
        public LiveTrade Trade { get; }

        public TradeStatusUpdateEventArgs(LiveTrade trade)
        {
            Trade = trade ?? throw new ArgumentNullException(nameof(trade));
        }
    }

    public delegate void OpenPositionEventHandler(object sender, OpenPositionEventArgs e);
    public class OpenPositionEventArgs
    {
        public LivePosition Position { get; }

        public OpenPositionEventArgs(LivePosition position)
        {
            Position = position;
        }
    }

    public delegate void LiveQuoteEventHandler(object sender, LiveQuoteEventArgs e);
    public class LiveQuoteEventArgs : EventArgs
    {
        public Security security { get; }
        public LiveQuoteType QuoteType { get; }
        public DateTime QuoteTime { get; }
        public decimal QuotePrice { get; }
        public long QuoteVolume { get; }

        public LiveQuoteEventArgs(Security security, LiveQuoteType quoteType, DateTime quoteTime, decimal quotePrice, long quoteVolume)
        {
            this.security = security ?? throw new ArgumentNullException(nameof(security));
            QuoteType = quoteType;
            QuoteTime = quoteTime;
            QuotePrice = quotePrice;
            QuoteVolume = quoteVolume;
        }
    }

    #endregion
}
