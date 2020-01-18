using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    #region Formatting and Display Related Attributes

    //
    // Attributes relating to how data is displayed or provided to the UI
    //

    /// <summary>
    /// Apply to financeial to specify return format for output
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class StringOutputFormatAttribute : Attribute
    {
        string MethodTitle;
        public StringOutputFormatAttribute(string title)
        {
            MethodTitle = (title + ":").PadRight(30) + "{0}";
        }
        public string ToString(object value)
        {
            decimal val = Convert.ToDecimal(value);
            return string.Format(MethodTitle, string.Format($"{val.ToString(" $0.00;-$0.00")}").PadLeft(12));
        }
    }

    /// <summary>
    /// Apply to methods to identify values which should be displayed by the UI within an object
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class UiDisplayTextAttribute : Attribute
    {
        public int Order { get; set; }

        public UiDisplayTextAttribute(int order)
        {
            Order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class ChartableResultAttribute : Attribute
    {
        public string displayText;

        public ChartableResultAttribute(string displayText)
        {
            this.displayText = displayText ?? throw new ArgumentNullException(nameof(displayText));
        }
    }

    /// <summary>
    /// Apply to any value to indicate the Format String which should be applied to the value when displayed
    /// </summary>
    public class DisplayFormatAttribute : Attribute
    {
        public string format;

        public DisplayFormatAttribute(string format)
        {
            this.format = "{0:" + format + "}";
        }
    }

    #endregion
    #region Parameter Related Attributes

    //
    // Attributes used to tag Parameters which can be modified at runtime
    //

    /// <summary>
    /// Generic attribute to identify modifiable properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ParameterAttribute : Attribute
    {
        public string ValueName;
        public string ValueDescription;

        public abstract string ToStringMinMaxValues();
    }

    /// <summary>
    /// Applied to variables which can be changed within a simulation (period, etc)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class TradeSystemParameterDecimalAttribute : ParameterAttribute
    {
        public double Minimum;
        public double Maximum;
        public double Step;

        public TradeSystemParameterDecimalAttribute(string valueName, string valueDescription, double minimum, double maximum, double step)
        {
            ValueName = valueName ?? throw new ArgumentNullException(nameof(valueName));
            ValueDescription = valueDescription ?? throw new ArgumentNullException(nameof(valueDescription));
            Minimum = minimum;
            Maximum = maximum;
            Step = step;
        }

        public override string ToStringMinMaxValues()
        {
            return string.Format($"Min: {Minimum,5:0.00}{Environment.NewLine}Max: {Maximum,5:0.00}");
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TradeSystemParameterIntAttribute : ParameterAttribute
    {
        public int Minimum;
        public int Maximum;
        public int Step;

        public TradeSystemParameterIntAttribute(string valueName, string valueDescription, int minimum, int maximum, int step)
        {
            ValueName = valueName ?? throw new ArgumentNullException(nameof(valueName));
            ValueDescription = valueDescription ?? throw new ArgumentNullException(nameof(valueDescription));
            Minimum = minimum;
            Maximum = maximum;
            Step = step;
        }

        public override string ToStringMinMaxValues()
        {
            return string.Format($"Min: {Minimum,5:#}{Environment.NewLine}Max: {Maximum,5:#}");
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TradeStrategyFilterAttribute : ParameterAttribute
    {
        public TradeStrategyFilterAttribute(string valueName, string valueDescription)
        {
            ValueName = valueName ?? throw new ArgumentNullException(nameof(valueName));
            ValueDescription = valueDescription ?? throw new ArgumentNullException(nameof(valueDescription));
        }

        public override string ToStringMinMaxValues()
        {
            return "NA";
        }
    }

    #endregion
    #region Helper Attributes

    /// <summary>
    /// Applied to methods which must be called immediately upon object creation
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class InitializerAttribute : Attribute
    {

    }

    #endregion
}

