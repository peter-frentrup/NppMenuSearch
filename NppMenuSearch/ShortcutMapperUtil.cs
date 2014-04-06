using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NppPluginNET;
using System.Windows.Forms;

namespace NppMenuSearch
{
	class ShortcutMapperUtil
	{
		public const uint IDD_SHORTCUTMAPPER_DLG =     2600;
		public const uint IDD_BABYGRID_ID1      =(IDD_SHORTCUTMAPPER_DLG + 1);
		public const uint IDM_BABYGRID_MODIFY  = (IDD_SHORTCUTMAPPER_DLG + 2);
		public const uint IDM_BABYGRID_DELETE  = (IDD_SHORTCUTMAPPER_DLG + 3);
		public const uint IDC_BABYGRID_TABBAR = (IDD_SHORTCUTMAPPER_DLG + 4);

		static IDictionary<NppMenuCmd, string> MenuCommandsToSciCmd = GetMenuCommandsToSciCmd();

		public static bool GotoGridItem(IntPtr hwndShortcutMapper, IntPtr hwndGrid, MenuItem menuItem)
		{
			if (Win32.GetClassName(hwndGrid) != "BABYGRID")
				return false;

			IntPtr hwndTab = GetTabBar(hwndShortcutMapper);

			if (hwndTab != IntPtr.Zero)
			{
				if (IsMacroMenuItem(menuItem))
					DialogHelper.ChangeTabPage(hwndShortcutMapper, hwndTab, 1);
				else if (IsUserMenuItem(menuItem))
					DialogHelper.ChangeTabPage(hwndShortcutMapper, hwndTab, 2);
				else if (IsPluginMenuItem(menuItem))
					DialogHelper.ChangeTabPage(hwndShortcutMapper, hwndTab, 3);
				else if (IsScintillaMenuItem(menuItem))
					DialogHelper.ChangeTabPage(hwndShortcutMapper, hwndTab, 4);
			}

			string searchText = menuItem.Text.Replace("&", "");
			if (menuItem.Parent != null)
			{
				switch (menuItem.Parent.Text)
				{
					case "TextFX Convert":
						searchText = "C:" + searchText;
						break;

					case "TextFX HTML Tidy":
						searchText = "D:" + searchText;
						break;

					case "TextFX Edit":
						searchText = "E:" + searchText;
						break;

					case "TextFX Insert":
						searchText = "I:" + searchText;
						break;

					case "TextFX Quick":
						searchText = "Q:" + searchText;
						break;

					case "TextFX Settings":
						searchText = "S:" + searchText;
						break;

					case "TextFX Tools":
						searchText = "T:" + searchText;
						break;

					case "TextFX Viz":
						searchText = "V:" + searchText;
						break;

					case "TextFX Viz Settings":
						searchText = "W:" + searchText;
						break;
				}
			}

			if (IsScintillaMenuItem(menuItem))
			{
				searchText = MenuCommandsToSciCmd[(NppMenuCmd)menuItem.CommandId];

				//Console.WriteLine("search for {0}", searchText);
			}

			List<int> possibleRows = FindRows(hwndGrid, searchText, partialMatch: false).ToList();
			if (possibleRows.Count != 1 && menuItem.Shortcut != "")
			{
				var newRows = new List<int>();

				StringBuilder sb = new StringBuilder(1000);
				foreach (int row in possibleRows)
				{
					_BGCELL cell;
					cell.Column = 2;
					cell.Row = row;
					Win32.SendMessage(hwndGrid, BabyGridMsg.BGM_GETCELLDATA, ref cell, sb);

					string shortcut = sb.ToString();

					if (shortcut == menuItem.Shortcut)
					{
						newRows.Add(row);
					}
				}

				if (newRows.Count == 1)
				{
					possibleRows = newRows;
				}
				else
				{
					possibleRows = FindRows(hwndGrid, menuItem.Shortcut, partialMatch: false, column: 2).ToList();
				}
			}

			if (possibleRows.Count == 1)
			{
				int row = possibleRows[0];

				int currentRow = (int)Win32.SendMessage(hwndGrid, BabyGridMsg.BGM_GETROW, 0, 0);

				while (currentRow < row)
				{
					++currentRow;
					Win32.SendMessage(hwndGrid, Win32.WM_KEYDOWN, (int)Keys.Down, IntPtr.Zero);
				}

				while (currentRow > row)
				{
					--currentRow;
					Win32.SendMessage(hwndGrid, Win32.WM_KEYDOWN, (int)Keys.Down, IntPtr.Zero);
				}

				Win32.SetFocus(hwndGrid);
				return true;
			}
			else
			{
				if (possibleRows.Count > 1)
				{
					StringBuilder sb = new StringBuilder();
					sb.Append(Win32.GetWindowText(hwndShortcutMapper));
					sb.AppendFormat(" (matches in rows {0}", possibleRows[0]);
					for (int i = 1; i < possibleRows.Count; ++i)
					{
						if (i + 1 < possibleRows.Count)
							sb.AppendFormat(", {0}", possibleRows[i]);
						else
							sb.AppendFormat(" and {0}", possibleRows[i]);
					}
					sb.Append(")");

					Win32.SetWindowText(hwndShortcutMapper, sb.ToString());
				}

				Console.WriteLine("{0} matches found", possibleRows.Count);
				foreach (var row in possibleRows)
				{
					Console.WriteLine("  in row {0}", row);
				}
			}

			return false;
		}

