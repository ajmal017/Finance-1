using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data.Entity;
using System.Windows.Forms.DataVisualization.Charting;
using Finance.TradeStrategies;
using Finance.PositioningStrategies;
using Finance;
using Finance.Data;
using IBApi;

namespace Finance
{
    public static class Helpers
    {
        #region Controls and UI

        /// <summary>
        /// Console-style font
        /// </summary>
        /// <param name="size"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        public static Font SystemFont(float size = 10, FontStyle fontStyle = FontStyle.Regular)
        {
            return new System.Drawing.Font("Consolas", size, fontStyle, System.Drawing.GraphicsUnit.Point, 0);
        }

        /// <summary>
        /// Toggles all controls in a control collection to the 'enabled' bool state
        /// </summary>
        /// <param name="me"></param>
        /// <param name="enabled"></param>
        public static void ToggleControls(this System.Windows.Forms.Control.ControlCollection me, bool enabled)
        {
            foreach (System.Windows.Forms.Control control in me)
            {
                control.Enabled = enabled;
            }
        }

        /// <summary>
        /// Sets a control location based on location and docking style of target control
        /// </summary>
        /// <param name="me"></param>
        /// <param name="target"></param>
        /// <param name="dockToSide"></param>
        /// <param name="buffer"></param>
        public static void DockTo(this Control me, Control target, ControlEdge dockToSide, int buffer = 0)
        {
            if (target == null)
                return;

            switch (dockToSide)
            {
                case ControlEdge.Left:
                    me.Location = new Point((target.Left - me.Width) - buffer, target.Top);
                    break;
                case ControlEdge.Right:
                    me.Location = new Point(target.Right + buffer, target.Top);
                    break;
                case ControlEdge.Top:
                    me.Location = new Point(target.Left, (target.Top - me.Height) - buffer);
                    break;
                case ControlEdge.Bottom:
                    me.Location = new Point(target.Left, (target.Bottom + buffer));
                    break;
                default:
                    break;
            }
        }

        public static bool EdgesAreClose(this Form me, ControlEdge myEdge, Form other, int distance)
        {
            switch (myEdge)
            {
                case ControlEdge.Left:
                    if (Math.Abs(me.Left - other.Right) <= distance &&
                        ((me.Top.IsBetween(other.Top, other.Bottom) || me.Bottom.IsBetween(other.Top, other.Bottom)) ||
                        other.Top.IsBetween(me.Top, me.Bottom) || other.Bottom.IsBetween(me.Top, me.Bottom)))
                        return true;
                    break;
                case ControlEdge.Right:
                    if (Math.Abs(me.Right - other.Left) <= distance &&
                        ((me.Top.IsBetween(other.Top, other.Bottom) || me.Bottom.IsBetween(other.Top, other.Bottom)) ||
                        other.Top.IsBetween(me.Top, me.Bottom) || other.Bottom.IsBetween(me.Top, me.Bottom)))
                        return true;
                    break;
                case ControlEdge.Top:
                    if (Math.Abs(me.Top - other.Bottom) <= distance &&
                        ((me.Left.IsBetween(other.Left, other.Right) || me.Right.IsBetween(other.Left, other.Right)) ||
                        other.Left.IsBetween(me.Left, me.Right) || other.Right.IsBetween(me.Left, me.Right)))
                        return true;
                    break;
                case ControlEdge.Bottom:
                    if (Math.Abs(me.Bottom - other.Top) <= distance &&
                        ((me.Left.IsBetween(other.Left, other.Right) || me.Right.IsBetween(other.Left, other.Right)) ||
                        other.Left.IsBetween(me.Left, me.Right) || other.Right.IsBetween(me.Left, me.Right)))
                        return true;
                    break;
                default:
                    break;
            }
            return false;
        }
        public static ControlEdge Opposite(this ControlEdge me)
        {
            switch (me)
            {
                case ControlEdge.None:
                    return ControlEdge.None;
                case ControlEdge.Left:
                    return ControlEdge.Right;
                case ControlEdge.Right:
                    return ControlEdge.Left;
                case ControlEdge.Top:
                    return ControlEdge.Bottom;
                case ControlEdge.Bottom:
                    return ControlEdge.Top;
                default:
                    return ControlEdge.None;
            }
        }
        public static void SnapTo(this Form me, Form target, ControlEdge myEdge)
        {
            if (target == null)
                return;

            switch (myEdge)
            {
                case ControlEdge.Left:
                    me.Location = new Point((target.Right), me.Location.Y);
                    break;
                case ControlEdge.Right:
                    me.Location = new Point((target.Left - me.Width), me.Location.Y);
                    break;
                case ControlEdge.Top:
                    me.Location = new Point(me.Location.X, target.Bottom);
                    break;
                case ControlEdge.Bottom:
                    me.Location = new Point(me.Location.X, target.Top - me.Height);
                    break;
                default:
                    break;
            }
        }

