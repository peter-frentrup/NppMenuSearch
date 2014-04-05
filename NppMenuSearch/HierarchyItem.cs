using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NppMenuSearch
{
	public class HierarchyItem
	{
		public HierarchyItem Parent { get; protected set; }
		private List<HierarchyItem> subitems;

		public string Text;

		public int Count
		{
			get
			{
				if (subitems == null)
					return 0;
				return subitems.Count;
			}
		}

		public HierarchyItem this[int i]
		{
			get
			{
				return subitems[i];
			}
		}

		public HierarchyItem(string text = "")
		{
			Text = text;
		}

		public void AddItem(HierarchyItem item)
		{
			if (item == null)
				throw new ArgumentException("item");

			if (item.Parent != null)
				throw new InvalidOperationException("item.Parent must be null");

			if (subitems == null)
				subitems = new List<HierarchyItem>();

			item.Parent = this;
			subitems.Add(item);
		}

		public void RemoveItem(HierarchyItem item)
		{
			if (subitems.Remove(item))
			{
				item.Parent = null;
			}
		}

		public IEnumerable<HierarchyItem> EnumItems()
		{
			if (subitems == null)
				yield break;

			foreach (HierarchyItem sub in subitems)
				yield return sub;
		}

		public IEnumerable<HierarchyItem> EnumFinalItems()
		{
			if (EnumItems().Any())
			{
				foreach (HierarchyItem sub in EnumItems())
					foreach (HierarchyItem final in sub.EnumFinalItems())
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

			List<string> unmatchedWords = new List<string>();
			foreach (string word in words)
			{
				wordCharsCount+= word.Length;
				int pos = text.IndexOf(word, 0, text.Length, StringComparison.InvariantCultureIgnoreCase);
				if (pos >= 0)
				{
					matchedWordCharsCount += word.Length;
					for (int i = pos; i < pos + word.Length; ++i)
						matched[i] = true;
				}
				else
					unmatchedWords.Add(word);
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
				result = result * 0.5 + 0.5 * Parent.MatchingSimilarity(unmatchedWords);

			unmatchedWords.Clear();
			unmatchedWords = null;

			return result;
		}

		public HierarchyItem RemoveRedundantHeadings()
		{
			if (subitems == null)
				return this;

			for (int i = 0; i < subitems.Count; ++i)
			{
				var newItem = subitems[i].RemoveRedundantHeadings();
				if (newItem.Parent == null)
					newItem.Parent = this;

				if (newItem.Parent == this)
					subitems[i] = newItem;
			}

			if (subitems.Count == 1 && subitems[0].Text == Text)
			{
				var newItem = subitems[0];
				RemoveItem(newItem);
				return newItem;
			}

			return this;
		}

		public override string ToString()
		{
			string result = "";

			if (Parent != null)
			{
				result = Parent.ToString();

				if (result.Length > 0)
					result += " → ";
			}

			result += Text.Replace("&", "");

			return result;
		}
	}
}
