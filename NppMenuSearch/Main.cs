using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;
using NppMenuSearch.Forms;
using System.Diagnostics;

namespace NppMenuSearch
{
    class Main
    {
        internal const string PluginName  = "NppMenuSearch";
        static string 		  iniFilePath = null;
        static bool 		  someSetting = false;

		internal static SearchForm SearchForm { get; private set; }

        internal static void CommandMenuInit()
        {
#if DEBUG
			Win32.AllocConsole();
			Console.WriteLine(PluginName + " debug mode");
#endif

            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");
            someSetting = (Win32.GetPrivateProfileInt("SomeSection", "SomeKey", 0, iniFilePath) != 0);

			SearchForm = new SearchForm();

            PluginBase.SetCommand(0, "Menu Search...", MenuSearchFunction, new ShortcutKey(true,  false, false, Keys.M));
			PluginBase.SetCommand(1, "About...", 	   AboutFunction, 	   new ShortcutKey(false, false, false, Keys.None));
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
            Win32.WritePrivateProfileString("SomeSection", "SomeKey", someSetting ? "1" : "0", iniFilePath);
        }

		internal static void MakeNppOwnerOf(Form form)
		{
			IntPtr dummy;
			IntPtr thisThread = Win32.GetWindowThreadProcessId(form.Handle, out dummy);
			IntPtr parent = PluginBase.nppData._nppHandle;
			while (parent != IntPtr.Zero)
			{
				IntPtr grandParent = Win32.GetParent(parent);

				if (Win32.GetWindowThreadProcessId(grandParent, out dummy) != thisThread)
					break;

				parent = grandParent;
			}

			Win32.SetWindowLongPtr(form.Handle, Win32.GWL_HWNDPARENT, parent);
		}

		internal static void MenuSearchFunction()
        {
			SearchForm.SelectSearchField();
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