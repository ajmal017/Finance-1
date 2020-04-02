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


    public class PersistLayoutAttribute : Attribute
    {

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
    #region Settings Adjustment Attributes

    public class SettingsCategoryAttribute : Attribute
    {
        public SettingsType SettingsType { get; }
        public Type SettingsControlType { get; }

        public SettingsCategoryAttribute(SettingsType settingsType, Type settingsUnderlyingType)
        {
            SettingsType = settingsType;
            SettingsControlType = settingsUnderlyingType;
        }
    }
    public class DisplaySettingConditionAttribute : Attribute
    {

        public bool Display
        {
            get
            {
                var val = Settings.Instance.GetType()
                    .GetProperty(SettingPropertyName)
                    .GetValue(Settings.Instance);
                if (val.ToString() == SettingPropertyValue.ToString())
                    return true;
                else
                    return false;
            }
        }

        private string SettingPropertyName { get; }
        private object SettingPropertyValue { get; }

        public DisplaySettingConditionAttribute(string settingPropertyName, object value)
        {
            SettingPropertyName = settingPropertyName;
            SettingPropertyValue = value;
        }
    }

    #endregion
    #region Misc Attributes

    [AttributeUsage(AttributeTargets.Field)]
    public class FileNameAttribute : Attribute
    {
        public string FileName { get; }

        public FileNameAttribute(string filePath)
        {
            FileName = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }
    }
    public class IncludeAttribute : Attribute
    {
        public bool Include { get; set; }

        public IncludeAttribute(bool include)
        {
            Include = include;
        }
    }
    
    #endregion
    #region System Event Actions

    [AttributeUsage(AttributeTargets.Method)]
    public class SystemEventActionAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public TimeSpan ExecutionTime
        {
            get
            {
                return (TimeSpan)typeof(Settings).GetProperty(ExecutionTimeParameterString).GetValue(Settings.Instance);
            }
        }
        private string ExecutionTimeParameterString { get; }

        /// <param name="executionTimeParameterString">Name of Settings parameter representing the TimeSpan of event execution</param>
        public SystemEventActionAttribute(string displayName, string executionTimeParameterString)
        {
            this.DisplayName = displayName;
            ExecutionTimeParameterString = executionTimeParameterString ?? throw new ArgumentNullException(nameof(executionTimeParameterString));
        }
    }

    #endregion
    #region Live Trading

    public class DisplayValueAttribute : Attribute
    {
        public string Description { get; }
        public string DisplayFormat { get; }

        public DisplayValueAttribute(string description, string displayFormat)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            DisplayFormat = displayFormat ?? throw new ArgumentNullException(nameof(displayFormat));
        }
    }

    #endregion
}

