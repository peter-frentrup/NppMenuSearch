using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;

namespace NppMenuSearch
{
    class Settings
    {
        public static void Load(string filename)
        {
            if (!File.Exists(filename))
                return;

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(filename);

                Main.RecentlyUsedCommands.Clear();
                XmlElement xmlRecentlyUsedItems = (XmlElement)doc.SelectSingleNode("/Settings/RecentlyUsedItems");
                foreach (XmlElement xmlItem in xmlRecentlyUsedItems.ChildNodes)
                {
                    string idString = xmlItem.GetAttribute("id");

                    uint uid;
                    if (uint.TryParse(idString, out uid))
                    {
                        Main.RecentlyUsedCommands.AddLast(uid);
                        continue;
                    }

                    int id;
                    if (int.TryParse(idString, out id))
                    {
                        Main.RecentlyUsedCommands.AddLast((uint)id);
                        continue;
                    }
                }

                Main.PreferredToolbarWidth = 0;
                {
                    XmlElement xmlPreferredToolbarWidth = doc.SelectNodes("/Settings/PreferredToolbarWidth")
                        .OfType<XmlElement>()
                        .FirstOrDefault();
                    if (xmlPreferredToolbarWidth != null && xmlPreferredToolbarWidth.HasAttribute("value"))
                    {
                        string widthString = xmlPreferredToolbarWidth.GetAttribute("value");
                        int width;
                        if (int.TryParse(widthString, out width) && width > 0)
                            Main.PreferredToolbarWidth = width;
                    }
                }

                Main.PreferredResultsWindowSize = new Size(0, 0);
                {
                    XmlElement xmlPreferredResultsWindowSize = doc.SelectNodes("/Settings/PreferredResultsWindowSize")
                        .OfType<XmlElement>()
                        .FirstOrDefault();
                    if (xmlPreferredResultsWindowSize != null && 
                        xmlPreferredResultsWindowSize.HasAttribute("width") &&
                        xmlPreferredResultsWindowSize.HasAttribute("height"))
                    {
                        string widthString = xmlPreferredResultsWindowSize.GetAttribute("width");
                        string heightString = xmlPreferredResultsWindowSize.GetAttribute("height");
                        int width, height;
                        if (int.TryParse(widthString, out width) &&
                            int.TryParse(heightString, out height) && 
                            width > 0 && height > 0)
                        {
                            Main.PreferredResultsWindowSize = new Size(width, height);
                        }
                    }
                }

                Main.IsDarkModeEnabled = false;
                {
                    XmlElement xmlIsDarkModeEnabled = doc.SelectNodes("/Settings/IsDarkModeEnabled")
                        .OfType<XmlElement>()
                        .FirstOrDefault();
                    if (xmlIsDarkModeEnabled != null && xmlIsDarkModeEnabled.HasAttribute("value"))
                    {
                        string enabledString = xmlIsDarkModeEnabled.GetAttribute("value");
                        bool isDarkModeEnabled;
                        if (bool.TryParse(enabledString, out isDarkModeEnabled))
                            Main.IsDarkModeEnabled = isDarkModeEnabled;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        public static void Save(string filename)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                var xmlRoot = doc.CreateElement("Settings");
                var xmlRecentlyUsedItems = doc.CreateElement("RecentlyUsedItems");

                doc.AppendChild(xmlRoot);
                xmlRoot.AppendChild(xmlRecentlyUsedItems);

                foreach (uint id in Main.RecentlyUsedCommands)
                {
                    var xmlItem = doc.CreateElement("Item");
                    xmlItem.SetAttribute("id", ((int)id).ToString());
                    xmlRecentlyUsedItems.AppendChild(xmlItem);
                }

                {
                    var xmlPreferredToolbarWidth = doc.CreateElement("PreferredToolbarWidth");
                    xmlPreferredToolbarWidth.SetAttribute("value", Main.PreferredToolbarWidth.ToString());
                    xmlRoot.AppendChild(xmlPreferredToolbarWidth);
                }

                {
                    var xmlPreferredResultsWindowSize = doc.CreateElement("PreferredResultsWindowSize");
                    xmlPreferredResultsWindowSize.SetAttribute("width", Main.PreferredResultsWindowSize.Width.ToString());
                    xmlPreferredResultsWindowSize.SetAttribute("height", Main.PreferredResultsWindowSize.Height.ToString());
                    xmlRoot.AppendChild(xmlPreferredResultsWindowSize);
                }

                {
                    var IsDarkModeEnabled = doc.CreateElement("IsDarkModeEnabled");
                    IsDarkModeEnabled.SetAttribute("value", Main.IsDarkModeEnabled.ToString());
                    xmlRoot.AppendChild(IsDarkModeEnabled);
                }

                doc.Save(filename);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
    }
}