        #endregion
        #region Charting Helpers

        /// <summary>
        /// Returns PriceBar values as High-Low-Open-Close for use in a Candlestick charting element
        /// </summary>
        /// <param name="bar"></param>
        /// <returns></returns>
        public static double[] AsChartingValue(this PriceBar bar)
        {
            return new double[]
            {
                bar.High.ToDouble(),
                bar.Low.ToDouble(),
                bar.Open.ToDouble(),
                bar.Close.ToDouble()
            };
        }

        public static int ChartPixelWidth(this ChartArea me)
        {
            var min = me.AxisX.ValueToPixelPosition(me.AxisX.Minimum);
            var max = me.AxisX.ValueToPixelPosition(me.AxisX.Maximum);

            return Convert.ToInt32(max - min);
        }
        public static int ChartPointPixelWidth(this ChartArea me)
        {
            int chartPixelWidth = ChartPixelWidth(me);
            var min = me.AxisX.Minimum;
            var max = me.AxisX.Maximum;
            var chartIntervalWidth = (max - min);

            return Convert.ToInt32(chartPixelWidth / chartIntervalWidth);
        }

        public static string ToShorthand(this string me)
        {
            StringBuilder sb = new StringBuilder();
            if (me.Split().Count() > 1)
            {
                foreach (var word in me.Split())
                {
                    if (word.Length > 4)
                        sb.Append($"{word.Substring(0, 4)}.");
                    else
                        sb.Append(word);
                }
                return sb.ToString();
            }
            return me;
        }

        #endregion
        #region Simple Helpers

        public static T AddAndReturn<T>(this IList<T> me, T obj)
        {
            me.Add(obj);
            return obj;
        }
        public static TEntity AddAndReturn<TEntity>(this DbSet<TEntity> me, TEntity obj) where TEntity : class
        {
            me.Add(obj);
            return obj;
        }
        public static TValue AddAndReturn<TKey, TValue>(this Dictionary<TKey, TValue> me, TKey key, TValue value)
        {
            me.Add(key, value);
            return value;
        }

        public static bool IsBetween<T>(this T me, T smaller, T larger, bool inclusive = true) where T : IComparable
        {
            if (!inclusive)
                if (me.CompareTo(smaller) > 0 && me.CompareTo(larger) < 0)
                    return true;

            if (inclusive)
                if (me.CompareTo(smaller) >= 0 && me.CompareTo(larger) <= 0)
                    return true;

            return false;
        }

        /// <summary>
        /// Returns the name of the currently executed method for debugging purposes
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }

        public static TimeSpan Span(this (DateTime, DateTime) me)
        {
            return (me.Item2 - me.Item1);
        }

        public static string JustifyStrings(string leftStr, string rightStr, int totalLength)
        {
            if (totalLength < (leftStr.Length + rightStr.Length))
                totalLength = leftStr.Length + rightStr.Length + 1;

            StringBuilder sb = new StringBuilder();
            sb.Append(leftStr);
            sb.Append(' ', (totalLength - leftStr.Length - rightStr.Length));
            sb.Append(rightStr);

            return sb.ToString();
        }

        public static DateTime DateTimeMin(DateTime obj1, DateTime obj2)
        {
            return obj1.CompareTo(obj2) <= 0 ? obj1 : obj2;
        }
        public static DateTime DateTimeMax(DateTime obj1, DateTime obj2)
        {
            return obj1.CompareTo(obj2) >= 0 ? obj1 : obj2;
        }

        public static DateTime MonthAndYear(this DateTime me)
        {
            return new DateTime(me.Year, me.Month, 1);
        }

        /// <summary>
        /// Rounds an int up to a whole number based on rank (100s, 1000s, 10000s, etc)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int RoundUpWholeNumberLargestPlace(int input)
        {
            int divisor = 1;
            while (input / divisor > 9)
                divisor *= 10;

            return divisor * ((input / divisor) + 1);
        }
        public static int RoundUpWholeNumber2ndLargestPlace(int input)
        {
            int divisor = 1;
            while (input / divisor > 99)
                divisor *= 10;

            return divisor * ((input / divisor) + 1);
        }

