using System;
using System.Windows.Forms;
using NppPluginNET;

namespace NppMenuSearch
{
    class NppListener : NativeWindow
    {
        public delegate void HideShowEventHandler(bool show);

        public event HideShowEventHandler BeforeHideShowToolbar;
        public event HideShowEventHandler AfterHideShowToolbar;

        /// <summary>
        /// Note that this event normally fires twice in a row: N++ first switches to English and then to the selected language.
        /// </summary>
        public event EventHandler AfterReloadNativeLang;

        protected override void WndProc(ref Message m)
        {
            if (!Main.IsClosing)
            {
                switch (m.Msg)
                {
                    case (int)NppMsg.NPPM_HIDETOOLBAR:
                        HandleNppmHideToolbar(ref m);
                        return;

                    case (int)NppMsg.NPPM_INTERNAL_RELOADNATIVELANG:
                        base.WndProc(ref m);
                        OnAfterReloadNativeLang();
                        return;
                }
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

        protected virtual void OnAfterReloadNativeLang()
        {
            var handler = AfterReloadNativeLang;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
