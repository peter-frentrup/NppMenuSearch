using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;
using NppMenuSearch.Forms;

namespace NppMenuSearch
{
    class Main
    {
        internal const string PluginName  = "NppMenuSearch";
        static string 		  iniFilePath = null;
        static bool 		  someSetting = false;
        static Bitmap 		  tbBmp 	  = Properties.Resources.star;
        static Bitmap 		  tbBmp_tbTab = Properties.Resources.star_bmp;

		public static SearchForm SearchForm { get; private set; }


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

		internal static void PluginReady()
		{
			SearchForm.AddToToolbar();
			SearchForm.Show();
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
				"Notepad++ Menu Search Plugin by Peter Frentrup",
				"NppMenuSearch",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}
    }
}