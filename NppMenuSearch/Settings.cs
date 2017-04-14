using System;
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
                XmlElement xmlToolbarWidth = doc.SelectNodes("/Settings/PreferredToolbarWidth")
                    .OfType<XmlElement>()
                    .FirstOrDefault();
                if(xmlToolbarWidth != null && xmlToolbarWidth.HasAttribute("value"))
                {
                    string widthString = xmlToolbarWidth.GetAttribute("value");
                    int width;
                    if (int.TryParse(widthString, out width) && width > 0)
                        Main.PreferredToolbarWidth = width;
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

                var xmlPreferredToolbarWidth = doc.CreateElement("PreferredToolbarWidth");
                xmlPreferredToolbarWidth.SetAttribute("value", Main.PreferredToolbarWidth.ToString());
                xmlRoot.AppendChild(xmlPreferredToolbarWidth);

                doc.Save(filename);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
    }
}