        /// <summary>
        /// Rounds a decimal up to a whole number based on rank (100s, 1000s, 10000s, etc)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static decimal RoundUpWholeNumberLargestPlace(decimal input)
        {
            int newInput = (int)Math.Ceiling(input);
            int divisor = 1;
            while (newInput / divisor > 9)
                divisor *= 10;

            return divisor * ((newInput / divisor) + 1);
        }
        public static decimal RoundUpWholeNumber2ndLargestPlace(decimal input)
        {
            int newInput = (int)Math.Ceiling(input);
            int divisor = 1;
            while (newInput / divisor > 99)
                divisor *= 10;

            return divisor * ((newInput / divisor) + 1);
        }

        /// <summary>
        /// Rounds an int down to a whole number based on rank (100s, 1000s, 10000s, etc)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int RoundDownWholeNumberLargestPlace(int input)
        {
            int divisor = 1;
            while (input / divisor > 9)
                divisor *= 10;

            return divisor * ((input / divisor));
        }
        public static int RoundDownWholeNumber2ndLargestPlace(int input)
        {
            int divisor = 1;
            while (input / divisor > 99)
                divisor *= 10;

            return divisor * ((input / divisor));
        }

        /// <summary>
        /// Rounds a decimal down to a whole number based on rank (100s, 1000s, 10000s, etc)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static decimal RoundDownWholeNumberLargestPlace(decimal input)
        {
            int newInput = (int)Math.Floor(input);
            int divisor = 1;
            while (newInput / divisor > 9)
                divisor *= 10;

            return divisor * ((newInput / divisor));
        }
        public static decimal RoundDownWholeNumber2ndLargestPlace(decimal input)
        {
            int newInput = (int)Math.Floor(input);
            int divisor = 1;
            while (newInput / divisor > 99)
                divisor *= 10;

            return divisor * ((newInput / divisor));
        }

        public static bool HasFlag(this Enum me, Enum flag)
        {
            return (me.ToInt() & flag.ToInt()) == flag.ToInt();
        }

        #endregion
        #region Converters

        public static DateTime FromIbkrTimeFormat(this long me)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(me).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        /// Returns a datetime string per IBKR format convention
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static string ToIbkrFormat(this DateTime me)
        {
            return me.ToString("yyyyMMdd HH:mm:ss");
        }

        /// <summary>
        /// Converts an IBKR datetime string to DateTime struct
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static DateTime FromIbkrFormat(this string me)
        {
            if (DateTime.TryParseExact(me, new string[] { "yyyyMMdd HH:mm:ss", "yyyyMMdd" }, CultureInfo.CurrentCulture, DateTimeStyles.AllowInnerWhite, out DateTime ret))
                return ret;

            throw new FormatException();
        }

        /// <summary>
        /// Returns a formatted string to use for IBKR duration, i.e. '3 D'
        /// </summary>
        /// <param name="startDate">Inclusive</param>
        /// <param name="endDate">Inclusive</param>
        /// <returns></returns>
        public static string ToIbkrDuration(DateTime startDate, DateTime endDate)
        {
            if (startDate.CompareTo(endDate) > 0) throw new InvalidDateOrderException();

            TimeSpan span = (endDate - startDate);

            // Requests over 365 days must be made in years
            if (span.TotalDays >= 365)
            {
                int years = ((int)span.TotalDays / 365);
                return string.Format($"{years} Y");
            }
            else
                return string.Format($"{span.Days + 1} D");
        }

