using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

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
        public static void DockTo(this Control me, Control target, DockSide dockToSide, int buffer = 0)
        {
            switch (dockToSide)
            {
                case DockSide.Left:
                    me.Location = new Point((target.Left - me.Width) - buffer, target.Top);
                    break;
                case DockSide.Right:
                    me.Location = new Point(target.Right + buffer, target.Top);
                    break;
                case DockSide.Top:
                    me.Location = new Point(target.Left, (target.Top - me.Height) - buffer);
                    break;
                case DockSide.Bottom:
                    me.Location = new Point(target.Left, (target.Bottom + buffer));
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

        /// <summary>
        /// Returns a SecuritySeries for the given security
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static SecuritySeries ToChartSeries(this Security me)
        {
            return new SecuritySeries(me);
        }

        #endregion
        #region Simple Helpers

        /// <summary>
        /// Adds a generic item to a list and returns a reference to the same object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="me"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T AddAndReturn<T>(this IList<T> me, T obj)
        {
            me.Add(obj);
            return obj;
        }

        /// <summary>
        /// Adds an entry to a dictionary and returns the Value added
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="me"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TValue AddAndReturn<TKey, TValue>(this Dictionary<TKey, TValue> me, TKey key, TValue value)
        {
            me.Add(key, value);
            return value;
        }

        public static bool IsBetween<T>(this T me, T first, T last, bool inclusive = true) where T : IComparable
        {
            if (!inclusive)
                if (me.CompareTo(first) > 0 && me.CompareTo(last) < 0)
                    return true;

            if (inclusive)
                if (me.CompareTo(first) >= 0 && me.CompareTo(last) <= 0)
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

        public static TimeSpan Span(this Tuple<DateTime, DateTime> me)
        {
            return (me.Item2 - me.Item1);
        }

        #endregion
        #region Converters

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

        /// <summary>
        /// Converts date to custom format for log output
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static string ToLogFormat(this DateTime me)
        {
            return me.ToString("yyyyMMdd HH:mm:ss.fff");
        }

        public static decimal ToDecimal(this double me)
        {
            return Convert.ToDecimal(me);
        }
        public static double ToDouble(this decimal me)
        {
            return Convert.ToDouble(me);
        }
        public static int ToInt(this Enum me)
        {
            return (int)(me as object);
        }

        /// <summary>
        /// Converts a sorted list of daily price bars to weekly bars
        /// </summary>
        /// <param name="Me"></param>
        /// <returns></returns>
        public static List<PriceBar> ToWeekly(this List<PriceBar> Me)
        {
            var ret = new List<PriceBar>();
            DateTime startDate = Me[0].BarDateTime;
            while (startDate.DayOfWeek != DayOfWeek.Monday)
                startDate = startDate.AddDays(1);
            Security security = Me[0].Security;

            while (startDate < Me.Last().BarDateTime)
            {
                var week = Me.Where(x => x.BarDateTime.IsBetween(startDate, startDate.AddDays(7))).ToList();
                var min = (from day in week select day.Low).Min();
                var max = (from day in week select day.High).Max();
                var open = week.First().Open;
                var close = week.Last().Close;

                ret.Add(new PriceBar(week.First().BarDateTime, security, open, max, min, close));
                startDate = startDate.AddDays(7);
            }

            return ret;
        }
        /// <summary>
        /// Converts a sorted list of daily price bars to monthly bars
        /// </summary>
        /// <param name="Me"></param>
        /// <returns></returns>
        public static List<PriceBar> ToMonthly(this List<PriceBar> Me)
        {
            var ret = new List<PriceBar>();
            DateTime startDate = Me[0].BarDateTime;
            while (startDate.Day != 1)
                startDate = startDate.AddDays(-1);
            Security security = Me[0].Security;

            while (startDate < Me.Last().BarDateTime)
            {
                var month = Me.Where(x => x.BarDateTime.IsBetween(startDate, startDate.AddMonths(1).AddDays(-1))).ToList();
                var min = (from day in month select day.Low).Min();
                var max = (from day in month select day.High).Max();
                var open = month.First().Open;
                var close = month.Last().Close;

                ret.Add(new PriceBar(month.First().BarDateTime, security, open, max, min, close));
                startDate = startDate.AddMonths(1);
            }

            return ret;
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
            return new List<TradeStrategyBase>()
            {
                new TradeStrategy_1(),
                new TradeStrategy_2()
            };
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

        /// <summary>
        /// Return a list of all modifiable parameters in an object
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static List<PropertyInfo> GetAvailableParameterList(this object me)
        {
            var ret = (from prop in me.GetType().GetProperties()
                       where Attribute.IsDefined(prop, typeof(ParameterAttribute))
                       select prop).ToList();

            return ret;
        }

        /// <summary>
        /// Attempts to set the value of a parameter object.  Returns false if any errors
        /// </summary>
        /// <param name="property"></param>
        /// <param name="targetObject"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ModifyParameterValue(PropertyInfo property, object targetObject, object value)
        {

            if (Attribute.IsDefined(property, typeof(TradeSystemParameterIntAttribute)))
            {
                var attr = (property.GetCustomAttribute(typeof(ParameterAttribute))
                    as TradeSystemParameterIntAttribute);

                if (!int.TryParse(value as string, out int assignmentValue))
                    return false;

                if (assignmentValue > attr.Maximum) return false;
                else if (assignmentValue < attr.Minimum) return false;
                else
                    property.SetValue(targetObject, assignmentValue);

                return true;
            }
            if (Attribute.IsDefined(property, typeof(TradeSystemParameterDecimalAttribute)))
            {
                var currentValue = property.GetValue(targetObject);

                var attr = (property.GetCustomAttribute(typeof(ParameterAttribute))
                    as TradeSystemParameterDecimalAttribute);

                if (!double.TryParse(value as string, out double assignmentValue))
                    return false;

                if (assignmentValue > attr.Maximum) return false;
                else if (assignmentValue < attr.Minimum) return false;
                else
                    property.SetValue(targetObject, Convert.ToDecimal(assignmentValue));
            }
            if (Attribute.IsDefined(property, typeof(TradeStrategyFilterAttribute)))
            {
                var attr = (property.GetCustomAttribute(typeof(ParameterAttribute))
                    as TradeStrategyFilterAttribute);
                var assignmentValue = double.Parse(value as string);
                property.SetValue(targetObject, Convert.ToDecimal(assignmentValue));
            }

            return true;

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

            // TODO: TestMethod for this stuff
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

        #endregion
        #region File Helpers

        private static int fileCount = 0;
        /// <summary>
        /// Writes a list of strings to a notepad file and opens.
        /// </summary>
        /// <param name="text"></param>
        public static void OutputToTextFile(List<string> text, string fileName = "TestOutput")
        {
            using (var sw = new StreamWriter(System.Environment.CurrentDirectory + $@"\{fileName}_{++fileCount}.txt", false))
            {
                text.ForEach(t => sw.WriteLine(t));
                sw.Close();
            }

            Process.Start(System.Environment.CurrentDirectory + $@"\{fileName}_{fileCount}.txt");
        }

        /// <summary>
        /// Returns a list of Symbol,Long Name from a preformatted CSV placed in the \Resources\ directory
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ReadSymbols(string fileName)
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

        #endregion 
    }
}
