using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization;
using System.Windows.Forms.DataVisualization.Charting;

namespace Finance
{
    public class SingleSecurityResultChart : Chart
    {

        #region Adjustable Parameters

        public double YAxisBufferPercent { get; set; } = .10;

        #endregion

        ChartArea chartArea = new ChartArea("default");

        SingleSecurityResultSeries stockSeries = null;
        SingleSecurityResultSeries signalSeries = null;
        SingleSecurityResultSeries balanceSeries = null;
        SingleSecurityResultSeries tradeSeries = null;

        SimulationResultSeriesBuilder simulationResultSeries = null;

        public void InitializeChart(Control parent)
        {
            Parent = parent;
            _InitializeChartArea();
        }

        /// <summary>
        /// Set the ChartArea properties
        /// </summary>
        private void _InitializeChartArea()
        {
            // X Axis
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Days;
            chartArea.AxisX.IsStartedFromZero = false;
            chartArea.AxisX.Interval = 5;

            // Y Axis
            chartArea.AxisY.IntervalType = DateTimeIntervalType.Number;
            chartArea.AxisY.IsStartedFromZero = false;

            // Background
            chartArea.BackColor = Color.FromArgb(255, 0, 0, 8);

            ChartAreas.Clear();
            ChartAreas.Add(chartArea);
        }

        public void ViewResult(SimulationResultSeriesBuilder resultSeries)
        {
            simulationResultSeries = resultSeries;
            LoadResultSeries();
        }

        private void LoadResultSeries()
        {
            Series.Clear();

            stockSeries = simulationResultSeries.StockSeries;
            signalSeries = simulationResultSeries.SignalSeries;
            balanceSeries = simulationResultSeries.BalanceSeries;
            tradeSeries = simulationResultSeries.TradeSeries;

            SetAxisX();
            SetAxisY();

            stockSeries.ChartArea = "default";
            stockSeries.YAxisType = AxisType.Primary;
            Series.Add(stockSeries);

            signalSeries.ChartArea = "default";
            signalSeries.YAxisType = AxisType.Primary;
            Series.Add(signalSeries);

            tradeSeries.ChartArea = "default";
            tradeSeries.YAxisType = AxisType.Primary;
            Series.Add(tradeSeries);

            balanceSeries.ChartArea = "default";
            balanceSeries.YAxisType = AxisType.Secondary;
            Series.Insert(0, balanceSeries);

            Update();
        }
        private void SetAxisX()
        {
            // Min and Max dates of the simulation
            DateTime minX = stockSeries.MinXValue;
            DateTime maxX = stockSeries.MaxXValue;

            chartArea.AxisX.Minimum = minX.ToOADate();
            chartArea.AxisX.Maximum = maxX.ToOADate();

            chartArea.AxisX.MajorGrid.Enabled = false;
        }
        private void SetAxisY()
        {
            // Min and max price values with a small buffer - primary

            double minY = Math.Floor(Math.Min(stockSeries.MinYValue, tradeSeries.MinYValue) * (1 - YAxisBufferPercent));
            double maxY = Math.Ceiling(Math.Max(stockSeries.MaxYValue, tradeSeries.MaxYValue) * (1 + YAxisBufferPercent));

            chartArea.AxisY.Minimum = minY;
            chartArea.AxisY.Maximum = maxY;

            chartArea.AxisY.LabelStyle.Format = "$0.00";
            chartArea.AxisY.Title = "Share Price";

            // Gridlines
            var span = maxY - minY;
            chartArea.AxisY.Interval = Math.Floor(span / 10);
            chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(128, 255, 255, 255);


            // Secondary Y Axis for balance values
            chartArea.AxisY2.Enabled = AxisEnabled.True;

            maxY = balanceSeries.MaxYValue * 3; // Buffer the display value to 1/3rd of the chart max
            chartArea.AxisY2.Minimum = 0;
            chartArea.AxisY2.Maximum = maxY;
            chartArea.AxisY2.Title = "Shares Held";
        }

    }

