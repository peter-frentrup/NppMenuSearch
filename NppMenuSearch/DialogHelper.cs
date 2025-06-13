using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NppPluginNET;

namespace NppMenuSearch
{
    public static class DialogHelper
    {
        static DLGPROC DialogProcedureDelegate = new DLGPROC(DialogProcedure);

        public static IntPtr LoadNppDialog(IntPtr hwndParent, int dialogResoucreId)
        {
            if (dialogResoucreId < 0 || dialogResoucreId > 0xFFFF)
                return IntPtr.Zero;

            IntPtr exeModule = Win32.GetModuleHandle(null);


            // No need to pin DialogProcedureDelegate, because that is a static field.
            return CreateDialogParam(exeModule, (IntPtr)dialogResoucreId, hwndParent, DialogProcedureDelegate, IntPtr.Zero);
        }

        static IntPtr DialogProcedure(IntPtr hwndDlg, uint uMsg, IntPtr wParam, IntPtr lParam)
        {
            return IntPtr.Zero;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyWindow(IntPtr hwnd);

        delegate IntPtr DLGPROC(IntPtr hwndDlg, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr CreateDialogParam(IntPtr hInstance, IntPtr templateId,
           IntPtr hwndParent, DLGPROC lpDialogFunc, IntPtr dwInitParam);




        public static void ChangeTabPage(IntPtr hwndDialog, IntPtr hwndTabControl, int index)
        {
            Win32.NMHDR nmhdr = new Win32.NMHDR();
            nmhdr.hwndFrom = hwndTabControl;
            nmhdr.idFrom = (UIntPtr)(uint)Win32.GetDlgCtrlID(hwndTabControl);

            // does not send a TCN_SELCHANGING or TCN_SELCHANGE notification code:
            Win32.SendMessage(hwndTabControl, (NppMsg)Win32.TCM_SETCURSEL, index, 0);

            nmhdr.code = unchecked((uint)Win32.TCN_SELCHANGE);
            Win32.SendMessage(hwndDialog, Win32.WM_NOTIFY, (int)nmhdr.idFrom, ref nmhdr);
        }

        public static void ChangeListboxSelection(IntPtr hwndDialog, IntPtr hwndListboxControl, int index)
        {
            // does not send a CBN_SELCHANGE command:
            Win32.SendMessage(hwndListboxControl, (NppMsg)Win32.LB_SETCURSEL, index, 0);

            uint wID = (uint)Win32.GetDlgCtrlID(hwndListboxControl);
            uint wNotifyCode = Win32.CBN_SELCHANGE;

            int wParam = unchecked((int)((wID & 0xFFFF) | ((wNotifyCode & 0xFFFF) << 16)));

            Win32.SendMessage(hwndDialog, Win32.WM_COMMAND, wParam, hwndListboxControl);
        }

        /// <summary>
        /// Navigates to and returns the first child control in <paramref name="hwndForm"/> from the list of 
        /// alternatives <paramref name="hwndChild"/> that is visible on the sub-dialog (list box)
        /// identified by the 1-based <paramref name="pageIdx"/> or to <c><paramref name="hwndChild"/>[0]</c> 
        /// if <c><paramref name="pageIdx"/> == 0</c> or <c><paramref name="hwndChild"/>.Count == 1</c>.
        /// <para>
        /// Does not work with nested/multiple tab controls.
        /// </para>
        /// </summary>
        public static IntPtr NavigateToChild(IntPtr hwndForm, List<IntPtr> hwndChild, uint pageIdx)
        {
            if (hwndChild.Count == 1 && Win32.IsWindowVisible(hwndChild[0]))
                return hwndChild[0];

            /* Before N++ 6.4.0, the preferences dialog used a tab-control.
			 * Since 6.4.0, it uses a list-box for the various settings dialogs.
			 */

            IntPtr hwndTab = IntPtr.Zero;
            IntPtr hwndTabList = IntPtr.Zero;
            Win32.EnumChildWindows(hwndForm, hwndFormChild =>
            {
                if (!Win32.IsWindowVisible(hwndFormChild))
                    return true;

                switch (Win32.GetClassName(hwndFormChild))
                {
                    case "SysTabControl32":
                        hwndTab = hwndFormChild;
                        return false;

                    case "ListBox":
                        if (Win32.GetWindowLongPtr(hwndFormChild, Win32.GWL_ID) == (IntPtr)NppResources.IDC_LIST_DLGTITLE)
                        {
                            hwndTabList = hwndFormChild;
                            return false;
                        }
                        break;
                }

                return true;
            });

            if (hwndTabList != IntPtr.Zero)
            {
                int count = (int)Win32.SendMessage(hwndTabList, (NppMsg)Win32.LB_GETCOUNT, 0, 0);
                int sel = (int)Win32.SendMessage(hwndTabList, (NppMsg)Win32.LB_GETCURSEL, 0, 0);

                if (pageIdx != 0 && pageIdx <= count && hwndChild.Count > 1)
                {
                    ChangeListboxSelection(hwndForm, hwndTabList, (int)pageIdx - 1);

                    foreach (var hwnd in hwndChild)
                    {
                        if (Win32.IsWindowVisible(hwnd))
                            return hwnd;
                    }

                    if (hwndChild.Count > 1)
                    {
                        // Not found on the given page. Stop there instead of searching other pages for controls with the same ID (other hwndChild)
                        // (if whe have only a single candidate, it does not harm to search other pages).
                        return IntPtr.Zero;
                    }
                }

                Console.WriteLine("navigate via listbox, count: {0}, sel: {1}", count, sel);

                for (int i = 0; i < count; ++i)
                {
                    ChangeListboxSelection(hwndForm, hwndTabList, i);

                    if (Win32.IsWindowVisible(hwndChild[0]))
                        return hwndChild[0];
                }

                Win32.SendMessage(hwndTabList, (NppMsg)Win32.LB_SETCURSEL, sel, 0);
            }

            if (hwndTab != IntPtr.Zero)
            {
                int count = (int)Win32.SendMessage(hwndTab, (NppMsg)Win32.TCM_GETITEMCOUNT, 0, 0);
                int sel = (int)Win32.SendMessage(hwndTab, (NppMsg)Win32.TCM_GETCURSEL, 0, 0);

                if (pageIdx != 0 && pageIdx <= count && hwndChild.Count > 1)
                {
                    ChangeTabPage(hwndForm, hwndTab, (int)pageIdx - 1);

                    foreach (var hwnd in hwndChild)
                    {
                        if (Win32.IsWindowVisible(hwnd))
                            return hwnd;
                    }


                    if (hwndChild.Count > 1)
                    {
                        // Not found on the given page. Stop there instead of searching other pages for controls with the same ID (other hwndChild)
                        // (if whe have only a single candidate, it does not harm to search other pages).
                        return IntPtr.Zero;
                    }
                }

                for (int i = 0; i < count; ++i)
                {
                    ChangeTabPage(hwndForm, hwndTab, i);

                    if (Win32.IsWindowVisible(hwndChild[0]))
                        return hwndChild[0];
                }

                Win32.SendMessage(hwndTab, (NppMsg)Win32.TCM_SETCURSEL, sel, 0);
            }

            return IntPtr.Zero;
        }

    }
}
