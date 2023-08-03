using System;
using System.IO;

namespace NppMenuSearch
{
    public class TabItem
    {
        public int ViewNumber { get; set; }
        public int Index { get; set; }
        public string FullFileName { get; set; }

        public bool MatchesSearchTerm(string search)
        {
            if (FullFileName == null)
                return false;

            return Path.GetFileName(FullFileName).IndexOf(search, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        public override string ToString()
        {
            if (FullFileName == null)
                return "";

            return $"{Path.GetFileName(FullFileName)} ({Path.GetDirectoryName(FullFileName)})";
        }
    }
}
