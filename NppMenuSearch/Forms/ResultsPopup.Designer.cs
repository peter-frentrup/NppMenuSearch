namespace NppMenuSearch.Forms
{
	partial class ResultsPopup
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
            this.timerIdle = new System.Windows.Forms.Timer(this.components);
            this.timerBlink = new System.Windows.Forms.Timer(this.components);
            this.panInfo = new System.Windows.Forms.Panel();
            this.lblHelp = new System.Windows.Forms.Label();
            this.viewResults = new System.Windows.Forms.ListView();
            this.popupMenu = new System.Windows.Forms.ContextMenu();
            this.menuGotoShortcutDefinition = new System.Windows.Forms.MenuItem();
            this.menuExecute = new System.Windows.Forms.MenuItem();
            this.menuSelectTab = new System.Windows.Forms.MenuItem();
            this.menuOpenDialog = new System.Windows.Forms.MenuItem();
            this.panInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // timerIdle
            // 
            this.timerIdle.Interval = 1;
            // 
            // panInfo
            // 
            this.panInfo.Controls.Add(this.lblHelp);
            this.panInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panInfo.Location = new System.Drawing.Point(0, 453);
            this.panInfo.Margin = new System.Windows.Forms.Padding(4);
            this.panInfo.Name = "panInfo";
            this.panInfo.Size = new System.Drawing.Size(592, 21);
            this.panInfo.TabIndex = 2;
            // 
            // lblHelp
            // 
            this.lblHelp.AutoSize = true;
            this.lblHelp.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHelp.Location = new System.Drawing.Point(0, 2);
            this.lblHelp.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblHelp.Name = "lblHelp";
            this.lblHelp.Size = new System.Drawing.Size(574, 14);
            this.lblHelp.TabIndex = 0;
            this.lblHelp.Text = "Press CTRL+M again to show all results. TAB to switch groups: Recently Used ↔ Men" +
    "u ↔ Open Files ↔ Preferences";
            // 
            // viewResults
            // 
            this.viewResults.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.viewResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.viewResults.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.viewResults.FullRowSelect = true;
            this.viewResults.HideSelection = false;
            this.viewResults.Location = new System.Drawing.Point(0, 0);
            this.viewResults.Margin = new System.Windows.Forms.Padding(4);
            this.viewResults.MultiSelect = false;
            this.viewResults.Name = "viewResults";
            this.viewResults.OwnerDraw = true;
            this.viewResults.ShowItemToolTips = true;
            this.viewResults.Size = new System.Drawing.Size(592, 453);
            this.viewResults.TabIndex = 1;
            this.viewResults.TileSize = new System.Drawing.Size(317, 16);
            this.viewResults.UseCompatibleStateImageBehavior = false;
            this.viewResults.View = System.Windows.Forms.View.Tile;
            this.viewResults.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.viewResults_DrawItem);
            this.viewResults.MouseClick += new System.Windows.Forms.MouseEventHandler(this.viewResults_MouseClick);
            this.viewResults.Resize += new System.EventHandler(this.viewResults_Resize);
            // 
            // popupMenu
            // 
            this.popupMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuGotoShortcutDefinition,
            this.menuExecute,
            this.menuSelectTab,
            this.menuOpenDialog});
            this.popupMenu.Popup += new System.EventHandler(this.popupMenu_Popup);
            // 
            // menuGotoShortcutDefinition
            // 
            this.menuGotoShortcutDefinition.Index = 0;
            this.menuGotoShortcutDefinition.Text = "Change &Shortcut";
            this.menuGotoShortcutDefinition.Click += new System.EventHandler(this.menuGotoShortcutDefinition_Click);
            // 
            // menuExecute
            // 
            this.menuExecute.DefaultItem = true;
            this.menuExecute.Index = 1;
            this.menuExecute.Text = "E&xecute";
            this.menuExecute.Click += new System.EventHandler(this.menuExecute_Click);
            // 
            // menuSelectTab
            // 
            this.menuSelectTab.DefaultItem = true;
            this.menuSelectTab.Index = 2;
            this.menuSelectTab.Text = "&Select Tab";
            this.menuSelectTab.Click += new System.EventHandler(this.menuExecute_Click);
            // 
            // menuOpenDialog
            // 
            this.menuOpenDialog.DefaultItem = true;
            this.menuOpenDialog.Index = 3;
            this.menuOpenDialog.Text = "&Open Dialog";
            this.menuOpenDialog.Click += new System.EventHandler(this.menuExecute_Click);
            // 
            // ResultsPopup
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(592, 474);
            this.ControlBox = false;
            this.Controls.Add(this.viewResults);
            this.Controls.Add(this.panInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "ResultsPopup";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.SizeChanged += new System.EventHandler(this.ResultsPopup_SizeChanged);
            this.VisibleChanged += new System.EventHandler(this.ResultsPopup_VisibleChanged);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OwnerTextBox_KeyDown);
            this.panInfo.ResumeLayout(false);
            this.panInfo.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Timer timerBlink;
		private System.Windows.Forms.Panel panInfo;
		private System.Windows.Forms.Label lblHelp;
		private System.Windows.Forms.ListView viewResults;
		internal System.Windows.Forms.Timer timerIdle;
		private System.Windows.Forms.ContextMenu popupMenu;
		private System.Windows.Forms.MenuItem menuGotoShortcutDefinition;
		private System.Windows.Forms.MenuItem menuExecute;
		private System.Windows.Forms.MenuItem menuOpenDialog;
        private System.Windows.Forms.MenuItem menuSelectTab;
    }
}