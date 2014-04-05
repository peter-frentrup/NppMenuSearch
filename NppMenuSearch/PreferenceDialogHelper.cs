using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NppPluginNET;
using System.Xml;
using System.Globalization;

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
				(uint)NppResources.IDD_PREFERENCE_BAR_BOX,
				"General",
				"Global");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_MARGEIN_BOX,
				"Editing",
				"Scintillas");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_NEWDOCSETTING_BOX,
				"New Document",
				"NewDoc");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_DEFAULTDIRECTORY_BOX,
				"Default Directory",
				"DefaultDir");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_RECENTFILESHISTORY_BOX,
				"Recent Files History",
				"RecentFilesHistory");

			yield return new DialogInfo(
				(uint)NppResources.IDD_REGEXT_BOX,
				"File Association",
				"FileAssoc");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_LANG_BOX,
				"Language Menu",
				"LangMenu");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_TABSETTINGS_BOX,
				"Tab Settings",
				"TabSettings");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_PRINT_BOX,
				"Print",
				"Print");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_BACKUP_BOX,
				"Backup",
				"Backup");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_AUTOCOMPLETION_BOX,
				"Auto-Completion",
				"AutoCompletion");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_MULTIINSTANCE_BOX,
				"Multi-Instance",
				"MultiInstance");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_DELIMITERSETTINGS_BOX,
				"Delimiter",
				"Delimiter");

			yield return new DialogInfo(
				(uint)NppResources.IDD_PREFERENCE_SETTING_BOX,
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
