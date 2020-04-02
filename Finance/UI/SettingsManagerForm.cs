using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Configuration;
using static Finance.Helpers;

namespace Finance
{
    public class SettingsManagerForm : Form
    {
        private static SettingsManagerForm _Instance { get; set; }
        public static SettingsManagerForm Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new SettingsManagerForm();
                return _Instance;
            }
        }

        Size _defaultSize = new Size(750, 750);

        private SettingsManagerForm()
        {
            this.InitializeMe();

            this.FormClosing += (s, e) =>
            {
                this.Hide();
                e.Cancel = true;
            };
        }

        TabControl tabSettingsGroups;
        TabPage pageApplicationSettings;
        TabPage pageDataSettings;
        TabPage pageLiveTradingSettings;
        TabPage pageTradingSettings;
        TabPage pageRisk;
        TabPage pageTestingSettings;
        TabPage pageGlobalStyles;
        TabPage pageIndexDefaults;

        [Initializer]
        private void InitializeStyles()
        {
            Size = _defaultSize;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Text = "Settings";
            StartPosition = FormStartPosition.CenterScreen;
        }
        [Initializer]
        private void InitializeTabControl()
        {
            tabSettingsGroups = new TabControl()
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.Normal                
            };
            this.Controls.Add(tabSettingsGroups);
        }
        [Initializer]
        private void InitializeApplicationSettingsPage()
        {
            pageApplicationSettings = new TabPage("Application");
            pageApplicationSettings.Controls.Add(CreateSettingsTabPagePanel(SettingsType.Application));
            tabSettingsGroups.TabPages.Add(pageApplicationSettings);
        }
        [Initializer]
        private void InitializeDataSettingsPage()
        {
            pageDataSettings = new TabPage("Data");
            pageDataSettings.Controls.Add(CreateSettingsTabPagePanel(SettingsType.ReferenceData));
            tabSettingsGroups.TabPages.Add(pageDataSettings);

            Settings.Instance.PropertyChanged += (s, e) =>
            {
                if (this.IsDisposed)
                    return;

                Invoke(new Action(() =>
                {
                    if (e.PropertyName == "RefDataProvider")
                    {
                        SuspendLayout();
                        pageDataSettings.Controls.Clear();
                        pageDataSettings.Controls.Add(CreateSettingsTabPagePanel(SettingsType.ReferenceData));
                        ResumeLayout();
                    }
                }));
            };
        }
        [Initializer]
        private void InitializeLiveTradingSettingsPage()
        {
            pageLiveTradingSettings = new TabPage("Live Trading/Data");
            pageLiveTradingSettings.Controls.Add(CreateSettingsTabPagePanel(SettingsType.LiveTrading));
            tabSettingsGroups.TabPages.Add(pageLiveTradingSettings);

            Settings.Instance.PropertyChanged += (s, e) =>
            {
                if (this.IsDisposed)
                    return;

                Invoke(new Action(() =>
                {
                    if (e.PropertyName == "LiveDataProvider")
                    {
                        SuspendLayout();
                        pageLiveTradingSettings.Controls.Clear();
                        pageLiveTradingSettings.Controls.Add(CreateSettingsTabPagePanel(SettingsType.LiveTrading));
                        ResumeLayout();
                    }
                }));
            };
        }
        [Initializer]
        private void InitializeTradingSettingsPage()
        {
            pageTradingSettings = new TabPage("Trading");
            pageTradingSettings.Controls.Add(CreateSettingsTabPagePanel(SettingsType.Trading));
            tabSettingsGroups.TabPages.Add(pageTradingSettings);
        }
        [Initializer]
        private void InitializeRiskSettingsPage()
        {
            pageRisk = new TabPage("Risk");
            pageRisk.BackColor = Color.Red;
            pageRisk.Controls.Add(CreateSettingsTabPagePanel(SettingsType.LiveRisk));
            tabSettingsGroups.TabPages.Add(pageRisk);
        }
        [Initializer]
        private void InitializeTestingSettingsPage()
        {
            pageTestingSettings = new TabPage("Testing Defaults");
            pageTestingSettings.Controls.Add(CreateSettingsTabPagePanel(SettingsType.Testing));
            tabSettingsGroups.TabPages.Add(pageTestingSettings);
        }
        [Initializer]
        private void InitializeGlobalStylesPage()
        {
            pageGlobalStyles = new TabPage("Global Styles");
            pageGlobalStyles.Controls.Add(CreateSettingsTabPagePanel(SettingsType.Style));
            tabSettingsGroups.TabPages.Add(pageGlobalStyles);

            Settings.Instance.PropertyChanged += (s, e) =>
            {
                if (this.IsDisposed)
                    return;

                Invoke(new Action(() =>
                {
                    if (e.PropertyName == "ResetDefaultTrendColors")
                    {
                        SuspendLayout();
                        pageGlobalStyles.Controls.Clear();
                        pageGlobalStyles.Controls.Add(CreateSettingsTabPagePanel(SettingsType.Style));
                        ResumeLayout();
                    }
                }));
            };
        }
        [Initializer]
        private void InitializeIndexDefaultsPage()
        {
            pageIndexDefaults = new TabPage("Indices");
            pageIndexDefaults.Controls.Add(CreateSettingsTabPagePanel(SettingsType.IndexManagementParameters));
            tabSettingsGroups.TabPages.Add(pageIndexDefaults);

            Settings.Instance.PropertyChanged += (s, e) =>
            {
                if (this.IsDisposed)
                    return;
            };
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
                Size = tabSettingsGroups.ClientRectangle.Size,
                FlowDirection = FlowDirection.TopDown,
            };

            var settings = Settings.GetSettingsByCategoryTag(settingsType);

            foreach (var setting in settings)
            {
                // If the setting has a display condition attached and it is currently marked as NotDisplayed, skip
                if (Attribute.IsDefined(setting, typeof(DisplaySettingConditionAttribute)) &&
                    !setting.GetCustomAttribute<DisplaySettingConditionAttribute>().Display)
                    continue;

                var ctrl = PropertyControl(setting);
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
        private Panel PropertyControl(PropertyInfo property)
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
            // Enum value - ComboBox with all values
            //
            if (propertyType.IsEnum)
                ret.Controls.Add(PropertyControlEnum(property, ret), 1, 0);

            //
            // Int value - TextBox
            //
            if (propertyType == typeof(int))
                ret.Controls.Add(PropertyControlInt(property, ret), 1, 0);

            //
            // Currency (decimal) value - MaskedTextBox
            //
            if (propertyType == typeof(Currency))
                ret.Controls.Add(PropertyControlCurrency(property, ret), 1, 0);

            //
            // String value - TextBox
            //
            if (propertyType == typeof(string))
                ret.Controls.Add(PropertyControlString(property, ret), 1, 0);

            //
            // Directory value - TextBox
            //
            if (propertyType == typeof(Directory))
                ret.Controls.Add(PropertyControlDirectory(property, ret), 1, 0);

            //
            // Time value - DateTimePicker
            //
            if (propertyType == typeof(TimeSpan))
                ret.Controls.Add(PropertyControlTime(property, ret), 1, 0);

            //
            // Date value - DateTimePicker
            //
            if (propertyType == typeof(DateTime))
                ret.Controls.Add(PropertyControlDateTime(property, ret), 1, 0);

            //
            // Bool value - ComboBox
            //
            if (propertyType == typeof(bool))
                ret.Controls.Add(PropertyControlBool(property, ret), 1, 0);

            //
            // Color value - TextBox picker
            //
            if (propertyType == typeof(Color))
                ret.Controls.Add(PropertyControlColor(property, ret), 1, 0);

            //
            // Button
            //
            if (propertyType == typeof(Button))
                ret.Controls.Add(PropertyControlButton(property, ret), 1, 0);
            
            //
            // Decimal
            //
            if (propertyType == typeof(decimal))
                ret.Controls.Add(PropertyControlDecimal(property, ret), 1, 0);

            return ret;
        }

        private ComboBox PropertyControlEnum(PropertyInfo property, TableLayoutPanel panel)
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
                IndexOf(Enum.GetName(enumType, property.GetValue(Settings.Instance)));

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
                object val = Enum.Parse(enumType, (ctrl.SelectedItem as string).EnumFromDescription(enumType).ToString());
                property.SetValue(Settings.Instance, val);

                panel.BackColor = Color.LawnGreen;
                highlightTimer.Start();
            };

            return ctrl;

        }
        private ComboBox PropertyControlBool(PropertyInfo property, TableLayoutPanel panel)
        {
            Size _defaultComboBoxSize = new Size(100, 20);

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

            ctrl.SelectedItem = property.GetValue(Settings.Instance);

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

                property.SetValue(Settings.Instance, (bool)ctrl.SelectedItem);

                panel.BackColor = Color.LawnGreen;
                highlightTimer.Start();
            };

            return ctrl;

        }
        private TextBox PropertyControlInt(PropertyInfo property, TableLayoutPanel panel)
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

            ctrl.Text = property.GetValue(Settings.Instance).ToString();

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
                    property.SetValue(Settings.Instance, val);
                    panel.BackColor = Color.LawnGreen;
                    valueChanged = false;
                    panel.Focus();
                }
                else
                {
                    // Reset text to settings value
                    ctrl.Text = property.GetValue(Settings.Instance).ToString();
                    valueChanged = false;
                    panel.BackColor = Color.PaleVioletRed;
                    ctrl.SelectAll();
                }
                highlightTimer.Start();
            };

            return ctrl;
        }
        private TextBox PropertyControlCurrency(PropertyInfo property, TableLayoutPanel panel)
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

            setText((decimal)property.GetValue(Settings.Instance));

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
                    property.SetValue(Settings.Instance, val);
                    panel.BackColor = Color.LawnGreen;
                    setText(val);
                    valueChanged = false;
                    panel.Focus();
                }
                else
                {
                    // Reset text to settings value
                    setText((decimal)property.GetValue(Settings.Instance));
                    valueChanged = false;
                    panel.BackColor = Color.PaleVioletRed;
                    ctrl.SelectAll();
                }
                highlightTimer.Start();
            };

            return ctrl;
        }
        private TextBox PropertyControlDecimal(PropertyInfo property, TableLayoutPanel panel)
        {
            Size _defaultDecimalTextBoxSize = new Size(100, 20);

            //
            // Create TextBox
            //
            var ctrl = new TextBox()
            {
                Size = _defaultDecimalTextBoxSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2),
                TextAlign = HorizontalAlignment.Right
            };

            ctrl.Text = property.GetValue(Settings.Instance).ToString();

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
                    property.SetValue(Settings.Instance, val);
                    panel.BackColor = Color.LawnGreen;                    
                    valueChanged = false;
                    panel.Focus();
                }
                else
                {
                    // Reset text to settings value
                    ctrl.Text = property.GetValue(Settings.Instance).ToString();
                    valueChanged = false;
                    panel.BackColor = Color.PaleVioletRed;
                    ctrl.SelectAll();
                }
                highlightTimer.Start();
            };

            return ctrl;
        }

        private TextBox PropertyControlString(PropertyInfo property, TableLayoutPanel panel)
        {
            Size _defaultStringTextBoxSize = new Size(200, 20);

            //
            // Create TextBox
            //
            var ctrl = new TextBox()
            {
                Size = _defaultStringTextBoxSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2),
                TextAlign = HorizontalAlignment.Right
            };

            ctrl.Text = property.GetValue(Settings.Instance).ToString();

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
                property.SetValue(Settings.Instance, ctrl.Text);
                valueChanged = false;
                panel.BackColor = Color.LawnGreen;
                panel.Focus();

                highlightTimer.Start();
            };

            return ctrl;
        }
        private TextBox PropertyControlDirectory(PropertyInfo property, TableLayoutPanel panel)
        {
            Size _defaultDirectoryPathBoxSize = new Size(300, 20);

            //
            // Create TextBox
            //
            var ctrl = new TextBox()
            {
                Size = _defaultDirectoryPathBoxSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2),
                TextAlign = HorizontalAlignment.Left,
            };

            ctrl.Text = property.GetValue(Settings.Instance).ToString();

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
            ctrl.Click += (s, e) =>
            {
                using (FolderBrowserDialog dialog = new FolderBrowserDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        ctrl.Text = dialog.SelectedPath;

                        // Update settings value
                        property.SetValue(Settings.Instance, ctrl.Text);
                        panel.BackColor = Color.LawnGreen;
                        panel.Focus();

                        highlightTimer.Start();
                    }
                }
            };

            return ctrl;
        }
        private DateTimePicker PropertyControlTime(PropertyInfo property, TableLayoutPanel panel)
        {
            Size _defaultTimePickerSize = new Size(100, 20);

            //
            // Create Time Picker
            //
            var ctrl = new DateTimePicker()
            {
                Size = _defaultTimePickerSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true
            };

            ctrl.Value = DateTime.Parse(property.GetValue(Settings.Instance).ToString());

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
                property.SetValue(Settings.Instance, ctrl.Value.TimeOfDay);
                panel.BackColor = Color.LawnGreen;
                highlightTimer.Start();
            };

            return ctrl;
        }
        private DateTimePicker PropertyControlDateTime(PropertyInfo property, TableLayoutPanel panel)
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

            ctrl.Value = DateTime.Parse(property.GetValue(Settings.Instance).ToString());

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
                property.SetValue(Settings.Instance, ctrl.Value);
                panel.BackColor = Color.LawnGreen;
                highlightTimer.Start();
            };

            return ctrl;
        }
        private Panel PropertyControlColor(PropertyInfo property, TableLayoutPanel panel)
        {
            Size _defaultColorPickerSize = new Size(100, 20);

            //
            // Create Time Picker
            //
            var ctrl = new Panel()
            {
                Size = _defaultColorPickerSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2)
            };

            Color color = (Color)property.GetValue(Settings.Instance);
            ctrl.Tag = color;
            ctrl.BackColor = Color.FromArgb(255, color.R, color.G, color.B);

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
            ctrl.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        int opacity = ((Color)property.GetValue(Settings.Instance)).A;
                        Color newColor = Color.FromArgb(opacity, cd.Color.R, cd.Color.G, cd.Color.B);
                        property.SetValue(Settings.Instance, newColor);
                        ctrl.BackColor = cd.Color;
                    }
                }
            };


            ctrl.BackColorChanged += (s, e) =>
            {
                panel.BackColor = Color.LawnGreen;
                highlightTimer.Start();
            };

            return ctrl;
        }
        private Button PropertyControlButton(PropertyInfo property, TableLayoutPanel panel)
        {
            Size _defaultButtonSize = new Size(100, 20);

            //
            // Create button
            //
            var ctrl = new Button()
            {
                Size = _defaultButtonSize,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 15, 2)
            };

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
            ctrl.Click += (s, e) =>
            {
                property.SetValue(Settings.Instance, true);
            };

            ctrl.BackColorChanged += (s, e) =>
            {
                panel.BackColor = Color.LawnGreen;
                highlightTimer.Start();
            };

            return ctrl;
        }
    }
}
