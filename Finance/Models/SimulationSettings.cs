using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Configuration;
using Finance.TradeStrategies;
using Finance.PositioningStrategies;
using System.Drawing;
using static Finance.Helpers;

namespace Finance
{
    public class SimulationSettings
    {
        public Simulation Simulation { get; set; }

        public SimulationSettings(Simulation simulation)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        }

        public (object Source, List<PropertyInfo> Properties) GetSettingsByCategoryTag(SettingsType settingsType)
        {
            var ret = new List<PropertyInfo>();

            object SettingsSource = null;
            switch (settingsType)
            {
                case SettingsType.SimulationParameters:
                    SettingsSource = this;
                    break;
                case SettingsType.SecurityFilterParameters:
                    SettingsSource = RiskManager;
                    break;
                case SettingsType.PortfolioParameters:
                    SettingsSource = PortfolioSetup;
                    break;
                case SettingsType.RiskParameters:
                    SettingsSource = RiskManager;
                    break;
                case SettingsType.StrategyParameters:
                    SettingsSource = ActiveStrategy;
                    break;
                case SettingsType.PositionManagementParameters:
                    SettingsSource = ActivePositioningStrategy;
                    break;
                default:
                    return (null, null);
            }

            foreach (PropertyInfo property in SettingsSource.GetType().GetProperties())
            {
                if (Attribute.IsDefined(property, typeof(SettingsCategoryAttribute)) &&
                    (property.GetCustomAttribute<SettingsCategoryAttribute>().SettingsType == settingsType))
                {
                    ret.Add(property);
                }
            }

            return (SettingsSource, ret);
        }

        #region Simulation General Parameters

        [SettingsCategory(SettingsType.SimulationParameters, typeof(DateTime))]
        [SettingsDescription("Simulation Start Date")]
        public DateTime SimulationStartDate { get; set; } = Settings.Instance.DefaultSimulationStartDate;

        [SettingsCategory(SettingsType.SimulationParameters, typeof(DateTime))]
        [SettingsDescription("Simulation End Date")]
        public DateTime SimulationEndDate { get; set; } = Calendar.PriorTradingDay(Settings.Instance.DefaultSimulationStartDate.AddMonths(Settings.Instance.DefaultSimulationLengthMonths));

        [SettingsDescription("Simulation User Notes")]
        [SettingsCategory(SettingsType.SimulationParameters, typeof(LongText))]
        public string SimulationNotes
        {
            get => Simulation.SimulationNotes;
            set => Simulation.SimulationNotes = value;
        }

        #endregion

        #region Portfolio Setup

        public PortfolioSetup PortfolioSetup => Simulation.PortfolioSetup;

        #endregion

        #region Strategy Parameters

        public TradeStrategyBase ActiveStrategy => Simulation.StrategyManager.ActiveTradeStrategy;
        public PositioningStrategyBase ActivePositioningStrategy => RiskManager.ActivePositioningStrategy;

        #endregion

        #region Risk Parameters

        public RiskManager RiskManager => Simulation.RiskManager;

        #endregion

        #region Security Filters



        #endregion

