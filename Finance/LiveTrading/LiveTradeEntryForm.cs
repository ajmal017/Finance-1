using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finance;
using Finance.Data;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Diagnostics;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance.LiveTrading
{
    public class LiveTradeEntryForm : Form, INotifyPropertyChanged, IPersistLayout
    {
        private Button btnBuy;
        private Label label1;
        private NumericUpDown txtOrderSize;
        private Label label2;
        private NumericUpDown txtOrderPrice;
        private LiveMiniQuotePanel liveMiniQuotePanel1;
        private Button btnExecute;
        private Button btnRiskBased;
        private Label label3;
        private NumericUpDown txtRiskPercent;
        private DataGridView dgvAccount;
        private Label label4;
        private NumericUpDown txtRiskDollars;
        private Label label5;
        private NumericUpDown txtStopPrice;
        private Label label6;
        private NumericUpDown txtStopAtrMult;
        private Label label7;
        private Button btnSetPriceLast;
        private DataGridView dgvTradePreview;
        private Label label8;
        private Label label9;
        private DataGridView dgvStoplossPreview;
        private Button btnCalculator;
        private Button btnApproveTrade;
        private Button btnSell;

        private static LiveTradeEntryForm _Instance { get; set; }
        public static LiveTradeEntryForm Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new LiveTradeEntryForm();
                return _Instance;
            }
        }

        public bool Sizeable => false;

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event SelectedSecurityChangedEventHandler ActiveSecurityChanged;
        private void OnActiveSecurityChanged()
        {
            EnableControls();
            ActiveSecurityChanged?.Invoke(this, new SelectedSecurityEventArgs(this.Security));
        }

        public event LiveOrderEventHandler OrderChanged;
        private void OnOrderChanged()
        {
            ClearCurrentTrades();
            if (this.Security == null || this.OrderPrice == 0)
                return;

            OrderChanged?.Invoke(this, new LiveOrderEventArgs(Order));
        }

        #endregion

        #region Order Parameters

        private TradeActionBuySell _OrderDirection { get; set; } = TradeActionBuySell.Buy;
        public TradeActionBuySell OrderDirection
        {
            get => _OrderDirection;
            set
            {
                if (_OrderDirection != value && value != TradeActionBuySell.None)
                {
                    _OrderDirection = value;
                    StoplossOrderDirection = (TradeActionBuySell)(value.ToInt() * (-1));
                    OnOrderChanged();
                }
            }
        }
        public TradeActionBuySell StoplossOrderDirection { get; protected set; } = TradeActionBuySell.Sell;

        public TradeType OrderType { get; protected set; } = TradeType.Limit;

        private decimal _OrderPrice { get; set; }
        public decimal OrderPrice
        {
            get => _OrderPrice;
            set
            {
                if (_OrderPrice != value)
                {
                    _OrderPrice = value;
                    OnPropertyChanged("OrderPrice");
                    OnOrderChanged();
                }
            }
        }

        private decimal _OrderSize { get; set; }
        public decimal OrderSize
        {
            get => _OrderSize;
            set
            {
                if (_OrderSize != value)
                {
                    _OrderSize = value;
                    OnPropertyChanged("OrderSize");
                    OnOrderChanged();
                }
            }
        }

        private decimal _StopPrice { get; set; }
        public decimal StopPrice
        {
            get => _StopPrice;
            set
            {
                if (_StopPrice != value)
                {
                    _StopPrice = value;
                    OnPropertyChanged("StopPrice");
                    OnOrderChanged();
                }
            }
        }

        private decimal _StopAtrMult { get; set; } = 1.0m;
        public decimal StopAtrMult
        {
            get => _StopAtrMult;
            set
            {
                if (_StopAtrMult != value)
                {
                    _StopAtrMult = value;
                    OnPropertyChanged("StopAtrMult");
                }
            }
        }

        public LiveOrder Order
        {
            get => new LiveOrder(this.Security, this.OrderDirection, this.OrderSize, this.OrderPrice, this.OrderType);
        }
        public LiveOrder StoplossOrder
        {
            get
            {
                if (AutoRiskManagement == false)
                    return null;
                else
                    return new LiveOrder(this.Security,
                        this.StoplossOrderDirection,
                        this.OrderSize,
                        this.StopPrice,
                        TradeType.Stop);
            }
        }

        #endregion

        #region Risk Management

        private bool AutoRiskManagement { get; set; } = true;

        private decimal _PercentRisk { get; set; }
        public decimal PercentRisk
        {
            get => _PercentRisk;
            set
            {
                if (_PercentRisk != value)
                {
                    _PercentRisk = value;
                    OnPropertyChanged("PercentRisk");
                }
            }
        }

        private decimal _DollarsRisk { get; set; }
        public decimal DollarsRisk
        {
            get => _DollarsRisk;
            set
            {
                if (_DollarsRisk != value)
                {
                    _DollarsRisk = value;
                    OnPropertyChanged("DollarsRisk");
                }
            }
        }

        private decimal SecurityLastAtr
        {
            get => Security.GetLastBar(PriceBarSize.Daily).AverageTrueRange();
        }

        #endregion

        #region Trade Execution

        private LiveTrade Trade { get; set; }
        private LiveTrade StoplossTrade { get; set; }

        #endregion

        public Security Security { get; protected set; }
        public LiveAccount Account { get; protected set; }

        private bool SecurityOutOfDate { get; set; } = false;

        private LiveTradeEntryForm()
        {
            InitializeComponent();
            this.InitializeMe();

            this.Shown += (s, e) => LoadLayout();
            this.ResizeEnd += (s, e) => SaveLayout();

            btnCalculator.Click += (s, e) => Process.Start("calc");

            EnableControls();
        }

        private void InitializeComponent()
        {
            this.btnBuy = new System.Windows.Forms.Button();
            this.btnSell = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtOrderSize = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.txtOrderPrice = new System.Windows.Forms.NumericUpDown();
            this.btnExecute = new System.Windows.Forms.Button();
            this.btnRiskBased = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtRiskPercent = new System.Windows.Forms.NumericUpDown();
            this.dgvAccount = new System.Windows.Forms.DataGridView();
            this.label4 = new System.Windows.Forms.Label();
            this.txtRiskDollars = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.txtStopPrice = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.txtStopAtrMult = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.btnSetPriceLast = new System.Windows.Forms.Button();
            this.dgvTradePreview = new System.Windows.Forms.DataGridView();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.dgvStoplossPreview = new System.Windows.Forms.DataGridView();
            this.liveMiniQuotePanel1 = new Finance.LiveMiniQuotePanel();
            this.btnCalculator = new System.Windows.Forms.Button();
            this.btnApproveTrade = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.txtOrderSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtOrderPrice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRiskPercent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAccount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRiskDollars)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtStopPrice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtStopAtrMult)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTradePreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStoplossPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // btnBuy
            // 
            this.btnBuy.Location = new System.Drawing.Point(160, 13);
            this.btnBuy.Name = "btnBuy";
            this.btnBuy.Size = new System.Drawing.Size(213, 37);
            this.btnBuy.TabIndex = 1;
            this.btnBuy.Text = "BUY";
            this.btnBuy.UseVisualStyleBackColor = true;
            // 
            // btnSell
            // 
            this.btnSell.Location = new System.Drawing.Point(403, 13);
            this.btnSell.Name = "btnSell";
            this.btnSell.Size = new System.Drawing.Size(213, 37);
            this.btnSell.TabIndex = 1;
            this.btnSell.Text = "SELL";
            this.btnSell.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Calibri", 21.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(174, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 36);
            this.label1.TabIndex = 2;
            this.label1.Text = "QTY";
            // 
            // txtOrderSize
            // 
            this.txtOrderSize.Font = new System.Drawing.Font("Calibri", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOrderSize.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.txtOrderSize.Location = new System.Drawing.Point(243, 69);
            this.txtOrderSize.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.txtOrderSize.Name = "txtOrderSize";
            this.txtOrderSize.Size = new System.Drawing.Size(120, 43);
            this.txtOrderSize.TabIndex = 3;
            this.txtOrderSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtOrderSize.ThousandsSeparator = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Calibri", 21.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(385, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 36);
            this.label2.TabIndex = 2;
            this.label2.Text = "AT   $";
            // 
            // txtOrderPrice
            // 
            this.txtOrderPrice.DecimalPlaces = 2;
            this.txtOrderPrice.Font = new System.Drawing.Font("Calibri", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOrderPrice.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.txtOrderPrice.Location = new System.Drawing.Point(472, 69);
            this.txtOrderPrice.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.txtOrderPrice.Name = "txtOrderPrice";
            this.txtOrderPrice.Size = new System.Drawing.Size(131, 43);
            this.txtOrderPrice.TabIndex = 3;
            this.txtOrderPrice.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtOrderPrice.ThousandsSeparator = true;
            // 
            // btnExecute
            // 
            this.btnExecute.Enabled = false;
            this.btnExecute.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExecute.Location = new System.Drawing.Point(327, 525);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(283, 37);
            this.btnExecute.TabIndex = 1;
            this.btnExecute.Text = "EXECUTE";
            this.btnExecute.UseVisualStyleBackColor = true;
            // 
            // btnRiskBased
            // 
            this.btnRiskBased.Location = new System.Drawing.Point(12, 150);
            this.btnRiskBased.Name = "btnRiskBased";
            this.btnRiskBased.Size = new System.Drawing.Size(125, 109);
            this.btnRiskBased.TabIndex = 1;
            this.btnRiskBased.Text = "RISK BASED SIZING";
            this.btnRiskBased.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Calibri", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(143, 124);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 23);
            this.label3.TabIndex = 2;
            this.label3.Text = "RISK %";
            // 
            // txtRiskPercent
            // 
            this.txtRiskPercent.DecimalPlaces = 2;
            this.txtRiskPercent.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtRiskPercent.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.txtRiskPercent.Location = new System.Drawing.Point(147, 150);
            this.txtRiskPercent.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.txtRiskPercent.Name = "txtRiskPercent";
            this.txtRiskPercent.Size = new System.Drawing.Size(120, 37);
            this.txtRiskPercent.TabIndex = 3;
            this.txtRiskPercent.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtRiskPercent.ThousandsSeparator = true;
            // 
            // dgvAccount
            // 
            this.dgvAccount.AllowUserToAddRows = false;
            this.dgvAccount.AllowUserToDeleteRows = false;
            this.dgvAccount.AllowUserToResizeColumns = false;
            this.dgvAccount.AllowUserToResizeRows = false;
            this.dgvAccount.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAccount.ColumnHeadersVisible = false;
            this.dgvAccount.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvAccount.Location = new System.Drawing.Point(12, 297);
            this.dgvAccount.MultiSelect = false;
            this.dgvAccount.Name = "dgvAccount";
            this.dgvAccount.ReadOnly = true;
            this.dgvAccount.RowHeadersVisible = false;
            this.dgvAccount.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.dgvAccount.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvAccount.Size = new System.Drawing.Size(219, 86);
            this.dgvAccount.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Calibri", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(143, 196);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 23);
            this.label4.TabIndex = 2;
            this.label4.Text = "RISK $";
            // 
            // txtRiskDollars
            // 
            this.txtRiskDollars.DecimalPlaces = 2;
            this.txtRiskDollars.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtRiskDollars.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.txtRiskDollars.Location = new System.Drawing.Point(147, 222);
            this.txtRiskDollars.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.txtRiskDollars.Name = "txtRiskDollars";
            this.txtRiskDollars.Size = new System.Drawing.Size(120, 37);
            this.txtRiskDollars.TabIndex = 3;
            this.txtRiskDollars.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtRiskDollars.ThousandsSeparator = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(12, 277);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(157, 17);
            this.label5.TabIndex = 6;
            this.label5.Text = "Current Account Values";
            // 
            // txtStopPrice
            // 
            this.txtStopPrice.DecimalPlaces = 2;
            this.txtStopPrice.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStopPrice.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.txtStopPrice.Location = new System.Drawing.Point(313, 150);
            this.txtStopPrice.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.txtStopPrice.Name = "txtStopPrice";
            this.txtStopPrice.Size = new System.Drawing.Size(120, 37);
            this.txtStopPrice.TabIndex = 3;
            this.txtStopPrice.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtStopPrice.ThousandsSeparator = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Calibri", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(309, 124);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(64, 23);
            this.label6.TabIndex = 2;
            this.label6.Text = "STOP $";
            // 
            // txtStopAtrMult
            // 
            this.txtStopAtrMult.DecimalPlaces = 2;
            this.txtStopAtrMult.Font = new System.Drawing.Font("Calibri", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStopAtrMult.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.txtStopAtrMult.Location = new System.Drawing.Point(313, 222);
            this.txtStopAtrMult.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            131072});
            this.txtStopAtrMult.Name = "txtStopAtrMult";
            this.txtStopAtrMult.Size = new System.Drawing.Size(120, 37);
            this.txtStopAtrMult.TabIndex = 3;
            this.txtStopAtrMult.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtStopAtrMult.ThousandsSeparator = true;
            this.txtStopAtrMult.Value = new decimal(new int[] {
            10,
            0,
            0,
            131072});
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Calibri", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(309, 196);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(89, 23);
            this.label7.TabIndex = 2;
            this.label7.Text = "ATR Mult.";
            // 
            // btnSetPriceLast
            // 
            this.btnSetPriceLast.Location = new System.Drawing.Point(472, 119);
            this.btnSetPriceLast.Name = "btnSetPriceLast";
            this.btnSetPriceLast.Size = new System.Drawing.Size(131, 23);
            this.btnSetPriceLast.TabIndex = 7;
            this.btnSetPriceLast.Text = "Last";
            this.btnSetPriceLast.UseVisualStyleBackColor = true;
            // 
            // dgvTradePreview
            // 
            this.dgvTradePreview.AllowUserToAddRows = false;
            this.dgvTradePreview.AllowUserToDeleteRows = false;
            this.dgvTradePreview.AllowUserToResizeColumns = false;
            this.dgvTradePreview.AllowUserToResizeRows = false;
            this.dgvTradePreview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTradePreview.ColumnHeadersVisible = false;
            this.dgvTradePreview.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvTradePreview.Location = new System.Drawing.Point(247, 297);
            this.dgvTradePreview.MultiSelect = false;
            this.dgvTradePreview.Name = "dgvTradePreview";
            this.dgvTradePreview.ReadOnly = true;
            this.dgvTradePreview.RowHeadersVisible = false;
            this.dgvTradePreview.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.dgvTradePreview.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTradePreview.Size = new System.Drawing.Size(186, 123);
            this.dgvTradePreview.TabIndex = 5;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(244, 277);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(99, 17);
            this.label8.TabIndex = 6;
            this.label8.Text = "Trade Preview";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(439, 277);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(115, 17);
            this.label9.TabIndex = 6;
            this.label9.Text = "Stoploss Preview";
            // 
            // dgvStoplossPreview
            // 
            this.dgvStoplossPreview.AllowUserToAddRows = false;
            this.dgvStoplossPreview.AllowUserToDeleteRows = false;
            this.dgvStoplossPreview.AllowUserToResizeColumns = false;
            this.dgvStoplossPreview.AllowUserToResizeRows = false;
            this.dgvStoplossPreview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStoplossPreview.ColumnHeadersVisible = false;
            this.dgvStoplossPreview.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvStoplossPreview.Location = new System.Drawing.Point(439, 297);
            this.dgvStoplossPreview.MultiSelect = false;
            this.dgvStoplossPreview.Name = "dgvStoplossPreview";
            this.dgvStoplossPreview.ReadOnly = true;
            this.dgvStoplossPreview.RowHeadersVisible = false;
            this.dgvStoplossPreview.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.dgvStoplossPreview.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvStoplossPreview.Size = new System.Drawing.Size(186, 123);
            this.dgvStoplossPreview.TabIndex = 5;
            // 
            // liveMiniQuotePanel1
            // 
            this.liveMiniQuotePanel1.BackColor = System.Drawing.Color.Black;
            this.liveMiniQuotePanel1.Location = new System.Drawing.Point(12, 13);
            this.liveMiniQuotePanel1.MaximumSize = new System.Drawing.Size(125, 100);
            this.liveMiniQuotePanel1.MinimumSize = new System.Drawing.Size(125, 100);
            this.liveMiniQuotePanel1.Name = "liveMiniQuotePanel1";
            this.liveMiniQuotePanel1.Size = new System.Drawing.Size(125, 100);
            this.liveMiniQuotePanel1.TabIndex = 4;
            // 
            // btnCalculator
            // 
            this.btnCalculator.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btnCalculator.Location = new System.Drawing.Point(12, 119);
            this.btnCalculator.Name = "btnCalculator";
            this.btnCalculator.Size = new System.Drawing.Size(125, 23);
            this.btnCalculator.TabIndex = 8;
            this.btnCalculator.Text = "Calculator";
            this.btnCalculator.UseVisualStyleBackColor = false;
            // 
            // btnApproveTrade
            // 
            this.btnApproveTrade.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.btnApproveTrade.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnApproveTrade.Location = new System.Drawing.Point(12, 525);
            this.btnApproveTrade.Name = "btnApproveTrade";
            this.btnApproveTrade.Size = new System.Drawing.Size(283, 37);
            this.btnApproveTrade.TabIndex = 1;
            this.btnApproveTrade.Text = "SUBMIT FOR APPROVAL";
            this.btnApproveTrade.UseVisualStyleBackColor = false;
            // 
            // LiveTradeEntryForm
            // 
            this.ClientSize = new System.Drawing.Size(642, 574);
            this.Controls.Add(this.btnCalculator);
            this.Controls.Add(this.btnSetPriceLast);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.dgvStoplossPreview);
            this.Controls.Add(this.dgvTradePreview);
            this.Controls.Add(this.dgvAccount);
            this.Controls.Add(this.liveMiniQuotePanel1);
            this.Controls.Add(this.txtRiskDollars);
            this.Controls.Add(this.txtRiskPercent);
            this.Controls.Add(this.txtStopAtrMult);
            this.Controls.Add(this.txtStopPrice);
            this.Controls.Add(this.txtOrderPrice);
            this.Controls.Add(this.txtOrderSize);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnSell);
            this.Controls.Add(this.btnExecute);
            this.Controls.Add(this.btnApproveTrade);
            this.Controls.Add(this.btnRiskBased);
            this.Controls.Add(this.btnBuy);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "LiveTradeEntryForm";
            this.Text = " Trade Entry";
            ((System.ComponentModel.ISupportInitialize)(this.txtOrderSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtOrderPrice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRiskPercent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAccount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRiskDollars)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtStopPrice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtStopAtrMult)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTradePreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStoplossPreview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        [Initializer]
        private void InitializeWarningBanner()
        {
            this.Paint += (s, e) =>
            {
                if (!SecurityOutOfDate)
                    return;
                var rect = this.ClientRectangle;
                rect.Inflate(-4, -4);
                e.Graphics.DrawRectangle(new Pen(Color.Yellow, 8), rect);
            };
        }
        [Initializer]
        private void InitializeBuySellButtons()
        {
            btnBuy.Click += (s, e) =>
            {
                OrderDirection = TradeActionBuySell.Buy;
                txtOrderPrice.Value = this.Security.LastAsk;
                Refresh();
            };
            btnBuy.Paint += (s, e) =>
            {
                ControlPaint.DrawButton(e.Graphics, btnBuy.ClientRectangle,
                    OrderDirection == TradeActionBuySell.Buy && btnBuy.Enabled
                        ? ButtonState.Pushed : ButtonState.Normal);

                if (OrderDirection == TradeActionBuySell.Buy)
                {
                    var rect = btnBuy.ClientRectangle;
                    rect.Inflate(-2, -2);
                    e.Graphics.FillRectangle(new SolidBrush(btnBuy.Enabled ? Color.LawnGreen : Button.DefaultBackColor), rect);
                }

                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawString(
                    "BUY",
                    new Font("Calibri", 16, FontStyle.Bold),
                    new SolidBrush(btnBuy.Enabled ? Color.Black : Color.SlateGray),
                    btnBuy.ClientRectangle,
                    stringFormat);


            };

            btnSell.Click += (s, e) =>
            {
                OrderDirection = TradeActionBuySell.Sell;
                txtOrderPrice.Value = this.Security.LastBid;
                Refresh();
            };
            btnSell.Paint += (s, e) =>
            {
                ControlPaint.DrawButton(e.Graphics, btnSell.ClientRectangle,
                    OrderDirection == TradeActionBuySell.Sell && btnSell.Enabled ?
                        ButtonState.Pushed : ButtonState.Normal);

                if (OrderDirection == TradeActionBuySell.Sell)
                {
                    var rect = btnSell.ClientRectangle;
                    rect.Inflate(-2, -2);
                    e.Graphics.FillRectangle(new SolidBrush(btnSell.Enabled ? Color.Red : Button.DefaultBackColor), rect);
                }

                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawString(
                    "SELL",
                    new Font("Calibri", 16, FontStyle.Bold),
                    new SolidBrush(btnSell.Enabled ? Color.Black : Color.SlateGray),
                    btnSell.ClientRectangle,
                    stringFormat);
            };
        }
        [Initializer]
        private void InitializePreviewButton()
        {
            btnApproveTrade.Click += (s, e) => ApproveTrade();
        }
        [Initializer]
        private void InitializeExecuteButton()
        {
            btnExecute.Click += (s, e) => ExecuteTrade();
        }
        [Initializer]
        private void InitializeSizeAndPrice()
        {
            txtOrderSize.ValueChanged += (s, e) =>
            {
                OrderSize = txtOrderSize.Value;
            };
            txtOrderPrice.ValueChanged += (s, e) =>
            {
                OrderPrice = txtOrderPrice.Value;

                if (AutoRiskManagement && !ignoreValueChange)
                {
                    ignoreValueChange = true;
                    SetStopPrice();
                    ignoreValueChange = false;
                }
            };
            btnSetPriceLast.Click += (s, e) =>
            {
                switch (OrderDirection)
                {
                    case TradeActionBuySell.Sell:
                        SetOrderPrice(Security.LastBid);
                        break;
                    case TradeActionBuySell.Buy:
                        SetOrderPrice(Security.LastAsk);
                        break;
                }
            };
        }
        [Initializer]
        private void InitializeRiskBasedSizingButton()
        {
            btnRiskBased.Click += (s, e) =>
            {
                AutoRiskManagement = !AutoRiskManagement;
                EnableControls();
                Refresh();
            };
            btnRiskBased.Paint += (s, e) =>
            {
                ControlPaint.DrawButton(e.Graphics, btnRiskBased.ClientRectangle,
                    AutoRiskManagement && btnRiskBased.Enabled
                        ? ButtonState.Pushed : ButtonState.Normal);

                if (AutoRiskManagement)
                {
                    var rect = btnRiskBased.ClientRectangle;
                    rect.Inflate(-2, -2);
                    e.Graphics.FillRectangle(new SolidBrush(btnRiskBased.Enabled ? Color.Yellow : Button.DefaultBackColor), rect);
                }

                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawString(
                    AutoRiskManagement ?
                        $"AUTO RISK{Environment.NewLine}MANAGER ON" :
                        $"AUTO RISK{Environment.NewLine}MANAGER OFF",
                    new Font("Calibri", 12, FontStyle.Bold),
                    new SolidBrush(btnRiskBased.Enabled ? Color.Black : Color.SlateGray),
                    btnRiskBased.ClientRectangle,
                    stringFormat);
            };
        }
        [Initializer]
        private void InitializeAccountGrid()
        {
            Font _dgvFont = new Font("Calibri", 8, FontStyle.Bold);

            dgvAccount.Columns.Add("field", "");
            dgvAccount.Columns.Add("value", "");
            dgvAccount.AutoGenerateColumns = false;

            dgvAccount.Columns[0].Width = (int)(dgvAccount.Width * .50);
            dgvAccount.Columns[1].Width = (int)(dgvAccount.Width * .50);

            dgvAccount.Columns[0].DefaultCellStyle.Font = _dgvFont;
            dgvAccount.Columns[1].DefaultCellStyle.Font = _dgvFont;

            dgvAccount.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvAccount.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            dgvAccount.SelectionChanged += (s, e) => dgvAccount.ClearSelection();

            int i = 0;
            dgvAccount.Rows.Add(4);

            //
            // Available Funds
            // 
            dgvAccount.Rows[i].Cells["field"].Value = "Available Funds";
            dgvAccount.Rows[i].Cells["value"].Style.Format =
                typeof(LiveAccount).GetTypeInfo().GetProperty("AvailableFunds").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvAccount.Rows[i].Height = dgvAccount.ClientRectangle.Height / 4;

            i += 1;
            //
            // Equity with Loan
            // 
            dgvAccount.Rows[i].Cells["field"].Value = "Equity with Loan";
            dgvAccount.Rows[i].Cells["value"].Style.Format =
                typeof(LiveAccount).GetTypeInfo().GetProperty("AvailableFunds").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvAccount.Rows[i].Height = dgvAccount.ClientRectangle.Height / 4;

            i += 1;
            //
            // Buying Power
            // 
            dgvAccount.Rows[i].Cells["field"].Value = "Buying Power";
            dgvAccount.Rows[i].Cells["value"].Style.Format =
                typeof(LiveAccount).GetTypeInfo().GetProperty("AvailableFunds").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvAccount.Rows[i].Height = dgvAccount.ClientRectangle.Height / 4;

            i += 1;
            //
            // Initial Margin Requirement
            // 
            dgvAccount.Rows[i].Cells["field"].Value = "Initial Margin";
            dgvAccount.Rows[i].Cells["value"].Style.Format =
                typeof(LiveAccount).GetTypeInfo().GetProperty("AvailableFunds").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvAccount.Rows[i].Height = dgvAccount.ClientRectangle.Height / 4;

        }
        [Initializer]
        private void InitializeRiskAdjustmentButtons()
        {
            txtRiskPercent.ValueChanged += (s, e) =>
            {
                PercentRisk = txtRiskPercent.Value / 100;

                if (!ignoreValueChange)
                {
                    ignoreValueChange = true;
                    SetRiskDollars(PercentRisk);
                    SetOrderSize();
                    ignoreValueChange = false;
                }
            };
            txtRiskDollars.ValueChanged += (s, e) =>
            {
                DollarsRisk = txtRiskDollars.Value;

                if (!ignoreValueChange)
                {
                    ignoreValueChange = true;
                    SetRiskPercent(DollarsRisk);
                    SetOrderSize();
                    ignoreValueChange = false;
                }
            };
        }
        [Initializer]
        private void InitializeStopAdjustmentButtons()
        {
            txtStopPrice.ValueChanged += (s, e) =>
            {
                StopPrice = txtStopPrice.Value;

                if (!ignoreValueChange)
                {
                    ignoreValueChange = true;
                    SetOrderPrice();
                    SetOrderSize();
                    ignoreValueChange = false;
                }
            };
            txtStopAtrMult.ValueChanged += (s, e) =>
            {
                StopAtrMult = txtStopAtrMult.Value;

                if (!ignoreValueChange && Security != null)
                {
                    ignoreValueChange = true;
                    SetStopPrice(StopAtrMult);
                    SetOrderSize();
                    ignoreValueChange = false;
                }
            };
        }
        [Initializer]
        private void InitializeTradePreviewGrid()
        {
            Font _dgvFont = new Font("Calibri", 8, FontStyle.Bold);

            dgvTradePreview.Columns.Add("field", "");
            dgvTradePreview.Columns.Add("value", "");
            dgvTradePreview.AutoGenerateColumns = false;

            dgvTradePreview.Columns[0].Width = (int)(dgvTradePreview.Width * .50);
            dgvTradePreview.Columns[1].Width = (int)(dgvTradePreview.Width * .50);

            dgvTradePreview.Columns[0].DefaultCellStyle.Font = _dgvFont;
            dgvTradePreview.Columns[1].DefaultCellStyle.Font = _dgvFont;

            dgvTradePreview.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvTradePreview.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            dgvTradePreview.SelectionChanged += (s, e) => dgvTradePreview.ClearSelection();

            int i = 0;
            int rows = 6;
            dgvTradePreview.Rows.Add(rows);
            //
            // Direction
            // 
            dgvTradePreview.Rows[i].Cells["field"].Value = "Order Direction";
            dgvTradePreview.Rows[i].Height = dgvTradePreview.ClientRectangle.Height / rows;

            i += 1;
            //
            // Type
            //
            dgvTradePreview.Rows[i].Cells["field"].Value = "Order Type";
            dgvTradePreview.Rows[i].Height = dgvTradePreview.ClientRectangle.Height / rows;

            i += 1;
            //
            // Limit Px
            //
            dgvTradePreview.Rows[i].Cells["field"].Value = "Limit Price";
            dgvTradePreview.Rows[i].Cells["value"].Style.Format =
                typeof(LiveOrder).GetTypeInfo().GetProperty("LimitPrice").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvTradePreview.Rows[i].Height = dgvTradePreview.ClientRectangle.Height / rows;

            i += 1;
            //
            // Size
            //
            dgvTradePreview.Rows[i].Cells["field"].Value = "Order Size";
            dgvTradePreview.Rows[i].Cells["value"].Style.Format =
                typeof(LiveOrder).GetTypeInfo().GetProperty("OrderSize").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvTradePreview.Rows[i].Height = dgvTradePreview.ClientRectangle.Height / rows;

            i += 1;
            //
            // Total Money
            //
            dgvTradePreview.Rows[i].Cells["field"].Value = "Total Money";
            dgvTradePreview.Rows[i].Cells["value"].Style.Format =
                typeof(LiveOrder).GetTypeInfo().GetProperty("TotalMoney").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvTradePreview.Rows[i].Height = dgvTradePreview.ClientRectangle.Height / rows;

            i += 1;
            //
            // Commission
            //
            dgvTradePreview.Rows[i].Cells["field"].Value = "Comission";
            dgvTradePreview.Rows[i].Cells["value"].Style.Format =
                typeof(LiveOrder).GetTypeInfo().GetProperty("Commission").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvTradePreview.Rows[i].Height = dgvTradePreview.ClientRectangle.Height / rows;

            this.OrderChanged += (s, e) => UpdateTradePreviewDisplay();

            //
            // Apply cell-specific formats
            //
            dgvTradePreview.CellFormatting += (s, e) =>
            {
                if (e.Value == null || e.ColumnIndex == 0)
                    return;

                switch (e.RowIndex)
                {
                    case 0:
                        e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        e.CellStyle.BackColor = (string)e.Value == "BUY" ? Color.Green : Color.Red;
                        break;
                    case 4:
                    // Total Money
                    case 5:
                        // Commission
                        e.CellStyle.ForeColor = (decimal)e.Value > 0 ? Color.Green : Color.Red;
                        break;

                }
            };

        }
        [Initializer]
        private void InitializeStoplossPreviewGrid()
        {
            Font _dgvFont = new Font("Calibri", 8, FontStyle.Bold);

            dgvStoplossPreview.Columns.Add("field", "");
            dgvStoplossPreview.Columns.Add("value", "");
            dgvStoplossPreview.AutoGenerateColumns = false;

            dgvStoplossPreview.Columns[0].Width = (int)(dgvStoplossPreview.Width * .50);
            dgvStoplossPreview.Columns[1].Width = (int)(dgvStoplossPreview.Width * .50);

            dgvStoplossPreview.Columns[0].DefaultCellStyle.Font = _dgvFont;
            dgvStoplossPreview.Columns[1].DefaultCellStyle.Font = _dgvFont;

            dgvStoplossPreview.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvStoplossPreview.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            dgvStoplossPreview.SelectionChanged += (s, e) => dgvStoplossPreview.ClearSelection();

            int i = 0;
            int rows = 6;
            dgvStoplossPreview.Rows.Add(rows);
            //
            // Direction
            // 
            dgvStoplossPreview.Rows[i].Cells["field"].Value = "Order Direction";
            dgvStoplossPreview.Rows[i].Height = dgvStoplossPreview.ClientRectangle.Height / rows;

            i += 1;
            //
            // Type
            //
            dgvStoplossPreview.Rows[i].Cells["field"].Value = "Order Type";
            dgvStoplossPreview.Rows[i].Height = dgvStoplossPreview.ClientRectangle.Height / rows;

            i += 1;
            //
            // Limit Px
            //
            dgvStoplossPreview.Rows[i].Cells["field"].Value = "Limit Price";
            dgvStoplossPreview.Rows[i].Cells["value"].Style.Format =
                typeof(LiveOrder).GetTypeInfo().GetProperty("LimitPrice").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvStoplossPreview.Rows[i].Height = dgvStoplossPreview.ClientRectangle.Height / rows;

            i += 1;
            //
            // Size
            //
            dgvStoplossPreview.Rows[i].Cells["field"].Value = "Order Size";
            dgvStoplossPreview.Rows[i].Cells["value"].Style.Format =
                typeof(LiveOrder).GetTypeInfo().GetProperty("OrderSize").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvStoplossPreview.Rows[i].Height = dgvStoplossPreview.ClientRectangle.Height / rows;

            i += 1;
            //
            // Total Money
            //
            dgvStoplossPreview.Rows[i].Cells["field"].Value = "Total Money";
            dgvStoplossPreview.Rows[i].Cells["value"].Style.Format =
                typeof(LiveOrder).GetTypeInfo().GetProperty("TotalMoney").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvStoplossPreview.Rows[i].Height = dgvStoplossPreview.ClientRectangle.Height / rows;

            i += 1;
            //
            // Commission
            //
            dgvStoplossPreview.Rows[i].Cells["field"].Value = "Comission";
            dgvStoplossPreview.Rows[i].Cells["value"].Style.Format =
                typeof(LiveOrder).GetTypeInfo().GetProperty("Commission").GetCustomAttribute<DisplayValueAttribute>().DisplayFormat;
            dgvStoplossPreview.Rows[i].Height = dgvStoplossPreview.ClientRectangle.Height / rows;

            this.OrderChanged += (s, e) => UpdateTradePreviewDisplay();

            //
            // Apply cell-specific formats
            //
            dgvStoplossPreview.CellFormatting += (s, e) =>
            {
                if (e.Value == null || e.ColumnIndex == 0)
                    return;

                switch (e.RowIndex)
                {
                    case 0:
                        e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        e.CellStyle.BackColor = (string)e.Value == "BUY" ? Color.Green : Color.Red;
                        break;
                    case 4:
                    // Total Money
                    case 5:
                        // Commission
                        e.CellStyle.ForeColor = (decimal)e.Value > 0 ? Color.Green : Color.Red;
                        break;
                }
            };

        }
        [Initializer]
        private void InitializeHandlers()
        {
            SecurityManagerForm.Instance.SelectedSecurityChanged += (s, e) =>
            {
                SetActiveSecurity(e.SelectedSecurity);
            };
        }

        private void SetOrderPrice(decimal price)
        {
            txtOrderPrice.Value = price;

            if (AutoRiskManagement && !ignoreValueChange)
                SetStopPrice(StopAtrMult);
        }
        private void SetOrderPrice()
        {
            var stopSpan = StopAtrMult * SecurityLastAtr;
            ignoreValueChange = true;
            switch (OrderDirection)
            {
                case TradeActionBuySell.Sell:
                    SetOrderPrice(StopPrice - stopSpan);
                    break;
                case TradeActionBuySell.Buy:
                    SetOrderPrice(StopPrice + stopSpan);
                    break;
            }
            ignoreValueChange = false;
        }

        private void SetOrderSize()
        {
            // Order size is (Total Risk $)/(Risk per share)
            var riskPerShare = StopAtrMult * SecurityLastAtr;
            var orderSize = DollarsRisk / riskPerShare;

            // Round down
            OrderSize = Math.Floor(orderSize);
            txtOrderSize.Value = OrderSize;
        }

        private bool ignoreValueChange = false;
        private void SetRiskDollars(decimal percent)
        {
            if (Account == null)
            {
                txtRiskDollars.Value = 0;
                return;
            }

            var riskDollars = Account.EquityWithLoanValue * PercentRisk;
            txtRiskDollars.Value = riskDollars;
        }
        private void SetRiskPercent(decimal dollars)
        {
            if (Account == null)
            {
                txtRiskPercent.Value = 0;
                return;
            }

            var riskPercent = DollarsRisk / Account.EquityWithLoanValue;
            txtRiskPercent.Value = riskPercent * 100;
        }

        private void SetStopPrice()
        {
            SetStopPrice(StopAtrMult);
        }
        private void SetStopPrice(decimal AtrMult)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    SetStopPrice(AtrMult);
                    return;
                }));
            }

            if (Security == null || OrderPrice == 0)
                return;

            var stopSpan = AtrMult * SecurityLastAtr;
            ignoreValueChange = true;
            switch (OrderDirection)
            {
                case TradeActionBuySell.Sell:
                    txtStopPrice.Value = Math.Max((OrderPrice + stopSpan), 0);
                    break;
                case TradeActionBuySell.Buy:
                    txtStopPrice.Value = Math.Max((OrderPrice - stopSpan), 0);
                    break;
            }
            ignoreValueChange = false;
        }
        private void SetAtrMult(decimal stopDollars)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    SetAtrMult(stopDollars);
                    return;
                }));
            }

            var stopSpan = Math.Abs(OrderPrice - stopDollars);
            var stopAtrMult = stopSpan / SecurityLastAtr;
            StopAtrMult = stopAtrMult;
        }

        private void UpdateAccountDisplay()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { UpdateAccountDisplay(); }));
                return;
            }

            dgvAccount.Rows[0].Cells["value"].Value = Account.AvailableFunds;
            dgvAccount.Rows[1].Cells["value"].Value = Account.EquityWithLoanValue;
            dgvAccount.Rows[2].Cells["value"].Value = Account.BuyingPower;
            dgvAccount.Rows[3].Cells["value"].Value = Account.InitMarginReq;

            Refresh();
        }
        private void UpdateTradePreviewDisplay()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { UpdateTradePreviewDisplay(); }));
                return;
            }

            dgvTradePreview.Rows[0].Cells["value"].Value = Order.OrderDirection.Description();
            dgvTradePreview.Rows[1].Cells["value"].Value = Order.OrderType.Description();
            dgvTradePreview.Rows[2].Cells["value"].Value = Order.LimitPrice;
            dgvTradePreview.Rows[3].Cells["value"].Value = Order.OrderSize;
            dgvTradePreview.Rows[4].Cells["value"].Value = Order.TotalMoney;
            dgvTradePreview.Rows[5].Cells["value"].Value = Order.Commission;

            if (AutoRiskManagement)
            {
                dgvStoplossPreview.Rows[0].Cells["value"].Value = StoplossOrder.OrderDirection.Description();
                dgvStoplossPreview.Rows[1].Cells["value"].Value = StoplossOrder.OrderType.Description();
                dgvStoplossPreview.Rows[2].Cells["value"].Value = StoplossOrder.LimitPrice;
                dgvStoplossPreview.Rows[3].Cells["value"].Value = StoplossOrder.OrderSize;
                dgvStoplossPreview.Rows[4].Cells["value"].Value = StoplossOrder.TotalMoney;
                dgvStoplossPreview.Rows[5].Cells["value"].Value = StoplossOrder.Commission;
            }

            Refresh();
        }
        private void EnableControls()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    EnableControls();
                    return;
                }));
            }

            if (this.Security == null)
            {
                btnBuy.Enabled = false;
                btnSell.Enabled = false;
                btnApproveTrade.Enabled = false;
                btnExecute.Enabled = false;
                btnRiskBased.Enabled = false;
                btnSetPriceLast.Enabled = false;

                txtOrderSize.Enabled = false;
                txtOrderSize.Value = 0;
                txtOrderPrice.Enabled = false;
                txtOrderPrice.Value = 0;

                txtRiskPercent.Enabled = false;
                txtRiskDollars.Enabled = false;
                txtRiskPercent.Value = 0;
                txtStopAtrMult.Enabled = false;
                txtStopAtrMult.Value = 1.0m;
                txtStopPrice.Enabled = false;
                txtStopPrice.Value = 0;
            }
            else
            {
                btnBuy.Enabled = true;
                btnSell.Enabled = true;
                btnApproveTrade.Enabled = true;
                btnRiskBased.Enabled = true;
                btnSetPriceLast.Enabled = true;

                txtOrderPrice.Enabled = true;
                SetOrderPrice(OrderDirection == TradeActionBuySell.Buy ?
                    this.Security.LastAsk : this.Security.LastBid);

                txtOrderSize.Enabled = !AutoRiskManagement;

                txtRiskPercent.Enabled = AutoRiskManagement;
                SetRiskPercent(Settings.Instance.DefaultInitialPercentRisk);
                txtRiskDollars.Enabled = AutoRiskManagement;
                txtStopPrice.Enabled = AutoRiskManagement;
                txtStopAtrMult.Enabled = AutoRiskManagement;
            }
        }

        public void SetActiveSecurity(Security security)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    SetActiveSecurity(security);
                    return;
                }));
            }

            if (this.Security == security)
                return;

            this.Security = security;

            if (!Security.HasBar(Calendar.PriorTradingDay(DateTime.Today), PriceBarSize.Daily))
            {
                SecurityOutOfDate = true;
                MessageBox.Show("Security price data out of date!", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
                SecurityOutOfDate = false;

            // TODO: move this to the panel as a handler
            this.liveMiniQuotePanel1.LoadSecurity(this.Security);

            SetOrderPrice(OrderDirection == TradeActionBuySell.Buy ?
                this.Security.LastAsk : this.Security.LastBid);

            if (OrderPrice == 0)
                SetStopPrice(0);

            OnActiveSecurityChanged();
        }
        public void SetActiveAccount(LiveAccount account)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    SetActiveAccount(account);
                    return;
                }));
            }

            if (this.Account == account)
                return;

            this.Account = account;
            account.PropertyChanged += (s, e) => UpdateAccountDisplay();

            UpdateAccountDisplay();
        }

        private void ApproveTrade()
        {
            var approvedTrades = LiveTradingManager.Instance.ApproveOrder(this.Order, this.StoplossOrder);

            if (approvedTrades.HasValue)
            {
                this.Trade = approvedTrades.Value.trade;
                this.StoplossTrade = approvedTrades.Value.stopTrade;

                btnApproveTrade.Enabled = false;
                btnExecute.Enabled = true;
            }
            else
            {
                ClearCurrentTrades();
            }
        }
        private void ExecuteTrade()
        {
            if (Settings.Instance.EnableLiveTrading == false)
            {
                MessageBox.Show("Live trading currently disabled", "Error", MessageBoxButtons.OK);
                return;
            }

            if (AutoRiskManagement && (Trade == null || StoplossTrade == null))
                throw new TradingSystemException() { message = "Trade or Stoploss null" };
            else if(Trade == null)
                throw new TradingSystemException() { message = "Trade is null" };

            if (LiveTradingManager.Instance.ExecuteTrades(this.Trade, this.StoplossTrade) == true)
            {
                ClearCurrentTrades();
            }
        }
        private void ClearCurrentTrades()
        {
            this.Trade = null;
            this.StoplossTrade = null;

            Invoke(new Action(() =>
            {
                btnExecute.Enabled = false;
                btnApproveTrade.Enabled = true;
            }));
        }

        public void SaveLayout()
        {
            Settings.Instance.SaveFormLayout(this);
        }
        public void LoadLayout()
        {
            Settings.Instance.LoadFormLayout(this);
        }
    }
}