    /// <summary>
    /// Generates series from a simulation result and ticker to view a single security time-lapse
    /// </summary>
    public class SimulationResultSeriesBuilder
    {

        public Simulation Simulation { get; }
        public Security Security { get; }

        public SingleSecurityResultSeries StockSeries { get; private set; }
        public SingleSecurityResultSeries SignalSeries { get; private set; }
        public SingleSecurityResultSeries BalanceSeries { get; private set; }
        public SingleSecurityResultSeries TradeSeries { get; private set; }

        public SimulationResultSeriesBuilder(Simulation simulation, Security security)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            Security = security ?? throw new ArgumentNullException(nameof(security));
            GenerateValues();
        }

        private void GenerateValues()
        {
            StockSeries = new SingleSecurityResultSeries(Security, Simulation, SingleSecurityResultType.StockSeries);
            SignalSeries = new SingleSecurityResultSeries(Security, Simulation, SingleSecurityResultType.SignalSeries);
            BalanceSeries = new SingleSecurityResultSeries(Security, Simulation, SingleSecurityResultType.BalanceSeries);
            TradeSeries = new SingleSecurityResultSeries(Security, Simulation, SingleSecurityResultType.TradeSeries);
        }
    }
    public class SingleSecurityResultSeries : Series
    {
        public DateTime MinXValue
        {
            get
            {
                return DateTime.FromOADate((from pt in Points select pt.XValue).Min());
            }
        }
        public DateTime MaxXValue
        {
            get
            {
                return DateTime.FromOADate((from pt in Points select pt.XValue).Max());
            }
        }
        public double MinYValue
        {
            get
            {
                if (Points.Count == 0)
                    return 0;
                return (from pt in Points select pt.YValues.Min()).Min();
            }
        }
        public double MaxYValue
        {
            get
            {
                if (Points.Count == 0)
                    return 1;
                return (from pt in Points select pt.YValues.Max()).Max();
            }
        }

        private Security security;
        private Simulation simulation;
        public SingleSecurityResultType SeriesType { get; }

        public SingleSecurityResultSeries(Security security, Simulation simulation, SingleSecurityResultType seriesType)
        {
            this.security = security ?? throw new ArgumentNullException(nameof(security));
            this.simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            SeriesType = seriesType;
            _BuildSeries(seriesType);
        }

        private void _BuildSeries(SingleSecurityResultType seriesType)
        {
            DateTime start, end;
            start = simulation.SimulationTimeSpan.Item1;
            end = simulation.SimulationTimeSpan.Item2;

            switch (seriesType)
            {
                case SingleSecurityResultType.StockSeries:
                    _BuildStockSeries(start, end);
                    break;
                case SingleSecurityResultType.SignalSeries:
                    _BuildSignalSeries(start, end);
                    break;
                case SingleSecurityResultType.BalanceSeries:
                    _BuildBalanceSeries(start, end);
                    break;
                case SingleSecurityResultType.TradeSeries:
                    _BuildTradeSeries(start, end);
                    break;
                default:
                    throw new UnknownErrorException();
            }
        }

        /// <summary>
        /// Construct an OHLC time series chart
        /// </summary>
        private void _BuildStockSeries(DateTime start, DateTime end)
        {
            Points.Clear();

            ChartType = SeriesChartType.Candlestick;
            this["PriceUpColor"] = "Green";
            this["PriceDownColor"] = "Red";

            var barCollection = security.GetPriceBars(start, end);
            barCollection.Sort((x, y) => x.BarDateTime.CompareTo(y.BarDateTime));

            foreach (var bar in barCollection)
            {
                var pt = new DataPoint(this)
                {
                    XValue = bar.BarDateTime.ToOADate(),
                    YValues = bar.AsChartingValue(),
                    IsValueShownAsLabel = false,
                    Tag = bar
                };
                Points.Add(pt);
            }
        }

        /// <summary>
        /// Construct a point chart where signals are represented by geen or red (buy or sell) markers
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void _BuildSignalSeries(DateTime start, DateTime end)
        {
            Points.Clear();

            ChartType = SeriesChartType.Point;

            var signalCollection = simulation.StrategyManager.GetSignalHistory(security);

            foreach (var signal in signalCollection)
            {
                var pt = new DataPoint(this)
                {
                    XValue = signal.SignalDate.ToOADate(),
                    IsValueShownAsLabel = false,
                    Tag = signal
                };

                switch (signal.SignalAction)
                {
                    case TradeActionBuySell.None:
                        continue;
                    case TradeActionBuySell.Buy:
                        pt.MarkerStyle = MarkerStyle.Triangle;
                        pt.MarkerColor = Color.LightGreen;
                        pt.MarkerSize = 6;
                        pt.YValues = new double[] { security.GetPriceBar(signal.SignalDate).High.ToDouble() + .2 };
                        break;
                    case TradeActionBuySell.Sell:
                        pt.MarkerStyle = MarkerStyle.Circle;
                        pt.MarkerColor = Color.DarkRed;
                        pt.MarkerSize = 6;
                        pt.YValues = new double[] { security.GetPriceBar(signal.SignalDate).Low.ToDouble() - .2 };
                        break;
                    default:
                        continue;
                }

                Points.Add(pt);
            }
        }

        /// <summary>
        /// Construct a bar chart showing current balances
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void _BuildBalanceSeries(DateTime start, DateTime end)
        {
            Points.Clear();
            ChartType = SeriesChartType.Column;
            this["PointWidth"] = "1.0";

            var positions = simulation.PortfolioManager.Portfolio.GetPositions(security);

            for (DateTime i = start; i <= end; i = Calendar.NextTradingDay(i))
            {
                var pt = (from pos in positions
                          where pos.IsOpen(i)
                          select new DataPoint(this)
                          {
                              XValue = i.ToOADate(),
                              IsValueShownAsLabel = false,
                              Tag = pos,
                              YValues = new double[] { pos.Size(i) },
                              Color = Color.FromArgb(63, 255, 255, 0)
                          }).SingleOrDefault();
                if (pt != null)
                    Points.Add(pt);
            }



        }

        /// <summary>
        /// Construct a series with all trades/stops
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void _BuildTradeSeries(DateTime start, DateTime end)
        {
            Points.Clear();

            ChartType = SeriesChartType.Point;

            var tradeHistory = simulation.PortfolioManager.TradeManager.GetHistoricalTrades(security);

            foreach (var trade in tradeHistory)
            {
                var pt = new DataPoint(this)
                {
                    Tag = trade,
                    IsValueShownAsLabel = false,
                    XValue = trade.TradeDate.ToOADate()
                };

                switch (trade.TradeStatus)
                {
                    case TradeStatus.Executed:
                        pt.MarkerStyle = MarkerStyle.Cross;
                        pt.MarkerSize = 16;
                        switch (trade.TradeActionBuySell)
                        {
                            case TradeActionBuySell.Buy:
                                pt.Color = Color.LawnGreen;
                                break;
                            case TradeActionBuySell.Sell:
                                pt.Color = Color.Pink;
                                break;
                        }
                        pt.SetValueY(trade.ExecutedPrice);
                        break;
                    case TradeStatus.Cancelled:
                    case TradeStatus.Rejected:
                        pt.MarkerStyle = MarkerStyle.Diamond;
                        pt.MarkerSize = 6;
                        pt.Color = Color.Orange;
                        pt.SetValueY(trade.ExpectedExecutionPrice);
                        break;
                    case TradeStatus.Stoploss:
                        pt.MarkerStyle = MarkerStyle.Square;
                        pt.MarkerSize = 6;
                        pt.Color = Color.Orange;
                        pt.SetValueY(trade.StopPrice);
                        break;
                    default:
                        break;
                }

                Points.Add(pt);

            }
        }
    }


}
