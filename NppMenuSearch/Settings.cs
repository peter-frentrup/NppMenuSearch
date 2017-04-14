using System;
using System.IO;
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
                    var xmlItemId = doc.CreateAttribute("id");

                    xmlItemId.Value = ((int)id).ToString();

                    xmlItem.Attributes.Append(xmlItemId);
                    xmlRecentlyUsedItems.AppendChild(xmlItem);
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
