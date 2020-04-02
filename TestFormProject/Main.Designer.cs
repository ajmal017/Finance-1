namespace TestFormProject
{
    partial class Main
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuShowSecurityManager = new System.Windows.Forms.ToolStripMenuItem();
            this.menuShowSimulationManager = new System.Windows.Forms.ToolStripMenuItem();
            this.menuShowTradeManager = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuShowSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.pnlProviderMonitors = new Finance.ExpandoPanel();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCalculator = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.windowToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.toolsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(327, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // windowToolStripMenuItem
            // 
            this.windowToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuShowSecurityManager,
            this.menuShowSimulationManager,
            this.menuShowTradeManager});
            this.windowToolStripMenuItem.Name = "windowToolStripMenuItem";
            this.windowToolStripMenuItem.Size = new System.Drawing.Size(63, 20);
            this.windowToolStripMenuItem.Text = "Window";
            // 
            // menuShowSecurityManager
            // 
            this.menuShowSecurityManager.Name = "menuShowSecurityManager";
            this.menuShowSecurityManager.Size = new System.Drawing.Size(181, 22);
            this.menuShowSecurityManager.Text = "Security Manager";
            // 
            // menuShowSimulationManager
            // 
            this.menuShowSimulationManager.Name = "menuShowSimulationManager";
            this.menuShowSimulationManager.Size = new System.Drawing.Size(181, 22);
            this.menuShowSimulationManager.Text = "Simulation Manager";
            // 
            // menuShowTradeManager
            // 
            this.menuShowTradeManager.Name = "menuShowTradeManager";
            this.menuShowTradeManager.Size = new System.Drawing.Size(181, 22);
            this.menuShowTradeManager.Text = "Trading Manager";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuShowSettings});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // menuShowSettings
            // 
            this.menuShowSettings.Name = "menuShowSettings";
            this.menuShowSettings.Size = new System.Drawing.Size(180, 22);
            this.menuShowSettings.Text = "Show Settings";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Active Providers";
            // 
            // pnlProviderMonitors
            // 
            this.pnlProviderMonitors.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlProviderMonitors.Location = new System.Drawing.Point(12, 50);
            this.pnlProviderMonitors.Name = "pnlProviderMonitors";
            this.pnlProviderMonitors.Size = new System.Drawing.Size(294, 281);
            this.pnlProviderMonitors.TabIndex = 2;
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuCalculator});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // menuCalculator
            // 
            this.menuCalculator.Name = "menuCalculator";
            this.menuCalculator.Size = new System.Drawing.Size(180, 22);
            this.menuCalculator.Text = "Calculator";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(327, 338);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pnlProviderMonitors);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Main";
            this.Text = "Form1";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem windowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuShowSecurityManager;
        private System.Windows.Forms.ToolStripMenuItem menuShowSimulationManager;
        private System.Windows.Forms.ToolStripMenuItem menuShowTradeManager;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuShowSettings;
        private Finance.ExpandoPanel pnlProviderMonitors;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuCalculator;
    }
}

