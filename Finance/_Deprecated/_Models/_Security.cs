using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static Finance.Helpers;

namespace Finance.Models
{
    /// <summary>
    /// Properties
    /// </summary>
    public partial class Security
    {
        [Key]
        public string Ticker { get; set; }

        public string LongName { get; set; }

        public string Exchange { get; set; } = "UNK";

        public SecurityType SecurityType { get; set; } = SecurityType.Unknown;

        public virtual List<FundamentalDataPoint> FundamentalData { get; set; } = new List<FundamentalDataPoint>();

        public virtual List<PriceBar> PriceBarData { get; set; } = new List<PriceBar>();

    }

    /// <summary>
    /// Constructor Methods
    /// </summary>
    public partial class Security
    {
        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public Security()
        {
        }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="ticker"></param>
        public Security(string ticker, SecurityType securityType = SecurityType.USCommonEquity)
        {
            Ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
        }

    }

    /// <summary>
    /// IEquatable implementation
    /// </summary>
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

    /// <summary>
    /// Price Bar collection methods
    /// </summary>
    public partial class Security
    {
        /// <summary>
        /// Returns a price bar for a given date.  By default, if no bar exists, a new bar is created, added to the collection, and returned.
        /// </summary>
        /// <param name="BarDate"></param>
        /// <param name="Create"></param>
        /// <returns></returns>
        public PriceBar GetPriceBar(DateTime BarDate, bool Create = true)
        {
            if (!Calendar.IsTradingDay(BarDate))
                throw new InvalidTradingDateException();

            if (PriceBarData == null) PriceBarData = new List<PriceBar>();

            return PriceBarData.Find(x => x.BarDateTime == BarDate) ?? (Create ? PriceBarData.AddAndReturn(new PriceBar(BarDate, this)) : null);
        }

        public void SetFundamentalDataPoints(List<FundamentalDataPoint> dataPoints)
        {
            dataPoints.ForEach(x => x.Security = this);
            FundamentalData = dataPoints;
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
            // TODO: Test this
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
        /// Returns the most recent pricebar in the series
        /// </summary>
        /// <returns></returns>
        public PriceBar LastBar()
        {
            return PriceBarData.OrderBy(x => x.BarDateTime).LastOrDefault();
        }
    }
}
