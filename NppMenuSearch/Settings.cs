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
                    string pageIdxString = xmlItem.GetAttribute("pageIdx");

                    uint uid, upageIdx;
                    if (uint.TryParse(idString, out uid) && uint.TryParse(pageIdxString, out upageIdx))
                    {
                        var recentCmd = new UniqueControlIdx(uid, upageIdx);
                        Main.RecentlyUsedCommands.AddLast(recentCmd);
                        continue;
                    }

                    int id, ipageIdx;
                    if (int.TryParse(idString, out id) && int.TryParse(pageIdxString, out ipageIdx))
                    {
                        var recentCmd = new UniqueControlIdx((uint)id, (uint)ipageIdx);
                        Main.RecentlyUsedCommands.AddLast(recentCmd);
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

                Main.FixedToolbarWidth = false;
                {
                    XmlElement xmlPreferredToolbarWidth = doc.SelectNodes("/Settings/FixedToolbarWidth")
                        .OfType<XmlElement>()
                        .FirstOrDefault();
                    if (xmlPreferredToolbarWidth != null && xmlPreferredToolbarWidth.HasAttribute("value"))
                    {
                        string fixString = xmlPreferredToolbarWidth.GetAttribute("value");
                        int fix;
                        if (int.TryParse(fixString, out fix))
                            Main.FixedToolbarWidth = fix != 0;
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

                foreach (var recentCmd in Main.RecentlyUsedCommands)
                {
                    var xmlItem = doc.CreateElement("Item");
                    xmlItem.SetAttribute("id", ((int)recentCmd.ControlId).ToString());
                    xmlItem.SetAttribute("pageIdx", ((int)recentCmd.PageIdx).ToString());
                    xmlRecentlyUsedItems.AppendChild(xmlItem);
                }

                {
                    var xmlPreferredToolbarWidth = doc.CreateElement("PreferredToolbarWidth");
                    xmlPreferredToolbarWidth.SetAttribute("value", Main.PreferredToolbarWidth.ToString());
                    xmlRoot.AppendChild(xmlPreferredToolbarWidth);
                }

                {
                    var xmlPreferredToolbarWidth = doc.CreateElement("FixedToolbarWidth");
                    xmlPreferredToolbarWidth.SetAttribute("value", Main.FixedToolbarWidth ? "1" : "0");
                    xmlRoot.AppendChild(xmlPreferredToolbarWidth);
                }

                {
                    var xmlPreferredResultsWindowSize = doc.CreateElement("PreferredResultsWindowSize");
                    xmlPreferredResultsWindowSize.SetAttribute("width", Main.PreferredResultsWindowSize.Width.ToString());
                    xmlPreferredResultsWindowSize.SetAttribute("height", Main.PreferredResultsWindowSize.Height.ToString());
                    xmlRoot.AppendChild(xmlPreferredResultsWindowSize);
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
