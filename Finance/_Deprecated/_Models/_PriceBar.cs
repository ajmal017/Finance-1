using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Calendar;
using static Finance.Helpers;

namespace Finance.Models
{
    /// <summary>
    /// Properties
    /// </summary>
    public partial class PriceBar
    {
        [Key]
        public int PriceBarId { get; set; }
                
        public DateTime BarDateTime { get; set; }
                
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

    }


    /// <summary>
    /// Calculation Methods
    /// </summary>
    public partial class PriceBar
    {
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

        /// <summary>
        /// Returns the True Range of the current bar, which is defined as the greatest of:
        /// -Current HIGH minus previous CLOSE
        /// -Absolute value of the current HIGH minus previous CLOSE
        /// -Absolute value of the current LOW minus previous CLOSE
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Stores a list of Average True Ranges calculated for given periods, to speed processing
        /// </summary>
        [NotMapped]
        private Dictionary<int, decimal> MyAverageTrueRange = new Dictionary<int, decimal>();

        /// <summary>
        /// Returns the smoothed Average True Range for a given period by recursing backwards from the current bar.
        /// </summary>
        /// <param name="period"></param>
        /// <param name="movingAverageMethod"></param>
        /// <returns></returns>
        public decimal AverageTrueRange(int period = 14)
        {
            // TODO: Smoothing? https://www.macroption.com/atr-excel-wilder/

            try
            {
                // Return saved value if it exists
                if (MyAverageTrueRange.ContainsKey(period))
                    return MyAverageTrueRange[period];

                var pbc = PriorBars().Count;


                if (PriorBars().Count < period - 1)
                {
                    // If this bar falls within the first 'period' bars of stored data, save the ATR as the TR of this bar and return
                    return MyAverageTrueRange.AddAndReturn(period, TrueRange());
                }
                else if (PriorBars().Count == period - 1)
                {
                    // First ATR is the simple average of all prior true ranges
                    decimal ret = (from bar in PriorBars() select bar.TrueRange()).Average();
                    return MyAverageTrueRange.AddAndReturn(period, ret);
                }
                else
                {
                    // Calculate the ATR for this period, add to the list, and return.
                    return MyAverageTrueRange.AddAndReturn(period, (PriorBar.AverageTrueRange(period) * (period - 1) + TrueRange()) / period);
                }

            }
            catch (Exception ex)
            {
                var pbc2 = PriorBars().Count;

                throw ex;
            }


        }
    }

    /// <summary>
    /// Prior and Next
    /// </summary>
    public partial class PriceBar
    {
        public virtual PriceBar PriorBar
        {
            get
            {
                try
                {
                    return Security.GetPriceBar(PriorTradingDay(BarDateTime), false);
                }
                catch (Exception) { return null; }
            }
        }

        public virtual PriceBar NextBar
        {
            get
            {
                try
                {
                    return Security.GetPriceBar(NextTradingDay(BarDateTime), false);
                }
                catch (Exception) { return null; }
            }
        }

