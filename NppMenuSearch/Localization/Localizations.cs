using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace NppMenuSearch.Localization
{
    internal class Localizations
    {
        public LocalizedStrings Strings = new LocalizedStrings();

        public XmlDocument NativeLang { get; private set; } = null;
        public event EventHandler NativeLangChanged;

        public Localizations()
        {
            LoadNativeLangXml();
            Main.NppListener.AfterReloadNativeLang += Listener_AfterReloadNativeLang;
        }

        private void Listener_AfterReloadNativeLang(object sender, EventArgs e)
        {
            if (LoadNativeLangXml())
                NativeLangChanged?.Invoke(null, EventArgs.Empty);
        }

        private bool LoadNativeLangXml()
        {
            string nativeLangFile = Main.GetNativeLangXml();

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(nativeLangFile);

                NativeLang = doc;
                string origFileName = NativeLang.SelectSingleNode("/NotepadPlus/Native-Langue").Attributes["filename"].Value;
                Strings = OpenPluginLocalization(origFileName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        private static LocalizedStrings OpenPluginLocalization(string nppNativeLangOrigFileName)
        {
            // TODO: load from some XML file
            switch (nppNativeLangOrigFileName)
            {
                case "german.xml":
                    return new LocalizedStrings()
                    {
                        SearchWidgetTitle = "Notepad++ durchsuchen",
                        MenuTitle_RepeatCommand_Previous = "Letztes Kommando wiederholen",
                        MenuTitle_RepeatCommand_arg = "Kommando wiederholen: „{0}“",
                        GroupTitle_RecentlyUsed = "Zuletzt verwendet",
                        GroupTitle_Menu = "Menü",
                        GroupTitle_Preferences = "Optionen",
                        GroupTitle_OpenFiles = "Geöffnete Dateien",
                        SwitchGroupHelpText = "TAB wechselt Gruppen: Zuletzt verwendet ↔ Menü ↔ Geöffnete Dateien ↔ Optionen",
                        ShortcutHelpText_arg = "{0} für alle Ergebnisse."
                    };

                default: return new LocalizedStrings();
            }
        }

    }
}
