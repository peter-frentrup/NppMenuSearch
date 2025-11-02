using System;
using System.Runtime.InteropServices;
using NppPluginNET;

namespace NppMenuSearch
{
    public static class UnmanagedExports
    {
        public static void SetInfo(IntPtr nppHandle, IntPtr scintillaMainHandle, IntPtr scintillaSecondHandle)
        {
            PluginBase.nppData = new NppData { _nppHandle = nppHandle, _scintillaMainHandle = scintillaMainHandle, _scintillaSecondHandle = scintillaSecondHandle };
            Main.CommandMenuInit();
        }

        public static IntPtr GetFuncsArray(ref int nbF)
        {
            nbF = PluginBase._funcItems.Items.Count;
            return PluginBase._funcItems.NativePointer;
        }

        public static string GetName()
        {
            return Main.PluginName;
        }

        public static void BeNotified(IntPtr notifyCode)
        {
            SCNotification nc = (SCNotification)Marshal.PtrToStructure(notifyCode, typeof(SCNotification));

            try
            {

                switch (nc.nmhdr.code)
                {
                    case (uint)NppMsg.NPPN_READY:
                        Main.PluginReady();
                        break;

                    case (uint)NppMsg.NPPN_TBMODIFICATION:
                        PluginBase._funcItems.RefreshItems();
                        break;

                    case (uint)NppMsg.NPPN_SHUTDOWN:
                        Main.PluginCleanUp();
                        break;

                    case (uint)NppMsg.NPPN_DARKMODECHANGED:
                        DarkMode.OnChanged();
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
    }
}
