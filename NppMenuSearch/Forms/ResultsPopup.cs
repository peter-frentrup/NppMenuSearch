using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using NppPluginNET;

namespace NppMenuSearch.Forms
{
	public partial class ResultsPopup : Form
	{
		const int DefaultMaxMenuResults 	   = 12;
		const int DefaultMaxPreferencesResults = 7;
		const int RecentlyUsedListCount 	   = 5;
		const int BlinkRepeat 				   = 4;

		int MaxMenuResults 		  = DefaultMaxMenuResults;
		int MaxPreferencesResults = DefaultMaxPreferencesResults;

		public event EventHandler Finished;

		ListViewGroup resultGroupRecentlyUsed = new ListViewGroup("Recently Used", HorizontalAlignment.Left);
		ListViewGroup resultGroupMenu 		  = new ListViewGroup("Menu", 		   HorizontalAlignment.Left);
		ListViewGroup resultGroupPreferences  = new ListViewGroup("Preferences",   HorizontalAlignment.Left);

		public 	TextBox    OwnerTextBox;
		public 	MenuItem   MainMenu;
		private DialogItem PreferenceDialog;
		private bool isPreferenceDialogValid;

		public ResultsPopup()
		{
			InitializeComponent();

			viewResults.Groups.Add(resultGroupRecentlyUsed);
			viewResults.Groups.Add(resultGroupMenu);
			viewResults.Groups.Add(resultGroupPreferences);
			
			MainMenu = new MenuItem(IntPtr.Zero);
			PreferenceDialog = new DialogItem("Preferences");

			// Lazy initializing the dialog on first search then steals the keyboard focus :( So do it here.
			NeedPreferencesDialog();

			Main.NppListener.AfterReloadNativeLang += new EventHandler(NppListener_AfterReloadNativeLang);

			Main.MakeNppOwnerOf(this);

			viewResults.ContextMenu = popupMenu;
		}

		void NppListener_AfterReloadNativeLang(object sender, EventArgs e)
		{
			isPreferenceDialogValid = false;
		}

		protected void NeedPreferencesDialog()
		{
			if (isPreferenceDialogValid)
				return;

			isPreferenceDialogValid = true;

			PreferenceDialogHelper pdh = new PreferenceDialogHelper();
			pdh.LoadCurrentLocalization();

			IntPtr hwndDialogPage;
			PreferenceDialog = new DialogItem(pdh.PageTranslations[pdh.Global.InternalName]);

			hwndDialogPage = DialogHelper.LoadNppDialog(Handle, (int)pdh.Global.ResourceId);
			try
			{
				PreferenceDialog = DialogItem.CreateFromDialogFlat(hwndDialogPage, pdh.PageTranslations[pdh.Global.InternalName]);
			}
			finally
			{
				DialogHelper.DestroyWindow(hwndDialogPage);
			}

			foreach (var pageInfo in pdh.GetPages())
			{
				hwndDialogPage = DialogHelper.LoadNppDialog(Handle, (int)pageInfo.ResourceId);
				try
				{
					DialogItem pageItem = DialogItem.CreateFromDialogFlat(hwndDialogPage, pdh.PageTranslation(pageInfo.InternalName));

					pageItem.ReorderItemsByGroupBoxes(hwndDialogPage);

					PreferenceDialog.AddItem(pageItem);
				}
				finally
				{
					DialogHelper.DestroyWindow(hwndDialogPage);
				}
			}

			PreferenceDialog.Translate(pdh.ControlTranslations);
			PreferenceDialog.RemoveRedundantHeadings();
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case Win32.WM_ACTIVATEAPP:
					if (m.WParam == IntPtr.Zero)
						Hide();
					break;

				case Win32.WM_MOUSEACTIVATE:
					m.Result = (IntPtr)Win32.MA_ACTIVATE;
					return;

				case Win32.WM_ACTIVATE:
					if (((int)m.WParam & 0xFFFF) == Win32.WA_CLICKACTIVE)
					{
						m.Result = IntPtr.Zero;
						return;
					}
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

		public void ShowMoreResults()
		{
			MaxMenuResults 		  = int.MaxValue;
			MaxPreferencesResults = int.MaxValue;
			panInfo.Visible 	  = false;
			RebuildResultsList();
		}

		private void ResultsPopup_VisibleChanged(object sender, EventArgs e)
		{
			if (Visible)
			{
				MaxMenuResults = DefaultMaxMenuResults;
				MaxPreferencesResults = DefaultMaxPreferencesResults;
				panInfo.Visible = true;

				MainMenu = new MenuItem(Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_INTERNAL_GETMENU, 0, 0));
				NeedPreferencesDialog();

				OwnerTextBox.TextChanged += OwnerTextBox_TextChanged;
				OwnerTextBox.KeyDown 	 += OwnerTextBox_KeyDown;
				RebuildResultsList();
			}
			else
			{
				OwnerTextBox.TextChanged -= OwnerTextBox_TextChanged;
				OwnerTextBox.KeyDown 	 -= OwnerTextBox_KeyDown;
			}
		}