        public SimulationSettings Copy()
        {
            var ret = this.MemberwiseClone() as SimulationSettings;
            return ret;
        }

    }

    public class SimulationSettingsManagerPanel : Panel
    {

        public SimulationSettings SimSettings { get; private set; }

        TabControl tabSettingsGroup;
        TabPage pageSimulationGeneral;
        TabPage pagePositionMgmt;
        TabPage pageStrategyParameters;
        TabPage pagePortfolioParameters;
        TabPage pageSecurityFilter;
        TabPage pageRiskParameters;

        public void LoadSettings(SimulationSettings settings)
        {
            this.SimSettings = settings;

            if (SimSettings == null)
                return;

            this.Controls.Clear();
            this.InitializeMe();
        }

        public void Lock(bool locked)
        {
            Invoke(new Action(() =>
            {
                foreach (TabPage page in tabSettingsGroup.TabPages)
                {
                    foreach (Control ctrl in page.Controls)
                        ctrl.Enabled = !locked;
                }
            }));
        }

        [Initializer]
        private void InitializeTabControl()
        {
            tabSettingsGroup = new TabControl()
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.Normal
            };
            this.Controls.Add(tabSettingsGroup);
        }
        [Initializer]
        private void InitializeSimulationGeneralPage()
        {
            pageSimulationGeneral = new TabPage("Simulation");
            pageSimulationGeneral.Controls.Add(CreateSettingsTabPagePanel(SettingsType.SimulationParameters));
            tabSettingsGroup.TabPages.Add(pageSimulationGeneral);
        }
        [Initializer]
        private void InitializePortfolioParameters()
        {
            pagePortfolioParameters = new TabPage("Portfolio");
            pagePortfolioParameters.Controls.Add(CreateSettingsTabPagePanel(SettingsType.PortfolioParameters));
            tabSettingsGroup.TabPages.Add(pagePortfolioParameters);
        }
        [Initializer]
        private void InitializeStrategyParameters()
        {
            pageStrategyParameters = new TabPage("Strategy");
            pageStrategyParameters.Controls.Add(CreateSettingsTabPagePanel(SettingsType.StrategyParameters));
            tabSettingsGroup.TabPages.Add(pageStrategyParameters);

            SimSettings.Simulation.StrategyManager.StrategyChanged += (s, e) =>
            {
                Invoke(new Action(() =>
                {
                    SuspendLayout();
                    pageStrategyParameters.Controls.Clear();
                    pageStrategyParameters.Controls.Add(CreateSettingsTabPagePanel(SettingsType.StrategyParameters));
                    ResumeLayout();
                }));
            };
        }
        [Initializer]
        private void InitializeRiskParameters()
        {
            pageRiskParameters = new TabPage("Risk");
            pageRiskParameters.Controls.Add(CreateSettingsTabPagePanel(SettingsType.RiskParameters));
            tabSettingsGroup.TabPages.Add(pageRiskParameters);
        }
        [Initializer]
        private void InitializeSizingAndStoplossParameters()
        {
            pagePositionMgmt = new TabPage("Posn. Mgmt");
            pagePositionMgmt.Controls.Add(CreateSettingsTabPagePanel(SettingsType.PositionManagementParameters));
            tabSettingsGroup.TabPages.Add(pagePositionMgmt);

            SimSettings.RiskManager.PositionSizingStrategyChanged += (s, e) =>
            {
                Invoke(new Action(() =>
                {
                    SuspendLayout();
                    pagePositionMgmt.Controls.Clear();
                    pagePositionMgmt.Controls.Add(CreateSettingsTabPagePanel(SettingsType.PositionManagementParameters));
                    ResumeLayout();
                }));
            };
        }
        [Initializer]
        private void InitializeSecurityFilterParameters()
        {
            pageSecurityFilter = new TabPage("Sec Filter");
            pageSecurityFilter.Controls.Add(CreateSettingsTabPagePanel(SettingsType.SecurityFilterParameters));
            tabSettingsGroup.TabPages.Add(pageSecurityFilter);
        }

        /// <summary>
        /// Populates a Panel with all available settings for a given SettingsType
        /// </summary>
        /// <param name="name"></param>
        /// <param name="settingsType"></param>
        /// <returns></returns>
        private Panel CreateSettingsTabPagePanel(SettingsType settingsType)
        {
            FlowLayoutPanel pnlDisplay = new FlowLayoutPanel()
            {
                Size = tabSettingsGroup.ClientRectangle.Size,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true
            };

            var settings = SimSettings.GetSettingsByCategoryTag(settingsType);

            foreach (var setting in settings.Properties)
            {
                // If the setting has a display condition attached and it is currently marked as NotDisplayed, skip
                if (Attribute.IsDefined(setting, typeof(DisplaySettingConditionAttribute)) &&
                    !setting.GetCustomAttribute<DisplaySettingConditionAttribute>().Display)
                    continue;

                var ctrl = PropertyControl(setting, settings.Source);
                ctrl.Width = pnlDisplay.ClientRectangle.Width;
                pnlDisplay.Controls.Add(ctrl);
            }

            return pnlDisplay;
        }

        /// <summary>
        /// Returns an appropriate control for updating a property
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private Panel PropertyControl(PropertyInfo property, object source)
        {
            Size _defaultPanelSize = new Size(500, 25);

            TableLayoutPanel ret = new TableLayoutPanel()
            {
                Size = _defaultPanelSize,
                ColumnCount = 2,
                RowCount = 1
            };
            Label lblName = new Label()
            {
                Text = property.GetCustomAttribute<SettingsDescriptionAttribute>().Description,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };
            ret.Controls.Add(lblName, 0, 0);

            var propertyType = property.GetCustomAttribute<SettingsCategoryAttribute>().SettingsControlType;

            //
            // Date value - DateTimePicker
            //
            if (propertyType == typeof(DateTime))
                ret.Controls.Add(PropertyControlDateTime(property, ret, source), 1, 0);

            //
            // LongText value - Large text box
            //
            if (propertyType == typeof(LongText))
            {
                ret.Controls.Add(PropertyControlLongText(property, ret, source), 1, 0);
                ret.Height = 75;
            }

            //
            // Int value
            //
            if (propertyType == typeof(int))
                ret.Controls.Add(PropertyControlInt(property, ret, source), 1, 0);

            //
            // Enum value
            //
            if (propertyType.IsEnum)
                ret.Controls.Add(PropertyControlEnum(property, ret, source), 1, 0);

            //
            // Bool value
            //
            if (propertyType == typeof(bool))
                ret.Controls.Add(PropertyControlBool(property, ret, source), 1, 0);

            //
            // Decimal value
            //
            if (propertyType == typeof(decimal))
                ret.Controls.Add(PropertyControlDecimal(property, ret, source), 1, 0);

            //
            // Currency value
            //
            if (propertyType == typeof(Currency))
                ret.Controls.Add(PropertyControlCurrency(property, ret, source), 1, 0);

            //
            // Currency value
            //
            if (propertyType == typeof(OptionList))
            {
                ret.Controls.Add(PropertyControlListStringSelectBox(property, ret, source), 1, 0);
                ret.Height = 125;
            }

            return ret;
        }

        private DateTimePicker PropertyControlDateTime(PropertyInfo property, TableLayoutPanel panel, object source)
        {
            Size _defaultDateTimePickerSize = new Size(100, 20);

            //
            // Create Time Picker
            //
            var ctrl = new DateTimePicker()
            {
                Size = _defaultDateTimePickerSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2),
                Format = DateTimePickerFormat.Short
            };

            ctrl.Value = DateTime.Parse(property.GetValue(source).ToString());

            //
            // Highlight effect on change showing that the setting was updated
            //
            Timer highlightTimer = new Timer() { Interval = 2000 };
            highlightTimer.Tick += (s, e) =>
            {
                panel.BackColor = Panel.DefaultBackColor;
                highlightTimer.Stop();
            };

            //
            // Show a Directory chooser when the user clicks the box
            //
            ctrl.ValueChanged += (s, e) =>
            {
                property.SetValue(SimSettings, ctrl.Value);
                panel.BackColor = Color.LawnGreen;
                highlightTimer.Start();
            };

            return ctrl;
        }
        private TextBox PropertyControlLongText(PropertyInfo property, TableLayoutPanel panel, object source)
        {
            Size _defaultStringTextBoxSize = new Size(200, 40);

            //
            // Create TextBox
            //
            var ctrl = new TextBox()
            {
                Size = _defaultStringTextBoxSize,
                Multiline = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2),
                TextAlign = HorizontalAlignment.Left
            };

            ctrl.Text = property.GetValue(source).ToString();

            //
            // Highlight effect on change showing that the setting was updated
            //
            Timer highlightTimer = new Timer() { Interval = 2000 };
            highlightTimer.Tick += (s, e) =>
            {
                panel.BackColor = Panel.DefaultBackColor;
                highlightTimer.Stop();
            };

            //
            // Set the new value in Settings
            //
            bool valueChanged = false;
            ctrl.TextChanged += (s, e) => valueChanged = true;
            ctrl.KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    panel.Focus();
            };
            ctrl.Leave += (s, e) =>
            {
                if (!valueChanged)
                    return;

                // Update settings value
                property.SetValue(SimSettings, ctrl.Text);
                valueChanged = false;
                panel.BackColor = Color.LawnGreen;
                panel.Focus();

                highlightTimer.Start();
            };

            return ctrl;
        }
        private ComboBox PropertyControlEnum(PropertyInfo property, TableLayoutPanel panel, object source)
        {
            Size _defaultComboBoxSize = new Size(200, 20);

            //
            // Create ComboBox
            //
            Type enumType = property.GetCustomAttribute<SettingsCategoryAttribute>().SettingsControlType;
            var ctrl = new ComboBox()
            {
                Size = _defaultComboBoxSize,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2)
            };

            //
            // Add Enum names to items, then select current setting value
            //

            foreach (Enum i in Enum.GetValues(enumType))
            {
                var des = i.Description();
                ctrl.Items.Add(des);
            }

            //Enum.GetNames(enumType).ToList().ForEach(x => ctrl.Items.Add(x));

            ctrl.SelectedIndex = Enum.GetNames(enumType).ToList().
                IndexOf(Enum.GetName(enumType, property.GetValue(source)));

            //
            // Highlight effect on change showing that the setting was updated
            //
            Timer highlightTimer = new Timer() { Interval = 2000 };
            highlightTimer.Tick += (s, e) =>
            {
                panel.BackColor = Panel.DefaultBackColor;
                highlightTimer.Stop();
            };

            //
            // Set the new value in Settings
            //
            ctrl.SelectedValueChanged += (s, e) =>
            {
                if (ctrl.SelectedIndex == -1)
                    return;

                string enumString = (ctrl.SelectedItem as string);

                object val = Enum.Parse(enumType, enumString.EnumFromDescription(enumType)?.ToString() ?? enumString);
                property.SetValue(source, val);

                panel.BackColor = Color.LawnGreen;
                highlightTimer.Start();
            };

            return ctrl;

        }
        private TextBox PropertyControlInt(PropertyInfo property, TableLayoutPanel panel, object source)
        {
            Size _defaultIntTextBoxSize = new Size(50, 20);

            //
            // Create TextBox
            //
            var ctrl = new TextBox()
            {
                Size = _defaultIntTextBoxSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2),
                TextAlign = HorizontalAlignment.Right
            };

            ctrl.Text = property.GetValue(source).ToString();

            //
            // Highlight effect on change showing that the setting was updated
            //
            Timer highlightTimer = new Timer() { Interval = 2000 };
            highlightTimer.Tick += (s, e) =>
            {
                panel.BackColor = Panel.DefaultBackColor;
                highlightTimer.Stop();
            };

            //
            // Set the new value in Settings
            //
            bool valueChanged = false;
            ctrl.TextChanged += (s, e) => valueChanged = true;
            ctrl.KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    panel.Focus();
            };
            ctrl.Leave += (s, e) =>
            {
                if (!valueChanged)
                    return;

                if (int.TryParse(ctrl.Text, out int val))
                {
                    // Update settings value
                    property.SetValue(source, val);
                    panel.BackColor = Color.LawnGreen;
                    valueChanged = false;
                    panel.Focus();
                }
                else
                {
                    // Reset text to settings value
                    ctrl.Text = property.GetValue(source).ToString();
                    valueChanged = false;
                    panel.BackColor = Color.PaleVioletRed;
                    ctrl.SelectAll();
                }
                highlightTimer.Start();
            };

            return ctrl;
        }
        private TextBox PropertyControlDecimal(PropertyInfo property, TableLayoutPanel panel, object source)
        {
            Size _defaultIntTextBoxSize = new Size(50, 20);

            //
            // Create TextBox
            //
            var ctrl = new TextBox()
            {
                Size = _defaultIntTextBoxSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2),
                TextAlign = HorizontalAlignment.Right
            };

            ctrl.Text = property.GetValue(source).ToString();

            //
            // Highlight effect on change showing that the setting was updated
            //
            Timer highlightTimer = new Timer() { Interval = 2000 };
            highlightTimer.Tick += (s, e) =>
            {
                panel.BackColor = Panel.DefaultBackColor;
                highlightTimer.Stop();
            };

            //
            // Set the new value in Settings
            //
            bool valueChanged = false;
            ctrl.TextChanged += (s, e) => valueChanged = true;
            ctrl.KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    panel.Focus();
            };
            ctrl.Leave += (s, e) =>
            {
                if (!valueChanged)
                    return;

                if (decimal.TryParse(ctrl.Text, out decimal val))
                {
                    // Update settings value
                    property.SetValue(source, val);
                    panel.BackColor = Color.LawnGreen;
                    valueChanged = false;
                    panel.Focus();
                }
                else
                {
                    // Reset text to settings value
                    ctrl.Text = property.GetValue(source).ToString();
                    valueChanged = false;
                    panel.BackColor = Color.PaleVioletRed;
                    ctrl.SelectAll();
                }
                highlightTimer.Start();
            };

            return ctrl;
        }
        private ComboBox PropertyControlBool(PropertyInfo property, TableLayoutPanel panel, object source)
        {
            Size _defaultComboBoxSize = new Size(75, 20);

            //
            // Create ComboBox
            //            
            var ctrl = new ComboBox()
            {
                Size = _defaultComboBoxSize,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2)
            };
            ctrl.Items.AddRange(new object[] { true, false });

            ctrl.SelectedItem = property.GetValue(source);

            //
            // Highlight effect on change showing that the setting was updated
            //
            Timer highlightTimer = new Timer() { Interval = 2000 };
            highlightTimer.Tick += (s, e) =>
            {
                panel.BackColor = Panel.DefaultBackColor;
                highlightTimer.Stop();
            };

            //
            // Set the new value in Settings
            //
            ctrl.SelectedValueChanged += (s, e) =>
            {
                if (ctrl.SelectedIndex == -1)
                    return;

                property.SetValue(source, (bool)ctrl.SelectedItem);

                panel.BackColor = Color.LawnGreen;
                highlightTimer.Start();
            };

            return ctrl;

        }
        private TextBox PropertyControlCurrency(PropertyInfo property, TableLayoutPanel panel, object source)
        {
            Size _defaultCurrencyTextBoxSize = new Size(100, 20);

            //
            // Create TextBox
            //
            var ctrl = new TextBox()
            {
                Size = _defaultCurrencyTextBoxSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2),
                TextAlign = HorizontalAlignment.Right
            };

            Action<decimal> setText = new Action<decimal>((d) =>
            {
                ctrl.Text = d.ToString("$0.00");
            });

            setText((decimal)property.GetValue(source));

            //
            // Highlight effect on change showing that the setting was updated
            //
            Timer highlightTimer = new Timer() { Interval = 2000 };
            highlightTimer.Tick += (s, e) =>
            {
                panel.BackColor = Panel.DefaultBackColor;
                highlightTimer.Stop();
            };

            //
            // Set the new value in Settings
            //
            bool valueChanged = false;
            ctrl.TextChanged += (s, e) => valueChanged = true;
            ctrl.KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    panel.Focus();
            };
            ctrl.Leave += (s, e) =>
            {
                if (!valueChanged)
                    return;

                if (decimal.TryParse(ctrl.Text, out decimal val))
                {
                    // Update settings value
                    property.SetValue(source, val);
                    panel.BackColor = Color.LawnGreen;
                    setText(val);
                    valueChanged = false;
                    panel.Focus();
                }
                else
                {
                    // Reset text to settings value
                    setText((decimal)property.GetValue(source));
                    valueChanged = false;
                    panel.BackColor = Color.PaleVioletRed;
                    ctrl.SelectAll();
                }
                highlightTimer.Start();
            };

            return ctrl;
        }
        private Panel PropertyControlListStringSelectBox(PropertyInfo property, TableLayoutPanel panel, object source)
        {
            Size _defaultListSelectBoxSize = new Size(150, 100);
            Size _defaultContainerPanelSize = new Size(150, 120);
            Size _defaultButtonSize = new Size(75, 20);

            Panel pnlContainer = new Panel()
            {
                Size = _defaultContainerPanelSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2)
            };
            Button btnCheckAll = new Button()
            {
                Text = "Select All",
                Size = _defaultButtonSize,
                Margin = new Padding(0, 0, 0, 0)
            };
            Button btnUncheckAll = new Button()
            {
                Text = "Unselect All",
                Size = _defaultButtonSize,
                Margin = new Padding(0, 0, 0, 0)
            };

            //
            // Create List Box
            //
            var ctrl = new CheckedListBox()
            {
                Size = _defaultListSelectBoxSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0, 0, 0, 0),
                CheckOnClick = true
            };

            string defaultSource = property.GetCustomAttribute<DefaultSettingValueAttribute>().Value;
            List<string> defaultList = source.GetType().GetProperty(defaultSource).GetValue(source) as List<string>;
            List<string> currentSelectedList = property.GetValue(source) as List<string>;

            foreach (string item in defaultList)
            {
                ctrl.Items.Add(item);
                if (currentSelectedList.Contains(item))
                    ctrl.SetItemChecked(ctrl.Items.Count - 1, true);
            }

            pnlContainer.Controls.AddRange(new Control[] { ctrl, btnCheckAll, btnUncheckAll });

            ctrl.Location = new Point(0, 0);
            btnCheckAll.DockTo(ctrl, ControlEdge.Bottom, 0);
            btnUncheckAll.DockTo(btnCheckAll, ControlEdge.Right, 0);

            //
            // Set the new value in Settings
            //
            ctrl.ItemCheck += (s, e) =>
            {
                string item = Settings.Instance.MarketSectors[e.Index];
                if (e.NewValue == CheckState.Unchecked)
                {
                    var list = (property.GetValue(source) as List<string>);
                    list.Remove(item);
                    property.SetValue(source, list);
                }
                else
                {
                    var list = (property.GetValue(source) as List<string>);
                    list.Add(item);
                    property.SetValue(source, list);
                }
            };
            btnUncheckAll.Click += (s, e) =>
            {
                foreach (int i in ctrl.CheckedIndices)
                {
                    ctrl.SetItemCheckState(i, CheckState.Unchecked);
                }
            };
            btnCheckAll.Click += (s, e) =>
            {
                for (int i = 0; i < ctrl.Items.Count; i++)
                {
                    if (ctrl.GetItemCheckState(i) != CheckState.Checked)
                        ctrl.SetItemCheckState(i, CheckState.Checked);
                }
            };

            return pnlContainer;
        }

    }
}
