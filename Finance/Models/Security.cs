using Finance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using static Finance.Helpers;
using static Finance.Calendar;

namespace Finance
{
    public partial class Security : INotifyPropertyChanged
    {

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        [Key]
        public string Ticker { get; set; }

        public SecurityType SecurityType { get; set; } = SecurityType.Unknown;

        public string LongName { get; set; }
        public string Exchange { get; set; } = "UNK";

        public string Industry { get; set; } = "NOT_SET";
        public string Sector { get; set; } = "NOT_SET";
        public int SicCode { get; set; } = 0;

        [NotMapped]
        public string SicCodeName
        {
            get
            {
                if (this.SicCode == 0)
                    return "NOT SET";

                return Helpers.GetIndustryBySIC(this.SicCode);
            }
        }
        [NotMapped]
        public DateTime LastUpdate { get; set; }
        [NotMapped]
        public bool DataUpToDate
        {
            get
            {
                var lastDay = DateTime.Today;

                if (!Calendar.IsTradingDay(lastDay) || !AfterHours(DateTime.Now))
                    lastDay = Calendar.PriorTradingDay(lastDay);

                if (LastUpdate == lastDay)
                    return true;
                return false;
            }
        }
        [NotMapped]
        public bool Modified { get; set; } = false;

        #region Live Pricing Data

        [NotMapped]
        public decimal LastBid { get; set; }
        [NotMapped]
        public decimal LastAsk { get; set; }

        [NotMapped]
        public List<(TimeSpan time, decimal lastTick)> IntradayTicks { get; set; } = new List<(TimeSpan time, decimal lastTick)>();
        public void AddIntradayTick(TimeSpan time, decimal lastTick, bool initialPopulate)
        {
            if (IntradayTicks.Exists(x => x.time == time))
                IntradayTicks.RemoveAll(x => x.time == time);

            IntradayTicks.Add((time, lastTick));

            if (!initialPopulate)
                OnPropertyChanged("IntradayTicks");
        }
        public void IntradayTickInitialPopulateComplete()
        {
            OnPropertyChanged("IntradayTicks");
        }
        
        #endregion
        #region Custom Tags

