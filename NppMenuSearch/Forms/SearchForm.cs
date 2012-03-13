using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;

namespace NppMenuSearch.Forms
{
	public partial class SearchForm : Form
	{
		ResultsPopup ResultsPopup;

		public SearchForm()
		{
			InitializeComponent();
			ResultsPopup = new ResultsPopup();
			ResultsPopup.OwnerTextBox = txtSearch;

			MaximumSize = new Size(0, frmSearch.Size.Height);
		}

		public void AddToToolbar()
		{
			IntPtr main = GetNppMainWindow();
			IntPtr rebar = IntPtr.Zero;

			Win32.EnumChildWindows(main, child =>
			{
				StringBuilder sb = new StringBuilder(256);
				Win32.GetClassName(child, sb, sb.Capacity);

				if (sb.ToString() == "ReBarWindow32")
				{
					RECT rect;
					Win32.GetClientRect(child, out rect);

					if (rect.Right > 1 && rect.Bottom > 1)
					{
						rebar = child;
						return false;
					}
				}
				
				return true;
			});

			if (rebar == IntPtr.Zero)
			{
				Console.WriteLine("ReBarWindow32 not found.");
				return;
			}

			Win32.SetWindowLong(Handle, Win32.GWL_STYLE, Win32.WS_CHILD | Win32.GetWindowLong(Handle, Win32.GWL_STYLE));

			Win32.REBARBANDINFO band = new Win32.REBARBANDINFO();
			band.cbSize 			 = System.Runtime.InteropServices.Marshal.SizeOf(band);
			band.fMask 				 = Win32.RBBIM_CHILD | Win32.RBBIM_SIZE | Win32.RBBIM_IDEALSIZE | Win32.RBBIM_CHILDSIZE;
			band.hwndChild 			 = Handle;
			band.cx 				 = Size.Width;
			band.cxIdeal 			 = Size.Width;
			band.cxMinChild 		 = 120;
			band.cyMinChild 		 = frmSearch.Height;
			band.cyMaxChild 		 = frmSearch.Height;
			band.cyChild 			 = 0;
			band.cyIntegral 		 = 0;

			int count = (int)Win32.SendMessage(rebar, (NppMsg)Win32.RB_GETBANDCOUNT, 0, 0);

			Win32.SendMessage(rebar, Win32.RB_INSERTBANDW, count, ref band);
			if (count > 0)
			{
				Win32.SendMessage(rebar, (NppMsg)Win32.RB_MINIMIZEBAND, count - 1, 0);
				Win32.SendMessage(rebar, (NppMsg)Win32.RB_MAXIMIZEBAND, count - 1, 1);
			}
		}

		public void SelectSearchField()
		{
			if (ResultsPopup.Visible)
			{
				ResultsPopup.ShowMoreResults();
			}
			else
			{
				txtSearch.SelectAll();
				txtSearch.Focus();
				txtSearch_TextChanged(null, null);
			}
		}

		private IntPtr GetNppMainWindow()
		{
			IntPtr dummy;
			IntPtr thisThread = Win32.GetWindowThreadProcessId(Handle, out dummy);
			IntPtr parent = PluginBase.nppData._nppHandle;
			while (parent != IntPtr.Zero)
			{
				IntPtr grandParent = Win32.GetParent(parent);

				if (Win32.GetWindowThreadProcessId(grandParent, out dummy) != thisThread)
					break;

				parent = grandParent;
			}

			return parent;
		}

		private void SearchForm_Load(object sender, EventArgs e)
		{
			Win32.SendMessage(txtSearch.Handle, (NppMsg)Win32.EM_SETCUEBANNER, 0, "Search Menu (Ctrl+M)");
		}

		private void txtSearch_TextChanged(object sender, EventArgs e)
		{
			if (txtSearch.TextLength == 0)
			{
				ResultsPopup.Hide();
				return;
			}

			if (txtSearch.TextLength > 0 && !ResultsPopup.Visible)
			{
				Point pt = new Point(frmSearch.Width, frmSearch.Height);
				Win32.ClientToScreen(frmSearch.Handle, ref pt);

				pt.X -= ResultsPopup.Width;
				ResultsPopup.Location = pt;

				EventHandler shown = null;
				shown = (object _sender, EventArgs _e) =>
				{
					Console.WriteLine("shown");
					Win32.SetFocus(txtSearch.Handle);

					ResultsPopup.Activated -= shown;
				};
				ResultsPopup.Activated += shown;
				ResultsPopup.Show();
			}
		}

		private bool suppressKeyPress;

		private void txtSearch_KeyDown(object sender, KeyEventArgs e)
		{
			suppressKeyPress = false;

			switch (e.KeyCode)
			{
				case Keys.Escape:
				case Keys.Enter:
					e.Handled = true;
					suppressKeyPress = true;
					Win32.SetFocus(PluginBase.GetCurrentScintilla());
					break;

				case Keys.Tab:
					e.Handled = true;
					suppressKeyPress = true;
					break;
			}
		}

		private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (suppressKeyPress)
			{
				suppressKeyPress = false;
				e.Handled = true;
			}
		}
	}
}
