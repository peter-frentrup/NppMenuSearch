using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using NppPluginNET;

namespace NppMenuSearch
{
    public struct UniqueControlIdx : IEquatable<UniqueControlIdx>
    {
        public uint ControlId; // menu item ID or control ID within a page
        public uint PageIdx;   // index of a page (each page is a child dialog)

        public UniqueControlIdx(uint ctrlId, uint pageIdx)
        {
            ControlId = ctrlId;
            PageIdx = pageIdx;
        }

        public bool Equals(UniqueControlIdx other)
        {
            return ControlId == other.ControlId && PageIdx == other.PageIdx;
        }

        public override bool Equals(object obj)
        {
            if (obj is UniqueControlIdx other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ControlId.GetHashCode() ^ PageIdx.GetHashCode();
        }

        public static bool operator==(UniqueControlIdx left, UniqueControlIdx right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(UniqueControlIdx left, UniqueControlIdx right)
        {
            return !left.Equals(right);
        }
    }

    class PreferenceDialogHelper
    {
        public struct DialogInfo
        {
            public uint ResourceId;
            public string DefaultName;
            public string InternalName;

            public DialogInfo(uint resourceId, string defaultName, string internalName)
            {
                ResourceId = resourceId;
                DefaultName = defaultName;
                InternalName = internalName;
            }
        }

        public readonly DialogInfo Global = new DialogInfo(
            (uint)NppResources.IDD_PREFERENCE_BOX,
            "Preferences",
            "Preference");

        public IEnumerable<DialogInfo> GetPages()
        {
            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_GENRAL,
                "General",
                "Global");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_TOOLBAR,
                "Toolbar",
                "Toolbar");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_TABBAR,
                "Tab Bar",
                "Tabbar");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_EDITING,
                "Editing 1",
                "Scintillas");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_EDITING2,
                "Editing 2",
                "Scintillas2");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_DARKMODE,
                "Dark Mode",
                "DarkMode");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_MARGING_BORDER_EDGE,
                "Margins/Border/Edge",
                "MarginsBorderEdge");
            
            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_NEWDOCUMENT,
                "New Document",
                "NewDoc");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_DEFAULTDIRECTORY,
                "Default Directory",
                "DefaultDir");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_RECENTFILESHISTORY,
                "Recent Files History",
                "RecentFilesHistory"); // not used any more ...

            yield return new DialogInfo(
                (uint)NppResources.IDD_REGEXT_BOX,
                "File Association",
                "FileAssoc");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_LANGUAGE,
                "Language",
                "Language"); // was LangMenu

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_INDENTATION,
                "Indentation",
                "Indentation");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_HIGHLIGHTING,
                "Highlighting",
                "Highlighting");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_PRINT,
                "Print",
                "Print");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_SEARCHING,
                "Searching",
                "Searching");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_BACKUP,
                "Backup",
                "Backup");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_AUTOCOMPLETION,
                "Auto-Completion",
                "AutoCompletion");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_MULTIINSTANCE,
                "Multi-Instance",
                "MultiInstance");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_DELIMITER,
                "Delimiter",
                "Delimiter");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_PERFORMANCE,
                "Performance",
                "Performance");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_CLOUD_LINK,
                "Cloud & Link",
                "Cloud");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_SEARCHENGINE,
                "Search Engine",
                "SearchEngine");

            yield return new DialogInfo(
                (uint)NppResources.IDD_PREFERENCE_SUB_MISC,
                "MISC.",
                "MISC");
        }

        public IDictionary<UniqueControlIdx, string> ControlTranslations;
        public IDictionary<string, string> PageTranslations;
        private IDictionary<uint, string> PageIdxs;

        public PreferenceDialogHelper()
        {
            ControlTranslations = new Dictionary<UniqueControlIdx, string>();
            PageTranslations = new Dictionary<string, string>();
            PageIdxs = new Dictionary<uint, string>();

            uint pageIdx = 1;
            PageTranslations[Global.InternalName] = Global.DefaultName;
            foreach (var info in GetPages())
            {
                PageTranslations[info.InternalName] = info.DefaultName;
                PageIdxs[pageIdx] = info.InternalName;
                ++pageIdx;
            }
        }

        public string PageTranslation(string internalName)
        {
            string s;
            if (PageTranslations.TryGetValue(internalName, out s))
                return s;

            return internalName;
        }

        public void LoadCurrentLocalization()
        {
            string nativeLangFile = Main.GetNativeLangXml();

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(nativeLangFile);

                LoadLocalization((XmlElement)doc.SelectSingleNode("/NotepadPlus/Native-Langue/Dialog/Preference"), 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        protected void LoadLocalization(XmlElement xml, uint pageIdx)
        {
            if (xml.Name == "Item" && xml.HasAttribute("id") && xml.HasAttribute("name"))
            {
                uint controlId = 0;

                if (!uint.TryParse(
                        xml.GetAttribute("id"),
                        System.Globalization.NumberStyles.Number,
                        CultureInfo.InvariantCulture.NumberFormat,
                        out controlId))
                {
                    int id = 0;
                    int.TryParse(
                        xml.GetAttribute("id"),
                        System.Globalization.NumberStyles.Number,
                        CultureInfo.InvariantCulture.NumberFormat,
                        out id);

                    controlId = (uint)id;
                }

                ControlTranslations[new UniqueControlIdx(controlId, pageIdx)] = xml.GetAttribute("name");
                return;
            }

            string pageName = xml.Name;
            if (xml.HasAttribute("title"))
                PageTranslations[pageName] = xml.GetAttribute("title");

            pageIdx = GetPageIdx(pageName);

            foreach (var xmlChildNode in xml.ChildNodes)
            {
                XmlElement xmlChild = xmlChildNode as XmlElement;
                if (xmlChild != null)
                    LoadLocalization(xmlChild, pageIdx);
            }
        }

        public uint GetPageIdx(string pageInternalName)
        {
            return PageIdxs.FirstOrDefault(item => item.Value == pageInternalName).Key;
        }
    }
}
