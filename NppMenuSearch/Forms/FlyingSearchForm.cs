using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NppPluginNET;

namespace NppMenuSearch.Forms
{
    public partial class FlyingSearchForm : Form
    {
        public ResultsPopup ResultsPopup { get; private set; }

        public FlyingSearchForm()
        {
            InitializeComponent();

            ResultsPopup = new ResultsPopup();
            ResultsPopup.OwnerTextBox = txtSearch;

            if (components == null)
                components = new Container();
            components.Add(ResultsPopup);
        }

        private void FlyingSearchForm_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                Text = Main.GetMenuSearchTitle();
            }
            else
            {
                txtSearch.Text = "";
                ResultsPopup.Hide();
            }
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
                Point pt = Location;
                pt.Y += Height;
                pt.X += Width - ResultsPopup.Width;
                ResultsPopup.Location = pt;

                EventHandler activated = null;
                activated = (object _sender, EventArgs _e) =>
                {
                    Win32.SetFocus(txtSearch.Handle);

                    ResultsPopup.Activated -= activated;
                };
                ResultsPopup.Activated += activated;
                ResultsPopup.Show();
            }
        }
        
        public void SelectSearchField()
        {
            if (!Visible)
            {
                IntPtr hwndMain = Main.GetNppMainWindow();
                RECT rect;
                Win32.GetClientRect(hwndMain, out rect);

                Point pt = new Point(rect.Right - Width, rect.Top);
                Win32.ClientToScreen(hwndMain, ref pt);

                Location = pt;
                Show();
            }

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

        private bool suppressKeyPress;

        private string RemoveLastWord(string S)
        {
            int n = S.Length - 1;
            while (n >= 0 && Char.IsWhiteSpace(S[n])) { --n; } // skipping the trailing spaces
            while (n >= 0 && !Char.IsWhiteSpace(S[n])) { --n; } // skipping the last word
            while (n >= 0 && Char.IsWhiteSpace(S[n])) { --n; } // skipping the spaces before the last word
            return (n >= 0) ? S.Substring(0, n + 1) : "";
        }

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
                    ResultsPopup.OnFinished();
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
                        txtSearch.Text = RemoveLastWord(txtSearch.Text);
                        int n = txtSearch.Text.Length;
                        txtSearch.Select(n, n);
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
            else
            {
                if (e.KeyChar == '\x7F') // 0x7F corresponds to Ctrl+BackSpace. Why? Ask Microsoft...
                {
                    e.KeyChar = '\x00';
                    e.Handled = true;
                }
            }
        }

    }
}