        public static string IexToIbkrExchangeCode(this string me)
        {
            switch (me)
            {
                case "NYS":
                    return "NYSE";
                case "PSE":
                    return "PHLX";
                case "BATS":
                    return "BATS";
                case "ASE":
                    return "AMEX";
                case "NAS":
                    return "ISLAND";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Converts date to custom format for log output
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static string ToLogFormat(this DateTime me)
        {
            return me.ToString("yyyyMMdd HH:mm:ss.fff");
        }

        public static SecurityType FromIexCode(this string me)
        {
            switch (me)
            {
                case "ad":
                    return SecurityType.ADR;
                case "re":
                    return SecurityType.REIT;
                case "ce":
                    return SecurityType.ClosedEndFund;
                case "si":
                    return SecurityType.SecondaryIssue;
                case "lp":
                    return SecurityType.LimitedPartnership;
                case "cs":
                    return SecurityType.CommonStock;
                case "et":
                    return SecurityType.ETF;
                case "wt":
                    return SecurityType.Warrant;
                case "rt":
                    return SecurityType.Right;
                case "ut":
                    return SecurityType.Unit;
                case "temp":
                    return SecurityType.Temp;
                default:
                    return SecurityType.Unknown;
            }
        }

        public static decimal ToDecimal(this double me)
        {
            return Convert.ToDecimal(me);
        }
        public static decimal ToDecimal(this int me)
        {
            return Convert.ToDecimal(me);
        }
        public static double ToDouble(this decimal me)
        {
            return Convert.ToDouble(me);
        }
        public static double ToDouble(this int me)
        {
            return Convert.ToInt32(me);
        }
        public static int ToInt(this Enum me)
        {
            return (int)(me as object);
        }

        /// <summary>
        /// Converts a sorted list of daily price bars to weekly bars
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static List<PriceBar> ToWeekly(this List<PriceBar> me)
        {
            var ret = new List<PriceBar>();
            DateTime startDate = me.Min(x => x.BarDateTime);

            while (startDate.DayOfWeek != DayOfWeek.Monday)
                startDate = startDate.AddDays(1);
            Security security = me[0].Security;

            while (startDate < me.Last().BarDateTime)
            {
                var week = me.Where(x => x.BarDateTime.IsBetween(startDate, startDate.AddDays(7))).ToList();
                if (week.Count > 0)
                {
                    var min = (from day in week select day.Low).Min();
                    var max = (from day in week select day.High).Max();
                    var open = week.First().Open;
                    var close = week.Last().Close;
                    ret.Add(new PriceBar(week.First().BarDateTime, security, open, max, min, close));
                }

                startDate = startDate.AddDays(7);
            }

            return ret;
        }
        /// <summary>
        /// Converts a sorted list of daily price bars to monthly bars
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static List<PriceBar> ToMonthly(this List<PriceBar> me)
        {
            var ret = new List<PriceBar>();

            DateTime startDate = me.Min(x => x.BarDateTime);
            while (startDate.Day != 1)
                startDate = startDate.AddDays(-1);
            Security security = me.FirstOrDefault().Security;

            while (startDate < me.Last().BarDateTime)
            {
                var month = me.Where(x => x.BarDateTime.IsBetween(startDate, startDate.AddMonths(1).AddDays(-1))).ToList();
                if (month.Count == 0)
                {
                    startDate = startDate.AddMonths(1);
                    continue;
                }
                var min = (from day in month select day.Low).Min();
                var max = (from day in month select day.High).Max();
                var open = month.First().Open;
                var close = month.Last().Close;

                ret.Add(new PriceBar(month.First().BarDateTime, security, open, max, min, close));
                startDate = startDate.AddMonths(1);
            }

            return ret;
        }

        public static (int wholeYears, int daysRemainder) ToIbkrPartition(this TimeSpan span)
        {
            int wholeYears = ((int)span.TotalDays) / 365;
            int daysRemainder = ((int)span.TotalDays % 365);

            return (wholeYears, daysRemainder);
        }

        public static Contract GetContract(this Security me)
        {
            return new Contract()
            {
                Currency = "USD",
                Symbol = me.Ticker,
                Exchange = "SMART",
                PrimaryExch = me.Exchange.IexToIbkrExchangeCode(),
                SecType = "STK"
            };
        }

        #endregion
        #region Object Copiers

        /// <summary>
        /// Returns a deep copy of a list of trades
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static List<Trade> Copy(this List<Trade> me)
        {
            var ret = new List<Trade>();
            me.ForEach(trd => ret.Add(trd.Copy()));
            return ret;
        }

        /// <summary>
        /// Returns a deep copy of a list of positions
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static List<Position> Copy(this List<Position> me)
        {
            var ret = new List<Position>();
            me.ForEach(pos => ret.Add(pos.Copy()));
            return ret;
        }

        #endregion
        #region Attribute Helpers

        /// <summary>
        /// Returns a list of all available trade strategies
        /// </summary>
        /// <returns></returns>
        public static List<TradeStrategyBase> AllTradeStrategies()
        {
            var ret = new List<TradeStrategyBase>();

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.Namespace == "Finance.TradeStrategies" && type.GetCustomAttribute<IncludeAttribute>()?.Include == true)
                {
                    ret.Add(type.GetConstructor(Type.EmptyTypes).Invoke(null) as TradeStrategyBase);
                }
            }

            return ret;

        }

