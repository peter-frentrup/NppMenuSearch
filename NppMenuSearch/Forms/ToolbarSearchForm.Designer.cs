namespace NppMenuSearch.Forms
{
	partial class ToolbarSearchForm
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
            this.components = new System.ComponentModel.Container();
            this.frmSearch = new System.Windows.Forms.Panel();
            this.picClear = new System.Windows.Forms.PictureBox();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnOptions = new System.Windows.Forms.Button();
            this.timerDelay = new System.Windows.Forms.Timer(this.components);
            this.menuOptions = new System.Windows.Forms.ContextMenu();
            this.menuItemAbout = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItemFixWidgetSize = new System.Windows.Forms.MenuItem();
            this.frmSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picClear)).BeginInit();
            this.SuspendLayout();
            // 
            // frmSearch
            // 
            this.frmSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.frmSearch.BackColor = System.Drawing.SystemColors.Control;
            this.frmSearch.Controls.Add(this.picClear);
            this.frmSearch.Controls.Add(this.txtSearch);
            this.frmSearch.Controls.Add(this.btnOptions);
            this.frmSearch.Location = new System.Drawing.Point(0, 17);
            this.frmSearch.Margin = new System.Windows.Forms.Padding(4);
            this.frmSearch.Name = "frmSearch";
            this.frmSearch.Size = new System.Drawing.Size(612, 25);
            this.frmSearch.TabIndex = 1;
            // 
            // picClear
            // 
            this.picClear.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.picClear.BackColor = System.Drawing.SystemColors.Window;
            this.picClear.Location = new System.Drawing.Point(583, 4);
            this.picClear.Margin = new System.Windows.Forms.Padding(4);
            this.picClear.Name = "picClear";
            this.picClear.Size = new System.Drawing.Size(10, 10);
            this.picClear.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.picClear.TabIndex = 1;
            this.picClear.TabStop = false;
            this.picClear.Click += new System.EventHandler(this.picClear_Click);
            this.picClear.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picClear_MouseDown);
            this.picClear.MouseUp += new System.Windows.Forms.MouseEventHandler(this.picClear_MouseUp);
            // 
            // txtSearch
            // 
            this.txtSearch.CausesValidation = false;
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearch.Location = new System.Drawing.Point(0, 0);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(4);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(600, 20);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyDown);
            this.txtSearch.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSearch_KeyPress);
            // 
            // btnOptions
            // 
            this.btnOptions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnOptions.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnOptions.FlatAppearance.BorderSize = 0;
            this.btnOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOptions.Location = new System.Drawing.Point(600, 0);
            this.btnOptions.Margin = new System.Windows.Forms.Padding(1);
            this.btnOptions.Name = "btnOptions";
            this.btnOptions.Size = new System.Drawing.Size(12, 25);
            this.btnOptions.TabIndex = 2;
            this.btnOptions.Text = "⁞";
            this.btnOptions.UseVisualStyleBackColor = true;
            this.btnOptions.Click += new System.EventHandler(this.btnOptions_Click);
            // 
            // timerDelay
            // 
            this.timerDelay.Interval = 1;
            // 
            // menuOptions
            // 
            this.menuOptions.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemFixWidgetSize,
            this.menuItem1,
            this.menuItemAbout});
            // 
            // menuItemAbout
            // 
            this.menuItemAbout.Index = 2;
            this.menuItemAbout.Text = "&About";
            this.menuItemAbout.Click += new System.EventHandler(this.menuItemAbout_Click);
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 1;
            this.menuItem1.Text = "-";
            // 
            // menuItemFixWidgetSize
            // 
            this.menuItemFixWidgetSize.Index = 0;
            this.menuItemFixWidgetSize.Text = "&Fix widget size";
            this.menuItemFixWidgetSize.Click += new System.EventHandler(this.menuItemFixWidgetSize_Click);
            // 
            // ToolbarSearchForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(612, 59);
            this.Controls.Add(this.frmSearch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "ToolbarSearchForm";
            this.Text = "SearchForm";
            this.SizeChanged += new System.EventHandler(this.ToolbarSearchForm_SizeChanged);
            this.frmSearch.ResumeLayout(false);
            this.frmSearch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picClear)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel frmSearch;
		private System.Windows.Forms.TextBox txtSearch;
		private System.Windows.Forms.Timer timerDelay;
		private System.Windows.Forms.PictureBox picClear;
        private System.Windows.Forms.Button btnOptions;
        private System.Windows.Forms.ContextMenu menuOptions;
        private System.Windows.Forms.MenuItem menuItemFixWidgetSize;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItemAbout;
    }
}