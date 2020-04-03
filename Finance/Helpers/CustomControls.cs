using Finance.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms.DataVisualization.Charting;
using Finance.TradeStrategies;
using Finance.LiveTrading;
using Timer = System.Windows.Forms.Timer;
using static Finance.Helpers;
using static Finance.Logger;
using System.Data;
using System.ComponentModel;

namespace Finance
{
    #region Security List Box

    public class SecurityListGrid : Panel
    {
        #region Events

        public event SelectedSecurityChangedEventHandler SelectedSecurityChanged;
        private void OnSelectedSecurityChanged()
        {
            SelectedSecurityChanged?.Invoke(this, new SelectedSecurityEventArgs(this.SelectedSecurity));
        }

        #endregion

        private Security _SelectedSecurity { get; set; }
        public Security SelectedSecurity
        {
            get
            {
                return _SelectedSecurity;
            }
            protected set
            {
                if (_SelectedSecurity != value)
                {
                    _SelectedSecurity = value;
                    OnSelectedSecurityChanged();
                }
            }
        }

        Font _defaultFont = new Font("Calibri", 8);
        Font _smallFont = new Font("Calibri", 7);

        DataGridView securityGrid;
        BindingSource bindingSource;

        public SecurityListGrid()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeGrid()
        {
            this.SuspendLayout();

            securityGrid = new DataGridView()
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AutoSize = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = _defaultFont
            };
            this.Controls.Add(securityGrid);

