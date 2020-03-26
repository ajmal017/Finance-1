using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;
using System.IO;
using static Finance.Helpers;

namespace Finance
{
    public class Settings : ApplicationSettingsBase
    {
        private static Settings _Instance { get; set; }
        public static Settings Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new Settings();
                }
                return _Instance;
            }
        }

        public Settings() { }

        public static List<PropertyInfo> GetSettingsByCategoryTag(SettingsType settingsType)
        {
            var ret = new List<PropertyInfo>();

            foreach (PropertyInfo property in typeof(Settings).GetProperties())
            {
                if (Attribute.IsDefined(property, typeof(SettingsCategoryAttribute)) &&
                    (property.GetCustomAttribute<SettingsCategoryAttribute>().SettingsType == settingsType))
                {
                    ret.Add(property);
                }
            }

            return ret;
        }

        #region Resource File Paths

        private string FolderName_Resources = "Resources";

        [ApplicationScopedSetting()]
        public string FilePath_SicCodes
        {
            get
            {
                return Path.Combine(Environment.CurrentDirectory, FolderName_Resources, "SicCodes.txt");
            }
        }

        #endregion
        #region Stored Resources

        public List<string> MarketSectors
        {
            get
            {
                var ret = new List<string>();
                foreach (string val in _SectorsCsv.Split(','))
                    ret.Add(val);
                return ret;
            }
            set
            {
                StringBuilder sb = new StringBuilder();
                foreach (string val in value)
                    sb.Append($"{val},");

                string storedValue = sb.ToString().TrimEnd(',');
                _SectorsCsv = storedValue;
            }
        }

        [UserScopedSetting()]
        [DefaultSettingValue(" ")]
        public string _SectorsCsv
        {
            get
            {
                return (string)this["_SectorsCsv"];
            }
            set
            {
                this["_SectorsCsv"] = (string)value;
                Save();
            }
        }

        #endregion
        #region Application Mode

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Application, typeof(ApplicationMode))]
        [DefaultSettingValue("Testing")]
        [SettingsDescription("Application Mode")]
        public ApplicationMode ApplicationMode
        {
            get
            {
                return (ApplicationMode)this["ApplicationMode"];
            }
            set
            {
                this["ApplicationMode"] = (ApplicationMode)value;
                Save();
            }
        }

        //public string ApplicationModeString => Enum.GetName(typeof(ApplicationMode), ApplicationMode);
        public bool Debug => ApplicationMode == ApplicationMode.Testing;

        #endregion
        #region Data Provider Settings

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(DataProviderType))]
        [DefaultSettingValue("IEXCloud")]
        [SettingsDescription("Reference Data Provider")]
        public DataProviderType RefDataProvider
        {
            get
            {
                return (DataProviderType)this["RefDataProvider"];
            }
            set
            {
                this["RefDataProvider"] = (DataProviderType)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(bool))]
        [DefaultSettingValue("true")]
        [SettingsDescription("Auto Connect on Start")]
        public bool AutoConnectOnStart
        {
            get
            {
                return (bool)this["AutoConnectOnStart"];
            }
            set
            {
                this["AutoConnectOnStart"] = (bool)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(DateTime))]
        [DefaultSettingValue("Jan 1, 2006")]
        [SettingsDescription("Ref Data Request Start Limit")]
        public DateTime DataRequestStartLimit
        {
            get
            {
                return (DateTime)this["DataRequestStartLimit"];
            }
            set
            {
                this["DataRequestStartLimit"] = (DateTime)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(TimeSpan))]
        [DefaultSettingValue("03:05:00")]
        [SettingsDescription("Daily Security Update Time")]
        public TimeSpan DailySecurityUpdateTime
        {
            get
            {
                return (TimeSpan)this["DailySecurityUpdateTime"];
            }
            set
            {
                this["DailySecurityUpdateTime"] = (TimeSpan)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(TimeSpan))]
        [DefaultSettingValue("04:30:00")]
        [SettingsDescription("Daily Index Update Time")]
        public TimeSpan DailyIndexUpdateTime
        {
            get
            {
                return (TimeSpan)this["DailyIndexUpdateTime"];
            }
            set
            {
                this["DailyIndexUpdateTime"] = (TimeSpan)value;
                Save();
            }
        }

        #region Interactive Brokers

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(int))]
        [DefaultSettingValue("4002")]
        [SettingsDescription("Data Provider Connection Port")]
        [DisplaySettingCondition("RefDataProvider", DataProviderType.InteractiveBrokers)]
        public int DataProviderPort
        {
            get
            {
                return (int)this["DataProviderPort"];
            }
            set
            {
                this["DataProviderPort"] = (int)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(int))]
        [DefaultSettingValue("3")]
        [SettingsDescription("Data Provider Connection Timeout (Sec)")]
        [DisplaySettingCondition("RefDataProvider", DataProviderType.InteractiveBrokers)]
        public int DataProviderTimeoutSeconds
        {
            get
            {
                return (int)this["DataProviderTimeoutSeconds"];
            }
            set
            {
                this["DataProviderTimeoutSeconds"] = (int)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(TimeSpan))]
        [DefaultSettingValue("04:00:00")]
        [SettingsDescription("Daily Gateway Reconnect")]
        [DisplaySettingCondition("RefDataProvider", DataProviderType.InteractiveBrokers)]
        public TimeSpan DailyGatewayReconnect
        {
            get
            {
                return (TimeSpan)this["DailyGatewayReconnect"];
            }
            set
            {
                this["DailyGatewayReconnect"] = (TimeSpan)value;
                Save();
            }
        }

        #endregion

        #region IEX Cloud

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(IexCloudMode))]
        [DefaultSettingValue("Sandbox")]
        [SettingsDescription("IEX Cloud Mode")]
        [DisplaySettingCondition("RefDataProvider", DataProviderType.IEXCloud)]
        public IexCloudMode IexCloudMode
        {
            get
            {
                return (IexCloudMode)this["IexCloudMode"];
            }
            set
            {
                this["IexCloudMode"] = (IexCloudMode)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(string))]
        [DefaultSettingValue("pk_77f0c23e726943808ffcf4fdd11b6eae")]
        [SettingsDescription("Publishable Token - Production")]
        [DisplaySettingCondition("RefDataProvider", DataProviderType.IEXCloud)]
        public string IexPublishableTokenProduction
        {
            get
            {
                return (string)this["IexPublishableTokenProduction"];
            }
            set
            {
                this["IexPublishableTokenProduction"] = (string)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(string))]
        [DefaultSettingValue("")]
        [SettingsDescription("Secret Token - Production")]
        [DisplaySettingCondition("RefDataProvider", DataProviderType.IEXCloud)]
        public string IexSecretTokenProduction
        {
            get
            {
                return (string)this["IexSecretTokenProduction"];
            }
            set
            {
                this["IexSecretTokenProduction"] = (string)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(string))]
        [DefaultSettingValue("Tpk_8274575b54664423b1d766ce3d168b7e")]
        [SettingsDescription("Publishable Token - Sandbox ")]
        [DisplaySettingCondition("RefDataProvider", DataProviderType.IEXCloud)]
        public string IexPublishableTokenSandbox
        {
            get
            {
                return (string)this["IexPublishableTokenSandbox"];
            }
            set
            {
                this["IexPublishableTokenSandbox"] = (string)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(string))]
        [DefaultSettingValue("")]
        [SettingsDescription("Secret Token - Sandbox")]
        [DisplaySettingCondition("RefDataProvider", DataProviderType.IEXCloud)]
        public string IexSecretTokenSandbox
        {
            get
            {
                return (string)this["IexSecretTokenSandbox"];
            }
            set
            {
                this["IexSecretTokenSandbox"] = (string)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(int))]
        [DefaultSettingValue("0")]
        [SettingsDescription("IEX Message Count")]
        [DisplaySettingCondition("RefDataProvider", DataProviderType.IEXCloud)]
        public int IexMessageCount
        {
            get
            {
                // Roll the month over and reset the counter to 0 if this is the first time we have accessed in the new month

                if (IexCloudMode == IexCloudMode.Production &&
                            IexMessageCountMonth.MonthAndYear() != DateTime.Today.MonthAndYear())
                {
                    IexMessageCountMonth = DateTime.Today.MonthAndYear();
                    this["IexMessageCount"] = (int)0;
                }
                return (int)this["IexMessageCount"];

            }
            set
            {
                lock (this)
                {
                    if (IexCloudMode != IexCloudMode.Production)
                        return;
                    this["IexMessageCount"] = (int)value;
                    Save();
                }
            }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("Feb 1, 2020")]
        [SettingsDescription("IEX Message Count Month")]
        public DateTime IexMessageCountMonth
        {
            get
            {
                return (DateTime)this["IexMessageCountMonth"];
            }
            set
            {
                this["IexMessageCountMonth"] = (DateTime)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("5000000")]
        [SettingsDescription("IEX Message Count Limit")]
        public int IexMessageCountLimit
        {
            get
            {
                return (int)this["IexMessageCountLimit"];
            }
            set
            {
                this["IexMessageCountLimit"] = (int)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(int))]
        [DefaultSettingValue("100")]
        [SettingsDescription("IEX Batch Symbol Limit (100 Max)")]
        [DisplaySettingCondition("RefDataProvider", DataProviderType.IEXCloud)]
        public int IexBatchSymbolLimit
        {
            get
            {
                return (int)this["IexBatchSymbolLimit"];

            }
            set
            {
                this["IexBatchSymbolLimit"] = (int)value;
                Save();
            }
        }


        #endregion

        #endregion
        #region Live Trading

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.LiveTrading, typeof(bool))]
        [DefaultSettingValue("False")]
        [SettingsDescription("Live Trading Enabled")]
        public bool EnableLiveTrading
        {
            get
            {
                return (bool)this["EnableLiveTrading"];
            }
            set
            {
                this["EnableLiveTrading"] = (bool)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.LiveTrading, typeof(TradingProviderType))]
        [DefaultSettingValue("InteractiveBrokers")]
        [SettingsDescription("Trading Account Provider")]
        public TradingProviderType TradingProvider
        {
            get
            {
                return (TradingProviderType)this["TradingProvider"];
            }
            set
            {
                this["TradingProvider"] = (TradingProviderType)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.LiveTrading, typeof(int))]
        [DefaultSettingValue("4002")]
        [SettingsDescription("Trading Provider Connection Port")]
        [DisplaySettingCondition("TradingProvider", TradingProviderType.InteractiveBrokers)]
        public int IbkrTradingProviderPort
        {
            get
            {
                return (int)this["IbkrTradingProviderPort"];
            }
            set
            {
                this["IbkrTradingProviderPort"] = (int)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.LiveTrading, typeof(DataProviderType))]
        [DefaultSettingValue("InteractiveBrokers")]
        [SettingsDescription("Live Data Provider")]
        public DataProviderType LiveDataProvider
        {
            get
            {
                return (DataProviderType)this["LiveDataProvider"];
            }
            set
            {
                this["LiveDataProvider"] = (DataProviderType)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.LiveTrading, typeof(int))]
        [DefaultSettingValue("4002")]
        [SettingsDescription("Live Data Provider Connection Port")]
        [DisplaySettingCondition("LiveDataProvider", DataProviderType.InteractiveBrokers)]
        public int IbkrLiveDataPort
        {
            get
            {
                return (int)this["IbkrLiveDataPort"];
            }
            set
            {
                this["IbkrLiveDataPort"] = (int)value;
                Save();
            }
        }

        #endregion
        #region Database Settings

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(string))]
        [DefaultSettingValue(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Finance.Data.PriceDatabaseContext;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False")]
        [SettingsDescription("Database Connection String ** BE CAREFUL **")]
        public string DatabaseConnectionString
        {
            get
            {
                return (string)this["DatabaseConnectionString"];
            }
            set
            {
                this["DatabaseConnectionString"] = (string)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(bool))]
        [DefaultSettingValue("true")]
        [SettingsDescription("Auto Load Securities on Start")]
        public bool AutoLoadSecuritiesOnStart
        {
            get
            {
                return (bool)this["AutoLoadSecuritiesOnStart"];
            }
            set
            {
                this["AutoLoadSecuritiesOnStart"] = (bool)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.ReferenceData, typeof(bool))]
        [DefaultSettingValue("true")]
        [SettingsDescription("Auto-delete Invalid Securities (per Provider)")]
        public bool AutoDeleteInvalidSecurities
        {
            get
            {
                return (bool)this["AutoDeleteInvalidSecurities"];
            }
            set
            {
                this["AutoDeleteInvalidSecurities"] = (bool)value;
                Save();
            }
        }

        #endregion
        #region Logging Settings

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Application, typeof(Directory))]
        [SettingsDescription("Log Output Directory")]
        public string LogOutputDirectoryPath
        {
            get
            {
                return (string)this["LogOutputDirectoryPath"] ?? System.Environment.CurrentDirectory;

                //var ret = (string)this["LogOutputDirectoryPath"];
                //if (ret == null)
                //    return System.Environment.CurrentDirectory;
            }
            set
            {
                this["LogOutputDirectoryPath"] = (string)value;
                Save();
            }
        }

        #endregion
        #region Environment Settings

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Trading, typeof(TradingEnvironmentType))]
        [DefaultSettingValue("InteractiveBrokersApi")]
        [SettingsDescription("Trading Environment")]
        public TradingEnvironmentType TradingEnvironment
        {
            get
            {
                return (TradingEnvironmentType)this["TradingEnvironment"];
            }
            set
            {
                this["TradingEnvironment"] = (TradingEnvironmentType)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Trading, typeof(SecurityGroupName))]
        [DefaultSettingValue("All")]
        [SettingsDescription("Active Securities")]
        public SecurityGroupName SecurityGroup
        {
            get
            {
                return (SecurityGroupName)this["SecurityGroup"];
            }
            set
            {
                this["SecurityGroup"] = (SecurityGroupName)value;
                Save();
            }
        }

        #endregion
        #region Testing Defaults

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Testing, typeof(Currency))]
        [DefaultSettingValue("10000.00")]
        [SettingsDescription("Portfolio Starting Balance")]
        public decimal PortfolioStartingBalance
        {
            get
            {
                return (decimal)this["PortfolioStartingBalance"];
            }
            set
            {
                this["PortfolioStartingBalance"] = (decimal)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Testing, typeof(PortfolioDirection))]
        [DefaultSettingValue("LongShort")]
        [SettingsDescription("Portfolio Direction")]
        public PortfolioDirection PortfolioDirection
        {
            get
            {
                return (PortfolioDirection)this["PortfolioDirection"];
            }
            set
            {
                this["PortfolioDirection"] = (PortfolioDirection)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Testing, typeof(PortfolioMarginType))]
        [DefaultSettingValue("RegTMargin")]
        [SettingsDescription("Portfolio Margin Type")]
        public PortfolioMarginType PortfolioMarginType
        {
            get
            {
                return (PortfolioMarginType)this["PortfolioMarginType"];
            }
            set
            {
                this["PortfolioMarginType"] = (PortfolioMarginType)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Testing, typeof(DateTime))]
        [DefaultSettingValue("Jan 1, 2010")]
        [SettingsDescription("Default Simulation Start Date")]
        public DateTime DefaultSimulationStartDate
        {
            get
            {
                var ret = (DateTime)this["DefaultSimulationStartDate"];
                if (!Calendar.IsTradingDay(ret))
                    ret = Calendar.NextTradingDay(ret);

                return ret;
            }
            set
            {
                this["DefaultSimulationStartDate"] = (DateTime)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Testing, typeof(int))]
        [DefaultSettingValue("12")]
        [SettingsDescription("Default Simulation Length (Months)")]
        public int DefaultSimulationLengthMonths
        {
            get
            {
                return (int)this["DefaultSimulationLengthMonths"];
            }
            set
            {
                this["DefaultSimulationLengthMonths"] = (int)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Testing, typeof(bool))]
        [DefaultSettingValue("true")]
        [SettingsDescription("Testing Mode Limit Enabled")]
        public bool TestingModeSecurityEnabled
        {
            get
            {
                return (bool)this["TestingModeSecurityEnabled"];
            }
            set
            {
                this["TestingModeSecurityEnabled"] = (bool)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Testing, typeof(int))]
        [DefaultSettingValue("25")]
        [SettingsDescription("Testing Mode Security Count")]
        public int TestingModeSecurityCount
        {
            get
            {
                return (int)this["TestingModeSecurityCount"];
            }
            set
            {
                this["TestingModeSecurityCount"] = (int)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Testing, typeof(int))]
        [DefaultSettingValue("6")]
        [SettingsDescription("Default Swingpoint Bar Count")]
        public int DefaultSwingpointBarCount
        {
            get
            {
                return (int)this["DefaultSwingpointBarCount"];
            }
            set
            {
                this["DefaultSwingpointBarCount"] = (int)value;
                Save();
            }
        }

        #endregion
        #region Layout Settings

        [UserScopedSetting()]
        [DefaultSettingValue("Central Standard Time")]
        public string WorldClockZones
        {
            get
            {
                return (string)this["WorldClockZones"];
            }
            set
            {
                this["WorldClockZones"] = (string)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("")]
        public string _layoutConfigString
        {
            get
            {
                return (string)this["_layoutConfigString"];
            }
            set
            {
                this["_layoutConfigString"] = (string)value;
                Save();
            }
        }

        private struct FormLayoutConfig : IEquatable<FormLayoutConfig>
        {
            public string Name;
            public Point Location;
            public Size Size;

            public FormLayoutConfig(IPersistLayout form)
            {
                this.Name = form.Name;
                this.Location = form.Location;
                this.Size = form.Size;
            }
            public FormLayoutConfig(string name, int x, int y, int h, int w)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Location = new Point(x, y);
                Size = new Size(w, h);
            }

            public override bool Equals(object obj)
            {
                return obj is FormLayoutConfig config && Equals(config);
            }
            public bool Equals(FormLayoutConfig other)
            {
                return Name == other.Name;
            }
            public override string ToString()
            {
                return string.Format($"{Name},{Location.X},{Location.Y},{Size.Height},{Size.Width}");
            }
            public override int GetHashCode()
            {
                return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
            }
            public static bool operator ==(FormLayoutConfig left, FormLayoutConfig right)
            {
                return left.Equals(right);
            }
            public static bool operator !=(FormLayoutConfig left, FormLayoutConfig right)
            {
                return !(left == right);
            }
        }
        private List<FormLayoutConfig> FormLayoutConfigs
        {
            get
            {
                var ret = new List<FormLayoutConfig>();
                var configStrings = _layoutConfigString.Split('|').ToList();
                foreach (var str in configStrings)
                {
                    if (str.Length == 0)
                        continue;

                    var s = str.Split(',');
                    ret.Add(new FormLayoutConfig(
                        s[0],
                        Int32.Parse(s[1]),
                        Int32.Parse(s[2]),
                        Int32.Parse(s[3]),
                        Int32.Parse(s[4])));
                }

                return ret;
            }
            set
            {
                List<string> values = new List<string>();
                foreach (var config in value)
                    values.Add(config.ToString());

                _layoutConfigString = string.Join("|", values.ToArray());
            }
        }
        private string layoutConfigString(IPersistLayout form)
        {
            return String.Format($"{form.Name},{form.Location.X},{form.Location.Y},{form.Size.Width},{form.Size.Height}");
        }

        public void SaveFormLayout(IPersistLayout form)
        {
            if (form.Name == "")
                throw new UnknownErrorException() { message = $"Form must have name {nameof(form)}" };

            var newConfig = new FormLayoutConfig(form);
            var currentConfig = FormLayoutConfigs;
            currentConfig.RemoveAll(x => x == newConfig);
            currentConfig.Add(newConfig);
            FormLayoutConfigs = currentConfig;
        }
        public void LoadFormLayout(IPersistLayout form)
        {
            form.StartPosition = FormStartPosition.Manual;

            var currentConfig = FormLayoutConfigs;
            if (currentConfig.Exists(x => x.Name == form.Name))
            {
                var config = currentConfig.Find(x => x.Name == form.Name);
                form.Location = config.Location;

                if (form.Sizeable)
                    form.Size = config.Size;
            }
        }

        #endregion
        #region Global Styles

        [UserScopedSetting()]
        [DefaultSettingValue("SystemColors.Control")]
        private Color DefaultColor
        {
            get
            {
                return (Color)this["DefaultColor"];
            }
            set
            {
                this["DefaultColor"] = (Color)value;
            }
        }

        #region Trend Identification

        public Color GetColorByTrend(TrendQualification trendType)
        {
            switch (trendType)
            {
                case TrendQualification.NotSet:
                    return DefaultColor;
                case TrendQualification.AmbivalentSideways:
                    return ColorAmbivalentSidewaysTrend;
                case TrendQualification.SuspectSideways:
                    return ColorSuspectSidewaysTrend;
                case TrendQualification.ConfirmedSideways:
                    return ColorConfirmedSidewaysTrend;
                case TrendQualification.SuspectBullish:
                    return ColorSuspectBullishTrend;
                case TrendQualification.ConfirmedBullish:
                    return ColorConfirmedBullishTrend;
                case TrendQualification.SuspectBearish:
                    return ColorSuspectBearishTrend;
                case TrendQualification.ConfirmedBearish:
                    return ColorConfirmedBearishTrend;
                default:
                    return DefaultColor;
            }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("-8355840")]
        [SettingsCategory(SettingsType.Style, typeof(Color))]
        [SettingsDescription("Ambivalent Sideways Trend")]
        public Color ColorAmbivalentSidewaysTrend
        {
            get
            {
                return (Color)this["ColorAmbivalentSidewaysTrend"];
            }
            set
            {
                this["ColorAmbivalentSidewaysTrend"] = (Color)value;
                Save();
            }
        }
        [UserScopedSetting()]
        [DefaultSettingValue("-8355840")]
        [SettingsCategory(SettingsType.Style, typeof(Color))]
        [SettingsDescription("Suspect Sideways Trend")]
        public Color ColorSuspectSidewaysTrend
        {
            get
            {
                return (Color)this["ColorSuspectSidewaysTrend"];
            }
            set
            {
                this["ColorSuspectSidewaysTrend"] = (Color)value;
                Save();
            }
        }
        [UserScopedSetting()]
        [DefaultSettingValue("-8355840")]
        [SettingsCategory(SettingsType.Style, typeof(Color))]
        [SettingsDescription("Confirmed Sideways Trend")]
        public Color ColorConfirmedSidewaysTrend
        {
            get
            {
                return (Color)this["ColorConfirmedSidewaysTrend"];
            }
            set
            {
                this["ColorConfirmedSidewaysTrend"] = (Color)value;
                Save();
            }
        }
        [UserScopedSetting()]
        [DefaultSettingValue("-8355840")]
        [SettingsCategory(SettingsType.Style, typeof(Color))]
        [SettingsDescription("Suspect Bearish Trend")]
        public Color ColorSuspectBearishTrend
        {
            get
            {
                return (Color)this["ColorSuspectBearishTrend"];
            }
            set
            {
                this["ColorSuspectBearishTrend"] = (Color)value;
                Save();
            }
        }
        [UserScopedSetting()]
        [DefaultSettingValue("-8355840")]
        [SettingsCategory(SettingsType.Style, typeof(Color))]
        [SettingsDescription("Confirmed Bearish Trend")]
        public Color ColorConfirmedBearishTrend
        {
            get
            {
                return (Color)this["ColorConfirmedBearishTrend"];
            }
            set
            {
                this["ColorConfirmedBearishTrend"] = (Color)value;
                Save();
            }
        }
        [UserScopedSetting()]
        [DefaultSettingValue("-8355840")]
        [SettingsCategory(SettingsType.Style, typeof(Color))]
        [SettingsDescription("Suspect Bulish Trend")]
        public Color ColorSuspectBullishTrend
        {
            get
            {
                return (Color)this["ColorSuspectBullishTrend"];
            }
            set
            {
                this["ColorSuspectBullishTrend"] = (Color)value;
                Save();
            }
        }
        [UserScopedSetting()]
        [DefaultSettingValue("-8355840")]
        [SettingsCategory(SettingsType.Style, typeof(Color))]
        [SettingsDescription("Confirmed Bullish Trend")]
        public Color ColorConfirmedBullishTrend
        {
            get
            {
                return (Color)this["ColorConfirmedBullishTrend"];
            }
            set
            {
                this["ColorConfirmedBullishTrend"] = (Color)value;
                Save();
            }
        }

        [SettingsCategory(SettingsType.Style, typeof(Button))]
        [SettingsDescription("Reset Default Colors")]
        public bool ResetDefaultTrendColors
        {
            set
            {
                if (value)
                    _ResetDefaultTrendColors();

                OnPropertyChanged(this, new PropertyChangedEventArgs("ResetDefaultTrendColors"));
            }
        }

        private void _ResetDefaultTrendColors()
        {
            ColorAmbivalentSidewaysTrend = Color.FromArgb(64, Color.FromArgb(Int32.Parse(this.GetType().GetProperty("ColorConfirmedBullishTrend").GetCustomAttribute<DefaultSettingValueAttribute>().Value)));
            ColorSuspectSidewaysTrend = Color.FromArgb(64, Color.FromArgb(Int32.Parse(this.GetType().GetProperty("ColorSuspectSidewaysTrend").GetCustomAttribute<DefaultSettingValueAttribute>().Value)));
            ColorConfirmedSidewaysTrend = Color.FromArgb(64, Color.FromArgb(Int32.Parse(this.GetType().GetProperty("ColorConfirmedSidewaysTrend").GetCustomAttribute<DefaultSettingValueAttribute>().Value)));
            ColorSuspectBullishTrend = Color.FromArgb(64, Color.FromArgb(Int32.Parse(this.GetType().GetProperty("ColorSuspectBullishTrend").GetCustomAttribute<DefaultSettingValueAttribute>().Value)));
            ColorConfirmedBullishTrend = Color.FromArgb(128, Color.FromArgb(Int32.Parse(this.GetType().GetProperty("ColorConfirmedBullishTrend").GetCustomAttribute<DefaultSettingValueAttribute>().Value)));
            ColorSuspectBearishTrend = Color.FromArgb(64, Color.FromArgb(Int32.Parse(this.GetType().GetProperty("ColorSuspectBearishTrend").GetCustomAttribute<DefaultSettingValueAttribute>().Value)));
            ColorConfirmedBearishTrend = Color.FromArgb(128, Color.FromArgb(Int32.Parse(this.GetType().GetProperty("ColorConfirmedBearishTrend").GetCustomAttribute<DefaultSettingValueAttribute>().Value)));
        }

        #endregion

        #region Charting

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.Style, typeof(PriceBarSize))]
        [DefaultSettingValue("Daily")]
        [SettingsDescription("Default Chart View Bar Size")]
        public PriceBarSize DefaultChartViewBarSize
        {
            get
            {
                return (PriceBarSize)this["DefaultChartViewBarSize"];
            }
            set
            {
                this["DefaultChartViewBarSize"] = (PriceBarSize)value;
                Save();
            }
        }

        #endregion

        #endregion
        #region Custom Index Settings

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.IndexManagementParameters, typeof(int))]
        [DefaultSettingValue("100000")]
        [SettingsDescription("Minimum 30 D Avg. Volume for Index Inclusion")]
        public long Minimum_Index_Inclusion_Volume_30d
        {
            get
            {
                return (long)this["Minimum_Index_Inclusion_Volume_30d"];
            }
            set
            {
                this["Minimum_Index_Inclusion_Volume_30d"] = (long)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.IndexManagementParameters, typeof(Currency))]
        [DefaultSettingValue("10.00")]
        [SettingsDescription("Minimum price for Index Inclusion")]
        public decimal Minimum_Index_Inclusion_Price
        {
            get
            {
                return (decimal)this["Minimum_Index_Inclusion_Price"];
            }
            set
            {
                this["Minimum_Index_Inclusion_Price"] = (decimal)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.IndexManagementParameters, typeof(PriceBarSize))]
        [DefaultSettingValue("Daily")]
        [SettingsDescription("Default Sector Trend Bar Size")]
        public PriceBarSize Sector_Trend_Bar_Size
        {
            get
            {
                return (PriceBarSize)this["Sector_Trend_Bar_Size"];
            }
            set
            {
                this["Sector_Trend_Bar_Size"] = (PriceBarSize)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.IndexManagementParameters, typeof(bool))]
        [DefaultSettingValue("false")]
        [SettingsDescription("Include ETFs in Index Calculation")]
        public bool IncludeEtfsInIndex
        {
            get
            {
                return (bool)this["IncludeEtfsInIndex"];
            }
            set
            {
                this["IncludeEtfsInIndex"] = (bool)value;
                Save();
            }
        }

        [UserScopedSetting()]
        [SettingsCategory(SettingsType.IndexManagementParameters, typeof(int))]
        [DefaultSettingValue("6")]
        [SettingsDescription("Default Sector Trend Swingpoint Bar Count")]
        public int Sector_Trend_Bar_Count
        {
            get
            {
                return (int)this["Sector_Trend_Bar_Count"];
            }
            set
            {
                this["Sector_Trend_Bar_Count"] = (int)value;
                Save();
            }
        }

        #endregion

    }

}
