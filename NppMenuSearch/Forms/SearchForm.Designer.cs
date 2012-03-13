namespace NppMenuSearch.Forms
{
	partial class SearchForm
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
			this.frmSearch = new System.Windows.Forms.Panel();
			this.txtSearch = new System.Windows.Forms.TextBox();
			this.frmSearch.SuspendLayout();
			this.SuspendLayout();
			// 
			// frmSearch
			// 
			this.frmSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.frmSearch.BackColor = System.Drawing.SystemColors.Control;
			this.frmSearch.Controls.Add(this.txtSearch);
			this.frmSearch.Location = new System.Drawing.Point(0, 14);
			this.frmSearch.Name = "frmSearch";
			this.frmSearch.Size = new System.Drawing.Size(459, 20);
			this.frmSearch.TabIndex = 1;
			// 
			// txtSearch
			// 
			this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtSearch.Location = new System.Drawing.Point(0, 0);
			this.txtSearch.Name = "txtSearch";
			this.txtSearch.Size = new System.Drawing.Size(459, 20);
			this.txtSearch.TabIndex = 0;
			this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
			this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyDown);
			this.txtSearch.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSearch_KeyPress);
			// 
			// SearchForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(459, 48);
			this.Controls.Add(this.frmSearch);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "SearchForm";
			this.Text = "SearchForm";
			this.Load += new System.EventHandler(this.SearchForm_Load);
			this.frmSearch.ResumeLayout(false);
			this.frmSearch.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel frmSearch;
		private System.Windows.Forms.TextBox txtSearch;
	}
}