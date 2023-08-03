using System;
using System.IO;
using NppPluginNET;

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

        private string ViewName { get { return Index == (int)NppMsg.MAIN_VIEW ? "Primary View" : "Secondary View"; } }

        public string ToolTipText { get { return $"{ViewName}: {FullFileName}"; } }

        public override string ToString()
        {
            if (FullFileName == null)
                return "";

            return Path.GetFileName(FullFileName);
        }
    }
}
