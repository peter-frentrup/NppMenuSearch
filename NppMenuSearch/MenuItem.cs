using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NppPluginNET;

namespace NppMenuSearch
{
	public class MenuItem
	{
		public MenuItem Parent;

		public string Text;
		public string Shortcut;
		public uint   CommandId;

		public List<MenuItem> Submenu;

		public MenuItem(IntPtr hmenu)
		{
			Text	  = "";
			Shortcut  = "";
			CommandId = 0;

			Submenu = new List<MenuItem>();

			int count = Win32.GetMenuItemCount(hmenu);
			for (int i = 0; i < count; ++i)
			{
				string text = Win32.GetMenuItemString(hmenu, (uint)i, true);
				if (text == null)
					continue;

				uint   id  = Win32.GetMenuItemId(hmenu, (uint)i, true);
				IntPtr sub = Win32.GetSubMenu(hmenu, (uint)i, true);

				MenuItem item  = new MenuItem(sub);
				item.Parent    = this;
				item.Text 	   = text.Before("\t");
				item.Shortcut  = text.After("\t");
				item.CommandId = id;
				Submenu.Add(item);
			}
		}

		public IEnumerable<MenuItem> EnumFinalItems()
		{
			if (Submenu.Any())
			{
				foreach (MenuItem sub in Submenu)
					foreach (MenuItem final in sub.EnumFinalItems())
						yield return final;
			}
			else
				yield return this;
		}

		public double MatchingSimilarity(IEnumerable<string> words)
		{
			string text 				 = Text.Replace("&", "");
			bool[] matched 				 = new bool[text.Length];
			int    wordCharsCount 		 = 0;
			int    matchedWordCharsCount = 0;

			foreach (string word in words)
			{
				wordCharsCount+= word.Length;
				int pos = text.IndexOf(word, 0, text.Length, StringComparison.InvariantCultureIgnoreCase);
				if (pos >= 0)
				{
					matchedWordCharsCount+= word.Length;
					for (int i = pos; i < pos + word.Length; ++i)
						matched[i] = true;
				}
			}

			if (wordCharsCount == 0)
				return 0.0;

			int matchableCharCount = text.Length;
			int matchedCharCount   = 0;

			for (int i = 0; i < text.Length; ++i)
			{
				if (matched[i])
					++matchedCharCount;
				else if (text[i] <= ' ')
					--matchableCharCount;
			}

			matched = null;

			if (matchableCharCount < 1)
				matchableCharCount = 1;

			double result = matchedCharCount / (double)matchableCharCount;
			result *= matchedWordCharsCount / (double)wordCharsCount;

			if (Parent != null)
				result = result * 0.625 + 0.375 * Parent.MatchingSimilarity(words);

			return result;
		}

		public override string ToString()
		{
			string result = "";

			if (Parent != null && Parent.Text.Length > 0)
			{
				result = Parent.ToString() + " → ";
			}

			result += Text.Replace("&", "");

			if (Shortcut.Length > 0)
			{
				result += " (" + Shortcut + ")";
			}

			return result;
		}
	}
}