		private static bool IsMacroMenuItem(MenuItem menuItem)
		{
			return menuItem.CommandId >= (uint)NppMenuCmd.ID_MACRO &&
					menuItem.CommandId <= (uint)NppMenuCmd.ID_MACRO_LIMIT;
		}

		private static bool IsUserMenuItem(MenuItem menuItem)
		{
			return menuItem.CommandId >= (uint)NppMenuCmd.ID_USER_CMD &&
					menuItem.CommandId <= (uint)NppMenuCmd.ID_USER_CMD_LIMIT;
		}

		private static bool IsPluginMenuItem(MenuItem menuItem)
		{
			return menuItem.CommandId >= (uint)NppMenuCmd.ID_PLUGINS_CMD &&
					menuItem.CommandId <= (uint)NppMenuCmd.ID_PLUGINS_CMD_LIMIT;
		}

		private static bool IsScintillaMenuItem(MenuItem menuItem)
		{
			return MenuCommandsToSciCmd.ContainsKey((NppMenuCmd)menuItem.CommandId);
		}

		private static IEnumerable<int> FindRows(IntPtr hwndGrid, string searchText, bool partialMatch = false, int column = 1)
		{
			int rows = (int)Win32.SendMessage(hwndGrid, BabyGridMsg.BGM_GETROWS, 0, 0);
			//Console.WriteLine("# of rows: {0}", rows);

			StringBuilder sb = new StringBuilder(1000);
			for (int i = 1; i <= rows; ++i)
			{
				_BGCELL cell;
				cell.Column = column;
				cell.Row = i;
				Win32.SendMessage(hwndGrid, BabyGridMsg.BGM_GETCELLDATA, ref cell, sb);

				string name = sb.ToString();

				if (name == searchText)
				{
					yield return i;
				}
				else if (partialMatch && name.IndexOf(searchText) >= 0)
				{
					yield return i;
				}
			}
		}

		private static IntPtr GetTabBar(IntPtr hwndShortcutMapper)
		{
			IntPtr hwndTab = IntPtr.Zero;
			Win32.EnumChildWindows(hwndShortcutMapper, hwndFormChild =>
			{
				//if (!Win32.IsWindowVisible(hwndFormChild))
				//	return true;

				if (Win32.GetWindowLong(hwndFormChild, Win32.GWL_ID) != IDC_BABYGRID_TABBAR)
					return true;

				if (Win32.GetClassName(hwndFormChild) == "SysTabControl32")
				{
					hwndTab = hwndFormChild;
					return false;
				}

				return true;
			});
			return hwndTab;
		}

		static IDictionary<NppMenuCmd, string> GetMenuCommandsToSciCmd()
		{
			var dict = new Dictionary<NppMenuCmd, string>();

			dict[NppMenuCmd.IDM_EDIT_CUT] = "SCI_CUT";
			dict[NppMenuCmd.IDM_EDIT_COPY] = "SCI_COPY";
			dict[NppMenuCmd.IDM_EDIT_PASTE] = "SCI_PASTE";
			dict[NppMenuCmd.IDM_EDIT_SELECTALL] = "SCI_SELECTALL";
			dict[NppMenuCmd.IDM_EDIT_DELETE] = "SCI_CLEAR";
			// SCI_CLEARALL
			dict[NppMenuCmd.IDM_EDIT_UNDO] = "SCI_UNDO";
			dict[NppMenuCmd.IDM_EDIT_REDO] = "SCI_REDO";
			// SCI_NEWLINE
			dict[NppMenuCmd.IDM_EDIT_INS_TAB] = "SCI_TAB";
			dict[NppMenuCmd.IDM_EDIT_RMV_TAB] = "SCI_BACKTAB";
			// SCI_FORMFEED
			dict[NppMenuCmd.IDM_VIEW_ZOOMIN] = "SCI_ZOOMIN";
			dict[NppMenuCmd.IDM_VIEW_ZOOMOUT] = "SCI_ZOOMOUT";
			dict[NppMenuCmd.IDM_VIEW_ZOOMRESTORE] = "SCI_SETZOOM";
			dict[NppMenuCmd.IDM_EDIT_DUP_LINE] = "SCI_SELECTIONDUPLICATE";
			// ...

			// TODO: update when N++ adds more menu items for scintilla commands

			return dict;
		}
	}
}
