using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using NppMenuSearch.Forms;
using NppPluginNET;

namespace NppMenuSearch
{
    class Main
    {
		public static LinkedList<uint> RecentlyUsedCommands = new LinkedList<uint>();

        internal const string PluginName  = "NppMenuSearch";
        static string 		  xmlFilePath = null;

		internal static SearchForm SearchForm { get; private set; }

        internal static void CommandMenuInit()
        {
#if DEBUG
			Win32.AllocConsole();
			Console.WriteLine(PluginName + " debug mode");
#endif

			StringBuilder sbXmlFilePath = new StringBuilder(Win32.MAX_PATH);
			Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbXmlFilePath);
			xmlFilePath = sbXmlFilePath.ToString();
			if (!Directory.Exists(xmlFilePath)) Directory.CreateDirectory(xmlFilePath);
			xmlFilePath = Path.Combine(xmlFilePath, PluginName + ".xml");
			Settings.Load(xmlFilePath);

			SearchForm = new SearchForm();

            PluginBase.SetCommand(0, "Menu Search...",		 	   MenuSearchFunction, 	  new ShortcutKey(true,  false, false, Keys.M));
			PluginBase.SetCommand(1, "Clear “Recently Used” List", ClearRecentlyUsedList, new ShortcutKey(false, false, false, Keys.None));
			PluginBase.SetCommand(2, "---", 				 	   null);
			PluginBase.SetCommand(3, "About...", 			 	   AboutFunction, 	   	  new ShortcutKey(false, false, false, Keys.None));
        }

		internal static string GetNativeLangXml()
		{
			// %appdata%\Notepad++\nativeLang.xml

			string result = Path.Combine(
				Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					"Notepad++"),
				"nativeLang.xml");

			if (File.Exists(result))
				return result;

			StringBuilder sb = new StringBuilder(1024);
			Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETNPPDIRECTORY, sb.Capacity, sb);

			string nppDir = sb.ToString();
			result = Path.Combine(nppDir, "nativeLang.xml");
			if (File.Exists(result))
				return result;

			result = Path.Combine(Path.Combine(nppDir, "localization"), "english.xml");
			if (File.Exists(result))
				return result;

			return null;
		}

		internal static void PluginReady()
		{
			SearchForm.CheckToolbarVisiblity();
		}

        internal static void PluginCleanUp()
        {
			Settings.Save(xmlFilePath);
        }

		public static IntPtr GetMainWindow()
		{
			IntPtr dummy;
			IntPtr thisThread = Win32.GetWindowThreadProcessId(PluginBase.nppData._nppHandle, out dummy);
			IntPtr parent = PluginBase.nppData._nppHandle;
			while (parent != IntPtr.Zero)
			{
				IntPtr grandParent = Win32.GetParent(parent);

				if (Win32.GetWindowThreadProcessId(grandParent, out dummy) != thisThread)
					break;

				parent = grandParent;
			}

			return parent;
		}

		internal static void MakeNppOwnerOf(Form form)
		{
			Win32.SetWindowLongPtr(form.Handle, Win32.GWL_HWNDPARENT, GetMainWindow());
		}

		internal static void MenuSearchFunction()
        {
			SearchForm.SelectSearchField();
        }

		internal static void ClearRecentlyUsedList()
		{
			RecentlyUsedCommands.Clear();
		}

		internal static void AboutFunction()
		{
			MessageBox.Show(
				string.Format(
					"Notepad++ Menu Search Plugin, version {0}\r\n"+
					"by Peter Frentrup",
					typeof(Main).Assembly.GetName().Version),
				"NppMenuSearch",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}
    }
}