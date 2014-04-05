using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;

namespace NppMenuSearch
{
	class NppListener: NativeWindow
	{
		public delegate void HideShowEventHandler(bool show);

		public event HideShowEventHandler BeforeHideShowToolbar;
		public event HideShowEventHandler AfterHideShowToolbar;

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case (int)NppMsg.NPPM_HIDETOOLBAR:
					HandleNppmHideToolbar(ref m);
					return;
			}

			base.WndProc(ref m);
		}

		private void HandleNppmHideToolbar(ref Message m)
		{
			bool show = (m.LParam != (IntPtr)1);

			OnBeforeHideShowToolbar(show);
			base.WndProc(ref m);
			OnAfterHideShowToolbar(show);
		}

		protected virtual void OnBeforeHideShowToolbar(bool show)
		{
			var handler = BeforeHideShowToolbar;

			if (handler != null)
				handler(show);
		}

		protected virtual void OnAfterHideShowToolbar(bool show)
		{
			var handler = AfterHideShowToolbar;

			if (handler != null)
				handler(show);
		}
	}
}
