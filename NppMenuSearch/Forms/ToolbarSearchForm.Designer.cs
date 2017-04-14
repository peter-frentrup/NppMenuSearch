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
            this.timerDelay = new System.Windows.Forms.Timer(this.components);
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
            this.picClear.Location = new System.Drawing.Point(595, 4);
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
            this.txtSearch.Size = new System.Drawing.Size(612, 20);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyDown);
            this.txtSearch.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSearch_KeyPress);
            // 
            // timerDelay
            // 
            this.timerDelay.Interval = 1;
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
	}
}