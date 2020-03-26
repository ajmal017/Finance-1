using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using static Finance.Calendar;

namespace Finance
{
    /// <summary>
    /// Properties
    /// </summary>
    public partial class PriceBar
    {
        [Key]
        public int PriceBarBaseId { get; set; }

        public DateTime BarDateTime { get; set; }

        [Index(IsClustered = true, IsUnique = false)]
        public string Ticker
        {
            get
            {
                return Security.Ticker;
            }
        }

        public virtual Security Security { get; set; }

        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }

        public DataProviderType PriceDataProvider { get; set; }

        #region Custom Tags

        public CustomPriceBarTag CustomTags { get; set; }

        public void SetCustomTag(CustomPriceBarTag tag, bool isSet)
        {
            if (isSet)
            {
                CustomTags |= tag;
            }
            else
            {
                CustomTags &= ~tag;
            }
        }
        public bool GetCustomFlag(CustomPriceBarTag tag)
        {
            return (CustomTags &= tag) == tag;
        }

        #endregion

        [NotMapped]
        public PriceBarSize BarSize { get; set; } = PriceBarSize.Daily;

        
        private bool _ToUpdate { get; set; } = false;
        [NotMapped]
        public bool ToUpdate
        {
            get
            {
                if (BarSize != PriceBarSize.Daily)
                    return false;
                return _ToUpdate;
            }
            set
            {
                if (BarSize != PriceBarSize.Daily)
                    _ToUpdate = false;
                _ToUpdate = value;
            }
        }

        #region Swing Points

        [NotMapped]
        private Dictionary<int, TrendInfo> SwingPointTrendInfo = new Dictionary<int, TrendInfo>();

        public SwingPointType GetSwingPointType(int bars)
        {
            if (!Security.AreSwingPointsSet(this.BarSize, bars))
                this.Security.SetSwingPointsAndTrends(bars, this.BarSize);
            //throw new UnknownErrorException() { message = "Must Initialize prior to access" };
            if (!SwingPointTrendInfo.ContainsKey(bars))
                this.SwingPointTrendInfo.Add(bars, new TrendInfo());
            return SwingPointTrendInfo[bars].SwingPointType;
        }
        public void SetSwingPointType(int bars, SwingPointType value)
        {
            if (!Security.AreSwingPointsSet(this.BarSize, bars))
                this.Security.SetSwingPointsAndTrends(bars, this.BarSize);
            //throw new UnknownErrorException() { message = "Must Initialize prior to access" };
            if (!SwingPointTrendInfo.ContainsKey(bars))
                this.SwingPointTrendInfo.Add(bars, new TrendInfo());
            SwingPointTrendInfo[bars].SwingPointType = value;
        }

        public TrendQualification GetTrendType(int bars)
        {
            if (!Security.AreSwingPointsSet(this.BarSize, bars))
                this.Security.SetSwingPointsAndTrends(bars, this.BarSize);
            //throw new UnknownErrorException() { message = "Must Initialize prior to access" };
            if (!SwingPointTrendInfo.ContainsKey(bars))
                this.SwingPointTrendInfo.Add(bars, new TrendInfo());
            return SwingPointTrendInfo[bars].TrendType;
        }
        public void SetTrendType(int bars, TrendQualification value)
        {
            if (!Security.AreSwingPointsSet(this.BarSize, bars))
                this.Security.SetSwingPointsAndTrends(bars, this.BarSize);
            //throw new UnknownErrorException() { message = "Must Initialize prior to access" };
            if (!SwingPointTrendInfo.ContainsKey(bars))
                this.SwingPointTrendInfo.Add(bars, new TrendInfo());
            SwingPointTrendInfo[bars].TrendType = value;
        }

        public SwingPointTest GetSwingPointTestType(int bars)
        {
            if (!Security.AreSwingPointsSet(this.BarSize, bars))
                this.Security.SetSwingPointsAndTrends(bars, this.BarSize);
            //throw new UnknownErrorException() { message = "Must Initialize prior to access" };
            if (!SwingPointTrendInfo.ContainsKey(bars))
                this.SwingPointTrendInfo.Add(bars, new TrendInfo());
            return SwingPointTrendInfo[bars].SwingPointTestType;
        }
        public void SetSwingPointTestType(int bars, SwingPointTest value)
        {
            if (!Security.AreSwingPointsSet(this.BarSize, bars))
                this.Security.SetSwingPointsAndTrends(bars, this.BarSize);
            //throw new UnknownErrorException() { message = "Must Initialize prior to access" };
            if (!SwingPointTrendInfo.ContainsKey(bars))
                this.SwingPointTrendInfo.Add(bars, new TrendInfo());
            SwingPointTrendInfo[bars].SwingPointTestType = value;
        }

