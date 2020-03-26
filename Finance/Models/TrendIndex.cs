using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finance
{
    public class TrendIndex : IEquatable<TrendIndex>
    {
        [Key]
        public int TrendIndexId { get; set; }

        public string IndexName { get; set; }
        public PriceBarSize TrendPriceBarSize { get; set; }
        public int IndexSwingpointBarCount { get; set; }

        public virtual List<TrendIndexDay> IndexEntries { get; set; } = new List<TrendIndexDay>();

        public TrendIndex() { }
        public TrendIndex(string name, int indexSwingpointBarCount, PriceBarSize trendPriceBarSize)
        {
            IndexName = name;
            IndexSwingpointBarCount = indexSwingpointBarCount;
            TrendPriceBarSize = trendPriceBarSize;
        }

        [NotMapped]
        public DateTime? LatestDate
        {
            get
            {
                if (IndexEntries.Count > 0)
                    return (from day in IndexEntries select day.AsOf).Max();
                else
                    return null;
            }
        }

        public TrendIndexDay GetLatestIndexDay()
        {
            return IndexEntries.Find(x => x.AsOf == IndexEntries.Max(y => y.AsOf));
        }
        public TrendIndexDay GetIndexDay(DateTime AsOf, bool create = false)
        {
            var ret = IndexEntries.Find(x => x.AsOf == AsOf);
            if (ret != null || !create)
                return ret;

            return IndexEntries.AddAndReturn(new TrendIndexDay(this.IndexName, AsOf) { Parent = this });
        }
        public TrendQualification GetStrongestTrend(DateTime AsOf)
        {
            var entry = IndexEntries.Find(x => x.AsOf == AsOf);
            if (entry == null)
                return TrendQualification.NotSet;

            return entry.GetStrongestTrend();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TrendIndex);
        }
        public bool Equals(TrendIndex other)
        {
            return other != null &&
                   IndexName == other.IndexName &&
                   TrendPriceBarSize == other.TrendPriceBarSize &&
                   IndexSwingpointBarCount == other.IndexSwingpointBarCount;
        }
        public override int GetHashCode()
        {
            var hashCode = 2033134462;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(IndexName);
            hashCode = hashCode * -1521134295 + TrendPriceBarSize.GetHashCode();
            hashCode = hashCode * -1521134295 + IndexSwingpointBarCount.GetHashCode();
            return hashCode;
        }
        public static bool operator ==(TrendIndex left, TrendIndex right)
        {
            return EqualityComparer<TrendIndex>.Default.Equals(left, right);
        }
        public static bool operator !=(TrendIndex left, TrendIndex right)
        {
            return !(left == right);
        }
    }

    public class TrendIndexDay : IEquatable<TrendIndexDay>
    {
        [Key]
        public int TrendIndexDayId { get; set; }

        public string Name { get; set; }
        public DateTime AsOf { get; set; }

        public virtual TrendIndex Parent { get; set; }

        public decimal AmbSideways { get; set; } = 0;
        public decimal SusSideways { get; set; } = 0;
        public decimal ConSideways { get; set; } = 0;
        public decimal SusBullish { get; set; } = 0;
        public decimal ConBullish { get; set; } = 0;
        public decimal SusBearish { get; set; } = 0;
        public decimal ConBearish { get; set; } = 0;

        public int SecurityCount { get; set; }

        [NotMapped]
        public List<(TrendQualification trend, decimal percent)> TrendPercentages
        {
            get
            {
                return new List<(TrendQualification trend, decimal percent)>()
                {
                    (TrendQualification.AmbivalentSideways, AmbSideways),
                    (TrendQualification.SuspectSideways, SusSideways),
                    (TrendQualification.ConfirmedSideways, ConSideways),
                    (TrendQualification.SuspectBullish, SusBullish),
                    (TrendQualification.ConfirmedBullish, ConBullish),
                    (TrendQualification.SuspectBearish, SusBearish),
                    (TrendQualification.ConfirmedBearish, ConBearish)
                };
            }
        }

        public TrendIndexDay() { }
        public TrendIndexDay(string name, DateTime asOf)
        {
            Name = name;
            AsOf = asOf;
        }

        public void AddTrendEntry(TrendQualification trend, decimal percent)
        {
            switch (trend)
            {
                case TrendQualification.AmbivalentSideways:
                    AmbSideways = percent;
                    break;
                case TrendQualification.SuspectSideways:
                    SusSideways = percent;
                    break;
                case TrendQualification.ConfirmedSideways:
                    ConSideways = percent;
                    break;
                case TrendQualification.SuspectBullish:
                    SusBullish = percent;
                    break;
                case TrendQualification.ConfirmedBullish:
                    ConBullish = percent;
                    break;
                case TrendQualification.SuspectBearish:
                    SusBearish = percent;
                    break;
                case TrendQualification.ConfirmedBearish:
                    ConBearish = percent;
                    break;
            }
        }

        public TrendQualification GetStrongestTrend()
        {
            var sortedList = TrendPercentages.OrderByDescending(x => x.percent).ToList();
            var largestPercent = sortedList.First().percent;

            //
            // If there is a tie, return the largest value (most bearish)
            //
            if (TrendPercentages.Where(x => x.percent == largestPercent).Count() > 1)
            {
                return (from entry in TrendPercentages.Where(x => x.percent == largestPercent) select entry.trend).Max();
            }

            return TrendPercentages.Where(x => x.percent == largestPercent).FirstOrDefault().trend;
        }
        public (decimal bullish, decimal sideways, decimal bearish) GetTrendSummary()
        {
            decimal bullish = (from entry in TrendPercentages
                               where entry.trend == TrendQualification.SuspectBullish || entry.trend == TrendQualification.ConfirmedBullish
                               select entry.percent).Sum();

            decimal bearish = (from entry in TrendPercentages
                               where entry.trend == TrendQualification.SuspectBearish || entry.trend == TrendQualification.ConfirmedBearish
                               select entry.percent).Sum();

            decimal sideways = (from entry in TrendPercentages
                                where entry.trend == TrendQualification.AmbivalentSideways || entry.trend == TrendQualification.SuspectSideways || entry.trend == TrendQualification.ConfirmedSideways
                                select entry.percent).Sum();

            return (bullish, bearish, sideways);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TrendIndexDay);
        }
        public bool Equals(TrendIndexDay other)
        {
            return other != null &&
                   Name == other.Name &&
                   AsOf == other.AsOf &&
                   EqualityComparer<TrendIndex>.Default.Equals(Parent, other.Parent);
        }
        public override int GetHashCode()
        {
            var hashCode = 1856363107;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + AsOf.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<TrendIndex>.Default.GetHashCode(Parent);
            return hashCode;
        }
        public static bool operator ==(TrendIndexDay left, TrendIndexDay right)
        {
            return EqualityComparer<TrendIndexDay>.Default.Equals(left, right);
        }
        public static bool operator !=(TrendIndexDay left, TrendIndexDay right)
        {
            return !(left == right);
        }
    }

}