        /// <summary>
        /// Returns a list of all available position management strategies
        /// </summary>
        /// <returns></returns>
        public static List<PositioningStrategyBase> AllPositioningStrategies()
        {
            var ret = new List<PositioningStrategyBase>();

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.Namespace == "Finance.PositioningStrategies" && type.GetCustomAttribute<IncludeAttribute>()?.Include == true)
                {
                    ret.Add(type.GetConstructor(Type.EmptyTypes).Invoke(null) as PositioningStrategyBase);
                }
            }

            return ret;

        }

        /// <summary>
        /// Initializes an object by calling all methods tagged with an InitializerAttribute tag
        /// </summary>
        /// <param name="me"></param>
        public static void InitializeMe(this object me)
        {

            foreach (MethodInfo method in me.GetType().BaseType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(method, typeof(InitializerAttribute)))
                {
                    method.Invoke(me, null);
                }
            }
            foreach (MethodInfo method in me.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(method, typeof(InitializerAttribute)))
                {
                    method.Invoke(me, null);
                }
            }
        }

        public static string Description(this Enum me)
        {
            var value = me.GetType().GetMember(me.ToString());

            if (Attribute.IsDefined(value.FirstOrDefault(), typeof(DescriptionAttribute)))
                return value[0].GetCustomAttribute<DescriptionAttribute>().Description;
            else
                return Enum.GetName(me.GetType(), me);
        }

        public static Enum ToEnumValue(this string me, Type enumType)
        {
            var members = enumType.GetFields();

            foreach (var member in members)
            {
                if (Attribute.IsDefined(member, typeof(DescriptionAttribute)) && member.GetCustomAttribute<DescriptionAttribute>().Description == me)
                    return member.GetValue(enumType) as Enum;
            }

            return null;
        }

        #endregion
        #region Trading Related

        /// <summary>
        /// Returns true if the provided datetime falls after market close
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static bool AfterHours(DateTime time)
        {
            TimeSpan close = new TimeSpan(15, 0, 0);

            if (time.TimeOfDay > close)
                return true;

            return false;
        }

        /// <summary>
        /// Calculates the average basis cost of a sequence of trades
        /// </summary>
        /// <param name="trades">Ordered list of trades. Reordered in ascending date order upon entry.</param>
        /// <param name="direction">Calculates for a long position or short.  Throws an exception if the first trade in the sequence does not match</param>
        /// <returns>The average cost per share of remaining shares at the end of the trade sequence.  Throws an exception if the position ever flips long/short</returns>
        public static decimal AverageCost(this List<Trade> trades, PositionDirection direction)
        {
            // Order the trades
            trades = new List<Trade>(trades).OrderBy(x => x.TradeDate).ToList();

            // Check direction
            if ((int)trades.FirstOrDefault().TradeActionBuySell != (int)direction)
                throw new InvalidTradeForPositionException();

            // Iterate through the trades and compute average cost
            int sharesOpen = 0;
            decimal averageCost = 0;

            foreach (var trd in trades)
            {
                // Buy trades increase position and update average price
                // Sell trades reduce position
                if (trd.TradeActionBuySell.ToInt() == direction.ToInt())
                {
                    averageCost = ((averageCost * sharesOpen) + (trd.TotalCashImpactAbsolute)) / (sharesOpen += trd.Quantity);
                }
                else if (trd.TradeActionBuySell.ToInt() == -direction.ToInt())
                {
                    sharesOpen -= trd.Quantity;
                }

                // sharesOpen should always be positive, otherwise we have flipped long/short
                if (sharesOpen < 0)
                    throw new InvalidTradeForPositionException();
            }

            return Math.Round(averageCost, 3);
        }

        /// <summary>
        /// Class which contains information about a system event to execute by appropriate handler
        /// </summary>
        public class SystemEventAction
        {
            public string EventName { get; }
            public TimeSpan ExecutionTime { get; }
            private Action ExecutionAction { get; }

            public SystemEventAction(string name, TimeSpan executionTime, Action executionAction)
            {
                EventName = name;
                ExecutionTime = executionTime;
                ExecutionAction = executionAction ?? throw new ArgumentNullException(nameof(executionAction));
            }
            public void TryExecute()
            {
                try
                {
                    new Task(() => ExecutionAction()).Start();
                }
                catch (Exception ex)
                {
                    Logger.Log(new LogMessage($"SystemEvent -> {EventName}",
                        $"Event execution failed: {ex.Message}", LogMessageType.SystemError));

                    return;
                }
            }
        }

        public class Currency { };
        public class LongText { };
        public class OptionList { };


        #endregion
        #region File Helpers

        /// <summary>
        /// Writes a list of strings to a notepad file and opens.
        /// </summary>
        /// <param name="text"></param>
        public static void OutputToTextFile(List<string> text, string FilePath)
        {
            using (var sw = new StreamWriter(FilePath, false))
            {
                text.ForEach(t => sw.WriteLine(t));
                sw.Close();
            }
        }

        /// <summary>
        /// Returns a list of Symbol,Long Name from a preformatted CSV placed in the \Resources\ directory
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ReadSymbolsAndNames(string fileName)
        {
            if (!fileName.Contains("SymbolList"))
                throw new FileLoadException();

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $@"{fileName}");

            using (StreamReader reader = new StreamReader(path))
            {
                var ret = new Dictionary<string, string>();

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string symbol = line.Split(null)[0];
                    line = line.Remove(0, symbol.Length).Trim();
                    ret.Add(symbol, line);
                }
                reader.Close();
                return ret;
            }
        }

        /// <summary>
        /// Returns a list of Symbol,Long Name from a preformatted CSV placed in the \Resources\ directory
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static List<string> ReadSymbols(string fileName)
        {
            if (!fileName.Contains("SymbolList"))
                throw new FileLoadException();

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $@"{fileName}");

            using (StreamReader reader = new StreamReader(path))
            {
                var ret = new List<string>();

                while (!reader.EndOfStream)
                {
                    string symbol = reader.ReadLine();
                    symbol = symbol.Trim();
                    ret.Add(symbol);
                }
                reader.Close();
                return ret;
            }
        }

        /// <summary>
        /// Returns a list of symbol list files
        /// </summary>
        /// <returns></returns>
        public static string[] GetSymbolLists()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $@"Resources\");

            var ret = Directory.GetFiles(path).ToList();
            ret.RemoveAll(x => !x.Contains("SymbolList"));
            return ret.ToArray();
        }

        private static Dictionary<int, string> SICCodes;
        public static string GetIndustryBySIC(int SIC)
        {
            if (SICCodes == null)
                LoadSicCodes();

            if (SICCodes.ContainsKey(SIC))
                return SICCodes[SIC];
            else
                return "UNK";
        }
        public static List<int> GetSICByIndustry(string industryName)
        {
            if (SICCodes == null)
                LoadSicCodes();

            var ret = new List<int>();

            if (SICCodes.ContainsValue(industryName))
            {
                var vals = (from e in SICCodes where e.Value == industryName select e.Key).ToList();
                ret.AddRange(vals);
                return vals;
            }
            else
                return ret;
        }
        public static List<string> GetAllSICCodeStrings()
        {
            if (SICCodes == null)
                LoadSicCodes();

            var ret = new List<string>();
            foreach (var entry in SICCodes)
            {
                ret.Add(entry.Value);
            }
            return ret;
        }
        public static List<int> GetAllSICCodeInts()
        {
            if (SICCodes == null)
                LoadSicCodes();

            var ret = (from e in SICCodes select e.Key).ToList();
            return ret;
        }
        private static void LoadSicCodes()
        {
            SICCodes = new Dictionary<int, string>();
            string path = Settings.Instance.FilePath_SicCodes;

            if (!File.Exists(path))
                throw new FileNotFoundException("Could not locate SIC Codes file");

            using (var stream = new StreamReader(path))
            {
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();
                    int code = Int32.Parse(line.Split()[0]);
                    string des = line.Replace(code.ToString(), "").Trim();
                    SICCodes.Add(code, des);
                }
            }
        }

        #endregion
        #region List Helpers

        public static bool TryPopAt<T>(ref ConcurrentBag<T> me, int itemId, out T request) where T : RefDataProviderRequest
        {
            var temp = me.ToList();
            var i = temp.FindIndex(x => x.RequestID == itemId);

            if (i == -1)
            {
                request = null;
                return false;
            }

            request = temp[i];
            temp.RemoveAt(i);
            me = new ConcurrentBag<T>(temp.ToList());

            return true;
        }
        public static T PopAt<T>(ref ConcurrentBag<T> me, int itemId) where T : RefDataProviderRequest
        {
            var temp = me.ToList();
            var i = temp.FindIndex(x => x.RequestID == itemId);
            var ret = temp[i];
            temp.RemoveAt(i);
            me = new ConcurrentBag<T>(temp.ToList());
            return (T)Convert.ChangeType(ret, typeof(T));
        }
        public static T Pop<T>(ref ConcurrentBag<T> me) where T : RefDataProviderRequest
        {
            var temp = me.ToList();
            var ret = temp[0];
            temp.RemoveAt(0);
            me = new ConcurrentBag<T>(temp.ToList());
            return (T)Convert.ChangeType(ret, typeof(T));
        }
        public static void Push<T>(ref ConcurrentBag<T> me, T item) where T : RefDataProviderRequest
        {
            var temp = me.ToList();
            temp.Insert(0, item);
            me = new ConcurrentBag<T>(temp);
        }
        public static void PushEnd<T>(ConcurrentBag<T> me, T item) where T : RefDataProviderRequest
        {
            me.Add(item);
        }
        public static T PeekFirstPending<T>(this ConcurrentBag<T> me) where T : RefDataProviderRequest
        {
            var ret = me.First(x => x.RequestStatus == DataProviderRequestStatus.Pending);

            if (ret == null)
                return default;

            return (T)Convert.ChangeType(ret, typeof(T));
        }
        public static bool TryPopFirstPending<T>(ref ConcurrentBag<T> me, out T request) where T : RefDataProviderRequest
        {
            var temp = me.ToList();
            var req = temp.FirstOrDefault(x => x.RequestStatus == DataProviderRequestStatus.Pending);

            if (req == null)
            {
                request = null;
                return false;
            }

            request = req;
            temp.RemoveAt(temp.IndexOf(req));
            me = new ConcurrentBag<T>(temp.ToList());

            return true;
        }
        public static T PeekAt<T>(this ConcurrentBag<T> me, int itemId) where T : RefDataProviderRequest
        {
            var ret = me.First(x => x.RequestID == itemId);

            if (ret == null)
                return default;

            return (T)Convert.ChangeType(ret, typeof(T));
        }

        #endregion
        #region Swing Points and Trends

        public static bool IsTestingSwingPointHigh(this PriceBar me, PriceBar priorSwingPointHigh)
        {
            if (me.PriorBar == null)
                return false;

            if (me.PriorBar.Close < priorSwingPointHigh.High && me.High > priorSwingPointHigh.High)
                return true;
            else
                return false;
        }
        public static bool IsTestingSwingPointLow(this PriceBar me, PriceBar priorSwingPointLow)
        {
            if (me.PriorBar == null)
                return false;

            if (me.PriorBar.Close > priorSwingPointLow.Low && me.Low < priorSwingPointLow.Low)
                return true;
            else
                return false;
        }

        public static (SwingPointTestPriceResult priceTest, SwingPointTestVolumeResult volumeTest) SwingPointHighTest(PriceBar currentBar, PriceBar swingPointHighBar)
        {
            SwingPointTestPriceResult priceTest;
            SwingPointTestVolumeResult volumeTest;

            if (currentBar.High < swingPointHighBar.High)
                throw new SwingPointOperationException() { message = "Non-tested swing point high" };

            if (currentBar.Close > swingPointHighBar.High)
                priceTest = SwingPointTestPriceResult.CloseExceedsSwingPoint;
            else
                priceTest = SwingPointTestPriceResult.CloseDoesNotExceepSwingPoint;

            if (currentBar.Volume > swingPointHighBar.Volume)
                volumeTest = SwingPointTestVolumeResult.VolumeExpands;
            else
                volumeTest = SwingPointTestVolumeResult.VolumeContracts;

            return (priceTest, volumeTest);
        }
        public static (SwingPointTestPriceResult priceTest, SwingPointTestVolumeResult volumeTest) SwingPointLowTest(PriceBar currentBar, PriceBar swingPointLowBar)
        {
            SwingPointTestPriceResult priceTest;
            SwingPointTestVolumeResult volumeTest;

            if (currentBar.Low > swingPointLowBar.Low)
                throw new SwingPointOperationException() { message = "Non-tested swing point low" };

            if (currentBar.Close < swingPointLowBar.Low)
                priceTest = SwingPointTestPriceResult.CloseExceedsSwingPoint;
            else
                priceTest = SwingPointTestPriceResult.CloseDoesNotExceepSwingPoint;

            if (currentBar.Volume > swingPointLowBar.Volume)
                volumeTest = SwingPointTestVolumeResult.VolumeExpands;
            else
                volumeTest = SwingPointTestVolumeResult.VolumeContracts;

            return (priceTest, volumeTest);
        }

        public static TrendAlignment GetTrendAlignment(params TrendQualification[] trends)
        {
            // All bullish
            if (trends.ToList().All(x => x == TrendQualification.SuspectBullish || x == TrendQualification.ConfirmedBullish))
            {
                return Finance.TrendAlignment.Bullish;
            }
            // Mixed bullish and sideways
            if (trends.ToList().All(x => x == TrendQualification.SuspectBullish || x == TrendQualification.ConfirmedBullish
            || x == TrendQualification.AmbivalentSideways || x == TrendQualification.SuspectSideways || x == TrendQualification.ConfirmedSideways))
            {
                return Finance.TrendAlignment.SidewaysBullish;
            }

            // All bearish
            if (trends.ToList().All(x => x == TrendQualification.SuspectBearish || x == TrendQualification.ConfirmedBearish))
            {
                return Finance.TrendAlignment.Bearish;
            }
            // Mixed bearish and sideways
            if (trends.ToList().All(x => x == TrendQualification.SuspectBearish || x == TrendQualification.ConfirmedBearish
            || x == TrendQualification.AmbivalentSideways || x == TrendQualification.SuspectSideways || x == TrendQualification.ConfirmedSideways))
            {
                return Finance.TrendAlignment.SidewaysBearish;
            }

            // All Sideways
            if (trends.ToList().All(x => x == TrendQualification.AmbivalentSideways || x == TrendQualification.SuspectSideways || x == TrendQualification.ConfirmedSideways))
            {
                return Finance.TrendAlignment.Sideways;
            }

            // Bearish and bullish
            if (trends.ToList().All(x => x == TrendQualification.SuspectBearish || x == TrendQualification.ConfirmedBearish
            || x == TrendQualification.SuspectBullish || x == TrendQualification.ConfirmedBullish))
            {
                return Finance.TrendAlignment.Opposing;
            }

            return TrendAlignment.NotSet;

        }

        #endregion
        #region Helper Classes

        public class SecurityFilter
        {
            public List<string> IndustryFilters { get; set; } = new List<string>();
            public List<string> SectorFilters { get; set; } = new List<string>();
            public List<int> SicFilters { get; set; } = new List<int>();
            public List<SecurityType> TypeFilters { get; set; } = new List<SecurityType>();

            public List<TrendQualification> CurrentTrendFilters { get; set; } = new List<TrendQualification>();

            public bool ExcludeMissingData { get; set; } = false;
            public bool FavoritesOnly { get; set; } = false;
            public bool ExcludeZeroVolume { get; set; } = false;

            public void ClearFilters(SecurityFilterType filterType)
            {
                switch (filterType)
                {
                    case SecurityFilterType.None:
                        break;
                    case SecurityFilterType.Industry:
                        IndustryFilters.Clear();
                        break;
                    case SecurityFilterType.Sector:
                        SectorFilters.Clear();
                        break;
                    case SecurityFilterType.SIC:
                        SicFilters.Clear();
                        break;
                    case SecurityFilterType.SecurityType:
                        TypeFilters.Clear();
                        break;
                    case SecurityFilterType.Trend:
                        CurrentTrendFilters.Clear();
                        break;
                    default:
                        break;
                }
            }
            public void AddFilterValue(SecurityFilterType filterType, object value)
            {
                switch (filterType)
                {
                    case SecurityFilterType.None:
                        break;
                    case SecurityFilterType.Industry:
                        if (value is string ind)
                            AddIndustryFilter(ind);
                        else
                            throw new ArgumentException("Invalid filter type");
                        break;
                    case SecurityFilterType.Sector:
                        if (value is string sec)
                            AddSectorFilter(sec);
                        else
                            throw new ArgumentException("Invalid filter type");
                        break;
                    case SecurityFilterType.SIC:
                        if (value is int sic)
                            AddSicFilter(sic);
                        else
                            throw new ArgumentException("Invalid filter type");
                        break;
                    case SecurityFilterType.SecurityType:
                        if (value is SecurityType type)
                            AddTypeFilter(type);
                        else
                            throw new ArgumentException("Invalid filter type");
                        break;
                    case SecurityFilterType.Trend:
                        if (value is TrendQualification trendType)
                            AddTrendFilter(trendType);
                        else
                            throw new ArgumentException("Invalid filter type");
                        break;
                    default:
                        break;
                }
            }

            private void AddIndustryFilter(string value)
            {
                if (!IndustryFilters.Contains(value))
                    IndustryFilters.Add(value);
            }
            private void AddSectorFilter(string value)
            {
                if (!SectorFilters.Contains(value))
                    SectorFilters.Add(value);
            }
            private void AddSicFilter(int value)
            {
                if (!SicFilters.Contains(value))
                    SicFilters.Add(value);
            }
            private void AddTypeFilter(SecurityType value)
            {
                if (!TypeFilters.Contains(value))
                    TypeFilters.Add(value);
            }
            private void AddTrendFilter(TrendQualification value)
            {
                if (!CurrentTrendFilters.Contains(value))
                    CurrentTrendFilters.Add(value);
            }

        }
        
        public interface IPersistLayout
        {
            string Name { get; set; }
            Size Size { get; set; }
            Point Location { get; set; }
            FormStartPosition StartPosition { get; set; }
            bool Sizeable { get; }

            void SaveLayout();
            void LoadLayout();
        }

        #endregion
    }
}