        public string CustomTagsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var flag in GetCustomFlags())
                    sb.Append($"{flag.Description()},");
                return sb.ToString().TrimEnd(',');
            }
        }
        public CustomSecurityTag CustomTags { get; set; }

        public void SetCustomTag(CustomSecurityTag tag, bool isSet)
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
        public void SetCustomTag(CustomSecurityTag tag)
        {
            if (!GetCustomFlag(tag))
            {
                CustomTags |= tag;
            }
            else
            {
                CustomTags &= ~tag;
            }
        }
        public bool GetCustomFlag(CustomSecurityTag tag)
        {
            return CustomTags.HasFlag(tag);
        }
        public List<CustomSecurityTag> GetCustomFlags()
        {
            var ret = new List<CustomSecurityTag>();
            foreach (CustomSecurityTag flag in Enum.GetValues(typeof(CustomSecurityTag)))
            {
                if (flag == CustomSecurityTag.None)
                    continue;

                if (GetCustomFlag(flag))
                    ret.Add(flag);
            }
            return ret;
        }
        public void ClearCustomFlags()
        {
            CustomTags = 0;
        }

        [NotMapped]
        public bool Excluded
        {
            get
            {
                return GetCustomFlag(CustomSecurityTag.Excluded);
            }
            set
            {
                SetCustomTag(CustomSecurityTag.Excluded, value);
            }
        }
        [NotMapped]
        public bool Favorite
        {
            get
            {
                return GetCustomFlag(CustomSecurityTag.Favorite);
            }
            set
            {
                SetCustomTag(CustomSecurityTag.Favorite, value);
            }
        }
        [NotMapped]
        public bool MissingData
        {
            get
            {
                return GetCustomFlag(CustomSecurityTag.MissingData);
            }
            set
            {
                SetCustomTag(CustomSecurityTag.MissingData, value);
            }
        }
        [NotMapped]
        public bool ZeroVolume
        {
            get
            {
                return GetCustomFlag(CustomSecurityTag.ZeroVolume);
            }
            set
            {
                SetCustomTag(CustomSecurityTag.ZeroVolume, value);
            }
        }

        #endregion
        #region Price Bar Data Lists

        public virtual List<PriceBar> DailyPriceBarData { get; set; } = new List<PriceBar>();

        [NotMapped]
        private List<PriceBar> _WeeklyPriceBarData { get; set; }
        [NotMapped]
        public List<PriceBar> WeeklyPriceBarData
        {
            get
            {
                if (_WeeklyPriceBarData == null)
                    GenerateWeeklyBars();
                return _WeeklyPriceBarData.OrderBy(x => x.BarDateTime).ToList();
            }
        }
        [NotMapped]
        private List<PriceBar> _MonthlyPriceBarData { get; set; }
        [NotMapped]
        public List<PriceBar> MonthlyPriceBarData
        {
            get
            {
                if (_MonthlyPriceBarData == null)
                    GenerateMonthlyBars();
                return _MonthlyPriceBarData.OrderBy(x => x.BarDateTime).ToList();
            }
        }
        [NotMapped]
        private List<PriceBar> _QuarterlyPriceBarData { get; set; }
        [NotMapped]
        public List<PriceBar> QuarterlyPriceBarData
        {
            get
            {
                if (_QuarterlyPriceBarData == null)
                    GenerateQuarterlyBars();
                return _QuarterlyPriceBarData.OrderBy(x => x.BarDateTime).ToList();
            }
        }

        #endregion

        public Security() { }
        public Security(string ticker, SecurityType securityType = SecurityType.CommonStock)
        {
            Ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
        }

        [NotMapped]
        private List<(PriceBarSize barSize, int barCount)> SwingPointsSet { get; set; } = new List<(PriceBarSize, int)>();

        public void SetSecurityAtrValues(int period, PriceBarSize priceBarSize)
        {

            //TODO: Smoothing? https://www.macroption.com/atr-excel-wilder/

            List<PriceBar> BarsToProcess = null;

            switch (priceBarSize)
            {
                case PriceBarSize.Daily:
                    BarsToProcess = DailyPriceBarData;
                    break;
                case PriceBarSize.Weekly:
                    BarsToProcess = WeeklyPriceBarData;
                    break;
                case PriceBarSize.Monthly:
                    BarsToProcess = MonthlyPriceBarData;
                    break;
                case PriceBarSize.Quarterly:
                    BarsToProcess = QuarterlyPriceBarData;
                    break;
                default:
                    BarsToProcess = DailyPriceBarData;
                    break;
            }

            BarsToProcess.Sort((x, y) => x.BarDateTime.CompareTo(y.BarDateTime));
            for (int i = 0; i < BarsToProcess.Count; i++)
            {
                if (i < period - 1)
                {
                    decimal atr = BarsToProcess[i].TrueRange();
                    BarsToProcess[i].MyAverageTrueRange = (period, atr);
                }
                else if (i == period - 1)
                {
                    decimal atr = (BarsToProcess.Take(period - 1).Sum(x => x.MyAverageTrueRange.atr) + BarsToProcess[i].TrueRange()) / period;
                    BarsToProcess[i].MyAverageTrueRange = (period, atr);
                }
                else
                {
                    decimal atr = ((BarsToProcess[i - 1].MyAverageTrueRange.atr * (period - 1)) + BarsToProcess[i - 1].TrueRange()) / period;
                    BarsToProcess[i].MyAverageTrueRange = (period, atr);
                }
            }
        }
        public bool AreSwingPointsSet(PriceBarSize priceBarSize, int barCount)
        {
            if (SwingPointsSet.Exists(x => x.barSize == priceBarSize && x.barCount == barCount))
                return true;

            return false;
        }
        public void SetSwingPointsAndTrends(int barCount, PriceBarSize priceBarSize)
        {

            // Already set for this bar size and count
            if (SwingPointsSet.Exists(x => x.barSize == priceBarSize && x.barCount == barCount))
                return;

            // ** Very Compricated Follows **

            List<PriceBar> PriceBarsUsed;
            switch (priceBarSize)
            {
                case PriceBarSize.Weekly:
                    PriceBarsUsed = WeeklyPriceBarData;
                    break;
                case PriceBarSize.Monthly:
                    PriceBarsUsed = MonthlyPriceBarData;
                    break;
                case PriceBarSize.Quarterly:
                    PriceBarsUsed = QuarterlyPriceBarData;
                    break;
                case PriceBarSize.Daily:
                default:
                    PriceBarsUsed = DailyPriceBarData;
                    break;
            }

            if (PriceBarsUsed.Count < 2)
            {
                PriceBarsUsed.ForEach(x => x.SetSwingPointType(barCount, SwingPointType.None));
                return;
            }

            SwingPointsSet.Add((priceBarSize, barCount));

            int successiveLows = 0;
            int successiveHighs = 0;

            TrendQualification CurrentPrevailingTrend = TrendQualification.AmbivalentSideways;

            // Create a reference to the first bar and mark it as a potential low
            PriceBar potentialSwingPointLowBar = GetFirstBar(priceBarSize);

            potentialSwingPointLowBar.SetSwingPointType(barCount,
                (potentialSwingPointLowBar.GetSwingPointType(barCount) | SwingPointType.PotentialSwingPointLow));

            // Create a reference to the first bar and mark it was a potential high
            PriceBar potentialSwingPointHighBar = GetFirstBar(priceBarSize);

            potentialSwingPointHighBar.SetSwingPointType(barCount,
                (potentialSwingPointHighBar.GetSwingPointType(barCount) | SwingPointType.PotentialSwingPointHigh));

            // References to the last two recorded swing point highs and lows
            PriceBar firstPriorSwingPointLowBar = null;
            PriceBar firstPriorSwingPointHighBar = null;
            PriceBar secondPriorSwingPointLowBar = null;
            PriceBar secondPriorSwingPointHighBar = null;

            PriceBar currentBar = GetFirstBar(priceBarSize);

            TrendQualification LastSwingPointTestResult = TrendQualification.NotSet;

            bool HigherHighsHigherLows = false;
            bool LowerHighsLowerLows = false;

            while (currentBar != null)
            {

                if (currentBar.PriorBar == null)
                    currentBar.SetTrendType(barCount, TrendQualification.AmbivalentSideways);
                else
                    currentBar.SetTrendType(barCount, currentBar.PriorBar.GetTrendType(barCount));

                //
                // Swing Point LOW
                //
                if (successiveLows == barCount)
                {
                    // Actualize the PSPL if we have counted enough bars, and mark the current bar as the next potential

                    //potentialSwingPointLowBar.SwingPointType -= SwingPointType.PotentialSwingPointLow;
                    //potentialSwingPointLowBar.SwingPointType |= SwingPointType.SwingPointLow;

                    potentialSwingPointLowBar.SetSwingPointType(barCount,
                    (SwingPointType)(potentialSwingPointLowBar.GetSwingPointType(barCount) - SwingPointType.PotentialSwingPointLow + SwingPointType.SwingPointLow));

                    // Keep track of last two actualized bars
                    secondPriorSwingPointLowBar = firstPriorSwingPointLowBar;
                    firstPriorSwingPointLowBar = potentialSwingPointLowBar;

                    // Mark the next potential swing point bar
                    potentialSwingPointLowBar = currentBar;

                    //potentialSwingPointLowBar.SwingPointType |= SwingPointType.PotentialSwingPointLow;
                    potentialSwingPointLowBar.SetSwingPointType(barCount,
                    (SwingPointType)(potentialSwingPointLowBar.GetSwingPointType(barCount) | SwingPointType.PotentialSwingPointLow));


                    successiveLows = 1;
                }
                else if (currentBar.Low < potentialSwingPointLowBar.Low ||
                    (currentBar.Low == potentialSwingPointLowBar.Low && currentBar.Volume > potentialSwingPointLowBar.Volume))
                {
                    // If the current bar has a lower low, OR the lows are == and the current bar has a higher volume, replace potential low with current bar
                    //potentialSwingPointLowBar.SwingPointType -= SwingPointType.PotentialSwingPointLow;

                    potentialSwingPointLowBar.SetSwingPointType(barCount,
                    (SwingPointType)(potentialSwingPointLowBar.GetSwingPointType(barCount) - SwingPointType.PotentialSwingPointLow));

                    potentialSwingPointLowBar = currentBar;

                    //potentialSwingPointLowBar.SwingPointType |= SwingPointType.PotentialSwingPointLow;
                    potentialSwingPointLowBar.SetSwingPointType(barCount,
                    (SwingPointType)(potentialSwingPointLowBar.GetSwingPointType(barCount) | SwingPointType.PotentialSwingPointLow));

                    successiveLows = 1;
                }
                else
                {
                    // Otherwise increment the counter
                    successiveLows += 1;
                }

                //
                // Swing Point HIGH
                //
                if (successiveHighs == barCount)
                {
                    // Actualize the PSPH if we have counted enough bars, and mark the current bar as the next potential
                    //potentialSwingPointHighBar.SwingPointType -= SwingPointType.PotentialSwingPointHigh;
                    //potentialSwingPointHighBar.SwingPointType |= SwingPointType.SwingPointHigh;

                    potentialSwingPointHighBar.SetSwingPointType(barCount,
                    (SwingPointType)(potentialSwingPointHighBar.GetSwingPointType(barCount) - SwingPointType.PotentialSwingPointHigh + SwingPointType.SwingPointHigh));

                    // Keep track of last two actualized bars
                    secondPriorSwingPointHighBar = firstPriorSwingPointHighBar;
                    firstPriorSwingPointHighBar = potentialSwingPointHighBar;

                    // Mark the next potential swing point bar
                    potentialSwingPointHighBar = currentBar;

                    //potentialSwingPointHighBar.SwingPointType |= SwingPointType.PotentialSwingPointHigh;
                    potentialSwingPointHighBar.SetSwingPointType(barCount,
                    (SwingPointType)(potentialSwingPointHighBar.GetSwingPointType(barCount) | SwingPointType.PotentialSwingPointHigh));

                    successiveHighs = 1;
                }
                else if (currentBar.High > potentialSwingPointHighBar.High ||
                    (currentBar.High == potentialSwingPointHighBar.High && currentBar.Volume > potentialSwingPointHighBar.Volume))
                {
                    // If the current bar has a higher high, OR the highs are == and the current bar has a higher volume, replace potential high with current bar
                    //potentialSwingPointHighBar.SwingPointType -= SwingPointType.PotentialSwingPointHigh;

                    potentialSwingPointHighBar.SetSwingPointType(barCount,
                    (SwingPointType)(potentialSwingPointHighBar.GetSwingPointType(barCount) - SwingPointType.PotentialSwingPointHigh));

                    potentialSwingPointHighBar = currentBar;

                    //potentialSwingPointHighBar.SwingPointType |= SwingPointType.PotentialSwingPointHigh;
                    potentialSwingPointHighBar.SetSwingPointType(barCount,
                    (SwingPointType)(potentialSwingPointHighBar.GetSwingPointType(barCount) | SwingPointType.PotentialSwingPointHigh));

                    successiveHighs = 1;
                }
                else
                {
                    // Otherwise increment the counter
                    successiveHighs += 1;
                }


                //
                // Trend Identification
                //

                if (firstPriorSwingPointHighBar == null || firstPriorSwingPointLowBar == null ||
                secondPriorSwingPointHighBar == null || secondPriorSwingPointLowBar == null)
                {
                    /*
                     *  If we have not yet identified 2 each SPH and SPL, maintain ambivalence
                     */
                    CurrentPrevailingTrend = TrendQualification.AmbivalentSideways;
                    //currentBar = currentBar.NextBar;
                    //continue;
                }
                else
                {
                    /*
                     *  This keeps track of actualized and asserted swing points to determine if we have
                     *  the required higher highs and lows (for a bullish trend) or lower highs and lows 
                     *  (for a bearish trend).  The adjusted comparison bars are required for when a 
                     *  potential swing point (not yet actualized) is asserted, which means that a higher
                     *  high (for a SPH) or a lower low (for a SPL) is guaranteed.  We treat these as 'actualized'
                     *  in this comparison.
                     */

                    PriceBar adjustedFirstPriorHighBar = null;
                    PriceBar adjustedSecondPriorHighBar = null;

                    if (potentialSwingPointHighBar.High > firstPriorSwingPointHighBar.High)
                    {
                        adjustedFirstPriorHighBar = potentialSwingPointHighBar;
                        adjustedSecondPriorHighBar = firstPriorSwingPointHighBar;
                    }
                    else
                    {
                        adjustedFirstPriorHighBar = firstPriorSwingPointHighBar;
                        adjustedSecondPriorHighBar = secondPriorSwingPointHighBar;
                    }

                    PriceBar adjustedFirstPriorLowBar = null;
                    PriceBar adjustedSecondPriorLowBar = null;

                    if (potentialSwingPointLowBar.Low < firstPriorSwingPointLowBar.Low)
                    {
                        adjustedFirstPriorLowBar = potentialSwingPointLowBar;
                        adjustedSecondPriorLowBar = firstPriorSwingPointLowBar;
                    }
                    else
                    {
                        adjustedFirstPriorLowBar = firstPriorSwingPointLowBar;
                        adjustedSecondPriorLowBar = secondPriorSwingPointLowBar;
                    }

                    /*
                     *  If the last two actualized SPHs show as increasing, or if we know they will when
                     *  the next potential point is actualized, we can show highers highs
                     */
                    HigherHighsHigherLows = (
                    adjustedFirstPriorHighBar.High > adjustedSecondPriorHighBar.High &&
                    adjustedFirstPriorLowBar.Low > adjustedSecondPriorLowBar.Low);
                    /*
                     *  If the last two actualized SPLs show as decreasing, or if we know they will when
                     *  the next potential point is actualized, we can show lower lows
                     */
                    LowerHighsLowerLows = (
                    adjustedFirstPriorHighBar.High < adjustedSecondPriorHighBar.High &&
                    adjustedFirstPriorLowBar.Low < adjustedSecondPriorLowBar.Low);

                }

                //
                // Check for swing point tests and update a variable to show the trend implication.
                //               

                if (firstPriorSwingPointHighBar != null && currentBar.IsTestingSwingPointHigh(firstPriorSwingPointHighBar))
                {
                    //currentBar.SwingPointTestType = SwingPointTest.TestHigh;
                    currentBar.SetSwingPointTestType(barCount, SwingPointTest.TestHigh);

                    var testResult = SwingPointHighTest(currentBar, firstPriorSwingPointHighBar);
                    switch (CurrentPrevailingTrend)
                    {
                        case TrendQualification.NotSet:
                            throw new TrendOperationException() { message = "Prevailing trend not set" };
                        case TrendQualification.AmbivalentSideways:
                        case TrendQualification.SuspectSideways:
                        case TrendQualification.ConfirmedSideways:
                            if (testResult.priceTest == SwingPointTestPriceResult.CloseExceedsSwingPoint)
                            {
                                // What if the last two SPLs are not higher?  Not a bullish trend in that case
                                // If we have a swing point test prior to the next requires swing point actualizing
                                // Come back to this

                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    LastSwingPointTestResult = TrendQualification.ConfirmedBullish;
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    LastSwingPointTestResult = TrendQualification.SuspectBullish;
                                }
                            }
                            else if (testResult.priceTest == SwingPointTestPriceResult.CloseDoesNotExceepSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    // No Change to Prevailing trend.  Likely to see a retest (Increased odds of bullish transition in near future)
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    // No Change to Prevailing trend.  Less likely to see a retest (Decreased odds of bullish transition in near future)
                                }
                            }
                            break;
                        case TrendQualification.SuspectBullish:
                        case TrendQualification.ConfirmedBullish:
                            if (testResult.priceTest == SwingPointTestPriceResult.CloseExceedsSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    LastSwingPointTestResult = TrendQualification.ConfirmedBullish;
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    LastSwingPointTestResult = TrendQualification.SuspectBullish;
                                }
                            }
                            else if (testResult.priceTest == SwingPointTestPriceResult.CloseDoesNotExceepSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    // No Change to Prevailing trend.  Likely to see a retest (Increased odds of bullish continuation in near future)
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    // No Change to Prevailing trend.  Less likely to see a retest (Decreased odds of bullish continuation in near future)
                                }
                            }
                            break;
                        case TrendQualification.SuspectBearish:
                        case TrendQualification.ConfirmedBearish:
                            if (testResult.priceTest == SwingPointTestPriceResult.CloseExceedsSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    LastSwingPointTestResult = TrendQualification.ConfirmedSideways;
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    LastSwingPointTestResult = TrendQualification.SuspectSideways;
                                }
                            }
                            else if (testResult.priceTest == SwingPointTestPriceResult.CloseDoesNotExceepSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    // No Change to Prevailing trend.  Likely to see a retest (Increased odds of sideways transition in near future)
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    // No Change to Prevailing trend.  Less likely to see a retest (Decreased odds of sideways transition in near future)
                                }
                            }
                            break;
                    }
                }
                if (firstPriorSwingPointLowBar != null && currentBar.IsTestingSwingPointLow(firstPriorSwingPointLowBar))
                {
                    //currentBar.SwingPointTestType = SwingPointTest.TestLow;
                    currentBar.SetSwingPointTestType(barCount, SwingPointTest.TestLow);

                    var testResult = SwingPointLowTest(currentBar, firstPriorSwingPointLowBar);
                    switch (CurrentPrevailingTrend)
                    {
                        case TrendQualification.NotSet:
                            throw new TrendOperationException() { message = "Prevailing trend not set" };
                        case TrendQualification.AmbivalentSideways:
                        case TrendQualification.SuspectSideways:
                        case TrendQualification.ConfirmedSideways:
                            if (testResult.priceTest == SwingPointTestPriceResult.CloseExceedsSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    LastSwingPointTestResult = TrendQualification.ConfirmedBearish;
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    LastSwingPointTestResult = TrendQualification.SuspectBearish;
                                }
                            }
                            else if (testResult.priceTest == SwingPointTestPriceResult.CloseDoesNotExceepSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    // No Change to Prevailing trend.  Likely to see a retest (Increased odds of bearish transition in near future)
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    // No Change to Prevailing trend.  Less likely to see a retest (Decreased odds of bearish transition in near future)
                                }
                            }
                            break;
                        case TrendQualification.SuspectBullish:
                        case TrendQualification.ConfirmedBullish:
                            if (testResult.priceTest == SwingPointTestPriceResult.CloseExceedsSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    LastSwingPointTestResult = TrendQualification.ConfirmedSideways;
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    LastSwingPointTestResult = TrendQualification.SuspectSideways;
                                }
                            }
                            else if (testResult.priceTest == SwingPointTestPriceResult.CloseDoesNotExceepSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    // No Change to Prevailing trend.  Likely to see a retest (Increased odds of sideways transition in near future)
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    // No Change to Prevailing trend.  Less likely to see a retest (Decreased odds of sideways transition in near future)
                                }
                            }
                            break;
                        case TrendQualification.SuspectBearish:
                        case TrendQualification.ConfirmedBearish:
                            if (testResult.priceTest == SwingPointTestPriceResult.CloseExceedsSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    LastSwingPointTestResult = TrendQualification.ConfirmedBearish;
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    LastSwingPointTestResult = TrendQualification.SuspectBearish;
                                }
                            }
                            else if (testResult.priceTest == SwingPointTestPriceResult.CloseDoesNotExceepSwingPoint)
                            {
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeExpands)
                                {
                                    // No Change to Prevailing trend.  Likely to see a retest (Increased odds of bearish continuation in near future)
                                }
                                if (testResult.volumeTest == SwingPointTestVolumeResult.VolumeContracts)
                                {
                                    // No Change to Prevailing trend.  Less likely to see a retest (Decreased odds of bearish continuation in near future)
                                }
                            }
                            break;
                    }
                }

                //
                // If there was not a test on this bar, but the swing point arrangement has taken us to a sideways trend, update now
                //
                if (currentBar.GetSwingPointTestType(barCount) != SwingPointTest.None)
                    switch (CurrentPrevailingTrend)
                    {
                        case TrendQualification.SuspectBullish:
                        case TrendQualification.ConfirmedBullish:
                            if (!HigherHighsHigherLows)
                                CurrentPrevailingTrend = TrendQualification.AmbivalentSideways;
                            break;
                        case TrendQualification.SuspectBearish:
                        case TrendQualification.ConfirmedBearish:
                            if (!LowerHighsLowerLows)
                                CurrentPrevailingTrend = TrendQualification.AmbivalentSideways;
                            break;
                        default:
                            break;
                    }

                /*
                 *  Compare the last swing point test result against the current swing point arrangement
                 *  
                 *  To set a trend, the swing point test has to align with the arrangement of swing points,
                 *  either with highers highs and lows supporting a bullish transition, or lower highs and 
                 *  lows supporting a bearish transition.
                 */
                switch (LastSwingPointTestResult)
                {
                    case TrendQualification.NotSet:
                        break;
                    case TrendQualification.AmbivalentSideways:
                    case TrendQualification.SuspectSideways:
                    case TrendQualification.ConfirmedSideways:
                        CurrentPrevailingTrend = LastSwingPointTestResult;
                        break;
                    case TrendQualification.SuspectBullish:
                    case TrendQualification.ConfirmedBullish:
                        if (HigherHighsHigherLows)
                            CurrentPrevailingTrend = LastSwingPointTestResult;
                        break;
                    case TrendQualification.SuspectBearish:
                    case TrendQualification.ConfirmedBearish:
                        if (LowerHighsLowerLows)
                            CurrentPrevailingTrend = LastSwingPointTestResult;
                        break;
                    default:
                        break;
                }

                //currentBar.TrendType = CurrentPrevailingTrend;
                currentBar.SetTrendType(barCount, CurrentPrevailingTrend);
                currentBar = currentBar.NextBar;
            }

        }

        public TrendQualification LastTrend()
        {
            return GetLastBar(PriceBarSize.Daily).GetTrendType(Settings.Instance.DefaultSwingpointBarCount);
        }
        public double AverageVolume(DateTime AsOf, PriceBarSize priceBarSize, int days)
        {
            return (from bar in GetPriceBars(GetLastBar(PriceBarSize.Daily).BarDateTime, days, priceBarSize) select bar.Volume).Average();
        }
        public decimal PercentChange(int lastDays)
        {
            var bars = GetLastBar(PriceBarSize.Daily).PriorBars(lastDays, true).OrderBy(x => x.BarDateTime);
            var open = bars.First().Open;
            var close = bars.Last().Close;
            var ret = (close - open) / open;

            return ret;
        }

        public decimal PercentChange_1day => PercentChange(1);
        public decimal PercentChange_5day => PercentChange(5);
        public decimal PercentChange_15day => PercentChange(15);
        public decimal PercentChange_30day => PercentChange(30);

        #region Price Bar Methods

        public bool HasBar(DateTime barDate, PriceBarSize priceBarSize)
        {
            if (!Calendar.IsTradingDay(barDate))
                return false;

            if (GetPriceBar(barDate, priceBarSize, false) == null)
                return false;
            return true;
        }

        public PriceBar GetPriceBar(DateTime barDate, PriceBarSize priceBarSize, bool Create = false)
        {

            switch (priceBarSize)
            {
                case PriceBarSize.Weekly:
                    {
                        barDate = FirstTradingDayOfWeek(barDate);
                        var ret = WeeklyPriceBarData.Find(x => x.BarDateTime == barDate);
                        if (ret == null && Create)
                            throw new InvalidDataRequestException() { message = "Cannot use GET to create weekly bars" };
                        return ret;
                    }
                case PriceBarSize.Monthly:
                    {
                        barDate = FirstTradingDayOfMonth(barDate);
                        var ret = MonthlyPriceBarData.Find(x => x.BarDateTime == barDate);
                        if (ret == null && Create)
                            throw new InvalidDataRequestException() { message = "Cannot use GET to create monthly bars" };
                        return ret;
                    }
                case PriceBarSize.Quarterly:
                    {
                        barDate = FirstTradingDayOfQuarter(barDate);
                        var ret = QuarterlyPriceBarData.Find(x => x.BarDateTime == barDate);
                        if (ret == null && Create)
                            throw new InvalidDataRequestException() { message = "Cannot use GET to create quarterly bars" };
                        return ret;
                    }
                case PriceBarSize.Daily:
                default:
                    {
                        if (!Calendar.IsTradingDay(barDate))
                            throw new InvalidTradingDateException();

                        return DailyPriceBarData.Find(x => x.BarDateTime == barDate) ?? (Create ? DailyPriceBarData.AddAndReturn(new PriceBar(barDate, this)) : null);
                    }
            }
        }
        public PriceBar GetPriceBarOrLastPrior(DateTime barDate, PriceBarSize priceBarSize, int lookbackLimit)
        {

            var ret = GetPriceBar(barDate, priceBarSize, false);
            while (ret == null && lookbackLimit-- > 0)
            {
                switch (priceBarSize)
                {
                    case PriceBarSize.Daily:
                        barDate = Calendar.PriorTradingDay(barDate);
                        break;
                    case PriceBarSize.Weekly:
                        barDate = Calendar.PriorTradingWeekStart(barDate);
                        break;
                    case PriceBarSize.Monthly:
                        barDate = Calendar.PriorTradingMonthStart(barDate);
                        break;
                    case PriceBarSize.Quarterly:
                        barDate = Calendar.PriorTradingQuarterStart(barDate);
                        break;
                    default:
                        break;
                }
                ret = GetPriceBar(barDate, priceBarSize, false);
            }
            return ret;
        }
        public List<PriceBar> GetPriceBars(PriceBarSize priceBarSize)
        {
            switch (priceBarSize)
            {
                case PriceBarSize.Daily:
                    return (from bar in DailyPriceBarData select bar).OrderBy(x => x.BarDateTime).ToList();
                case PriceBarSize.Weekly:
                    return (from bar in WeeklyPriceBarData select bar).OrderBy(x => x.BarDateTime).ToList();
                case PriceBarSize.Monthly:
                    return (from bar in MonthlyPriceBarData select bar).OrderBy(x => x.BarDateTime).ToList();
                case PriceBarSize.Quarterly:
                    return (from bar in QuarterlyPriceBarData select bar).OrderBy(x => x.BarDateTime).ToList();
                default:
                    return (from bar in DailyPriceBarData select bar).OrderBy(x => x.BarDateTime).ToList();
            }
        }
        public List<PriceBar> GetPriceBars(DateTime startBarDate, DateTime endBarDate, PriceBarSize priceBarSize, bool inclusiveEndDate = false)
        {

            var ret = GetPriceBars(endBarDate, priceBarSize, inclusiveEndDate);
            ret = ret.Where(x => x.BarDateTime.IsBetween(startBarDate, endBarDate, true)).ToList();
            return ret;
        }
        public List<PriceBar> GetPriceBars(DateTime endBarDate, int count, PriceBarSize priceBarSize, bool inclusiveEndDate = false)
        {

            var ret = GetPriceBars(endBarDate, priceBarSize, inclusiveEndDate);

            ret.Reverse();
            ret = ret.Take(count).ToList();

            return ret;
        }
        public List<PriceBar> GetPriceBars(DateTime endBarDate, PriceBarSize priceBarSize, bool inclusiveEndDate = false)
        {

            List<PriceBar> PriceBarDataUsed;

            switch (priceBarSize)
            {
                case PriceBarSize.Daily:
                    PriceBarDataUsed = DailyPriceBarData;
                    break;
                case PriceBarSize.Weekly:
                    PriceBarDataUsed = WeeklyPriceBarData;
                    endBarDate = FirstTradingDayOfWeek(endBarDate);
                    break;
                case PriceBarSize.Monthly:
                    PriceBarDataUsed = MonthlyPriceBarData;
                    endBarDate = FirstTradingDayOfMonth(endBarDate);
                    break;
                case PriceBarSize.Quarterly:
                    PriceBarDataUsed = QuarterlyPriceBarData;
                    endBarDate = FirstTradingDayOfQuarter(endBarDate);
                    break;
                default:
                    PriceBarDataUsed = DailyPriceBarData;
                    break;
            }

            var ret = (from bar in PriceBarDataUsed
                       where bar.BarDateTime <= endBarDate
                       select bar).OrderBy(x => x.BarDateTime).ToList();

            if (!inclusiveEndDate && ret.Count > 0)
                ret.Remove(ret.Last());

            return ret;
        }
        public PriceBar GetLastBar(PriceBarSize priceBarSize)
        {

            switch (priceBarSize)
            {
                case PriceBarSize.Daily:
                    return DailyPriceBarData.OrderBy(x => x.BarDateTime).LastOrDefault();
                case PriceBarSize.Weekly:
                    return WeeklyPriceBarData.OrderBy(x => x.BarDateTime).LastOrDefault();
                case PriceBarSize.Monthly:
                    return MonthlyPriceBarData.OrderBy(x => x.BarDateTime).LastOrDefault();
                case PriceBarSize.Quarterly:
                    return QuarterlyPriceBarData.OrderBy(x => x.BarDateTime).LastOrDefault();
                default:
                    return DailyPriceBarData.OrderBy(x => x.BarDateTime).LastOrDefault();
            }
        }
        public PriceBar GetFirstBar(PriceBarSize priceBarSize)
        {

            switch (priceBarSize)
            {
                case PriceBarSize.Daily:
                    return DailyPriceBarData.OrderBy(x => x.BarDateTime).FirstOrDefault();
                case PriceBarSize.Weekly:
                    return WeeklyPriceBarData.OrderBy(x => x.BarDateTime).FirstOrDefault();
                case PriceBarSize.Monthly:
                    return MonthlyPriceBarData.OrderBy(x => x.BarDateTime).FirstOrDefault();
                case PriceBarSize.Quarterly:
                    return QuarterlyPriceBarData.OrderBy(x => x.BarDateTime).FirstOrDefault();
                default:
                    return DailyPriceBarData.OrderBy(x => x.BarDateTime).FirstOrDefault();
            }
        }

        private void GenerateWeeklyBars()
        {

            _WeeklyPriceBarData = new List<PriceBar>();

            // Get the first week start.  Skip incomplete first week
            DateTime startDate = GetFirstBar(PriceBarSize.Daily).BarDateTime;
            if (startDate != FirstTradingDayOfWeek(startDate))
                startDate = NextTradingWeekStart(startDate);

            DateTime lastDate = GetLastBar(PriceBarSize.Daily).BarDateTime;
            // This will return if we have less than a full week of data
            if (startDate > lastDate)
                return;

            while (startDate < lastDate)
            {
                _WeeklyPriceBarData.Add(WeeklyBar(startDate));
                startDate = NextTradingWeekStart(startDate);
            }
        }
        private void GenerateMonthlyBars()
        {

            _MonthlyPriceBarData = new List<PriceBar>();

            // Get the first month start.  Skip incomplete first month
            DateTime startDate = GetFirstBar(PriceBarSize.Daily).BarDateTime;
            if (startDate != FirstTradingDayOfMonth(startDate))
                startDate = NextTradingMonthStart(startDate);

            DateTime lastDate = GetLastBar(PriceBarSize.Daily).BarDateTime;
            // This will return if we have less than a full month of data
            if (startDate > lastDate)
                return;

            while (startDate < lastDate)
            {
                _MonthlyPriceBarData.Add(MonthlyBar(startDate));
                startDate = NextTradingMonthStart(startDate);
            }
        }
        private void GenerateQuarterlyBars()
        {

            _QuarterlyPriceBarData = new List<PriceBar>();

            // Get the first quarter start.  Skip incomplete first quarter
            DateTime startDate = GetFirstBar(PriceBarSize.Daily).BarDateTime;
            if (startDate != FirstTradingDayOfQuarter(startDate))
                startDate = NextTradingQuarterStart(startDate);

            DateTime lastDate = GetLastBar(PriceBarSize.Daily).BarDateTime;
            // This will return if we have less than a full quarter of data
            if (startDate > lastDate)
                return;

            while (startDate < lastDate)
            {
                _QuarterlyPriceBarData.Add(QuarterlyBar(startDate));
                startDate = NextTradingQuarterStart(startDate);
            }
        }

        private PriceBar WeeklyBar(DateTime BarDate)
        {

            DateTime startDate = FirstTradingDayOfWeek(BarDate);
            DateTime endDate = PriorTradingDay(NextTradingWeekStart(BarDate));

            var bars = GetPriceBars(startDate, endDate, PriceBarSize.Daily, true);

            if (bars.First().BarDateTime > bars.Last().BarDateTime)
                throw new UnknownErrorException() { message = "You didn't order the bars correctly" };

            PriceBar ret = new PriceBar(startDate, this, bars.First().Open, bars.Max(x => x.High), bars.Min(x => x.Low), bars.Last().Close)
            {
                Volume = bars.Sum(x => x.Volume),
                PriceDataProvider = bars.First().PriceDataProvider,
                BarSize = PriceBarSize.Weekly
            };

            return ret;
        }
        private PriceBar MonthlyBar(DateTime BarDate)
        {

            DateTime startDate = FirstTradingDayOfMonth(BarDate);
            DateTime endDate = PriorTradingDay(NextTradingMonthStart(BarDate));

            var bars = GetPriceBars(startDate, endDate, PriceBarSize.Daily, true);

            if (bars.First().BarDateTime > bars.Last().BarDateTime)
                throw new UnknownErrorException() { message = "You didn't order the bars correctly" };

            PriceBar ret = new PriceBar(startDate, this, bars.First().Open, bars.Max(x => x.High), bars.Min(x => x.Low), bars.Last().Close)
            {
                Volume = bars.Sum(x => x.Volume),
                PriceDataProvider = bars.First().PriceDataProvider,
                BarSize = PriceBarSize.Monthly
            };

            return ret;
        }
        private PriceBar QuarterlyBar(DateTime BarDate)
        {

            DateTime startDate = FirstTradingDayOfQuarter(BarDate);
            DateTime endDate = PriorTradingDay(NextTradingQuarterStart(BarDate));

            var bars = GetPriceBars(startDate, endDate, PriceBarSize.Daily, true);

            if (bars.First().BarDateTime > bars.Last().BarDateTime)
                throw new UnknownErrorException() { message = "You didn't order the bars correctly" };

            PriceBar ret = new PriceBar(startDate, this, bars.First().Open, bars.Max(x => x.High), bars.Min(x => x.Low), bars.Last().Close)
            {
                Volume = bars.Sum(x => x.Volume),
                PriceDataProvider = bars.First().PriceDataProvider,
                BarSize = PriceBarSize.Monthly
            };

            return ret;
        }

        #endregion
        #region UI Info Display Methods

        [UiDisplayText(-5)]
        public string CompanyName()
        {
            string title = "Company Name:";
            return string.Format($"{title,-15} {LongName}");
        }
        [UiDisplayText(-4)]
        public string CompanyIndustry()
        {
            string title = "Industry:";
            return string.Format($"{title,-15} {Industry}");
        }
        [UiDisplayText(-3)]
        public string CompanySector()
        {
            string title = "Sector:";
            return string.Format($"{title,-15} {Sector}");
        }
        [UiDisplayText(-2)]
        public string CompanySic()
        {
            string title = "SIC Cat:";
            return string.Format($"{title,-15} {this.SicCode} {GetIndustryBySIC(this.SicCode)}");
        }
        [UiDisplayText(-1)]
        public string ExchangeName()
        {
            string title = "Exchange Name";
            return string.Format($"{title,-15} {(Exchange == "ISLAND" ? "NASDAQ" : Exchange)}");
        }
        [UiDisplayText(0)]
        public string NumberOfBars()
        {

            string title = "Bar Count:";
            return string.Format($"{title,-15} {DailyPriceBarData.Count}");
        }
        [UiDisplayText(1)]
        public string FirstBar()
        {

            if (DailyPriceBarData.Count == 0)
                return "Earliest Bar: NO DATA";

            string title = "Earliest Bar:";
            return string.Format($"{title,-15} {(from bar in DailyPriceBarData select bar.BarDateTime).Min().ToShortDateString()}");
        }
        [UiDisplayText(2)]
        public string LatestBar()
        {

            if (DailyPriceBarData.Count == 0)
                return "Latest Bar: NO DATA";

            string title = "Latest Bar:";
            return string.Format($"{title,-15} {(from bar in DailyPriceBarData select bar.BarDateTime).Max().ToShortDateString()}");
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

            if (DailyPriceBarData.Count == 0)
                return "Last Close: NO DATA";

            string title = "Last Close:";
            return string.Format($"{title,-15} {GetLastBar(PriceBarSize.Daily).Close:$0.00}");
        }
        [UiDisplayText(5)]
        public string LastVolume()
        {

            if (DailyPriceBarData.Count == 0)
                return "Last Close: NO DATA";

            string title = "Last Volume:";
            return string.Format($"{title,-15} {GetLastBar(PriceBarSize.Daily).Volume}");
        }
        [UiDisplayText(6)]
        public string AverageVolume()
        {

            if (DailyPriceBarData.Count == 0)
                return "Last Close: NO DATA";

            var avgVolume = (from bar in GetPriceBars(GetLastBar(PriceBarSize.Daily).BarDateTime, 30, PriceBarSize.Daily) select bar.Volume).Average();

            string title = "30-Day Avg Volume:";
            return string.Format($"{title,-15} {avgVolume:0}");
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
