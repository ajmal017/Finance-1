using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance;
using Finance.Data;
using System.Threading;
using static Finance.Helpers;
using static Finance.Calendar;
using static Finance.Logger;

namespace Finance
{
    public class IndexManager
    {
        /*
         *  Maintains functions and methods for generating and analysing composite security indicies
         *  
         *  Indicies are stored as Security objects created at run-time and not saved to the database (for now)
         */

        private static IndexManager _Instance { get; set; }
        public static IndexManager Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new IndexManager();
                return _Instance;
            }
        }

        public IndexDatabase Database { get; private set; }
        private List<TrendIndex> SectorTrends { get; set; }

        public IndexManager()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void Initialize()
        {
            Database = new IndexDatabase();
            SectorTrends = Database.GetAllTrendIndices();
        }

        public void UpdateAllIndices()
        {
            new Thread(() =>
            {
                UpdateSectorTrendIndices();
            }).Start();
        }
        public void RepopulateAllIndices()
        {
            new Thread(() =>
            {
                PopulateSectorTrendIndices(Settings.Instance.Sector_Trend_Bar_Size);
            }).Start();
        }

        #region Sector Trend Indices

        public TrendIndex GetTrendIndexBySector(string sector, PriceBarSize priceBarSize)
        {
            if (Settings.Instance.MarketSectors.Find(x => x.ToUpper() == sector.ToUpper()) == null)
                return null;

            if (SectorTrends == null)
                PopulateSectorTrendIndices(priceBarSize);

            var ret = SectorTrends.Find(x => x.IndexName == sector &&
                    x.TrendPriceBarSize == priceBarSize &&
                    x.IndexSwingpointBarCount == Settings.Instance.Sector_Trend_Bar_Count);

            if (ret == null)
                PopulateSectorTrendIndices(priceBarSize);

            return SectorTrends.Find(x => x.IndexName == sector &&
                    x.TrendPriceBarSize == priceBarSize &&
                    x.IndexSwingpointBarCount == Settings.Instance.Sector_Trend_Bar_Count);
        }
        public List<TrendIndex> GetAllTrendIndices(PriceBarSize priceBarSize)
        {
            return SectorTrends.Where(x => x.TrendPriceBarSize == priceBarSize).
                                Where(x => x.IndexSwingpointBarCount == Settings.Instance.DefaultSwingpointBarCount).ToList();
        }

        private void PopulateSectorTrendIndices(PriceBarSize priceBarSize)
        {
            foreach (string sector in Settings.Instance.MarketSectors)
            {
                Log(new LogMessage("IndexManager", $"Populating Trend Index [{sector} {priceBarSize.ToString()}]"));
                //
                // If a trend with the same identifiers was loaded, find and remove from local list
                //
                var existingTrendIndex =
                    SectorTrends.Find(x => x.IndexName == sector &&
                    x.TrendPriceBarSize == priceBarSize &&
                    x.IndexSwingpointBarCount == Settings.Instance.Sector_Trend_Bar_Count);

                if (existingTrendIndex != null)
                    SectorTrends.Remove(existingTrendIndex);

                //
                // Create and add the new trend to the list
                //
                var newTrend = SectorTrends.AddAndReturn(CreateSectorIndex(sector, RefDataManager.Instance.GetAllSecurities(), priceBarSize));

                //
                // Save to database - this will overwrite an existing trend with the same identifiers
                //
                Database.SetTrendIndex(newTrend);
            }

            Log(new LogMessage("IndexManager", $"Trend Index Population Complete"));
        }

        private void UpdateSectorTrendIndices()
        {
            if (SectorTrends.Count == 0)
                PopulateSectorTrendIndices(Settings.Instance.Sector_Trend_Bar_Size);
            else
            {
                foreach (var index in SectorTrends.ToList())
                {
                    UpdateSectorTrendIndex(index.IndexName, index.TrendPriceBarSize);
                }
            }
        }
        private void UpdateSectorTrendIndex(string sectorName, PriceBarSize priceBarSize)
        {

            Log(new LogMessage("IndexManager", $"Updating Trend Index [{sectorName} {priceBarSize.ToString()} {Settings.Instance.Sector_Trend_Bar_Count}]"));

            //
            // Find existing trend to update, or skip if none is found
            //
            var existingTrendIndex =
                SectorTrends.Find(x => x.IndexName == sectorName &&
                x.TrendPriceBarSize == priceBarSize &&
                x.IndexSwingpointBarCount == Settings.Instance.Sector_Trend_Bar_Count);

            TrendIndex updateIndexTrend = null;

            //
            // If the index exists, remove from local list, update, and re-add; otherwise create a new one and add
            //
            if (existingTrendIndex == null)
            {
                updateIndexTrend = CreateSectorIndex(sectorName, RefDataManager.Instance.GetAllSecurities(), priceBarSize);

                if (updateIndexTrend != null)
                    SectorTrends.Add(updateIndexTrend);
            }
            else
            {
                SectorTrends.Remove(existingTrendIndex);

                updateIndexTrend = UpdateSectorIndex(existingTrendIndex,
                    RefDataManager.Instance.GetAllSecurities().Where(x => x.Sector == sectorName).ToList());

                if (updateIndexTrend != null)
                    SectorTrends.Add(updateIndexTrend);
            }

            //
            // Save to database - this will overwrite an existing trend with the same identifiers
            //
            if (updateIndexTrend != null)
                Database.SetTrendIndex(updateIndexTrend);
        }

        #endregion

        #region Index Builders

        public static TrendIndex CreateSectorIndex(string sectorName, List<Security> securities, PriceBarSize priceBarSize)
        {
            var trendIndex = IndexManager.Instance.Database.GetTrendIndex(sectorName, priceBarSize, Settings.Instance.Sector_Trend_Bar_Count);

            var usedSecurities = ApplyDefaultStaticFilters(securities).
                Where(x => x.Sector == sectorName);

            if (usedSecurities.Count() == 0)
                return trendIndex;

            var currentDate = EarliestDate(usedSecurities, Settings.Instance.Sector_Trend_Bar_Size);
            var latestDate = LatestDate(usedSecurities, Settings.Instance.Sector_Trend_Bar_Size);

            foreach (Security security in usedSecurities)
            {
                security.SetSwingPointsAndTrends(Settings.Instance.Sector_Trend_Bar_Count, Settings.Instance.Sector_Trend_Bar_Size);
            }

            while (currentDate <= latestDate)
            {
                TrendIndexDay indexDay = trendIndex.GetIndexDay(currentDate, true);

                var dailySecurityList = ApplyDefaultAsOfFilters(usedSecurities, currentDate);

                if (dailySecurityList.Count() > 0)
                {
                    foreach (TrendQualification trend in Enum.GetValues(typeof(TrendQualification)))
                    {
                        if (trend == TrendQualification.NotSet)
                            continue;

                        var totalTrending = dailySecurityList.Where(x => x.GetPriceBar(currentDate, Settings.Instance.Sector_Trend_Bar_Size).
                                                            GetTrendType(Settings.Instance.Sector_Trend_Bar_Count) == trend).Count();

                        indexDay.AddTrendEntry(trend, totalTrending.ToDecimal() / dailySecurityList.Count().ToDecimal());
                    }

                    indexDay.SecurityCount = dailySecurityList.Count();
                }

                switch (Settings.Instance.Sector_Trend_Bar_Size)
                {
                    case PriceBarSize.Daily:
                        currentDate = NextTradingDay(currentDate);
                        break;
                    case PriceBarSize.Weekly:
                        currentDate = NextTradingWeekStart(currentDate);
                        break;
                    case PriceBarSize.Monthly:
                        currentDate = NextTradingMonthStart(currentDate);
                        break;
                    case PriceBarSize.Quarterly:
                        currentDate = NextTradingQuarterStart(currentDate);
                        break;
                }
            }

            return trendIndex;
        }
        public static TrendIndex UpdateSectorIndex(TrendIndex indexToUpdate, List<Security> securities)
        {
            if (securities.Count == 0)
                return null;

            var usedSecurities = securities.
                 Where(x => x.Sector == indexToUpdate.IndexName).
                 Where(x => x.DailyPriceBarData.Count > 0).
                 Where(x => !x.MissingData && !x.Excluded).ToList();

            DateTime currentDate = indexToUpdate.LatestDate ??
                (from sec in usedSecurities select sec.GetFirstBar(indexToUpdate.TrendPriceBarSize).BarDateTime).Min();

            var priceBarSize = indexToUpdate.TrendPriceBarSize;
            var barCount = indexToUpdate.IndexSwingpointBarCount;

            foreach (Security security in usedSecurities)
            {
                security.SetSwingPointsAndTrends(barCount, priceBarSize);
            }

            while (currentDate <= LatestDate(usedSecurities, priceBarSize))
            {
                TrendIndexDay indexDay = indexToUpdate.GetIndexDay(currentDate, true);

                var dailySecurityList = ApplyDefaultAsOfFilters(usedSecurities, currentDate);
                if (dailySecurityList.Count() > 0)
                {
                    foreach (TrendQualification trend in Enum.GetValues(typeof(TrendQualification)))
                    {
                        if (trend == TrendQualification.NotSet)
                            continue;

                        var totalTrending = dailySecurityList.Where(x => x.GetPriceBar(currentDate, priceBarSize).GetTrendType(barCount) == trend).Count();
                        indexDay.AddTrendEntry(trend, totalTrending.ToDecimal() / dailySecurityList.Count().ToDecimal());
                    }
                }

                switch (priceBarSize)
                {
                    case PriceBarSize.Daily:
                        currentDate = NextTradingDay(currentDate);
                        break;
                    case PriceBarSize.Weekly:
                        currentDate = NextTradingWeekStart(currentDate);
                        break;
                    case PriceBarSize.Monthly:
                        currentDate = NextTradingMonthStart(currentDate);
                        break;
                    case PriceBarSize.Quarterly:
                        currentDate = NextTradingQuarterStart(currentDate);
                        break;
                }
            }

            return indexToUpdate;
        }

        private static DateTime EarliestDate(IEnumerable<Security> securities, PriceBarSize priceBarSize)
        {
            return (from sec in securities
                    where sec.DailyPriceBarData.Count > 0
                    select sec.GetFirstBar(Settings.Instance.Sector_Trend_Bar_Size).BarDateTime).Min();
        }
        private static DateTime LatestDate(IEnumerable<Security> securities, PriceBarSize priceBarSize)
        {
            return (from sec in securities
                    where sec.DailyPriceBarData.Count > 0
                    select sec.GetLastBar(Settings.Instance.Sector_Trend_Bar_Size).BarDateTime).Max();
        }

        private static IEnumerable<Security> ApplyDefaultStaticFilters(IEnumerable<Security> securities)
        {
            //
            // Applies filters which are not date-dependent
            //
            var STG = Settings.Instance;

            return securities.
                Where(x => STG.IncludeEtfsInIndex ? true : x.SecurityType != SecurityType.ETF).
                Where(x => !x.Excluded);

        }
        private static IEnumerable<Security> ApplyDefaultAsOfFilters(IEnumerable<Security> securities, DateTime AsOf)
        {
            //
            // Applies filters which ARE date dependent
            //
            var STG = Settings.Instance;

            return securities.
                Where(x => x.HasBar(AsOf, PriceBarSize.Daily)).
                Where(x => x.AverageVolume(AsOf, PriceBarSize.Daily, 30) >= STG.Minimum_Index_Inclusion_Volume_30d).
                Where(x => x.GetPriceBar(AsOf, PriceBarSize.Daily).Close >= STG.Minimum_Index_Inclusion_Price);
        }

        #endregion

    }
}