        public SwingPointTestPriceResult GetSwingPointTestPriceResult(int bars)
        {
            if (!Security.AreSwingPointsSet(this.BarSize, bars))
                this.Security.SetSwingPointsAndTrends(bars, this.BarSize);
            //throw new UnknownErrorException() { message = "Must Initialize prior to access" };
            if (!SwingPointTrendInfo.ContainsKey(bars))
                this.SwingPointTrendInfo.Add(bars, new TrendInfo());
            return SwingPointTrendInfo[bars].SwingPointTestPriceResult;
        }
        public void SetSwingPointTestPriceResult(int bars, SwingPointTestPriceResult value)
        {
            if (!Security.AreSwingPointsSet(this.BarSize, bars))
                this.Security.SetSwingPointsAndTrends(bars, this.BarSize);
            //throw new UnknownErrorException() { message = "Must Initialize prior to access" };
            if (!SwingPointTrendInfo.ContainsKey(bars))
                this.SwingPointTrendInfo.Add(bars, new TrendInfo());
            SwingPointTrendInfo[bars].SwingPointTestPriceResult = value;
        }

        public SwingPointTestVolumeResult GetSwingPointTestVolumeResult(int bars)
        {
            if (!Security.AreSwingPointsSet(this.BarSize, bars))
                this.Security.SetSwingPointsAndTrends(bars, this.BarSize);
            //throw new UnknownErrorException() { message = "Must Initialize prior to access" };
            if (!SwingPointTrendInfo.ContainsKey(bars))
                this.SwingPointTrendInfo.Add(bars, new TrendInfo());
            return SwingPointTrendInfo[bars].SwingPointTestVolumeResult;
        }
        public void SetSwingPointTestVolumeResult(int bars, SwingPointTestVolumeResult value)
        {
            if (!Security.AreSwingPointsSet(this.BarSize, bars))
                this.Security.SetSwingPointsAndTrends(bars, this.BarSize);
            //throw new UnknownErrorException() { message = "Must Initialize prior to access" };
            if (!SwingPointTrendInfo.ContainsKey(bars))
                this.SwingPointTrendInfo.Add(bars, new TrendInfo());
            SwingPointTrendInfo[bars].SwingPointTestVolumeResult = value;
        }

        #endregion

        [NotMapped]
        public decimal Change
        {
            get
            {
                return (Close - Open);
            }
        }
        [NotMapped]
        public decimal Range
        {
            get
            {
                return (High - Low);
            }
        }

