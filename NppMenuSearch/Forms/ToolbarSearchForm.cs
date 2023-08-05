using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;

namespace NppMenuSearch.Forms
{
    public partial class ToolbarSearchForm : Form
    {
        public ResultsPopup ResultsPopup { get; private set; }

        IntPtr hwndRebar = IntPtr.Zero;
        public IntPtr HwndToolbar { get; private set; }

        bool currentlyCheckingToolbarVisiblity = false;

        public ToolbarSearchForm()
        {
            InitializeComponent();
            ResultsPopup = new ResultsPopup();
            ResultsPopup.OwnerTextBox = txtSearch;

            if (components == null)
                components = new Container();
            components.Add(ResultsPopup);

            Main.NppListener.AfterHideShowToolbar += new NppListener.HideShowEventHandler(NppListener_AfterHideShowToolbar);

            frmSearch.Height = txtSearch.Height;
            frmSearch.Top = (Height - frmSearch.Height) / 2;
            picClear.Top = (frmSearch.Height - picClear.Height) / 2;

            picClear.Visible = false;
            //uint margins = (uint)Win32.SendMessage(txtSearch.Handle, (NppMsg)Win32.EM_GETMARGINS, 0, 0);
            //uint rightMargin = margins >> 16;
            //rightMargin+= 16;
            //Win32.SendMessage(txtSearch.Handle, (NppMsg)Win32.EM_SETMARGINS, Win32.EC_RIGHTMARGIN, (int)(rightMargin << 16));

            DarkMode.Changed += DarkMode_Changed;
            DarkMode_Changed();

            txtSearch.HandleCreated += (sender, e) => DarkMode.ApplyTheme((Control)sender);
        }

        private void DarkMode_Changed()
        {
            DarkMode.ApplyThemeRecursive(this);
        }

        void NppListener_AfterHideShowToolbar(bool show)
        {
            CheckToolbarVisiblity();
        }

        void SetClearImage(Image img)
        {
            if (img == picClear.Image)
                return;

            uint margins = (uint)Win32.SendMessage(txtSearch.Handle, (NppMsg)Win32.EM_GETMARGINS, 0, 0);
            uint rightMargin = margins >> 16;

            if (picClear.Visible)
                rightMargin -= 16;

            if (img == null)
            {
                picClear.Visible = false;
            }
            else
            {
                rightMargin += 16;
                picClear.Visible = true;
            }

            picClear.Image = img;
            Win32.SendMessage(txtSearch.Handle, (NppMsg)Win32.EM_SETMARGINS, Win32.EC_RIGHTMARGIN, (int)(rightMargin << 16));
        }

        void InitToolbar()
        {
            IntPtr main = Main.GetNppMainWindow();
            hwndRebar = IntPtr.Zero;
            HwndToolbar = IntPtr.Zero;

            Win32.EnumChildWindows(main, child =>
            {
                if (Win32.GetParent(child) != main)
                    return true;

                StringBuilder sb = new StringBuilder(256);
                Win32.GetClassName(child, sb, sb.Capacity);

                /* There are two rebar controls: one for the tool bar, the other for incemental search
				 */
                if (sb.ToString() == "ReBarWindow32")
                {
                    sb = null;

                    RECT rect;
                    Win32.GetClientRect(child, out rect);

                    Win32.EnumChildWindows(child, rebarChild =>
                    {
                        StringBuilder sb2 = new StringBuilder(256);
                        Win32.GetClassName(rebarChild, sb2, sb2.Capacity);

                        if (sb2.ToString() == "ToolbarWindow32")
                        {
                            HwndToolbar = rebarChild;
                            return false;
                        }

                        return true;
                    });

                    if (HwndToolbar != IntPtr.Zero)
                    {
                        hwndRebar = child;
                        return false;
                    }
                }

                sb = null;
                return true;
            });

            if (hwndRebar == IntPtr.Zero)
            {
                RECT rect;
                Win32.GetClientRect(main, out rect);

                FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
                Location = new Point(rect.Left - Width, rect.Top);

                ClientSize = frmSearch.Size;

                MaximumSize = new Size(0, Size.Height);
            }
            else
            {
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;

                MaximumSize = new Size(0, frmSearch.Size.Height);
            }
        }

