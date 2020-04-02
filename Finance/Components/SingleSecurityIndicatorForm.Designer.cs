namespace Finance
{
    partial class SingleSecurityIndicatorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.trendAveragePerformanceTile1 = new Finance.TrendAveragePerformanceTile();
            this.trendNormalizedPerformanceTile1 = new Finance.TrendNormalizedPerformanceTile();
            this.SuspendLayout();
            // 
            // trendAveragePerformanceTile1
            // 
            this.trendAveragePerformanceTile1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.trendAveragePerformanceTile1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.trendAveragePerformanceTile1.Location = new System.Drawing.Point(12, 12);
            this.trendAveragePerformanceTile1.MaximumSize = new System.Drawing.Size(500, 400);
            this.trendAveragePerformanceTile1.MinimumSize = new System.Drawing.Size(500, 400);
            this.trendAveragePerformanceTile1.Name = "trendAveragePerformanceTile1";
            this.trendAveragePerformanceTile1.Size = new System.Drawing.Size(500, 400);
            this.trendAveragePerformanceTile1.TabIndex = 1;
            // 
            // trendNormalizedPerformanceTile1
            // 
            this.trendNormalizedPerformanceTile1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.trendNormalizedPerformanceTile1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.trendNormalizedPerformanceTile1.Location = new System.Drawing.Point(518, 12);
            this.trendNormalizedPerformanceTile1.MaximumSize = new System.Drawing.Size(500, 400);
            this.trendNormalizedPerformanceTile1.MinimumSize = new System.Drawing.Size(500, 400);
            this.trendNormalizedPerformanceTile1.Name = "trendNormalizedPerformanceTile1";
            this.trendNormalizedPerformanceTile1.Size = new System.Drawing.Size(500, 400);
            this.trendNormalizedPerformanceTile1.TabIndex = 0;
            // 
            // SingleSecurityIndicatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1031, 420);
            this.Controls.Add(this.trendAveragePerformanceTile1);
            this.Controls.Add(this.trendNormalizedPerformanceTile1);
            this.Name = "SingleSecurityIndicatorForm";
            this.Text = "TestForm";
            this.ResumeLayout(false);

        }

        #endregion

        private TrendNormalizedPerformanceTile trendNormalizedPerformanceTile1;
        private TrendAveragePerformanceTile trendAveragePerformanceTile1;
    }
}