		void OwnerTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Down:
					e.Handled = true;
					if (viewResults.Items.Count > 0)
					{
						if (viewResults.SelectedItems.Count == 0)
							viewResults.Items[0].Selected = true;
						else if(viewResults.SelectedIndices[0] + 1 < viewResults.Items.Count)
							viewResults.Items[viewResults.SelectedIndices[0] + 1].Selected = true;
						else
							viewResults.Items[0].Selected = true;

						viewResults.SelectedItems[0].EnsureVisible();
					}
					break;

				case Keys.Up:
					e.Handled = true;
					if (viewResults.Items.Count > 0)
					{
						if (viewResults.SelectedItems.Count == 0)
							viewResults.Items[0].Selected = true;
						else if (viewResults.SelectedIndices[0] > 0)
							viewResults.Items[viewResults.SelectedIndices[0] - 1].Selected = true;
						else
							viewResults.Items[viewResults.Items.Count - 1].Selected = true;

						viewResults.SelectedItems[0].EnsureVisible();
					}
					break;

				case Keys.Tab:
					e.Handled = true;
					if (viewResults.Items.Count > 0)
					{
						int groupIndex = viewResults.Groups.IndexOf(viewResults.SelectedItems[0].Group);
						if (e.Shift)
						{
							--groupIndex;
							while (groupIndex >= 0 && viewResults.Groups[groupIndex].Items.Count == 0)
								--groupIndex;

							if (groupIndex < 0)
							{
								groupIndex = viewResults.Groups.Count - 1;
								while (groupIndex >= 0 && viewResults.Groups[groupIndex].Items.Count == 0)
									--groupIndex;
							}

							if (groupIndex >= 0)
								viewResults.Groups[groupIndex].Items[0].Selected = true;

							viewResults.SelectedItems[0].EnsureVisible();
						}
						else
						{
							++groupIndex;
							while (groupIndex < viewResults.Groups.Count && viewResults.Groups[groupIndex].Items.Count == 0)
								++groupIndex;

							if (groupIndex >= viewResults.Groups.Count)
							{
								groupIndex = 0;
								while (groupIndex < viewResults.Groups.Count && viewResults.Groups[groupIndex].Items.Count == 0)
									++groupIndex;
							}

							if (groupIndex < viewResults.Groups.Count)
								viewResults.Groups[groupIndex].Items[0].Selected = true;

							viewResults.SelectedItems[0].EnsureVisible();
						}
					}
					break;

				case Keys.Enter:
					e.Handled = true;
					ItemSelected();
					break;

