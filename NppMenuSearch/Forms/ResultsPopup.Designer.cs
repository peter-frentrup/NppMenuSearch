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
			this.label1 = new System.Windows.Forms.Label();
			this.viewResults = new System.Windows.Forms.ListView();
			this.panInfo.SuspendLayout();
			this.SuspendLayout();
			// 
			// timerIdle
			// 
			this.timerIdle.Interval = 1;
			// 
			// panInfo
			// 
			this.panInfo.Controls.Add(this.label1);
			this.panInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panInfo.Location = new System.Drawing.Point(0, 497);
			this.panInfo.Name = "panInfo";
			this.panInfo.Size = new System.Drawing.Size(604, 17);
			this.panInfo.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(0, 2);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(507, 14);
			this.label1.TabIndex = 0;
			this.label1.Text = "Press CTRL+M again to show all results. TAB to switch groups: Recently Used ↔ Men" +
				"u ↔ Preferences";
			// 
			// viewResults
			// 
			this.viewResults.Activation = System.Windows.Forms.ItemActivation.OneClick;
			this.viewResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.viewResults.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.viewResults.FullRowSelect = true;
			this.viewResults.HideSelection = false;
			this.viewResults.Location = new System.Drawing.Point(0, 0);
			this.viewResults.MultiSelect = false;
			this.viewResults.Name = "viewResults";
			this.viewResults.OwnerDraw = true;
			this.viewResults.Size = new System.Drawing.Size(604, 497);
			this.viewResults.TabIndex = 1;
			this.viewResults.TileSize = new System.Drawing.Size(317, 16);
			this.viewResults.UseCompatibleStateImageBehavior = false;
			this.viewResults.View = System.Windows.Forms.View.Tile;
			this.viewResults.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.viewResults_DrawItem);
			this.viewResults.Click += new System.EventHandler(this.viewResults_Click);
			this.viewResults.Resize += new System.EventHandler(this.viewResults_Resize);
			// 
			// ResultsPopup
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(604, 514);
			this.ControlBox = false;
			this.Controls.Add(this.viewResults);
			this.Controls.Add(this.panInfo);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.KeyPreview = true;
			this.Name = "ResultsPopup";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.VisibleChanged += new System.EventHandler(this.ResultsPopup_VisibleChanged);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OwnerTextBox_KeyDown);
			this.panInfo.ResumeLayout(false);
			this.panInfo.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Timer timerBlink;
		private System.Windows.Forms.Panel panInfo;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ListView viewResults;
		internal System.Windows.Forms.Timer timerIdle;
	}
}