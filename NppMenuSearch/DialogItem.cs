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

		public DialogItem(XmlElement dialog)
		{
			Text 	  = "";
			ControlId = 0;

			if (dialog.HasAttribute("id"))
			{
				if (!uint.TryParse(
						dialog.GetAttribute("id"),
						System.Globalization.NumberStyles.Number,
						CultureInfo.InvariantCulture.NumberFormat,
						out ControlId))
				{
					int id = 0;
					int.TryParse(
						dialog.GetAttribute("id"),
						System.Globalization.NumberStyles.Number,
						CultureInfo.InvariantCulture.NumberFormat,
						out id);

					ControlId = (uint)id;
				}
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
					if (id == 0)
						return true;

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

			foreach (var innerGroup in groupBoxes.OrderByDescending(kv => kv.Value.Width))
			{
				foreach (var outerGroup in groupBoxes.OrderBy(kv => kv.Value.Width))
				{
					if (outerGroup.Key == innerGroup.Key)
						continue;

					if (outerGroup.Key.Parent != this)
						continue;

					if (outerGroup.Value.Contains(innerGroup.Value))
					{
						innerGroup.Key.Parent.RemoveItem(innerGroup.Key);
						outerGroup.Key.AddItem(innerGroup.Key);
						break;
					}
				}
			}

			foreach (var other in otherItems)
			{
				foreach (var group in groupBoxes.OrderBy(kv => kv.Value.Width))
				{
					if (group.Value.Contains(other.Value))
					{
						RemoveItem(other.Key);
						group.Key.AddItem(other.Key);
						groupBoxes.Remove(other.Key);
						break;
					}
				}
			}
		}
	}
}
