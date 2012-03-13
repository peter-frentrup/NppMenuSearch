using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Globalization;

namespace NppMenuSearch
{
	public class DialogItem : HierarchyItem
	{
		public uint ControlId;

		public DialogItem(XmlElement dialog)
		{
			Text = "";
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

	}
}