        public PriceBar()
        {
        }
        public PriceBar(DateTime barDateTime, Security security)
        {

            BarDateTime = barDateTime;
            Security = security ?? throw new ArgumentNullException(nameof(security));
        }
        public PriceBar(DateTime barDateTime,
            Security security,
            decimal open,
            decimal high,
            decimal low,
            decimal close)
        {
            BarDateTime = barDateTime;
            Security = security ?? throw new ArgumentNullException(nameof(security));
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

        public void SetPriceValues(decimal open, decimal high, decimal low, decimal close, long volume = 0)
        {
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }

        public PriceBar PriorBar
        {
            get
            {
                try
                {
                    switch (this.BarSize)
                    {
                        case PriceBarSize.Weekly:
                            return Security.GetPriceBar(PriorTradingWeekStart(BarDateTime), PriceBarSize.Weekly, false);
                        case PriceBarSize.Monthly:
                            return Security.GetPriceBar(PriorTradingMonthStart(BarDateTime), PriceBarSize.Monthly, false);
                        case PriceBarSize.Quarterly:
                            return Security.GetPriceBar(PriorTradingQuarterStart(BarDateTime), PriceBarSize.Quarterly, false);
                        case PriceBarSize.Daily:
                        default:
                            return Security.GetPriceBar(PriorTradingDay(BarDateTime), PriceBarSize.Daily, false);
                    }
                }
                catch (Exception) { return null; }
            }
        }
        public PriceBar NextBar
        {
            get
            {
                try
                {
                    switch (this.BarSize)
                    {
                        case PriceBarSize.Weekly:
                            return Security.GetPriceBar(NextTradingWeekStart(BarDateTime), PriceBarSize.Weekly, false);
                        case PriceBarSize.Monthly:
                            return Security.GetPriceBar(NextTradingMonthStart(BarDateTime), PriceBarSize.Monthly, false);
                        case PriceBarSize.Quarterly:
                            return Security.GetPriceBar(NextTradingQuarterStart(BarDateTime), PriceBarSize.Quarterly, false);
                        case PriceBarSize.Daily:
                        default:
                            return Security.GetPriceBar(NextTradingDay(BarDateTime), PriceBarSize.Daily, false);
                    }
                }
                catch (Exception) { return null; }
            }
        }

        public List<PriceBar> PriorBars(DateTime To, bool IncludeThisBar = false)
        {
            var ret = Security.GetPriceBars(To, this.BarDateTime, this.BarSize, true);

            if (!IncludeThisBar)
                ret.RemoveAll(x => x.BarDateTime == this.BarDateTime);
            return ret;

        }
        public List<PriceBar> PriorBars(int Count, bool IncludeThisBar = false)
        {
            var ret = Security.GetPriceBars(this.BarDateTime, Count, this.BarSize, true);

            if (!IncludeThisBar)
                ret.RemoveAll(x => x.BarDateTime == this.BarDateTime);

            return ret;
        }
        public List<PriceBar> PriorBars(bool IncludeThisBar = false)
        {
            var ret = Security.GetPriceBars(this.BarDateTime, this.BarSize, true);

            if (!IncludeThisBar)
                ret.RemoveAll(x => x.BarDateTime == this.BarDateTime);

            return ret;
        }
        public List<PriceBar> NextBars(DateTime To, bool IncludeThisBar = false)
        {
            var ret = Security.GetPriceBars(this.BarDateTime, To, this.BarSize, true);
            if (!IncludeThisBar)
                ret.RemoveAll(x => x.BarDateTime == this.BarDateTime);

            return ret;
        }
        public List<PriceBar> NextBars(int Count, bool IncludeThisBar = false)
        {
            var ret = Security.GetPriceBars(this.BarSize).Where(x => x.BarDateTime >= this.BarDateTime).ToList();

            if (!IncludeThisBar)
                ret.RemoveAll(x => x.BarDateTime == this.BarDateTime);

            return ret.Take(Count).ToList();
        }
        public List<PriceBar> NextBars(bool IncludeThisBar = false)
        {
            var ret = Security.GetPriceBars(this.BarSize).Where(x => x.BarDateTime >= this.BarDateTime).ToList();

            if (!IncludeThisBar)
                ret.RemoveAll(x => x.BarDateTime == this.BarDateTime);

            return ret;
        }

        public decimal TrueRange()
        {
            try
            {
                if (PriorBar == null)
                    return (High - Low);

                return Math.Max((High - Low), Math.Max(Math.Abs(High - PriorBar.Close), Math.Abs(Low - PriorBar.Close)));
            }
            catch (NullReferenceException) { return (High - Low); }
        }
        public decimal AverageTrueRange(int period = 14)
        {
            // Return saved value if it exists
            if (MyAverageTrueRange.period != period)
            {
                Security.SetSecurityAtrValues(period, this.BarSize);
            }

            return MyAverageTrueRange.atr;            
        }
        [NotMapped]
        public (int period, decimal atr) MyAverageTrueRange = (0, 0);
    }

    /// <summary>
    /// IEquatable implementation
    /// </summary>
    public partial class PriceBar : IEquatable<PriceBar>
    {
        public override bool Equals(object obj)
        {
            return Equals(obj as PriceBar);
        }
        public bool Equals(PriceBar other)
        {
            return other != null &&
                   BarDateTime == other.BarDateTime &&
                   EqualityComparer<Security>.Default.Equals(Security, other.Security);
        }
        public override int GetHashCode()
        {
            var hashCode = -1472049590;
            hashCode = hashCode * -1521134295 + BarDateTime.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Security>.Default.GetHashCode(Security);
            return hashCode;
        }
        public static bool operator ==(PriceBar bar1, PriceBar bar2)
        {
            return EqualityComparer<PriceBar>.Default.Equals(bar1, bar2);
        }
        public static bool operator !=(PriceBar bar1, PriceBar bar2)
        {
            return !(bar1 == bar2);
        }
    }

}
