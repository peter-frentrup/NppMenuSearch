using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		[DllImport("user32.dll", SetLastError=true)]
		static extern IntPtr CreateDialogParam(IntPtr hInstance, IntPtr templateId,
		   IntPtr hwndParent, DLGPROC lpDialogFunc, IntPtr dwInitParam);

	}
}
