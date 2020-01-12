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
    /// <summary>
    /// Displays results of a simulation as various time series on the Portfolio
    /// </summary>
    public class PortfolioSimulatioResultChart : Chart
    {

        ChartArea chartArea = new ChartArea("default");

        PortfolioSimulationResultSeries resultSeries = null;

        Simulation simulation = null;

        public bool Initialized { get; private set; } = false;

        public void InitializeChart(Simulation simulation, Control parent)
        {
            this.simulation = simulation;
            _InitializeChartObject(parent);
            _InitializeChartArea();
            Initialized = true;
        }

        public void ViewResult(PortfolioSimulationResultType seriesType)
        {
            LoadResultSeries(seriesType);
        }

        private void LoadResultSeries(PortfolioSimulationResultType seriesType)
        {
            Series.Clear();

            // TODO: add switch for different series

            resultSeries = new PortfolioSimulationResultSeries(simulation, PortfolioSimulationResultType.NetLiquidationValue);

            SetAxisX();
            SetAxisY();

            resultSeries.ChartArea = "default";
            resultSeries.YAxisType = AxisType.Primary;
            Series.Insert(0, resultSeries);

            Update();
        }

        private void AddMinMaxLabels()
        {
            DataPoint maxYpoint = resultSeries.MaxYPoint;
            DataPoint minYpoint = resultSeries.MinYPoint;

            var maxLabel = new Label()
            {
                Text = maxYpoint.YValues.Max().ToString("$0.00")
            };

            


        }

        private void SetAxisX()
        {
            // Min and Max dates of the simulation
            DateTime minX = resultSeries.MinXValue;
            DateTime maxX = resultSeries.MaxXValue;

            chartArea.AxisX.Minimum = minX.ToOADate();
            chartArea.AxisX.Maximum = maxX.ToOADate();

            chartArea.AxisX.MajorGrid.Enabled = false;
        }

        private void SetAxisY()
        {
            // Min and max price values with a small buffer - primary
            double minY = resultSeries.MinYValue * (.90);
            double maxY = resultSeries.MaxYValue * (1.10);

            chartArea.AxisY.Minimum = minY;
            chartArea.AxisY.Maximum = maxY;

            chartArea.AxisY.LabelStyle.Format = "$0.00";
            chartArea.AxisY.Title = "NLV";

            // Gridlines
            var span = maxY - minY;
            chartArea.AxisY.Interval = Math.Floor(span / 10);
            chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(128, 255, 255, 255);

        }

        /// <summary>
        /// Set Chart control properties
        /// </summary>
        private void _InitializeChartObject(Control parent)
        {
            Parent = parent;
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
            chartArea.BackColor = Color.LightGray;

            ChartAreas.Clear();
            ChartAreas.Add(chartArea);
        }

    }

    /// <summary>
    /// Maintains a time series based on user indicated value
    /// </summary>
    public class PortfolioSimulationResultSeries : Series
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
                return (from pt in Points select pt.YValues.Min()).Min();
            }
        }
        public double MaxYValue
        {
            get
            {
                return (from pt in Points select pt.YValues.Max()).Max();
            }
        }

        public DataPoint MaxYPoint { get; private set; }
        public DataPoint MinYPoint { get; private set; }

        private Simulation simulation;
        public PortfolioSimulationResultType resultType { get; }

        public PortfolioSimulationResultSeries(Simulation simulation, PortfolioSimulationResultType resultType)
        {
            this.simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.resultType = resultType;

            _BuildSeries(resultType);
        }

        private void _BuildSeries(PortfolioSimulationResultType resultType)
        {
            switch (resultType)
            {
                case PortfolioSimulationResultType.NetLiquidationValue:
                    _BuildNetLiquidationValueSeries(simulation.SimulationTimeSpan.Item1, simulation.SimulationTimeSpan.Item2);
                    break;
                default:
                    break;
            }
        }

        private void _BuildNetLiquidationValueSeries(DateTime start, DateTime end)
        {
            Points.Clear();

            ChartType = SeriesChartType.Line;

            decimal dailyEquity = 0m;

            for (DateTime i = start; i <= end; i = Calendar.NextTradingDay(i))
            {
                dailyEquity = (simulation.PortfolioManager.Portfolio.NetLiquidationValue(i, TimeOfDay.MarketEndOfDay));
                var pt = new DataPoint(this)
                {
                    XValue = i.ToOADate(),
                    IsValueShownAsLabel = false,
                    YValues = new double[] { dailyEquity.ToDouble() },
                    MarkerSize = 6,
                    MarkerBorderColor = Color.Black,
                    MarkerColor = Color.Black,
                    Tag = i                    
                };

                if (MaxYPoint == null || pt.YValues.Max() > MaxYPoint.YValues.Max())
                    MaxYPoint = pt;
                if (MinYPoint == null || pt.YValues.Min() > MinYPoint.YValues.Min())
                    MinYPoint = pt;

                Points.Add(pt);
            }
        }

    }
}