        /// <summary>
        /// Returns a list of Count bars occurring prior to the current bar, in descending orderf
        /// </summary>
        /// <returns></returns>
        public List<PriceBar> PriorBars(DateTime To, bool IncludeThisBar = false)
        {
            try
            {
                if (IncludeThisBar)
                    return Security.GetPriceBars(To, BarDateTime).OrderByDescending(x => x.BarDateTime).ToList();
                else
                    return Security.GetPriceBars(To, PriorBar.BarDateTime).OrderByDescending(x => x.BarDateTime).ToList();
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"EXCEPTION:{Helpers.GetCurrentMethod()}  {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns a list of Count bars occurring subsequent to the current bar, in ascending order
        /// </summary>
        /// <returns></returns>
        public List<PriceBar> NextBars(DateTime To, bool IncludeThisBar = false)
        {
            try
            {
                if (IncludeThisBar)
                    return Security.GetPriceBars(BarDateTime, To).OrderBy(x => x.BarDateTime).ToList();
                else
                    return Security.GetPriceBars(NextBar.BarDateTime, To).OrderBy(x => x.BarDateTime).ToList();
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"EXCEPTION:{Helpers.GetCurrentMethod()}  {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns a list of Count bars occurring prior to the current bar. Including the current bar will count against the Count requested
        /// </summary>
        /// <param name="Count"></param>
        /// <returns></returns>
        public List<PriceBar> PriorBars(int Count, bool IncludeThisBar = false)
        {
            try
            {
                if (IncludeThisBar)
                    return (from bar in Security.PriceBarData
                            where bar.BarDateTime <= BarDateTime
                            select bar).OrderByDescending(x => x.BarDateTime).Take(Count).ToList();
                else
                    return (from bar in Security.PriceBarData
                            where bar.BarDateTime < BarDateTime
                            select bar).OrderByDescending(x => x.BarDateTime).Take(Count).ToList();
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"EXCEPTION:{Helpers.GetCurrentMethod()}  {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns a list of Count bars occurring next to and excluding the current bar.  Including the current bar will count against the Count requested
        /// </summary>
        /// <param name="Count"></param>
        /// <returns></returns>
        public List<PriceBar> NextBars(int Count, bool IncludeThisBar = false)
        {
            try
            {
                if (IncludeThisBar)
                    return (from bar in Security.PriceBarData
                            where bar.BarDateTime >= BarDateTime
                            select bar).OrderBy(x => x.BarDateTime).Take(Count).ToList();
                else
                    return (from bar in Security.PriceBarData
                            where bar.BarDateTime < BarDateTime
                            select bar).OrderByDescending(x => x.BarDateTime).Take(Count).ToList();
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"EXCEPTION:{Helpers.GetCurrentMethod()}  {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns all prior bars
        /// </summary>
        /// <param name="includeThisBar"></param>
        /// <returns></returns>
        public List<PriceBar> PriorBars(bool IncludeThisBar = false)
        {
            try
            {
                if (IncludeThisBar)
                    return (from bar in Security.PriceBarData where bar.BarDateTime <= BarDateTime select bar).OrderByDescending(x => x.BarDateTime).ToList();
                else
                    return (from bar in Security.PriceBarData where bar.BarDateTime < BarDateTime select bar).OrderByDescending(x => x.BarDateTime).ToList();
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"EXCEPTION:{Helpers.GetCurrentMethod()}  {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns all subsequent bars
        /// </summary>
        /// <param name="includeThisBar"></param>
        /// <returns></returns>
        public List<PriceBar> NextBars(bool IncludeThisBar = false)
        {
            try
            {
                if (IncludeThisBar)
                    return (from bar in Security.PriceBarData where bar.BarDateTime >= BarDateTime select bar).OrderBy(x => x.BarDateTime).ToList();
                else
                    return (from bar in Security.PriceBarData where bar.BarDateTime > BarDateTime select bar).OrderBy(x => x.BarDateTime).ToList();
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"EXCEPTION:{Helpers.GetCurrentMethod()}  {ex.Message}");
                throw;
            }
        }

    }

    /// <summary>
    /// Constructor Methods
    /// </summary>
    public partial class PriceBar
    {
        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public PriceBar()
        {
        }

        /// <summary>
        /// Parameterized Constructor
        /// </summary>
        /// <param name="barDateTime"></param>
        /// <param name="security"></param>
        public PriceBar(DateTime barDateTime, Security security)
        {

            BarDateTime = barDateTime;
            Security = security ?? throw new ArgumentNullException(nameof(security));
        }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="barDateTime"></param>
        /// <param name="security"></param>
        /// <param name="open"></param>
        /// <param name="high"></param>
        /// <param name="low"></param>
        /// <param name="close"></param>
        public PriceBar(DateTime barDateTime, Security security, decimal open, decimal high, decimal low, decimal close)
        {
            BarDateTime = barDateTime;
            Security = security ?? throw new ArgumentNullException(nameof(security));
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

        /// <summary>
        /// Sets price and volume values
        /// </summary>
        /// <param name="open"></param>
        /// <param name="high"></param>
        /// <param name="low"></param>
        /// <param name="close"></param>
        /// <param name="volume"></param>
        public void SetPriceValues(decimal open, decimal high, decimal low, decimal close, long volume = 0)
        {
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }
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
