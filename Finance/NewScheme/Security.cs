using Finance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Helpers;

namespace Finance
{
    public partial class Security
    {

        [Key]
        public string Ticker { get; set; }
        public string LongName { get; set; }
        public string Exchange { get; set; } = "UNK";

        public SecurityType SecurityType { get; set; } = SecurityType.Unknown;
        public bool DataUpToDate
        {
            get
            {
                if (PriceBarData.Count == 0)
                    return false;

                var lastDay = DateTime.Today;

                if (!Calendar.IsTradingDay(lastDay) || !AfterHours(DateTime.Now))
                    lastDay = Calendar.PriorTradingDay(lastDay);

                if (LastBar().BarDateTime == lastDay)
                    return true;
                return false;
            }
        }

        public virtual List<PriceBar> PriceBarData { get; set; } = new List<PriceBar>();

        public Security() { }
        public Security(string ticker, SecurityType securityType = SecurityType.USCommonEquity)
        {
            Ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
        }

        /// <summary>
        /// Populates all associated price bars with ATR calculation for a given period
        /// </summary>
        /// <param name="period"></param>
        public void SetSecurityAtrValues(int period)
        {
            // TODO: Smoothing? https://www.macroption.com/atr-excel-wilder/

            PriceBarData = PriceBarData.OrderBy(x => x.BarDateTime).ToList();

            for (int i = 0; i < PriceBarData.Count; i++)
            {
                if (i < period - 1)
                {
                    decimal atr = PriceBarData[i].TrueRange();
                    PriceBarData[i].MyAverageTrueRange.TryAdd(period, atr);
                }
                else if (i == period - 1)
                {
                    decimal atr = (PriceBarData.Take(period - 1).Sum(x => x.MyAverageTrueRange[period]) + PriceBarData[i].TrueRange()) / period;
                    PriceBarData[i].MyAverageTrueRange.TryAdd(period, atr);
                }
                else
                {
                    decimal atr = ((PriceBarData[i - 1].MyAverageTrueRange[period] * (period - 1)) + PriceBarData[i - 1].TrueRange()) / period;
                    PriceBarData[i].MyAverageTrueRange.TryAdd(period, atr);
                }
            }
        }

        #region Price Bar Methods

        /// <summary>
        /// Returns a price bar for a given date.  By default, if no bar exists, a new bar is created, added to the collection, and returned.
        /// </summary>
        /// <param name="BarDate"></param>
        /// <param name="Create"></param>
        /// <returns></returns>
        public PriceBar GetPriceBar(DateTime BarDate, bool Create = false)
        {
            if (!Calendar.IsTradingDay(BarDate))
                throw new InvalidTradingDateException();

            if (PriceBarData == null) PriceBarData = new List<PriceBar>();

            return PriceBarData.Find(x => x.BarDateTime == BarDate) ?? (Create ? PriceBarData.AddAndReturn(new PriceBar(BarDate, this)) : null);
        }

        /// <summary>
        /// Returns all existing price bars
        /// </summary>
        /// <returns></returns>
        public List<PriceBar> GetPriceBars()
        {
            var ret = (from bar in PriceBarData
                       select bar).OrderByDescending(x => x.BarDateTime).ToList();

            return ret;
        }

        /// <summary>
        /// Returns all price bars which fall within specified dates, inclusive.  Bars are not created if not found.
        /// </summary>
        /// <param name="StartBarDate"></param>
        /// <param name="EndBarDate"></param>
        /// <returns></returns>
        public List<PriceBar> GetPriceBars(DateTime StartBarDate, DateTime EndBarDate)
        {
            if (StartBarDate.CompareTo(EndBarDate) > 0)
                throw new FormatException(message: "StartDate must be before or equal to EndDate");

            var ret = (from bar in PriceBarData
                       where bar.BarDateTime.IsBetween(StartBarDate, EndBarDate)
                       select bar).OrderByDescending(x => x.BarDateTime).ToList();

            return ret;
        }

        /// <summary>
        /// Returns a requested number of bars preceding the EndBarDate
        /// </summary>
        /// <param name="EndBarDate"></param>
        /// <param name="Count"></param>
        /// <param name="includeSelf"></param>
        /// <returns></returns>
        public List<PriceBar> GetPriceBars(DateTime EndBarDate, int Count, bool includeSelf = false)
        {
            if (!includeSelf)
                EndBarDate = Calendar.PriorTradingDay(EndBarDate);

            DateTime StartBarDate = EndBarDate;

            while (--Count > 0)
            {
                StartBarDate = Calendar.PriorTradingDay(StartBarDate);
            }

            return GetPriceBars(StartBarDate, EndBarDate);

        }

        /// <summary>
        /// Returns a requested number of bars preceding the EndBarDate
        /// </summary>
        /// <param name="EndBarDate"></param>
        /// <param name="Count"></param>
        /// <param name="includeSelf"></param>
        /// <returns></returns>
        public List<PriceBar> GetPriceBars(DateTime EndBarDate, bool includeSelf = false)
        {
            if (!includeSelf)
                EndBarDate = Calendar.PriorTradingDay(EndBarDate);

            var ret = (from bar in PriceBarData
                       where bar.BarDateTime < EndBarDate
                       select bar).OrderByDescending(x => x.BarDateTime).ToList();

            return ret;
        }

        /// <summary>
        /// Returns the most recent pricebar in the series
        /// </summary>
        /// <returns></returns>
        public PriceBar LastBar()
        {
            return PriceBarData.OrderBy(x => x.BarDateTime).LastOrDefault();
        }

        #endregion
        #region UI Info Display Methods

        [UiDisplayText(-1)]
        public string CompanyName()
        {
            string title = "Company Name:";
            return string.Format($"{title,-15} {LongName}");
        }

        [UiDisplayText(0)]
        public string NumberOfBars()
        {
            string title = "Bar Count:";
            return string.Format($"{title,-15} {PriceBarData.Count}");
        }

        [UiDisplayText(1)]
        public string FirstBar()
        {
            if (PriceBarData.Count == 0)
                return "Earliest Bar: NO DATA";

            string title = "Earliest Bar:";
            return string.Format($"{title,-15} {(from bar in PriceBarData select bar.BarDateTime).Min().ToShortDateString()}");
        }

        [UiDisplayText(2)]
        public string LatestBar()
        {
            if (PriceBarData.Count == 0)
                return "Latest Bar: NO DATA";

            string title = "Latest Bar:";
            return string.Format($"{title,-15} {(from bar in PriceBarData select bar.BarDateTime).Max().ToShortDateString()}");
        }

        [UiDisplayText(3)]
        public string UpToDate()
        {
            string title = "Up to date:";
            return string.Format($"{title,-15} {(DataUpToDate ? "Yes" : "No")}");
        }

        [UiDisplayText(4)]
        public string LastPrice()
        {
            if (PriceBarData.Count == 0)
                return "Last Close: NO DATA";

            string title = "Last Close:";
            return string.Format($"{title,-15} {LastBar().Close:$0.00}");
        }

        #endregion

    }
    public partial class Security : IEquatable<Security>
    {
        public override bool Equals(object obj)
        {
            return Equals(obj as Security);
        }
        public bool Equals(Security other)
        {
            return other != null &&
                   Ticker == other.Ticker;
        }
        public override int GetHashCode()
        {
            return 1453024139 + EqualityComparer<string>.Default.GetHashCode(Ticker);
        }
        public static bool operator ==(Security security1, Security security2)
        {
            return EqualityComparer<Security>.Default.Equals(security1, security2);
        }
        public static bool operator !=(Security security1, Security security2)
        {
            return !(security1 == security2);
        }
    }
}