				case Keys.Apps:
					if(viewResults.SelectedItems.Count > 0){
						var item = viewResults.SelectedItems[0];

						e.Handled = true;
						popupMenu.Show(viewResults, new Point(item.Bounds.Right, item.Bounds.Bottom), LeftRightAlignment.Right);
					}
					break;
			}
		}

		void RebuildResultsList()
		{
			var words = OwnerTextBox.Text.SplitAt(' ');

			MenuItem[] menuItems = MainMenu
				.EnumFinalItems()
				.Select(item => new KeyValuePair<double, HierarchyItem>(item.MatchingSimilarity(words), item))
				.Where(kv => kv.Key > 0.0)
				.OrderByDescending(kv => kv.Key)
				//.Take(MaxMenuResults)
				.Select(kv => (MenuItem)kv.Value)
				.ToArray();

			DialogItem[] prefDialogItems = PreferenceDialog
				.EnumFinalItems()
				.Select(item => new KeyValuePair<double, HierarchyItem>(item.MatchingSimilarity(words), item))
				.Where(kv => kv.Key > 0.0)
				.OrderByDescending(kv => kv.Key)
				//.Take(MaxPreferencesResults)
				.Select(kv => (DialogItem)kv.Value)
				.ToArray();

			HierarchyItem[] recentlyUsed = Main.RecentlyUsedCommands
				.Select(id =>
					(HierarchyItem)menuItems.Where(      item => item.CommandId == id).FirstOrDefault() ??
					(HierarchyItem)prefDialogItems.Where(item => item.ControlId == id).FirstOrDefault())
				.Where(item=>item != null)
				.Take(RecentlyUsedListCount)
				.ToArray();

			viewResults.Items.Clear();

			resultGroupMenu.Header 		  = string.Format("Menu ({0})", 	   menuItems.Length 	  - recentlyUsed.Where(hi => hi is MenuItem).Count());
			resultGroupPreferences.Header = string.Format("Preferences ({0})", prefDialogItems.Length - recentlyUsed.Where(hi => hi is DialogItem).Count());

			foreach (var hi in recentlyUsed)
			{
				ListViewItem item = new ListViewItem();
				item.Tag 		  = hi;
				item.Text 		  = hi + "";
				item.Group 		  = resultGroupRecentlyUsed;
				viewResults.Items.Add(item);
#if DEBUG
				item.Text = string.Format("[{1:0.0000}] {0}", hi, hi.MatchingSimilarity(words));
#endif
			}

			int i = 0;
			foreach (var item in menuItems)
			{
				if (recentlyUsed.Contains(item))
					continue;

				if (i++ == MaxMenuResults)
					break;

				ListViewItem lvitem = new ListViewItem();
				lvitem.Tag 			= item;
				lvitem.Text 		= item + "";
				lvitem.Group 		= resultGroupMenu;
				viewResults.Items.Add(lvitem);
#if DEBUG
				lvitem.Text 		= string.Format("[{1:0.0000}] {0}", item, item.MatchingSimilarity(words));
#endif
			}

			i = 0;
			foreach (var item in prefDialogItems)
			{
				if (recentlyUsed.Contains(item))
					continue;

				if (i++ == MaxPreferencesResults)
					break;

				ListViewItem lvitem = new ListViewItem();
				lvitem.Tag 			= item;
				lvitem.Text 		= item + "";
				lvitem.Group 	  	= resultGroupPreferences;
				viewResults.Items.Add(lvitem);
#if DEBUG
				lvitem.Text 		= string.Format("[{1}] {0}", item, item.MatchingSimilarity(words));
#endif
			}

			if (viewResults.Items.Count > 0)
				viewResults.Items[0].Selected = true;
		}

		void OwnerTextBox_TextChanged(object sender, EventArgs e)
		{
			MaxMenuResults 		  = DefaultMaxMenuResults;
			MaxPreferencesResults = DefaultMaxPreferencesResults;
			panInfo.Visible 	  = true;

			RebuildResultsList();
		}

		void ItemSelected()
		{
			if (viewResults.SelectedItems.Count == 0)
				return;

			MenuItem menuItem = viewResults.SelectedItems[0].Tag as MenuItem;
			if (menuItem != null)
			{
				Main.RecentlyUsedCommands.Remove(menuItem.CommandId);
				Main.RecentlyUsedCommands.AddFirst(menuItem.CommandId);

				//Console.WriteLine("Selected {0}", item.CommandId);
				Win32.SendMessage(PluginBase.nppData._nppHandle, (NppMsg)Win32.WM_COMMAND, (int)menuItem.CommandId, 0);
				Hide();
				OwnerTextBox.Text = "";

				if (OwnerTextBox.Focused)
				{
					Win32.SetFocus(PluginBase.GetCurrentScintilla());
				}

				Main.RecalcRepeatLastCommandMenuItem();
				OnFinished();
				return;
			}

			DialogItem dialogItem = viewResults.SelectedItems[0].Tag as DialogItem;
			if (dialogItem != null)
			{
				Main.RecentlyUsedCommands.Remove(dialogItem.ControlId);
				Main.RecentlyUsedCommands.AddFirst(dialogItem.ControlId);

				OpenPreferences(dialogItem.ControlId);
				Hide();
				OwnerTextBox.Text = "";

				OnFinished();
				return;
			}
		}

		public void OnFinished()
		{
			if (Finished != null)
				Finished(this, new EventArgs());
		}

		public void Highlight(IntPtr hwnd)
		{
			int counter = 2 * BlinkRepeat;

			EventHandler tick = null;
			tick = (sender, e) =>
			{
				if (--counter == 0 || !Win32.IsWindowVisible(hwnd))
				{
					((Timer)sender).Stop();
					((Timer)sender).Tick -= tick;
				}

				RECT rect;
				Win32.GetClientRect(hwnd, out rect);
				IntPtr hdc = Win32.GetWindowDC(hwnd);
				{
					Win32.PatBlt(hdc,
						rect.Left,
						rect.Top,
						rect.Right - rect.Left,
						rect.Bottom - rect.Top,
						Win32.DSTINVERT);
				}
				Win32.ReleaseDC(hwnd, hdc);
			};

			timerBlink.Tick += tick;
			timerBlink.Start();
		}


		static IntPtr hwndPreferences = IntPtr.Zero;
		public static IntPtr FindPreferencesDialog()
		{
			if (hwndPreferences != IntPtr.Zero)
				return hwndPreferences;

			IntPtr hwndClosebutton;
			hwndPreferences = FindDialogByChildControlId(6001, out hwndClosebutton);
			return hwndPreferences;
		}

		public static IntPtr FindDialogByChildControlId(uint controlId, out IntPtr hwndControl)
		{
			IntPtr form = Win32.GetActiveWindow();//Win32.GetForegroundWindow();

			hwndControl = IntPtr.Zero;
			if (controlId == 0)
				return form;

			IntPtr control = IntPtr.Zero;
			Predicate<IntPtr> callback = hwndChild =>
			{
				if (Win32.GetDlgCtrlID(hwndChild) == controlId)
				{
					control = hwndChild;
					//hwndPreferences = form;
					return false;
				}
				return true;
			};

			Win32.EnumChildWindows(form, callback);
			//if (control == IntPtr.Zero && hwndPreferences != IntPtr.Zero)
			//{
			//	form = hwndPreferences;
			//	Win32.EnumChildWindows(form, callback);
			//}

			hwndControl = control;
			return form;
		}

		public void OpenPreferences(uint destinationControlId)
		{
			/* WM_TIMER messages have the lowest priority, so the following EventHandler will be called 
			 * (immediately) after the Preferences Dialog is shown [becuase we use a tick count of 1ms]
			 * 
			 * This does not work when the Preferences window is already visible, because it wont be 
			 * activated by Notepad++
			 */
			EventHandler tick = null;
			tick = (timer, ev) =>
			{
				((Timer)timer).Stop();
				((Timer)timer).Tick -= tick;

				IntPtr hwndDestinationControl;
				IntPtr hwndPreferences = FindDialogByChildControlId(destinationControlId, out hwndDestinationControl);

				if (hwndDestinationControl != IntPtr.Zero)
				{
					DialogHelper.NavigateToChild(hwndPreferences, hwndDestinationControl);
					if (Win32.IsWindowVisible(hwndDestinationControl))
					{
						Win32.SetFocus(hwndDestinationControl);
						Highlight(hwndDestinationControl);
					}
				}
			};

			timerIdle.Tick += tick;

			timerIdle.Start();
			Win32.SendMessage(PluginBase.nppData._nppHandle, (NppMsg)Win32.WM_COMMAND, (int)NppMenuCmd.IDM_SETTING_PREFERECE, 0);
		}

		private void viewResults_Resize(object sender, EventArgs e)
		{
			viewResults.TileSize = new Size(viewResults.ClientSize.Width - 20, viewResults.TileSize.Height);
		}

		private void viewResults_DrawItem(object sender, DrawListViewItemEventArgs e)
		{
			Color backgroundColor;
			Color foregroundColor;

			if (e.Item.Selected)
			{
				backgroundColor = Color.LightGray;
				foregroundColor = Color.Black;
			}
			else
			{
				backgroundColor = SystemColors.Window;
				foregroundColor = SystemColors.WindowText;
			}

			using (Brush background = new SolidBrush(backgroundColor))
			using (Brush foreground = new SolidBrush(foregroundColor))
			{
				Rectangle bounds 	 = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top, e.Bounds.Width - 10, e.Bounds.Height);
				Rectangle textBounds = new Rectangle(bounds.Left + 16, 	 bounds.Top,   bounds.Width - 16, 	bounds.Height);

				e.Graphics.FillRectangle(background, bounds);

				StringFormat format = new StringFormat(
					StringFormatFlags.NoWrap | StringFormatFlags.NoClip | StringFormatFlags.FitBlackBox);
				format.SetTabStops(20f, new float[] { 20f });

				if (e.Item.Tag is DialogItem)
				{
					e.Graphics.DrawImage(
						Properties.Resources.Gear,
						bounds.Left,
						bounds.Top);
				}

				e.Graphics.DrawString(
					e.Item.Text,
					e.Item.Font ?? e.Item.ListView.Font,
					foreground,
					textBounds.Location,
					format);
			}
		}

		private void viewResults_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ItemSelected();
				return;
			}

			//OwnerTextBox.Focus();
		}

		private void menuGotoShortcutDefinition_Click(object sender, EventArgs e)
		{
			if (viewResults.SelectedItems.Count == 0)
				return;

			MenuItem menuItem = viewResults.SelectedItems[0].Tag as MenuItem;
			if (menuItem != null)
			{
				OpenShortcutMapper(menuItem);

				OnFinished();
				return;
			}

			Win32.MessageBeep(Win32.BeepType.MB_ICONERROR);
		}

		private void OpenShortcutMapper(MenuItem menuItem)
		{
			Console.WriteLine("search shortcut for {0} ({1})", menuItem.CommandId, menuItem);

			Hide();
			OwnerTextBox.Text = "";

			EventHandler tick = null;
			tick = (timer, ev) =>
			{
				((Timer)timer).Stop();
				((Timer)timer).Tick -= tick;

				IntPtr hwndGrid;
				IntPtr hwndShortcutMapper = FindDialogByChildControlId(ShortcutMapperUtil.IDD_BABYGRID_ID1, out hwndGrid);

				if (hwndShortcutMapper != IntPtr.Zero && hwndGrid != IntPtr.Zero)
				{
					if (ShortcutMapperUtil.GotoGridItem(hwndShortcutMapper, hwndGrid, menuItem))
						return;
				}

				Win32.MessageBeep(Win32.BeepType.MB_ICONERROR);
			};

			timerIdle.Tick += tick;

			timerIdle.Start();
			Win32.SendMessage(PluginBase.nppData._nppHandle, (NppMsg)Win32.WM_COMMAND, (int)NppMenuCmd.IDM_SETTING_SHORTCUT_MAPPER, 0);
		}

		private void popupMenu_Popup(object sender, EventArgs e)
		{
			menuGotoShortcutDefinition.Enabled = false;
			menuOpenDialog.Visible = false;

			if (viewResults.SelectedItems.Count > 0)
			{
				MenuItem menuItem = viewResults.SelectedItems[0].Tag as MenuItem;
				if (menuItem != null)
				{
					menuGotoShortcutDefinition.Enabled = true;
				}

				DialogItem dialogItem = viewResults.SelectedItems[0].Tag as DialogItem;
				if (dialogItem != null)
				{
					menuOpenDialog.Visible = true;
				}
			}

			menuExecuteMenuItem.Visible = !menuOpenDialog.Visible;
		}

		private void menuExecuteMenuItem_Click(object sender, EventArgs e)
		{
			ItemSelected();
		}

		private void menuOpenDialog_Click(object sender, EventArgs e)
		{
			ItemSelected();
		}
	}
}
