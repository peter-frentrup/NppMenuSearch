using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using NppPluginNET;

namespace NppMenuSearch
{
	public class DialogHelper
	{
		public static IntPtr LoadNppDialog(int dialogResoucreId)
		{
			if (dialogResoucreId < 0 || dialogResoucreId > 0xFFFF)
				return IntPtr.Zero;

			IntPtr exeModule = Win32.GetModuleHandle(null);

			// no need to pin DialogProcedure, becuase that is a static method
			return CreateDialogParam(exeModule, (IntPtr)dialogResoucreId, IntPtr.Zero, DialogProcedure, IntPtr.Zero);
		}

		static IntPtr DialogProcedure(IntPtr hwndDlg, uint uMsg, IntPtr wParam, IntPtr lParam)
		{
			switch (uMsg)
			{
				case (uint)Win32.WM_INITDIALOG:
					return (IntPtr)1;

				default:
					return IntPtr.Zero;
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyWindow(IntPtr hwnd); 

		delegate IntPtr DLGPROC(IntPtr hwndDlg, uint uMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		static extern IntPtr CreateDialogParam(IntPtr hInstance, IntPtr templateId,
		   IntPtr hwndParent, DLGPROC lpDialogFunc, IntPtr dwInitParam);

	}
}
