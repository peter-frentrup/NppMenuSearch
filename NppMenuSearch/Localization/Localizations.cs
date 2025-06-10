using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            string langFile = Assembly.GetExecutingAssembly().Location + "." + nppNativeLangOrigFileName;

#if DEBUG
            Console.WriteLine($"OpenPluginLocalization: search '{langFile}'");
#endif

            if (File.Exists(langFile))
                return LoadStrings(langFile);

            return new LocalizedStrings();
        }

        private static LocalizedStrings LoadStrings(string translationXmlFile)
        {
            try
            {
                var translation = new LocalizedStrings();

                var doc = new XmlDocument();
                doc.Load(translationXmlFile);

                var root = (XmlElement)doc.SelectSingleNode("/NppMenuSearch/Native-Lang");

                TryRead(ref translation.SearchWidgetTitle,                root, "General/SearchWidgetTitle");
                TryRead(ref translation.MenuTitle_RepeatCommand_Previous, root, "MenuTitles/RepeatPreviousCommand");
                TryRead(ref translation.MenuTitle_RepeatCommand_arg,      root, "MenuTitles/RepeatCommand");
                TryRead(ref translation.MenuTitle_About,                  root, "MenuTitles/About");
                TryRead(ref translation.MenuTitle_FixWidgetSize,          root, "MenuTitles/FixWidgetSize");
                TryRead(ref translation.MenuTitle_ChangeShortcut,         root, "MenuTitles/ChangeShortcut");
                TryRead(ref translation.MenuTitle_Execute,                root, "MenuTitles/Execute");
                TryRead(ref translation.MenuTitle_SelectTab,              root, "MenuTitles/SelectTab");
                TryRead(ref translation.MenuTitle_OpenDialog,             root, "MenuTitles/OpenDialog");
                TryRead(ref translation.GroupTitle_RecentlyUsed,          root, "GroupTitles/RecentlyUsed");
                TryRead(ref translation.GroupTitle_Menu,                  root, "GroupTitles/Menu");
                TryRead(ref translation.GroupTitle_Preferences,           root, "GroupTitles/Preferences");
                TryRead(ref translation.GroupTitle_OpenFiles,             root, "GroupTitles/OpenFiles");
                TryRead(ref translation.SwitchGroupHelpText,              root, "Help/SwitchGroup");
                TryRead(ref translation.ShortcutHelpText_arg,             root, "Help/RepeatForAllResults");

                return translation;
            }
            catch(Exception ex)
            {
                return new LocalizedStrings();
            }
        }

        private static void TryRead(ref string text, XmlElement translations, string xpath)
        {
            var elem = translations.SelectNodes(xpath).OfType<XmlElement>().FirstOrDefault();
            if (elem != null)
                TryReadString(ref text, elem);
        }

        private static void TryReadString(ref string text, XmlElement elem)
        {
            if (elem == null)
                return;

            text = elem.InnerText;
        }
    }
}
