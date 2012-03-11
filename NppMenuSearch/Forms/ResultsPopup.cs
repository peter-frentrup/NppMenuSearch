using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;

namespace NppMenuSearch.Forms
{
	public partial class ResultsPopup : Form
	{
		public TextBox OwnerTextBox;
		public MenuItem MainMenu;

		public ResultsPopup()
		{
			InitializeComponent();
			MainMenu = new MenuItem(IntPtr.Zero);

			Main.MakeNppOwnerOf(this);
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case Win32.WM_ACTIVATEAPP:
					if (m.WParam == IntPtr.Zero)
						Hide();
					break;
			}

			base.WndProc(ref m);
		}

		protected override bool ShowWithoutActivation
		{
			get
			{
				return true;
			}
		}

		private void ResultsPopup_VisibleChanged(object sender, EventArgs e)
		{
			if (Visible)
			{
				MainMenu = new MenuItem(Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_INTERNAL_GETMENU, 0, 0));

				if (OwnerTextBox != null)
				{
					OwnerTextBox.TextChanged += OwnerTextBox_TextChanged;
					OwnerTextBox.KeyDown += OwnerTextBox_KeyDown;
				}
				OwnerTextBox_TextChanged(null, null);
			}
			else
			{
				if (OwnerTextBox != null)
				{
					OwnerTextBox.TextChanged -= OwnerTextBox_TextChanged;
					OwnerTextBox.KeyDown -= OwnerTextBox_KeyDown;
				}
			}
		}

		void OwnerTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Down:
					e.Handled = true;
					if (lstResults.SelectedIndex + 1 < lstResults.Items.Count)
						lstResults.SelectedIndex += 1;
					break;

				case Keys.Up:
					e.Handled = true;
					if (lstResults.SelectedIndex > 0)
						lstResults.SelectedIndex -= 1;
					break;

				case Keys.Escape:
					e.Handled = true;
					Hide();
					break;

				case Keys.Enter:
					e.Handled = true;
					ItemSelected();
					break;
			}
		}

		void OwnerTextBox_TextChanged(object sender, EventArgs e)
		{
			var words = OwnerTextBox.Text.SplitAt(' ');
			var allItems = MainMenu.EnumFinalItems();
			var itemMatches = allItems.Select(mi => new KeyValuePair<double, MenuItem>(mi.MatchingSimilarity(words), mi));
			itemMatches = itemMatches.Where(kv => kv.Key > 0.0).OrderByDescending(kv=>kv.Key);
			
			MenuItem[] items = itemMatches.Select(kv => kv.Value).ToArray();

			for (int i = 0; i < lstResults.Items.Count && i < items.Length; ++i)
				lstResults.Items[i] = items[i];

			for (int i = lstResults.Items.Count - 1; i >= items.Length; --i)
				lstResults.Items.RemoveAt(i);

			for (int i = lstResults.Items.Count; i < items.Length; ++i)
				lstResults.Items.Add(items[i]);

			if (items.Length > 0)
				lstResults.SelectedIndex = 0;
		}

		void ItemSelected()
		{
			MenuItem item = lstResults.SelectedItem as MenuItem;
			if (item != null)
			{
				//Console.WriteLine("Selected {0}", item.CommandId);
				Win32.SendMessage(PluginBase.nppData._nppHandle, (NppMsg)Win32.WM_COMMAND, (int)item.CommandId, 0);
				Hide();
				if (OwnerTextBox != null)
					OwnerTextBox.Text = "";
			}
		}

		private void lstResults_Click(object sender, EventArgs e)
		{
			ItemSelected();
		}

		private void lstResults_KeyDown(object sender, KeyEventArgs e)
		{

		}
	}
}
