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
			this.lstResults = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			// 
			// lstResults
			// 
			this.lstResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lstResults.Font = new System.Drawing.Font("Arial Unicode MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lstResults.IntegralHeight = false;
			this.lstResults.ItemHeight = 15;
			this.lstResults.Location = new System.Drawing.Point(0, 0);
			this.lstResults.Name = "lstResults";
			this.lstResults.Size = new System.Drawing.Size(586, 387);
			this.lstResults.TabIndex = 0;
			this.lstResults.Click += new System.EventHandler(this.lstResults_Click);
			this.lstResults.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lstResults_KeyDown);
			// 
			// ResultsPopup
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(586, 387);
			this.ControlBox = false;
			this.Controls.Add(this.lstResults);
			this.Name = "ResultsPopup";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.VisibleChanged += new System.EventHandler(this.ResultsPopup_VisibleChanged);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListBox lstResults;
	}
}