        // -1 on error
        static int GetRebarBandIndexByChildHandle(IntPtr hwndRebar, IntPtr hwndChild)
        {
            Win32.REBARBANDINFO band = new Win32.REBARBANDINFO();
            band.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(band);
            int count = (int)Win32.SendMessage(hwndRebar, (NppMsg)Win32.RB_GETBANDCOUNT, 0, 0);

            for (int i = 0; i < count; ++i)
            {
                band.fMask = Win32.RBBIM_CHILD;
                band.hwndChild = IntPtr.Zero;
                Win32.SendMessage(hwndRebar, Win32.RB_GETBANDINFOW, i, ref band);

                if (band.hwndChild == hwndChild)
                    return i;
            }

            return -1;
        }

        /*public bool IsToolbarVisible()
		{
			return IntPtr.Zero != Win32.SendMessage(
				PluginBase.nppData._nppHandle, 
				NppMsg.NPPM_ISTOOLBARHIDDEN, 
				0, 
				0);
		}*/

        public void CheckToolbarVisiblity()
        {

            if (currentlyCheckingToolbarVisiblity)
                return;

            try
            {
                currentlyCheckingToolbarVisiblity = true;
                if (!Win32.IsWindow(hwndRebar) || !Win32.IsWindow(HwndToolbar))
                {
                    InitToolbar();

                    Win32.SetWindowLong(
                        Handle,
                        Win32.GWL_STYLE,
                        Win32.WS_CHILD | Win32.GetWindowLong(Handle, Win32.GWL_STYLE));
                }

                Win32.REBARBANDINFO band = new Win32.REBARBANDINFO();
                band.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(band);
                
                bool show = false; //Win32.IsWindowVisible(hwndToolbar);
                int toolbarIndex = GetRebarBandIndexByChildHandle(hwndRebar, HwndToolbar);
                if (toolbarIndex >= 0)
                {
                    band.fMask = Win32.RBBIM_STYLE;
                    Win32.SendMessage(hwndRebar, Win32.RB_GETBANDINFOW, toolbarIndex, ref band);
                    show = (band.fStyle & Win32.RBBS_HIDDEN) == 0;
                }

                if (show == Visible)
                    return;

                int oldPreferredWidth = Main.PreferredToolbarWidth;
                int searchBarIndex = GetRebarBandIndexByChildHandle(hwndRebar, Handle);
                if (searchBarIndex >= 0)
                {
                    Win32.SendMessage(hwndRebar, (NppMsg)Win32.RB_SHOWBAND, searchBarIndex, show ? 1 : 0);
                    Win32.SendMessage(hwndRebar, (NppMsg)Win32.RB_MINIMIZEBAND, searchBarIndex, 0);
                    Win32.SendMessage(hwndRebar, (NppMsg)Win32.RB_MAXIMIZEBAND, searchBarIndex, 1);
                    return;
                }

                // not yet inserted

                band.fMask = Win32.RBBIM_CHILD | Win32.RBBIM_SIZE | Win32.RBBIM_IDEALSIZE | Win32.RBBIM_CHILDSIZE;
                band.fStyle = Win32.RBBS_GRIPPERALWAYS;
                band.hwndChild = Handle;
                band.cx = Size.Width;
                band.cxMinChild = 170;
                band.cxIdeal = 0;
                band.cyMinChild = frmSearch.Height;
                band.cyMaxChild = frmSearch.Height;
                band.cyChild = 0;
                band.cyIntegral = 0;

                Width = band.cxMinChild;

                if (!show)
                    band.fStyle |= Win32.RBBS_HIDDEN;

                searchBarIndex = (int)Win32.SendMessage(hwndRebar, (NppMsg)Win32.RB_GETBANDCOUNT, 0, 0);
                Win32.SendMessage(hwndRebar, Win32.RB_INSERTBANDW, searchBarIndex, ref band);

                if (searchBarIndex > 0 && show)
                {
                    if (oldPreferredWidth < band.cxMinChild)
                        oldPreferredWidth = band.cxMinChild;

                    Win32.SendMessage(hwndRebar, Win32.RB_SETBANDWIDTH, searchBarIndex, oldPreferredWidth);
                    int extraMargin = Width - oldPreferredWidth;
                    if(extraMargin > 0 && extraMargin < oldPreferredWidth)
                        Win32.SendMessage(hwndRebar, Win32.RB_SETBANDWIDTH, searchBarIndex, oldPreferredWidth - extraMargin);
                }

                string cuebanner = Main.GetMenuSearchTitle();
                Win32.SendMessage(txtSearch.Handle, (NppMsg)Win32.EM_SETCUEBANNER, 0, cuebanner);
            }
            finally
            {
                currentlyCheckingToolbarVisiblity = false;
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

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (txtSearch.TextLength == 0)
            {
                SetClearImage(null);
                ResultsPopup.Hide();
                return;
            }

            if (txtSearch.TextLength > 0 && !ResultsPopup.Visible)
            {
                Point pt = new Point(frmSearch.Width, frmSearch.Height);
                Win32.ClientToScreen(frmSearch.Handle, ref pt);
                //Win32.ScreenToClient(Win32.GetParent(ResultsPopup.Handle), ref pt);

                pt.X -= ResultsPopup.Width;
                //if(pt.X < 0)
                //	pt.X = 0;
                ResultsPopup.Location = pt;

                EventHandler activated = null;
                activated = (object _sender, EventArgs _e) =>
                {
                    Win32.SetFocus(txtSearch.Handle);

                    ResultsPopup.Activated -= activated;
                };
                ResultsPopup.Activated += activated;
                ResultsPopup.Show();

                SetClearImage(DarkMode.ClearNormalIcon);
            }
        }

        private bool suppressKeyPress;

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            suppressKeyPress = false;

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    e.Handled = true;
                    suppressKeyPress = true;
                    //txtSearch.Text 	 = "";
                    ResultsPopup.Hide();
                    Win32.SetFocus(PluginBase.GetCurrentScintilla());
                    break;

                case Keys.Enter:
                case Keys.Tab:
                    e.Handled = true;
                    suppressKeyPress = true;
                    break;

                case Keys.Back:
                    if (e.Control)
                    {
                        // Ctrl+BackSpace
                        suppressKeyPress = true; // Ctrl+BackSpace triggers a KeyPress with e.KeyChar == '\x7F'
                        int pos = txtSearch.SelectionStart;
                        if (txtSearch.SelectionLength == 0)
                        {
                            int len = txtSearch.Text.Length;
                            txtSearch.Text = txtSearch.Text.RemovePreviousWord(pos);
                            pos -= (len - txtSearch.Text.Length);
                        }
                        else
                        {
                            txtSearch.Text = txtSearch.Text.Substring(0, pos) + txtSearch.Text.Substring(pos + txtSearch.SelectionLength);
                        }
                        txtSearch.Select(pos, 0);
                    }
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

        private void picClear_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (txtSearch.TextLength > 0)
                {
                    picClear.Image = DarkMode.ClearPressedIcon;
                }
            }
        }

        private void picClear_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (txtSearch.TextLength > 0)
                {
                    picClear.Image = DarkMode.ClearNormalIcon;
                }
            }
        }

        private void picClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            Win32.SetFocus(PluginBase.GetCurrentScintilla());
        }

        private void ToolbarSearchForm_SizeChanged(object sender, EventArgs e)
        {
            if (Visible)
                Main.PreferredToolbarWidth = Width;
        }
    }
}