            //
            // Add display value columns
            //
            securityGrid.Columns.Add("Ticker", "Ticker");
            securityGrid.Columns["Ticker"].DataPropertyName = "Ticker";
            securityGrid.Columns["Ticker"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            securityGrid.Columns["Ticker"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["Ticker"].FillWeight = 5;

            securityGrid.Columns.Add("LongName", "Company");
            securityGrid.Columns["LongName"].DataPropertyName = "LongName";
            securityGrid.Columns["LongName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            securityGrid.Columns["LongName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["LongName"].FillWeight = 15;

            securityGrid.Columns.Add("Industry", "Industry");
            securityGrid.Columns["Industry"].DataPropertyName = "Industry";
            securityGrid.Columns["Industry"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            securityGrid.Columns["Industry"].DefaultCellStyle.Font = _smallFont;
            securityGrid.Columns["Industry"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["Industry"].FillWeight = 10;
            securityGrid.Columns["Industry"].SortMode = DataGridViewColumnSortMode.Automatic;

            securityGrid.Columns.Add("Sector", "Sector");
            securityGrid.Columns["Sector"].DataPropertyName = "Sector";
            securityGrid.Columns["Sector"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            securityGrid.Columns["Sector"].DefaultCellStyle.Font = _smallFont;
            securityGrid.Columns["Sector"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["Sector"].FillWeight = 10;
            securityGrid.Columns["Sector"].SortMode = DataGridViewColumnSortMode.Automatic;

            securityGrid.Columns.Add("SicCode", "SIC");
            securityGrid.Columns["SicCode"].DataPropertyName = "SicCodeName";
            securityGrid.Columns["SicCode"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            securityGrid.Columns["SicCode"].DefaultCellStyle.Font = _smallFont;
            securityGrid.Columns["SicCode"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["SicCode"].FillWeight = 20;
            securityGrid.Columns["SicCode"].SortMode = DataGridViewColumnSortMode.Automatic;

            securityGrid.Columns.Add("SecurityType", "Type");
            securityGrid.Columns["SecurityType"].DataPropertyName = "SecurityType";
            securityGrid.Columns["SecurityType"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            securityGrid.Columns["SecurityType"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["SecurityType"].FillWeight = 5;

            securityGrid.Columns.Add("PercentChange_1day", "1 Day");
            securityGrid.Columns["PercentChange_1day"].DataPropertyName = "PercentChange_1day";
            securityGrid.Columns["PercentChange_1day"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            securityGrid.Columns["PercentChange_1day"].DefaultCellStyle.Format = "0.00%";
            securityGrid.Columns["PercentChange_1day"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["PercentChange_1day"].FillWeight = 5;

            securityGrid.Columns.Add("PercentChange_5day", "5 Day");
            securityGrid.Columns["PercentChange_5day"].DataPropertyName = "PercentChange_5day";
            securityGrid.Columns["PercentChange_5day"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            securityGrid.Columns["PercentChange_5day"].DefaultCellStyle.Format = "0.00%";
            securityGrid.Columns["PercentChange_5day"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["PercentChange_5day"].FillWeight = 5;

            securityGrid.Columns.Add("PercentChange_15day", "15 Day");
            securityGrid.Columns["PercentChange_15day"].DataPropertyName = "PercentChange_15day";
            securityGrid.Columns["PercentChange_15day"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            securityGrid.Columns["PercentChange_15day"].DefaultCellStyle.Format = "0.00%";
            securityGrid.Columns["PercentChange_15day"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["PercentChange_15day"].FillWeight = 5;

            securityGrid.Columns.Add("PercentChange_30day", "30 Day");
            securityGrid.Columns["PercentChange_30day"].DataPropertyName = "PercentChange_30day";
            securityGrid.Columns["PercentChange_30day"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            securityGrid.Columns["PercentChange_30day"].DefaultCellStyle.Format = "0.00%";
            securityGrid.Columns["PercentChange_30day"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["PercentChange_30day"].FillWeight = 5;

            securityGrid.Columns.Add("Flags", "CustomTags");
            securityGrid.Columns["Flags"].DataPropertyName = "CustomTagsString";
            securityGrid.Columns["Flags"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            securityGrid.Columns["Flags"].DefaultCellStyle.Font = _smallFont;
            securityGrid.Columns["Flags"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            securityGrid.Columns["Flags"].FillWeight = 15;

            this.ResumeLayout();
        }

        [Initializer]
        private void InitializeEvents()
        {

            securityGrid.SelectionChanged += (s, e) =>
            {
                if (securityGrid.SelectedCells.Count == 0)
                    return;

                SelectedSecurity = securityGrid.SelectedRows[0].DataBoundItem as Security;
            };

            securityGrid.CellDoubleClick += (s, e) =>
            {
                LiveQuoteForm.Instance?.SetActiveSecurity(this.SelectedSecurity);
                LiveTradeEntryForm.Instance?.SetActiveSecurity(this.SelectedSecurity);
                SingleSecurityIndicatorForm.Instance?.SetSecurity(this.SelectedSecurity);
            };
        }

        public void LoadSecurityList()
        {
            Invoke(new Action(() =>
            {
                bindingSource = new BindingSource();
                bindingSource.DataSource = RefDataManager.Instance.GetAllSecurities();
                securityGrid.DataSource = bindingSource;

                securityGrid.CellFormatting += SecurityGrid_CellFormatting;
            }));

        }

        private void SecurityGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == securityGrid.Columns["PercentChange_1day"].Index)
            {
                e.CellStyle.BackColor = (decimal)e.Value < 0 ? Color.PaleVioletRed : Color.PaleGreen;
            }
            if (e.ColumnIndex == securityGrid.Columns["PercentChange_5day"].Index)
            {
                e.CellStyle.BackColor = (decimal)e.Value < 0 ? Color.PaleVioletRed : Color.PaleGreen;
            }
            if (e.ColumnIndex == securityGrid.Columns["PercentChange_15day"].Index)
            {
                e.CellStyle.BackColor = (decimal)e.Value < 0 ? Color.PaleVioletRed : Color.PaleGreen;
            }
            if (e.ColumnIndex == securityGrid.Columns["PercentChange_30day"].Index)
            {
                e.CellStyle.BackColor = (decimal)e.Value < 0 ? Color.PaleVioletRed : Color.PaleGreen;
            }
        }

        public void FilterSecurityList(SecurityFilter filter)
        {
            Invoke(new Action(() =>
            {
                SuspendLayout();

                bindingSource.DataSource =
                    RefDataManager.Instance.GetAllSecurities().
                    Where(sec => filter.IndustryFilters.Contains(sec.Industry)).
                    Where(sec => filter.SectorFilters.Contains(sec.Sector)).
                    Where(sec => filter.SicFilters.Contains(sec.SicCode)).
                    Where(sec => filter.TypeFilters.Contains(sec.SecurityType)).
                    Where(sec => filter.ExcludeMissingData ? !sec.MissingData : true).
                    Where(sec => filter.FavoritesOnly ? sec.Favorite : true).
                    Where(sec => filter.ExcludeZeroVolume ? !sec.ZeroVolume : true).
                    Where(sec => filter.CurrentTrendFilters.Contains(sec.LastTrend()));

                ResumeLayout();
            }));
        }

    }

    public class SecurityFilterBox : Panel
    {

        #region Events

        public event EventHandler SelectedFiltersChanged;
        private void OnSelectedFiltersChanged()
        {
            SelectedFiltersChanged?.Invoke(this, new EventArgs());
        }

        #endregion

        public SecurityFilter ActiveFilters { get; protected set; } = new SecurityFilter();

        Size _defaultListSize = new Size(200, 300);
        Size _defaultButtonSize = new Size(100, 25);

        Font _defaultFont = new Font("Calibri", 8);

        ListBox listIndustries;
        ListBox listSectors;
        ListBox listSicCodes;
        ListBox listSecurityTypes;
        ListBox listTrendTypes;

        CheckBox chkExcludeMissingData;
        CheckBox chkFavoritesOnly;
        CheckBox chkExcludeZeroVolume;

        Button btnApplyFilters;

        public SecurityFilterBox()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeIndustriesListBox()
        {
            listIndustries = new ListBox()
            {
                SelectionMode = SelectionMode.MultiSimple,
                Size = _defaultListSize,
                Location = new Point(5, 25),
                Font = _defaultFont
            };
            Label lblIndustries = new Label()
            {
                Text = "Selected Industries",
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(listIndustries);
            this.Controls.Add(lblIndustries);
            lblIndustries.DockTo(listIndustries, ControlEdge.Top, 0);
        }
        [Initializer]
        private void InitializeSectorsListBox()
        {
            listSectors = new ListBox()
            {
                SelectionMode = SelectionMode.MultiSimple,
                Size = _defaultListSize,
                Font = _defaultFont
            };
            Label lblSectors = new Label()
            {
                Text = "Selected Sectors",
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(listSectors);
            this.Controls.Add(lblSectors);
            listSectors.DockTo(listIndustries, ControlEdge.Right, 10);
            lblSectors.DockTo(listSectors, ControlEdge.Top, 0);
        }
        [Initializer]
        private void InitializeSicCodeListBox()
        {
            listSicCodes = new ListBox()
            {
                SelectionMode = SelectionMode.MultiSimple,
                Size = new Size(300, _defaultListSize.Height),
                Font = _defaultFont
            };
            Label lblSic = new Label()
            {
                Text = "SIC Code",
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(listSicCodes);
            this.Controls.Add(lblSic);
            listSicCodes.DockTo(listSectors, ControlEdge.Right, 10);
            lblSic.DockTo(listSicCodes, ControlEdge.Top, 0);
        }
        [Initializer]
        private void InitializeSecurityTypeListBox()
        {
            listSecurityTypes = new ListBox()
            {
                SelectionMode = SelectionMode.MultiSimple,
                Size = new Size(100, 100),
                Font = _defaultFont
            };
            Label lblType = new Label()
            {
                Text = "Type",
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(listSecurityTypes);
            this.Controls.Add(lblType);
            listSecurityTypes.DockTo(listSicCodes, ControlEdge.Right, 10);
            lblType.DockTo(listSecurityTypes, ControlEdge.Top, 0);
        }
        [Initializer]
        private void InitializeTrendTypeListBox()
        {
            listTrendTypes = new ListBox()
            {
                SelectionMode = SelectionMode.MultiSimple,
                Size = new Size(200, 125),
                Font = _defaultFont
            };
            Label lblTrend = new Label()
            {
                Text = "Trend",
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(listTrendTypes);
            this.Controls.Add(lblTrend);
            lblTrend.DockTo(listIndustries, ControlEdge.Bottom, 0);
            listTrendTypes.DockTo(lblTrend, ControlEdge.Bottom, 0);
        }
        [Initializer]
        private void InitializeSingleSelectionFilters()
        {
            //
            // Exclude missing data
            //
            chkExcludeMissingData = new CheckBox()
            {
                Text = "Exclude Missing Data",
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 200
            };
            chkExcludeMissingData.DockTo(listSecurityTypes, ControlEdge.Bottom, 5);
            this.Controls.Add(chkExcludeMissingData);

            //
            // Favorites only
            //
            chkFavoritesOnly = new CheckBox()
            {
                Text = "Favorites",
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 200
            };
            chkFavoritesOnly.DockTo(chkExcludeMissingData, ControlEdge.Bottom, 2);
            this.Controls.Add(chkFavoritesOnly);

            //
            // Exclude zero volume
            //
            chkExcludeZeroVolume = new CheckBox()
            {
                Text = "Exclude Zero Volume",
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 200
            };
            chkExcludeZeroVolume.DockTo(chkFavoritesOnly, ControlEdge.Bottom, 2);
            this.Controls.Add(chkExcludeZeroVolume);
        }
        [Initializer]
        private void InitializeApplyfilterButton()
        {
            btnApplyFilters = new Button()
            {
                Text = "Apply Filters",
                Size = _defaultButtonSize
            };
            this.Controls.Add(btnApplyFilters);
            btnApplyFilters.DockTo(chkExcludeZeroVolume, ControlEdge.Bottom, 10);

            btnApplyFilters.Click += (s, e) => ApplyFilters();
        }

        public void LoadFilterValues()
        {
            //
            // Industry List Filter
            //
            var industryList = (from sec in RefDataManager.Instance.GetAllSecurities()
                                select sec.Industry).Distinct().ToList();

            listIndustries.DataSource = industryList;
            listIndustries.SelectedIndex = -1;

            //
            // Sector List Filter
            //
            var sectorList = (from sec in RefDataManager.Instance.GetAllSecurities()
                              select sec.Sector).Distinct().ToList();

            listSectors.DataSource = sectorList;
            listSectors.SelectedIndex = -1;

            //
            // SIC Code List Filter
            //
            var sicList = Helpers.GetAllSICCodeStrings();

            listSicCodes.DataSource = sicList;
            listSicCodes.SelectedIndex = -1;

            listSicCodes.SelectedIndexChanged += (s, e) =>
            {
                Console.WriteLine(listSicCodes.SelectedIndex);
            };

            //
            // Type Filters
            //
            var typeList = (from sec in RefDataManager.Instance.GetAllSecurities()
                            select Enum.GetName(typeof(SecurityType), sec.SecurityType)).Distinct().ToList();

            listSecurityTypes.DataSource = typeList;
            listSecurityTypes.SelectedIndex = -1;

            //
            // Trend Filters
            //
            var trendList = (from trendName in Enum.GetNames(typeof(TrendQualification)) select trendName).ToList();

            listTrendTypes.DataSource = trendList;
            listTrendTypes.SelectedIndex = -1;

        }

        private void ApplyFilters()
        {
            SetSelectedIndustries();
            SetSelectedSectors();
            SetSelectedSICs();
            SetSelectedTypes();
            SetSelectedTrends();
            SetSingleSecurityFilters();

            OnSelectedFiltersChanged();
        }
        private void SetSelectedIndustries()
        {
            var ret = new List<string>();
            for (int i = 0; i < listIndustries.Items.Count; i++)
            {
                if (listIndustries.GetSelected(i))
                    ret.Add(listIndustries.Items[i] as string);
            }

            // If nothing selected, return all values (no filters)
            if (ret.Count == 0)
            {
                foreach (var val in listIndustries.Items)
                    ret.Add(val as string);
            }

            ActiveFilters.ClearFilters(SecurityFilterType.Industry);
            ret.ForEach(x =>
            {
                ActiveFilters.AddFilterValue(SecurityFilterType.Industry, x);
            });
        }
        private void SetSelectedSectors()
        {
            var ret = new List<string>();
            for (int i = 0; i < listSectors.Items.Count; i++)
            {
                if (listSectors.GetSelected(i))
                    ret.Add(listSectors.Items[i] as string);
            }

            // If nothing selected, return all values (no filters)
            if (ret.Count == 0)
            {
                foreach (var val in listSectors.Items)
                    ret.Add(val as string);
            }

            ActiveFilters.ClearFilters(SecurityFilterType.Sector);
            ret.ForEach(x =>
            {
                ActiveFilters.AddFilterValue(SecurityFilterType.Sector, x);
            });
        }
        private void SetSelectedSICs()
        {
            var ret = new List<int>();
            for (int i = 0; i < listSicCodes.Items.Count; i++)
            {
                if (listSicCodes.GetSelected(i))
                {
                    List<int> codes = Helpers.GetSICByIndustry(listSicCodes.Items[i] as string);
                    ret.AddRange(codes);
                }
            }

            // If nothing selected, return all values (no filters)
            if (ret.Count == 0)
            {
                foreach (var val in listSicCodes.Items)
                {
                    List<int> codes = Helpers.GetAllSICCodeInts();
                    ret.AddRange(codes);
                }
            }

            ActiveFilters.ClearFilters(SecurityFilterType.SIC);
            ret.ForEach(x =>
            {
                ActiveFilters.AddFilterValue(SecurityFilterType.SIC, x);
            });
        }
        private void SetSelectedTypes()
        {
            var ret = new List<SecurityType>();
            for (int i = 0; i < listSecurityTypes.Items.Count; i++)
            {
                if (listSecurityTypes.GetSelected(i))
                {
                    ret.Add((SecurityType)Enum.Parse(typeof(SecurityType), listSecurityTypes.Items[i] as string));
                }
            }

            // If nothing selected, return all values (no filters)
            if (ret.Count == 0)
            {
                foreach (var val in listSecurityTypes.Items)
                {
                    foreach (var type in Enum.GetValues(typeof(SecurityType)))
                    {
                        ret.Add((SecurityType)type);
                    }
                }
            }

            ActiveFilters.ClearFilters(SecurityFilterType.SecurityType);
            ret.ForEach(x =>
            {
                ActiveFilters.AddFilterValue(SecurityFilterType.SecurityType, x);
            });
        }
        private void SetSelectedTrends()
        {
            var ret = new List<TrendQualification>();
            for (int i = 0; i < listTrendTypes.Items.Count; i++)
            {
                if (listTrendTypes.GetSelected(i))
                {
                    ret.Add((TrendQualification)Enum.Parse(typeof(TrendQualification), listTrendTypes.Items[i] as string));
                }
            }

            // If nothing selected, return all values (no filters)
            if (ret.Count == 0)
            {
                foreach (var val in listTrendTypes.Items)
                {
                    foreach (var type in Enum.GetValues(typeof(TrendQualification)))
                    {
                        ret.Add((TrendQualification)type);
                    }
                }
            }

            ActiveFilters.ClearFilters(SecurityFilterType.Trend);
            ret.ForEach(x =>
            {
                ActiveFilters.AddFilterValue(SecurityFilterType.Trend, x);
            });
        }
        private void SetSingleSecurityFilters()
        {
            ActiveFilters.ExcludeMissingData = chkExcludeMissingData.Checked;
            ActiveFilters.FavoritesOnly = chkFavoritesOnly.Checked;
            ActiveFilters.ExcludeZeroVolume = chkExcludeZeroVolume.Checked;
        }

    }

    #endregion
    #region Security Info Panel

    public class SecurityInfoPanel : Panel
    {
        public Security Security { get; private set; }

        GroupBox grpContents;
        Button btnUpdateSecurity;

        Size _defaultSize = new Size(350, 300);

        public SecurityInfoPanel() { }
        public SecurityInfoPanel(Security security)
        {
            this.Security = security;

            Size = _defaultSize;

            //
            // Group Box to hold all controls
            //
            grpContents = new GroupBox()
            {
                Text = "Security Data",
                Size = new Size(_defaultSize.Width - 5, _defaultSize.Height - 5),
                Location = new Point(2, 0)
            };

            //
            // Button to request datamanager update security
            //
            btnUpdateSecurity = new Button()
            {
                Name = "btnUpdateSecurity",
                Text = "Update Security",
                Size = new Size(175, 25),
                BackColor = Color.Orange,
                Dock = DockStyle.Bottom
            };
            btnUpdateSecurity.Click += (s, e) =>
            {
                if (this.Security == null)
                    return;

                RefDataManager.Instance.UpdateSecurityPriceData(this.Security, DateTime.Today);
                grpContents.Controls.Remove(btnUpdateSecurity);

                Label lblUpdating = new Label()
                {
                    Text = "Updating...",
                    Width = grpContents.Width,
                    Dock = DockStyle.Bottom,
                    Font = Helpers.SystemFont(12, FontStyle.Bold)
                };
                grpContents.Controls.Add(lblUpdating);
            };

            //
            // Link datamanager callback to refresh
            //
            RefDataManager.Instance.SecurityDataChanged += (s, e) =>
            {
                if (e.TryGetSecurity(this.Security.Ticker, out Security sec))
                {
                    this.Security = sec;
                    Redraw();
                }
            };

            Controls.Add(grpContents);
            Redraw();
        }

        public void Load(Security security)
        {
            this.Security = security;
            Redraw();
        }
        private void Redraw()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { Redraw(); }));
                return;
            }

            grpContents.Controls.Clear();
            if (Security == null)
            {
                grpContents.Controls.Add(new Label()
                {
                    Text = "No Security in Database",
                    Font = SystemFont(12, FontStyle.Bold),
                    Location = new Point(5, 20),
                    AutoSize = true
                });
                return;
            }


            //
            // Get a list of all UI Output methods in Security (marked with Attribute)
            //
            var methods = (from method in Security.GetType().GetMethods()
                           where Attribute.IsDefined(method, typeof(UiDisplayTextAttribute))
                           select method).
                          OrderBy(x => ((UiDisplayTextAttribute)x.GetCustomAttribute(typeof(UiDisplayTextAttribute))).Order);
            //
            // Add a label for each UI output method
            //
            foreach (var field in methods)
            {
                grpContents.Controls.Add(new Label()
                {
                    Text = field.Invoke(Security, null) as string,
                    Width = (int)(grpContents.Width * .90),
                    Font = Helpers.SystemFont(8),
                    AutoEllipsis = true
                });
            }

            //
            // Arrange labels
            //
            grpContents.Controls[0].Location = new Point(5, 20);
            for (int i = 1; i < grpContents.Controls.Count; i++)
                grpContents.Controls[i].DockTo(grpContents.Controls[i - 1], ControlEdge.Bottom);

            //
            // Add update button
            //
            if (!Security.DataUpToDate)
                ShowUpdateButton();

            Refresh();
        }
        private void ShowUpdateButton()
        {
            grpContents.Controls.Add(btnUpdateSecurity);
        }
    }

    #endregion
    #region Result Control Panel Components

    public class ComponentPanelBase_1 : Panel
    {
        Size _defaultSize = new Size(200, 600);
        public string Title { get; set; }

        GroupBox grpBox;
        Panel pnlContents;

        public ComponentPanelBase_1(string title)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        [Initializer]
        private void InitializeBaseStyle()
        {
            Size = _defaultSize;

            pnlContents = new Panel()
            {
                Dock = DockStyle.Fill
            };

            grpBox = new GroupBox()
            {
                Text = Title,
                Dock = DockStyle.Fill
            };

            this.Controls.Add(grpBox);
            grpBox.Controls.Add(pnlContents);

            OverrideControlAdded = true;
        }

        protected void ClearContents()
        {
            pnlContents.Controls.Clear();
        }

        private bool OverrideControlAdded = false;
        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            if (OverrideControlAdded)
            {
                this.Controls.Remove(e.Control);
                this.pnlContents.Controls.Add(e.Control);

                for (int i = 1; i < pnlContents.Controls.Count; i++)
                {
                    pnlContents.Controls[i].DockTo(pnlContents.Controls[i - 1], ControlEdge.Bottom, 5);
                }
            }
        }
    }

    public class PositionListPanel : ComponentPanelBase_1
    {
        public Simulation Simulation { get; private set; }
        public Security SelectedValue { get; private set; }

        public event EventHandler SelectedValueChanged;
        private void OnSelectedValueChanged()
        {
            SelectedValueChanged?.Invoke(this, null);
        }

        ListBox listPositions;

        public PositionListPanel(string title, Simulation simulation) : base(title)
        {
            this.InitializeMe();

            Simulation = simulation;
            if (Simulation == null)
                return;

            InitializeList();
        }

        public void LoadSimulation(Simulation simulation)
        {
            Simulation = simulation;
            if (Simulation == null)
                return;

            InitializeList();
        }

        private void InitializeList()
        {
            //
            // List of positions from the duration of the simulation; filter to remove duplicate tickers
            //
            List<Position> positions = Simulation.PortfolioManager.Portfolio.GetPositions(Simulation.SimulationTimeSpan.Item2);
            List<string> tickers = (from p in positions select p.Security.Ticker).Distinct().ToList();

            //
            // List Box
            //
            listPositions = new ListBox()
            {
                DataSource = tickers,
                Dock = DockStyle.Fill
            };
            listPositions.SelectedIndex = -1;
            listPositions.SelectedValueChanged += (s, e) =>
            {
                SelectedValue = (from p in positions
                                 where p.Security.Ticker == listPositions.SelectedValue as string
                                 select p.Security).FirstOrDefault();

                OnSelectedValueChanged();
            };

            this.Controls.Add(listPositions);
        }
    }
    public class AccountingSeriesSelectPanel : ComponentPanelBase_1
    {
        public AccountingSeriesValue SelectedValue { get; private set; }

        public event EventHandler SelectedValueChanged;
        private void OnSelectedValueChanged()
        {
            SelectedValueChanged?.Invoke(this, null);
        }

        ListBox listAccountingSeries;

        public AccountingSeriesSelectPanel(string title) : base(title)
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeList()
        {
            listAccountingSeries = new ListBox()
            {
                DataSource = Enum.GetNames(typeof(AccountingSeriesValue)),
                Dock = DockStyle.Fill
            };
            listAccountingSeries.SelectedValueChanged += (s, e) =>
            {
                SelectedValue = (AccountingSeriesValue)Enum.Parse(typeof(AccountingSeriesValue), listAccountingSeries.SelectedValue as string);
                OnSelectedValueChanged();
            };

            this.Controls.Add(listAccountingSeries);
        }
    }
    public class PositionSummaryPanel : ComponentPanelBase_1
    {
        PositionSummary PositionSummary { get; set; }

        public PositionSummaryPanel(string title) : base(title)
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyle()
        {
            this.Controls.Add(new Label() { Text = "No Data" });
        }

        public void LoadPosition(Position position)
        {
            PositionSummary = new PositionSummary(position);
            SetLabels();
        }
        public void LoadPosition(List<Position> positions)
        {
            PositionSummary = new PositionSummary(positions);
            SetLabels();
        }

        private void SetLabels()
        {
            this.ClearContents();
            if (PositionSummary == null)
            {
                this.Controls.Add(new Label() { Text = "No Data" });
                return;
            }

            this.Controls.Add(new Label() { Text = JustifyStrings("Days Held:", PositionSummary.DaysHeld.ToString(), 30), AutoSize = true, Font = Helpers.SystemFont(8) });
            this.Controls.Add(new Label() { Text = JustifyStrings("Trade Count:", PositionSummary.TradeCount.ToString(), 30), AutoSize = true, Font = Helpers.SystemFont(8) });
            this.Controls.Add(new Label() { Text = JustifyStrings("Posn Count:", PositionSummary.PositionCount.ToString(), 30), AutoSize = true, Font = Helpers.SystemFont(8) });
            this.Controls.Add(new Label() { Text = JustifyStrings("Net Return ($)", PositionSummary.NetReturnDollars.ToString("$0.00"), 30), AutoSize = true, Font = Helpers.SystemFont(8) });
            this.Controls.Add(new Label() { Text = JustifyStrings("Net Return ($/Day)", PositionSummary.NetReturnPerDayDollars.ToString("$0.00"), 30), AutoSize = true, Font = Helpers.SystemFont(8) });
            this.Controls.Add(new Label() { Text = JustifyStrings("Net Return (%)", PositionSummary.NetReturnPercent.ToString("0.00%"), 30), AutoSize = true, Font = Helpers.SystemFont(8) });
            this.Controls.Add(new Label() { Text = JustifyStrings("Net Return (%/Day)", PositionSummary.NetReturnPerDayPercent.ToString("0.00%"), 30), AutoSize = true, Font = Helpers.SystemFont(8) });
            this.Controls.Add(new Label() { Text = JustifyStrings("Annual Return (%)", PositionSummary.AnnualizedNetReturnPercent.ToString("0.00%"), 30), AutoSize = true, Font = Helpers.SystemFont(8) });

            Refresh();
        }
    }
    public class PositionDetailPanel : ComponentPanelBase_1
    {
        List<Position> Positions { get; set; }

        public PositionDetailPanel(string title) : base(title)
        {
            Positions = new List<Position>();
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyle()
        {
            this.Controls.Add(new Label() { Text = "No Data" });
        }

        public void LoadPosition(Position position)
        {
            Positions.Add(position);
            SetLabels();
        }
        public void LoadPosition(List<Position> positions)
        {
            Positions.AddRange(positions);
            SetLabels();
        }
        private void SetLabels()
        {

        }
    }
    public class AccountSummaryPanel : ComponentPanelBase_1
    {

        Simulation Simulation { get; set; }

        public AccountSummaryPanel(string title, Simulation simulation) : base(title)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeResults()
        {
            SetLabels();
        }

        private void SetLabels()
        {
            this.ClearContents();
            if (Simulation == null)
                return;

            SimulationResults results = Simulation.Results;

            foreach (string resultString in results.ToString())
            {
                if (resultString.Contains("Returns by month"))
                    continue;

                this.Controls.Add(new Label()
                {
                    Text = resultString,
                    Font = SystemFont(8),
                    MaximumSize = new Size(275, 0),
                    AutoSize = true
                });
            }
        }
    }

    #endregion
    #region ExpandoPanel

    /*
     *  Panel that expands to fit controls that are added
     */

    public class ExpandoPanel : FlowLayoutPanel
    {
        int marginBuffer = 5;

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl.Right > (this.Width - marginBuffer))
                {
                    this.Width = ctrl.Right + marginBuffer;
                }
                if (ctrl.Bottom > (this.Height - marginBuffer))
                {
                    this.Height = ctrl.Bottom + marginBuffer;
                };
            }
        }

    }

    #endregion
    #region Tiny Chart Sector Trend Panel

    public class SectorTrendPanel : Panel
    {

        public TrendIndex TrendIndex { get; protected set; }
        public DateTime IndexDate { get; protected set; }

        TinySectorTrendChart chart;
        Panel pnlInnerChartPanel;
        Label lblSectorName;

        public SectorTrendPanel()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void Initialize()
        {
            pnlInnerChartPanel = new Panel()
            {
                Width = this.Width,
                Height = this.Height - 20,
                Location = new Point(0, 20),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
            };
            this.Controls.Add(pnlInnerChartPanel);

            chart = new TinySectorTrendChart()
            {
                Dock = DockStyle.Fill
            };
            pnlInnerChartPanel.Controls.Add(chart);

            lblSectorName = new Label()
            {
                Location = new Point(5, 5),
                Text = string.Empty,
                AutoSize = true
            };
            this.Controls.Add(lblSectorName);
        }

        public void LoadTrendIndex(TrendIndex trendIndex, DateTime indexDate)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    LoadTrendIndex(trendIndex, indexDate);
                }));
                return;
            }

            this.TrendIndex = trendIndex;
            this.IndexDate = indexDate;

            chart.LoadTrendIndex(this.TrendIndex, this.IndexDate);
            lblSectorName.Text = trendIndex.IndexName;

            chart.Invalidate();
        }

        public void LoadTrendDate(DateTime indexDate)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    LoadTrendDate(indexDate);
                }));
                return;
            }

            this.IndexDate = indexDate;
            chart.LoadTrendIndex(this.TrendIndex, this.IndexDate);
            chart.Invalidate();
        }

    }

    #endregion
    #region Live Trading

    public class LiveQuotePanel : UserControl
    {

        Size _defaultSize = new Size(400, 250);

        Label lblSymbol;

        Panel pnlBid;
        Panel pnlAsk;
        Panel pnlLastTrade;
        Panel pnlChange;

        Label lblBidPrice;
        Label lblAskPrice;
        Label lblLastTradePrice;

        Label lblBidVolume;
        Label lblAskVolume;
        Label lblLastTradeVolume;

        Label lblBidTime;
        Label lblAskTime;
        Label lblLastTradeTime;

        Label lblPriorClose;
        Label lblSpread;
        Label lblChangeDollars;
        Label lblChangePercent;

        public Security ActiveSecurity { get; private set; }
        private decimal open { get; set; } = -1;
        private decimal lastBid { get; set; } = -1;
        private decimal lastAsk { get; set; } = -1;
        private decimal lastTrade { get; set; } = -1;

        public LiveQuotePanel()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyles()
        {
            this.Size = _defaultSize;
            this.BackColor = Color.Black;
            this.MinimumSize = this.MaximumSize = _defaultSize;

            lblSymbol = new Label()
            {
                Text = "-",
                Dock = DockStyle.Top,
                Font = SystemFont(26, FontStyle.Bold),
                ForeColor = Color.White,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblSymbol);
        }

        [Initializer]
        private void InitializeBidAskDiplay()
        {
            //
            // BID
            //
            pnlBid = new Panel()
            {
                Width = this.Width / 3,
                Height = 125,
                BackColor = this.BackColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            Label lblBid = new Label()
            {
                Text = "BID",
                ForeColor = Color.Red,
                Font = SystemFont(12, FontStyle.Bold),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblBidPrice = new Label()
            {
                Name = "BidPrice",
                Text = "$0.00",
                ForeColor = Color.Red,
                Font = SystemFont(16, FontStyle.Bold),
                Width = pnlBid.Width,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 20),
            };
            lblBidVolume = new Label()
            {
                Name = "BidVolume",
                Text = "0",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(12, FontStyle.Bold),
                Width = pnlBid.Width,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 70)
            };
            lblBidTime = new Label()
            {
                Name = "BidTime",
                Text = @"MM/dd/yy hh:mm:ss",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(8, FontStyle.Bold),
                Width = pnlBid.Width,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 100)
            };

            //
            // Last Trade
            //
            pnlLastTrade = new Panel()
            {
                Width = this.Width / 3,
                Height = 125,
                BackColor = this.BackColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            Label lblLastTrade = new Label()
            {
                Text = "LAST",
                ForeColor = Color.White,
                Font = SystemFont(12, FontStyle.Bold),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblLastTradePrice = new Label()
            {
                Name = "LastPrice",
                Text = "$0.00",
                ForeColor = Color.White,
                Font = SystemFont(16, FontStyle.Bold),
                Width = pnlLastTrade.Width,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 20),
            };
            lblLastTradeVolume = new Label()
            {
                Name = "LastVolume",
                Text = "0",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(12, FontStyle.Bold),
                Width = pnlBid.Width,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 70)
            };
            lblLastTradeTime = new Label()
            {
                Name = "LastTime",
                Text = @"MM/dd/yy hh:mm:ss",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(8, FontStyle.Bold),
                Width = pnlBid.Width,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 100)
            };

            //
            // ASK
            //
            pnlAsk = new Panel()
            {
                Width = this.Width / 3,
                Height = 125,
                BackColor = this.BackColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            Label lblAsk = new Label()
            {
                Text = "ASK",
                ForeColor = Color.LawnGreen,
                Font = SystemFont(12, FontStyle.Bold),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblAskPrice = new Label()
            {
                Name = "AskPrice",
                Text = "$0.00",
                ForeColor = Color.LawnGreen,
                Font = SystemFont(16, FontStyle.Bold),
                Width = pnlAsk.Width,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 20),
            };
            lblAskVolume = new Label()
            {
                Name = "AskVolume",
                Text = "0",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(12, FontStyle.Bold),
                Width = pnlAsk.Width,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 70)
            };
            lblAskTime = new Label()
            {
                Name = "AskTime",
                Text = @"MM/dd/yy hh:mm:ss",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(8, FontStyle.Bold),
                Width = pnlAsk.Width,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 100)
            };

            pnlBid.Controls.AddRange(new[] { lblBid, lblBidPrice, lblBidVolume, lblBidTime });
            pnlLastTrade.Controls.AddRange(new[] { lblLastTrade, lblLastTradePrice, lblLastTradeVolume, lblLastTradeTime });
            pnlAsk.Controls.AddRange(new[] { lblAsk, lblAskPrice, lblAskVolume, lblAskTime });

            pnlBid.Location = new Point(0, 50);
            pnlLastTrade.DockTo(pnlBid, ControlEdge.Right, 0);
            pnlAsk.DockTo(pnlLastTrade, ControlEdge.Right, 0);

            this.Controls.AddRange(new[] { pnlBid, pnlLastTrade, pnlAsk });
        }

        [Initializer]
        private void InitializePriceChangeDisplay()
        {
            pnlChange = new Panel()
            {
                Size = new Size(_defaultSize.Width, 75),
                Location = new Point(0, 175),
                BackColor = this.BackColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            //
            // Last Close
            //
            Label lblOpenTitle = new Label()
            {
                Text = "Last Close",
                ForeColor = Color.Gray,
                Font = SystemFont(8, FontStyle.Bold),
                Margin = new Padding(0),
                Padding = new Padding(0),
                Height = 16,
                Width = this.Width / 2
            };
            lblPriorClose = new Label()
            {
                Text = "$-",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(10, FontStyle.Bold),
                Height = 20,
                Width = this.Width / 2,
                TextAlign = ContentAlignment.MiddleCenter
            };

            //
            // Spread
            //
            Label lblSpreadTitle = new Label()
            {
                Text = "Spread",
                ForeColor = Color.Gray,
                Font = SystemFont(8, FontStyle.Bold),
                Margin = new Padding(0),
                Padding = new Padding(0),
                Height = 16,
                Width = this.Width / 2
            };
            lblSpread = new Label()
            {
                Text = "$-",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(10, FontStyle.Bold),
                Height = 20,
                Width = this.Width / 2,
                TextAlign = ContentAlignment.MiddleCenter
            };

            //
            // Change ($)
            //
            Label lblChangeDollarsTitle = new Label()
            {
                Text = "Change ($)",
                ForeColor = Color.Gray,
                Font = SystemFont(8, FontStyle.Bold),
                Margin = new Padding(0),
                Padding = new Padding(0),
                Height = 16,
                Width = this.Width / 2
            };
            lblChangeDollars = new Label()
            {
                Text = "$-",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(10, FontStyle.Bold),
                Height = 20,
                Width = this.Width / 2,
                TextAlign = ContentAlignment.MiddleCenter
            };

            //
            // Change (%)
            //
            Label lblChangePercentTitle = new Label()
            {
                Text = "Change (%)",
                ForeColor = Color.Gray,
                Font = SystemFont(8, FontStyle.Bold),
                Margin = new Padding(0),
                Padding = new Padding(0),
                Height = 16,
                Width = this.Width / 2
            };
            lblChangePercent = new Label()
            {
                Text = "-%",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(10, FontStyle.Bold),
                Height = 20,
                Width = this.Width / 2,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlChange.Controls.AddRange(new[]
            {
                lblOpenTitle, lblPriorClose,
                lblSpreadTitle, lblSpread,
                lblChangeDollarsTitle, lblChangeDollars,
                lblChangePercentTitle, lblChangePercent
            });

            lblOpenTitle.Location = new Point(0, 0);
            lblPriorClose.DockTo(lblOpenTitle, ControlEdge.Bottom, 0);

            lblSpreadTitle.DockTo(lblOpenTitle, ControlEdge.Right, 0);
            lblSpread.DockTo(lblSpreadTitle, ControlEdge.Bottom, 0);

            lblChangeDollarsTitle.DockTo(lblPriorClose, ControlEdge.Bottom, 0);
            lblChangeDollars.DockTo(lblChangeDollarsTitle, ControlEdge.Bottom, 0);

            lblChangePercentTitle.DockTo(lblSpread, ControlEdge.Bottom, 0);
            lblChangePercent.DockTo(lblChangePercentTitle, ControlEdge.Bottom, 0);

            this.Controls.Add(pnlChange);
        }

        [Initializer]
        private void InitializeDataHandler()
        {
            LiveDataProvider.Instance.LiveQuoteReceived += (s, e) =>
            {
                if (this.Created)
                    UpdateQuote(e.security, e.QuoteType, e.QuoteTime, e.QuotePrice, e.QuoteVolume);
            };
        }

        public void LoadSecurity(Security security)
        {
            if (this.ActiveSecurity == security)
                return;

            this.ActiveSecurity = security;
            UpdateSymbolLabel();
        }
        private void UpdateSymbolLabel()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateSymbolLabel()));
                return;
            }

            lblSymbol.Text = ActiveSecurity.Ticker;
            ClearAllQuotes();

            if (this.ActiveSecurity.LastBid != 0)
                UpdateBid(DateTime.MinValue, this.ActiveSecurity.LastBid, 0);

            if (this.ActiveSecurity.LastAsk != 0)
                UpdateAsk(DateTime.MinValue, this.ActiveSecurity.LastAsk, 0);

            if (this.ActiveSecurity.LastTrade != 0)
                UpdateLastTrade(DateTime.MinValue, this.ActiveSecurity.LastTrade, 0);

            if (this.ActiveSecurity.LastClose != 0)
                this.open = this.ActiveSecurity.LastClose;

            UpdateStats();

            Refresh();
        }

        public void UpdateQuote(Security security, LiveQuoteType quoteType, DateTime quoteTime, decimal quotePrice, long quoteVolume = 0)
        {
            if (security != ActiveSecurity)
                return;

            switch (quoteType)
            {
                case LiveQuoteType.NotSet:
                    break;
                case LiveQuoteType.Bid:
                    UpdateBid(quoteTime, quotePrice, quoteVolume);
                    break;
                case LiveQuoteType.Ask:
                    UpdateAsk(quoteTime, quotePrice, quoteVolume);
                    break;
                case LiveQuoteType.Open:
                    this.open = quotePrice;
                    UpdateStats();
                    break;
                case LiveQuoteType.Trade:
                    this.lastTrade = quotePrice;
                    UpdateLastTrade(quoteTime, quotePrice, quoteVolume);
                    UpdateStats();
                    break;
                default:
                    break;
            }
        }

        private void ClearAllQuotes()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ClearAllQuotes()));
                return;
            }

            lblBidPrice.Text = "$-";
            lblBidVolume.Text = "-";
            lblBidTime.Text = "-";

            lblLastTradePrice.Text = "$-";
            lblLastTradeVolume.Text = "-";
            lblLastTradeTime.Text = "-";

            lblAskPrice.Text = "$-";
            lblAskVolume.Text = "-";
            lblAskTime.Text = "-";

            lblPriorClose.Text = "$-";
            lblSpread.Text = "$-";
            lblChangeDollars.Text = "$-";
            lblChangePercent.Text = "-%";

            lblChangeDollars.ForeColor = Color.Goldenrod;
            lblChangePercent.ForeColor = Color.Goldenrod;
        }

        private void UpdateBid(DateTime quoteTime, decimal quotePrice, long quoteVolume)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateBid(quoteTime, quotePrice, quoteVolume)));
                return;
            }

            lastBid = quotePrice;

            lblBidPrice.Text = $"{quotePrice:$0.000}";
            lblBidVolume.Text = $"{quoteVolume:0}";

            if (quoteTime != DateTime.MinValue)
                lblBidTime.Text = $"{quoteTime:MM/dd/yy hh:mm:ss}";
            else
                lblBidTime.Text = "Stale";
        }
        private void UpdateAsk(DateTime quoteTime, decimal quotePrice, long quoteVolume)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateAsk(quoteTime, quotePrice, quoteVolume)));
                return;
            }

            lastAsk = quotePrice;

            lblAskPrice.Text = $"{quotePrice:$0.000}";
            lblAskVolume.Text = $"{quoteVolume:0}";

            if (quoteTime != DateTime.MinValue)
                lblAskTime.Text = $"{quoteTime:MM/dd/yy hh:mm:ss}";
            else
                lblAskTime.Text = "Stale";
        }
        private void UpdateLastTrade(DateTime tradeTime, decimal tradePrice, long tradeSize)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateLastTrade(tradeTime, tradePrice, tradeSize)));
                return;
            }

            lblLastTradePrice.Text = $"{tradePrice:$0.000}";
            lblLastTradeVolume.Text = $"{tradeSize:0}";

            if (tradeTime != DateTime.MinValue)
                lblLastTradeTime.Text = $"{tradeTime:MM/dd/yy hh:mm:ss}";
            else
                lblLastTradeTime.Text = "Stale";
        }
        private void UpdateStats()
        {
            if (open == -1)
                return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStats()));
                return;
            }

            lblPriorClose.Text = $"{open:$0.00}";
            lblSpread.Text = (lastAsk != -1 && lastBid != -1) ? $"{lastAsk - lastBid:$0.00}" : "$-";

            if (lastTrade == -1)
                return;

            var changeDollars = (lastTrade - open);
            var changePercent = (lastTrade - open) / open;

            lblChangeDollars.ForeColor = changeDollars < 0 ? Color.PaleVioletRed : Color.LightGreen;
            lblChangePercent.ForeColor = changeDollars < 0 ? Color.PaleVioletRed : Color.LightGreen;

            lblChangeDollars.Text = $"{changeDollars:$0.00}";
            lblChangePercent.Text = $"{changePercent:0.00%}";
        }

    }
    public class LiveMiniQuotePanel : UserControl
    {
        Size _defaultSize = new Size(125, 100);

        Label lblSymbol;
        Label lblBidAsk;
        Label lblChange;

        public Security ActiveSecurity { get; private set; }
        private decimal open { get; set; } = -1;
        private decimal lastBid { get; set; } = -1;
        private decimal lastAsk { get; set; } = -1;
        private decimal lastTrade { get; set; } = -1;

        public LiveMiniQuotePanel()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyles()
        {
            this.Size = _defaultSize;
            this.MinimumSize = _defaultSize;
            this.MaximumSize = _defaultSize;
            this.BackColor = Color.Black;

            lblSymbol = new Label()
            {
                Text = "SYMB",
                Width = this.Width,
                Font = SystemFont(24, FontStyle.Bold),
                ForeColor = Color.White,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 0)
            };
            this.Controls.Add(lblSymbol);
        }
        [Initializer]
        private void InitializeDataHandler()
        {
            LiveDataProvider.Instance.LiveQuoteReceived += (s, e) =>
            {
                if (this.Created)
                    UpdateQuote(e.security, e.QuoteType, e.QuoteTime, e.QuotePrice, e.QuoteVolume);
            };
        }
        [Initializer]
        private void InitializeBidAsk()
        {
            Label lblBA = new Label()
            {
                Text = "Bid  Ask",
                ForeColor = Color.Gray,
                Font = SystemFont(8, FontStyle.Bold),
                Width = this.Width,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 35)
            };
            lblBidAsk = new Label()
            {
                Name = "BidAsk",
                Text = "0.00  0.00",
                ForeColor = Color.Goldenrod,
                Font = SystemFont(9, FontStyle.Bold),
                Width = this.Width,
                Height = 20,
                TextAlign = ContentAlignment.TopCenter,
                Location = new Point(0, 55)
            };
            this.Controls.AddRange(new[] { lblBidAsk, lblBA });
        }
        [Initializer]
        private void InitializeChange()
        {
            lblChange = new Label()
            {
                Name = "Change",
                Text = "$0.00  0.00%",
                ForeColor = Color.White,
                Font = SystemFont(8, FontStyle.Bold),
                Width = this.Width,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 75)
            };
            this.Controls.Add(lblChange);
        }

        public void LoadSecurity(Security security)
        {
            if (this.ActiveSecurity == security)
                return;

            this.ActiveSecurity = security;

            this.ActiveSecurity.PropertyChanged += (s, e) =>
            {
                UpdateBidAsk();
                UpdateStats();
            };

            UpdateSymbolLabel();
        }
        private void UpdateSymbolLabel()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateSymbolLabel()));
                return;
            }

            lblSymbol.Text = ActiveSecurity.Ticker;
            ClearAllQuotes();

            if (this.ActiveSecurity.LastBid != 0 || this.ActiveSecurity.LastAsk != 0)
                UpdateBidAsk();

            if (this.ActiveSecurity.LastClose != 0)
                this.open = this.ActiveSecurity.LastClose;

            UpdateStats();

            Refresh();
        }

        private void ClearAllQuotes()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ClearAllQuotes()));
                return;
            }

            lblBidAsk.Text = " -  /  - ";
            lblChange.Text = "$-   -%";
            lblChange.ForeColor = Color.White;
        }

        public void UpdateQuote(Security security, LiveQuoteType quoteType, DateTime quoteTime, decimal quotePrice, long quoteVolume = 0)
        {
            if (security != ActiveSecurity)
                return;

            switch (quoteType)
            {
                case LiveQuoteType.NotSet:
                    break;
                case LiveQuoteType.Bid:
                    lastBid = quotePrice;
                    UpdateBidAsk();
                    break;
                case LiveQuoteType.Ask:
                    lastAsk = quotePrice;
                    UpdateBidAsk();
                    break;
                case LiveQuoteType.Open:
                    this.open = quotePrice;
                    UpdateStats();
                    break;
                case LiveQuoteType.Trade:
                    this.lastTrade = quotePrice;
                    UpdateStats();
                    break;
                default:
                    break;
            }
        }
        private void UpdateBidAsk()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateBidAsk()));
                return;
            }

            lblBidAsk.Text = $"{ActiveSecurity.LastBid:0.00} / {ActiveSecurity.LastAsk:0.00}";
        }
        private void UpdateStats()
        {
            if (ActiveSecurity.LastClose == 0)
                return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStats()));
                return;
            }

            if (ActiveSecurity.LastTrade == 0)
                return;

            var changeDollars = (ActiveSecurity.LastTrade - ActiveSecurity.LastClose);
            var changePercent = (ActiveSecurity.LastTrade - ActiveSecurity.LastClose) / ActiveSecurity.LastClose;

            lblChange.ForeColor = changeDollars < 0 ? Color.PaleVioletRed : Color.LightGreen;
            lblChange.Text = $"{changeDollars:$0.00}  {changePercent:0.00%}";
        }
    }

    public class LiveIntradayTickChartPanel : Panel
    {
        Size _defaultSize = new Size(500, 250);
        LiveIntradayTickChart Chart { get; set; }
        public Security ActiveSecurity { get; protected set; }

        public LiveIntradayTickChartPanel()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializePanel()
        {
            this.Size = _defaultSize;
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;

            Chart = new LiveIntradayTickChart()
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(Chart);
        }

        public void LoadSecurity(Security security)
        {
            if (ActiveSecurity == security)
                return;

            this.ActiveSecurity = security;
            Chart.LoadSecurity(ActiveSecurity);
        }
    }
    public class LiveIntradayTickChart : Chart
    {

        public Security Security { get; protected set; }
        private LiveIntradayChartArea ChartArea { get; set; }
        private LiveIntradayTickSeries ChartSeries { get; set; }

        public LiveIntradayTickChart()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyles()
        {
            this.ChartAreas.Clear();
            ChartArea = new LiveIntradayChartArea();
            this.ChartAreas.Add(ChartArea);
        }

        public void LoadSecurity(Security security)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => LoadSecurity(security)));
                return;
            }

            if (this.Security == security)
                return;

            this.Security = security;
            this.Series.Clear();
            ChartSeries = new LiveIntradayTickSeries(security);
            this.Series.Add(ChartSeries);

            ChartArea.SetYRange(ChartSeries.MinYValue() * .98m, ChartSeries.MaxYValue() * 1.02m);

            InitializeUpdateHandler();
        }

        protected void InitializeUpdateHandler()
        {
            Security.PropertyChanged += (s, e) =>
            {
                if (this.Security != s as Security)
                    return;

                if (e.PropertyName == nameof(Security.IntradayMinuteBars))
                {
                    Invoke(new Action(() =>
                    {
                        ChartSeries.RefreshSeries();
                        ChartArea.SetYRange(ChartSeries.MinYValue() * .98m, ChartSeries.MaxYValue() * 1.02m);
                    }));
                }
            };
        }
    }
    public class LiveIntradayChartArea : ChartArea
    {

        public LiveIntradayChartArea()
        {
            this.InitializeMe();
        }

        [Initializer]
        private void InitializeStyles()
        {
            this.BackColor = Color.Black;
            this.Name = "primary";

            //
            // X Axis
            //

            this.AxisX.Interval = 1;
            this.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            this.AxisX.Minimum = new TimeSpan(8, 0, 0).TotalMinutes;
            this.AxisX.Maximum = new TimeSpan(15, 30, 0).TotalMinutes;

            this.AxisX.MajorGrid.Interval = 30;
            this.AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Number;
            this.AxisX.MajorGrid.Enabled = true;
            this.AxisY.MajorGrid.IntervalOffset = 0;
            this.AxisX.MajorGrid.LineColor = Color.FromArgb(64, 64, 64, 64);

            this.AxisX.StripLines.Add(new StripLine()
            {
                IntervalType = DateTimeIntervalType.Minutes,
                IntervalOffsetType = DateTimeIntervalType.NotSet,
                IntervalOffset = new TimeSpan(8, 30, 0).TotalMinutes,
                StripWidth = 2,
                StripWidthType = DateTimeIntervalType.Number,
                BackColor = Color.FromArgb(255, 0, 128, 0)
            });
            this.AxisX.StripLines.Add(new StripLine()
            {
                IntervalType = DateTimeIntervalType.Minutes,
                IntervalOffsetType = DateTimeIntervalType.NotSet,
                IntervalOffset = new TimeSpan(15, 0, 0).TotalMinutes,
                StripWidth = 2,
                StripWidthType = DateTimeIntervalType.Number,
                BackColor = Color.FromArgb(255, 128, 0, 0)
            });

            //
            // Y Axis
            //
            this.AxisY.Interval = 0.1;
            this.AxisY.IntervalType = DateTimeIntervalType.Number;
            this.AxisY.MajorGrid.Enabled = false;

            //
            // Layout
            //
            this.Position.Auto = false;
            this.Position.X = 0;
            this.Position.Y = 0;
            this.Position.Height = 100;
            this.Position.Width = 100;
            this.AxisX.IsMarginVisible = false;

            this.InnerPlotPosition.Auto = false;
            this.InnerPlotPosition.X = 0;
            this.InnerPlotPosition.Y = 0;
            this.InnerPlotPosition.Height = 100;
            this.InnerPlotPosition.Width = 100;
        }

        public void SetYRange(decimal minY, decimal maxY)
        {
            this.AxisY.Minimum = minY.ToDouble();
            this.AxisY.Maximum = maxY.ToDouble();
        }

    }
    public class LiveIntradayTickSeries : FinanceSeries
    {

        public Security Security { get; }

        public LiveIntradayTickSeries(Security security)
        {
            Security = security ?? throw new ArgumentNullException(nameof(security));

            this.InitializeMe();
        }

        [Initializer]
        protected override void SetStyles()
        {
            this.ChartType = SeriesChartType.Line;
            this.ChartArea = "primary";
            this.MarkerSize = 8;
        }

        [Initializer]
        protected override void BuildSeries()
        {
            Points.Clear();

            foreach (var minuteBar in Security.IntradayMinuteBars)
            {
                var newPt = new DataPoint()
                {
                    XValue = minuteBar.BarDateTime.TimeOfDay.TotalMinutes,
                    YValues = new[] { minuteBar.Close.ToDouble() }
                };
                Points.Add(newPt);
            }

            if (Points.Count > 0)
            {
                Points.Last().IsValueShownAsLabel = true;
                Points.Last().LabelFormat = "$0.00";
                Points.Last().LabelForeColor = Color.White;
            }
        }

        public override void RefreshSeries()
        {
            foreach (var minuteBar in Security.IntradayMinuteBars)
            {
                var pt = Points.SingleOrDefault(x => x.XValue == minuteBar.BarDateTime.TimeOfDay.TotalMinutes);

                if (pt != null && pt.YValues[0] == minuteBar.Close.ToDouble())
                    continue;
                else if (pt != null)
                    Points.Remove(pt);

                var newPt = new DataPoint()
                {
                    XValue = minuteBar.BarDateTime.TimeOfDay.TotalMinutes,
                    YValues = new[] { minuteBar.Close.ToDouble() }
                };
                Points.Add(newPt);
            }

            foreach (var pt in Points)
                pt.IsValueShownAsLabel = false;

            if (Points.Count > 0)
            {
                Points.Last().IsValueShownAsLabel = true;
                Points.Last().LabelFormat = "$0.00";
                Points.Last().LabelForeColor = Color.White;
            }
        }
    }

    #endregion
    #region ComboBox<T>

    public class ComboBox<TEnum> : ComboBox where TEnum : struct
    {
        bool SuspendIndexChanged { get; set; } = false;
        bool SuspendItemChanged { get; set; } = false;
        bool SuspendValueChanged { get; set; } = false;

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            if (!SuspendIndexChanged)
                base.OnSelectedIndexChanged(e);

            SuspendIndexChanged = false;
        }
        protected override void OnSelectedItemChanged(EventArgs e)
        {
            if (!SuspendItemChanged)
                base.OnSelectedItemChanged(e);
            SuspendItemChanged = false;
        }
        protected override void OnSelectedValueChanged(EventArgs e)
        {
            if (!SuspendValueChanged)
                base.OnSelectedValueChanged(e);
            SuspendValueChanged = false;
        }

        public ComboBox()
        {
            this.Items.Clear();
            foreach (Enum e in Enum.GetValues(typeof(TEnum)))
            {
                this.Items.Add(e.Description());
            }
        }

        public void SetSelectedValue(TEnum value, bool suspendEvents = false)
        {
            if (suspendEvents)
            {
                SuspendIndexChanged = true;
                SuspendItemChanged = true;
                SuspendValueChanged = true;
            }

            base.SelectedIndex = this.Items.IndexOf((value as Enum).Description());

            SuspendIndexChanged = false;
            SuspendItemChanged = false;
            SuspendValueChanged = false;
        }
        public TEnum GetSelectedValue()
        {
            return (TEnum)(object)(base.SelectedItem as string).EnumFromDescription(typeof(TEnum));
        }
        public void AddItem(TEnum value)
        {
            this.Items.Add((value as Enum).Description());
        }
        public void RemoveItem(TEnum value)
        {
            if (this.Items.Contains((value as Enum).Description()))
                this.Items.Remove((value as Enum).Description());
        }
    }

    #endregion
}
