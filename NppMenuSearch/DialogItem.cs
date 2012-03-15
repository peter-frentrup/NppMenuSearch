using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Globalization;
using NppPluginNET;
using System.Drawing;

namespace NppMenuSearch
{
	public class DialogItem : HierarchyItem
	{
		public uint ControlId;

		public DialogItem()
		{
			ControlId = 0;
			Text 	  = "";
		}

		public DialogItem(IntPtr hwnd)
		{
			ControlId = (uint)Win32.GetDlgCtrlID(hwnd);

			{
				StringBuilder sb = new StringBuilder(Win32.GetWindowTextLength(hwnd) + 1);
				Win32.GetWindowText(hwnd, sb, sb.Capacity);
				Text = sb.ToString();
			}

			var groupBoxes = new Dictionary<IntPtr, KeyValuePair<DialogItem, Rectangle>>();
			Win32.EnumChildWindows(hwnd, descendent =>
			{
				if (Win32.GetParent(descendent) == hwnd)
				{
					if ((Win32.GetWindowLong(descendent, Win32.GWL_STYLE) & Win32.BS_TYPEMASK) == Win32.BS_GROUPBOX)
					{
						StringBuilder sb = new StringBuilder(256);
						Win32.GetClassName(descendent, sb, sb.Capacity);

						if (sb.ToString() == "Button")
						{
							RECT winRect;
							Win32.GetWindowRect(descendent, out winRect);
							
							Rectangle rect = new Rectangle(winRect.Left, winRect.Top, winRect.Right - winRect.Left, winRect.Bottom - winRect.Top);

							DialogItem item = new DialogItem(descendent);
							groupBoxes.Add(descendent, new KeyValuePair<DialogItem, Rectangle>(item, rect));
						}
					}
				}

				return true;
			});

			foreach (var group in groupBoxes)
			{
				AddItem(group.Value.Key);
			}


			Win32.EnumChildWindows(hwnd, descendent =>
				{
					if (Win32.GetParent(descendent) == hwnd)
					{
						if (!groupBoxes.ContainsKey(descendent))
						{
							DialogItem item = new DialogItem(descendent);
							if (item.Text == "")
								return true;

							if (item.ControlId == 0 && !item.EnumItems().Any())
								return true;
							
							RECT winRect;
							Win32.GetWindowRect(descendent, out winRect);

							Rectangle rect = new Rectangle(winRect.Left, winRect.Top, winRect.Right - winRect.Left, winRect.Bottom - winRect.Top);

							foreach (var group in groupBoxes)
							{
								if (group.Value.Value.IntersectsWith(rect))
								{
									group.Value.Key.AddItem(item);
									return true;
								}
							}

							AddItem(item);
						}
					}

					return true;
				});
		}

		public DialogItem(XmlElement dialog)
		{
			Text 	  = "";
			ControlId = 0;

			if (dialog.HasAttribute("id"))
			{
				uint.TryParse(
					dialog.GetAttribute("id"),
					System.Globalization.NumberStyles.Number,
					CultureInfo.InvariantCulture.NumberFormat,
					out ControlId);
			}

			if (dialog.HasAttribute("name"))
				Text = dialog.GetAttribute("name");

			if (dialog.HasAttribute("title"))
			{
				Text = dialog.GetAttribute("title");

				foreach (XmlElement item in dialog.ChildNodes)
				{
					AddItem(new DialogItem(item));
				}
			}
		}

		public void ReorderItemsByGroupBoxes(IntPtr hwndDialog)
		{
			var groupBoxes = new Dictionary<DialogItem, Rectangle>();
			var otherItems = new Dictionary<DialogItem, Rectangle>();

			Win32.EnumChildWindows(hwndDialog, descendent =>
			{
				if (Win32.GetParent(descendent) == hwndDialog)
				{
					RECT winRect;
					Win32.GetWindowRect(descendent, out winRect);

					Rectangle rect = new Rectangle(winRect.Left, winRect.Top, winRect.Right - winRect.Left, winRect.Bottom - winRect.Top);

					uint id = (uint)Win32.GetDlgCtrlID(descendent);
					DialogItem item = EnumItems().Cast<DialogItem>().Where(i => i.ControlId == id).FirstOrDefault();

					if(item == null)
						return true;

					if ((Win32.GetWindowLong(descendent, Win32.GWL_STYLE) & Win32.BS_TYPEMASK) == Win32.BS_GROUPBOX)
					{
						StringBuilder sb = new StringBuilder(256);
						Win32.GetClassName(descendent, sb, sb.Capacity);

						if (sb.ToString() == "Button")
						{
							groupBoxes.Add(item, rect);
							return true;
						}
					}

					otherItems.Add(item, rect);
				}

				return true;
			});

			foreach (var other in otherItems)
			{
				foreach (var group in groupBoxes)
				{
					if (group.Value.Contains(other.Value))
					{
						RemoveItem(other.Key);
						group.Key.AddItem(other.Key);
					}
				}
			}
		}
	}
}
