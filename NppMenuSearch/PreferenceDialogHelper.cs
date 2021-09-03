using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using NppPluginNET;

namespace NppMenuSearch
{
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
                (uint)NppResources.IDD_PREFERENCE_SUB_EDITING,
                "Editing",
                "Scintillas");

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
                (uint)NppResources.IDD_PREFERENCE_SUB_HIGHLIGHTING,
                "Tab Settings",
                "TabSettings");

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

        public IDictionary<uint, string> ControlTranslations;
        public IDictionary<string, string> PageTranslations;

        public PreferenceDialogHelper()
        {
            ControlTranslations = new Dictionary<uint, string>();
            PageTranslations = new Dictionary<string, string>();

            PageTranslations[Global.InternalName] = Global.DefaultName;
            foreach (var info in GetPages())
            {
                PageTranslations[info.InternalName] = info.DefaultName;
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

                LoadLocalization((XmlElement)doc.SelectSingleNode("/NotepadPlus/Native-Langue/Dialog/Preference"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        protected void LoadLocalization(XmlElement xml)
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

                ControlTranslations[controlId] = xml.GetAttribute("name");
                return;
            }

            if (xml.HasAttribute("title"))
                PageTranslations[xml.Name] = xml.GetAttribute("title");

            foreach (var xmlChildNode in xml.ChildNodes)
            {
                XmlElement xmlChild = xmlChildNode as XmlElement;
                if (xmlChild != null)
                    LoadLocalization(xmlChild);
            }
        }
    }
}
