using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using NppPluginNET;

namespace NppMenuSearch.Forms
{
    public partial class ResultsPopup : Form
    {
        const int DefaultMaxMenuResults = 10;
        const int DefaultMaxPreferencesResults = 7;
        const int RecentlyUsedListCount = 5;
        const int BlinkRepeat = 4;

        int MaxMenuResults = DefaultMaxMenuResults;
        int MaxPreferencesResults = DefaultMaxPreferencesResults;

        public event EventHandler Finished;

        ListViewGroup resultGroupRecentlyUsed = new ListViewGroup("Recently Used", HorizontalAlignment.Left);
        ListViewGroup resultGroupMenu         = new ListViewGroup("Menu",          HorizontalAlignment.Left);
        ListViewGroup resultGroupPreferences  = new ListViewGroup("Preferences",   HorizontalAlignment.Left);
        ListViewGroup resultGroupTabs         = new ListViewGroup("Open Files",    HorizontalAlignment.Left);

        public TextBox OwnerTextBox;
        public MenuItem MainMenu;
        private DialogItem PreferenceDialog;
        private List<TabItem> TabList;

        private int ReloadCounter = 0;

        public ResultsPopup()
        {
            InitializeComponent();

            viewResults.Groups.Add(resultGroupRecentlyUsed);
            viewResults.Groups.Add(resultGroupMenu);
            viewResults.Groups.Add(resultGroupTabs);
            viewResults.Groups.Add(resultGroupPreferences);

            MainMenu = new MenuItem(IntPtr.Zero);
            PreferenceDialog = new DialogItem("Preferences");
            TabList = new List<TabItem>();

            InitPreferencesDialog();
            UpdateLocalizedStrings();

            Main.Localization.NativeLangChanged += Localization_NativeLangChanged;

            Main.MakeNppOwnerOf(this);
            DarkMode.Changed += DarkMode_Changed;
            DarkMode_Changed();

            viewResults.ContextMenu = popupMenu;

            if (Main.PreferredResultsWindowSize.Width > 0 && Main.PreferredResultsWindowSize.Height > 0)
                Size = Main.PreferredResultsWindowSize;
        }

        private void Localization_NativeLangChanged(object sender, EventArgs e)
        {
            InitPreferencesDialog();
            UpdateLocalizedStrings();
        }

        private void UpdateLocalizedStrings()
        {
            resultGroupRecentlyUsed.Header = Main.Localization.Strings.GroupTitle_RecentlyUsed;
            resultGroupMenu.Header         = Main.Localization.Strings.GroupTitle_Menu;
            resultGroupPreferences.Header  = Main.Localization.Strings.GroupTitle_Preferences;
            resultGroupTabs.Header         = Main.Localization.Strings.GroupTitle_OpenFiles;

            menuGotoShortcutDefinition.Text = Main.Localization.Strings.MenuTitle_ChangeShortcut;
            menuExecute.Text                = Main.Localization.Strings.MenuTitle_Execute;
            menuOpenDialog.Text             = Main.Localization.Strings.MenuTitle_OpenDialog;
            menuSelectTab.Text              = Main.Localization.Strings.MenuTitle_SelectTab;

            // Help text label is updated when visibility changes.
        }

        private void DarkMode_Changed()
        {
            DarkMode.ApplyThemeRecursive(this);
        }

        protected void InitPreferencesDialog()
        {
#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();
            int stepIndex = 0;
#endif
            var hwndDummyDialogParent = Handle;

            int myCounter = ++ReloadCounter;

            //PreferenceDialog = LoadPreferenceDialogSteps(hwndDummyDialogParent);
            var loadStepEnumerator = LoadPreferenceDialogSteps(hwndDummyDialogParent).GetEnumerator();
            EventHandler handleStep = null;
            handleStep = (object sender, EventArgs e) =>
            {
#if DEBUG
                Stopwatch swStep = Stopwatch.StartNew();
#endif

                Timer sendingTimer = (Timer)sender;
                sendingTimer.Stop();
                sendingTimer.Tick -= handleStep;

                bool done = false;
                if(myCounter != ReloadCounter)
                {
                    done = true;
#if DEBUG
                    Console.WriteLine($"InitPreferencesDialog [{myCounter}]: cancelled at step {stepIndex} due to a newer InitPreferencesDialog request");
#endif
                }
                else if (loadStepEnumerator.MoveNext())
                {
                    var current = loadStepEnumerator.Current;
                    if (current.Key == LoadStep.Finished)
                    {
#if DEBUG
                        Console.WriteLine($"InitPreferencesDialog [{myCounter}]: final deferred step {stepIndex}");
#endif
                        PreferenceDialog = current.Value;
                    }

#if DEBUG
                    Console.WriteLine($"InitPreferencesDialog [{myCounter}]: deferred step {stepIndex} took {swStep.ElapsedMilliseconds}ms; {sw.ElapsedMilliseconds}ms after begin");
                    ++stepIndex;
#endif

                    sendingTimer.Tick += handleStep;
                    sendingTimer.Start();
                }
                else
                    done = true;

                if(done)
                {

#if DEBUG
                    Console.WriteLine($"InitPreferencesDialog [{myCounter}]: no more deferred steps; {sw.ElapsedMilliseconds}ms after begin");
                    ++stepIndex;
#endif

                    loadStepEnumerator.Dispose();
                    loadStepEnumerator = null;
                }
            };

            timerIdle.Tick += handleStep;
            timerIdle.Start();

#if DEBUG
            Console.WriteLine($"InitPreferencesDialog returns after {sw.ElapsedMilliseconds}ms");
#endif
        }

        enum LoadStep { More, Finished }
        private static IEnumerable<KeyValuePair<LoadStep, DialogItem>> LoadPreferenceDialogSteps(IntPtr hwndDummyDialogParent)
        {
            var ContinueLater = new KeyValuePair<LoadStep, DialogItem>(LoadStep.More, null);

            yield return ContinueLater;

            PreferenceDialogHelper pdh = new PreferenceDialogHelper();
            pdh.LoadCurrentLocalization();

            IntPtr hwndDialogPage;
            DialogItem preferenceDialog = new DialogItem(pdh.PageTranslations[pdh.Global.InternalName]);

            hwndDialogPage = DialogHelper.LoadNppDialog(hwndDummyDialogParent, (int)pdh.Global.ResourceId);
            try
            {
                preferenceDialog = DialogItem.CreateFromDialogFlat(hwndDialogPage, 0, pdh.PageTranslations[pdh.Global.InternalName]);
            }
            finally
            {
                DialogHelper.DestroyWindow(hwndDialogPage);
            }

            foreach (var pageInfo in pdh.GetPages())
            {
                yield return ContinueLater;

                hwndDialogPage = DialogHelper.LoadNppDialog(hwndDummyDialogParent, (int)pageInfo.ResourceId);
                try
                {
                    uint pageIdx = pdh.GetPageIdx(pageInfo.InternalName);
                    DialogItem pageItem = DialogItem.CreateFromDialogFlat(hwndDialogPage, pageIdx, pdh.PageTranslation(pageInfo.InternalName));

                    pageItem.ReorderItemsByGroupBoxes(hwndDialogPage);

                    preferenceDialog.AddItem(pageItem);
                }
                finally
                {
                    DialogHelper.DestroyWindow(hwndDialogPage);
                }
            }

            yield return ContinueLater;

            preferenceDialog.Translate(pdh.ControlTranslations);
            preferenceDialog.RemoveRedundantHeadings();

            yield return new KeyValuePair<LoadStep, DialogItem>(LoadStep.Finished, preferenceDialog);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Win32.WM_ACTIVATEAPP:
                    if (m.WParam == IntPtr.Zero)
                        Hide();
                    break;

                case Win32.WM_MOUSEACTIVATE:
                    m.Result = (IntPtr)Win32.MA_ACTIVATE;
                    return;

                case Win32.WM_ACTIVATE:
                    if (((int)m.WParam & 0xFFFF) == Win32.WA_CLICKACTIVE)
                    {
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    break;
            }

            base.WndProc(ref m);
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        public void ShowMoreResults()
        {
            MaxMenuResults = int.MaxValue;
            MaxPreferencesResults = int.MaxValue;
            lblHelp.Visible = false;
            RebuildResultsList();
        }

        private void ResultsPopup_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                Rectangle area = Screen.FromControl(this).WorkingArea;
                if (area.IntersectsWith(Bounds))
                {
                    area.Intersect(Bounds);
                    Bounds = area;
                }

                int toolbarButtonHeight = 1;
                if (Main.ToolbarSearchForm != null && Main.ToolbarSearchForm.HwndToolbar != IntPtr.Zero)
                {
                    IntPtr hImgList = Win32.SendMessage(Main.ToolbarSearchForm.HwndToolbar, Win32.TB_GETIMAGELIST, 0, 0);
                    if (hImgList != IntPtr.Zero)
                    {
                        if (Win32.ImageList_GetIconSize(hImgList, out int cx, out int cy))
                            toolbarButtonHeight = cy;
                    }
                }

                viewResults.TileSize = new Size(
                    viewResults.TileSize.Width,
                    Math.Max(toolbarButtonHeight, (int)(1.2 * viewResults.Font.Height)));

                string helpText = Main.Localization.Strings.SwitchGroupHelpText;
                string shortcut = Main.GetMenuSearchShortcut();
                if (shortcut != "")
                {
                    string shortcutHelp = Main.Localization.Strings.ShortcutHelpText_arg.Replace("{0}", shortcut);
                    if(shortcutHelp.Length > 0)
                    {
                        char lastChar = shortcutHelp[shortcutHelp.Length - 1];
                        if(lastChar <= ' ' || lastChar == '\u2028' /* line separator */ || lastChar == '\u2029' /* paragraph separator */ || lastChar == '\u200B' /* zero width space */)
                            helpText = shortcutHelp + helpText;
                        else
                            helpText = shortcutHelp + " " + helpText;
                    }
                }

                lblHelp.Text = helpText;

                MaxMenuResults = DefaultMaxMenuResults;
                MaxPreferencesResults = DefaultMaxPreferencesResults;
                lblHelp.Visible = true;
                LineBreakHelpText();

                MainMenu = new MenuItem(Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_INTERNAL_GETMENU, 0, 0));

                FillTabList();

                //NeedPreferencesDialog();

                OwnerTextBox.TextChanged += OwnerTextBox_TextChanged;
                OwnerTextBox.KeyDown += OwnerTextBox_KeyDown;
                RebuildResultsList();
            }
            else
            {
                OwnerTextBox.TextChanged -= OwnerTextBox_TextChanged;
                OwnerTextBox.KeyDown -= OwnerTextBox_KeyDown;
            }
        }

        private void FillTabList()
        {
            TabList = EnumOpenFileTabs(true).Concat(EnumOpenFileTabs(false)).ToList();
        }

        private static IEnumerable<TabItem> EnumOpenFileTabs(bool primaryView)
        {
            int count = Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, 0, primaryView ? (int)NppMsg.PRIMARY_VIEW : (int)NppMsg.SECOND_VIEW).ToInt32();

            using (ClikeStringArray nativeStringList = new ClikeStringArray(count, 2 * 1024))
            {
                int listFileCount = Win32.SendMessage(PluginBase.nppData._nppHandle, primaryView ? NppMsg.NPPM_GETOPENFILENAMESPRIMARY : NppMsg.NPPM_GETOPENFILENAMESSECOND, nativeStringList.NativePointer, count).ToInt32();

                List<string> filenameList = nativeStringList.ManagedStringsUnicode;

                for (int i = 0; i < listFileCount; i++)
                {
                    yield return new TabItem()
                    {
                        ViewNumber = primaryView ? (int)NppMsg.MAIN_VIEW : (int)NppMsg.SUB_VIEW,
                        Index = i,
                        FullFileName = filenameList[i]
                    };
                }
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            if (value)
            {
                /* fix for Issue 9: Results window behind Npp until Npp is deactivated and then activated again.
                 * I have no idea, why this happens.
                 * 
                 * We do not use BringToFront() because that tries to activate the window which causes
                 * short flickering.
                 */
                Win32.SetWindowPos(
                    Handle, Win32.HWND_TOP,
                    0, 0, 0, 0,
                    Win32.SWP_NOACTIVATE | Win32.SWP_NOMOVE | Win32.SWP_NOSIZE);
            }
        }

        void OwnerTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    e.Handled = true;
                    if (viewResults.Items.Count > 0)
                    {
                        if (viewResults.SelectedItems.Count == 0)
                            viewResults.Items[0].Selected = true;
                        else if (viewResults.SelectedIndices[0] + 1 < viewResults.Items.Count)
                            viewResults.Items[viewResults.SelectedIndices[0] + 1].Selected = true;
                        else
                            viewResults.Items[0].Selected = true;

                        viewResults.SelectedItems[0].EnsureVisible();
                    }
                    break;

                case Keys.Up:
                    e.Handled = true;
                    if (viewResults.Items.Count > 0)
                    {
                        if (viewResults.SelectedItems.Count == 0)
                            viewResults.Items[0].Selected = true;
                        else if (viewResults.SelectedIndices[0] > 0)
                            viewResults.Items[viewResults.SelectedIndices[0] - 1].Selected = true;
                        else
                            viewResults.Items[viewResults.Items.Count - 1].Selected = true;

                        viewResults.SelectedItems[0].EnsureVisible();
                    }
                    break;

                case Keys.Tab:
                    e.Handled = true;
                    if (viewResults.Items.Count > 0)
                    {
                        int groupIndex = viewResults.Groups.IndexOf(viewResults.SelectedItems[0].Group);
                        if (e.Shift)
                        {
                            --groupIndex;
                            while (groupIndex >= 0 && viewResults.Groups[groupIndex].Items.Count == 0)
                                --groupIndex;

                            if (groupIndex < 0)
                            {
                                groupIndex = viewResults.Groups.Count - 1;
                                while (groupIndex >= 0 && viewResults.Groups[groupIndex].Items.Count == 0)
                                    --groupIndex;
                            }

                            if (groupIndex >= 0)
                                viewResults.Groups[groupIndex].Items[0].Selected = true;

                            viewResults.SelectedItems[0].EnsureVisible();
                        }
                        else
                        {
                            ++groupIndex;
                            while (groupIndex < viewResults.Groups.Count && viewResults.Groups[groupIndex].Items.Count == 0)
                                ++groupIndex;

                            if (groupIndex >= viewResults.Groups.Count)
                            {
                                groupIndex = 0;
                                while (groupIndex < viewResults.Groups.Count && viewResults.Groups[groupIndex].Items.Count == 0)
                                    ++groupIndex;
                            }

                            if (groupIndex < viewResults.Groups.Count)
                                viewResults.Groups[groupIndex].Items[0].Selected = true;

                            viewResults.SelectedItems[0].EnsureVisible();
                        }
                    }
                    break;

                case Keys.Enter:
                    e.Handled = true;
                    ItemSelected();
                    break;

                case Keys.Apps:
                    if (viewResults.SelectedItems.Count > 0)
                    {
                        var item = viewResults.SelectedItems[0];

                        e.Handled = true;
                        popupMenu.Show(viewResults, new Point(item.Bounds.Right, item.Bounds.Bottom), LeftRightAlignment.Left);
                    }
                    break;
            }
        }

        void RebuildResultsList()
        {
            var words = OwnerTextBox.Text.SplitAt(' ');

            MenuItem[] menuItems = MainMenu
                .EnumFinalItems()
                .Select(item => new KeyValuePair<double, HierarchyItem>(item.MatchingSimilarity(words), item))
                .Where(kv => kv.Key > 0.0)
                .OrderByDescending(kv => kv.Key)
                .Select(kv => (MenuItem)kv.Value)
                .ToArray();

            DialogItem[] prefDialogItems = PreferenceDialog
                .EnumFinalItems()
                .Select(item => new KeyValuePair<double, HierarchyItem>(item.MatchingSimilarity(words), item))
                .Where(kv => kv.Key > 0.0)
                .OrderByDescending(kv => kv.Key)
                .Select(kv => (DialogItem)kv.Value)
                .ToArray();

            HierarchyItem[] recentlyUsed = Main.RecentlyUsedCommands
                .Select(id =>
                    (HierarchyItem)menuItems.Where(item => item.CommandId == id.ControlId).FirstOrDefault() ??
                    (HierarchyItem)prefDialogItems.Where(item => item.CtrlIdx == id).FirstOrDefault())
                .Where(item => item != null)
                .Take(RecentlyUsedListCount)
                .ToArray();

            List<TabItem> openTabsFiltered = TabList
                .Where(item => item.MatchesSearchTerm(OwnerTextBox.Text))
                .Take(MaxMenuResults)
                .ToList();


            viewResults.Items.Clear();

            resultGroupTabs.Header        = string.Format("{0} ({1})", Main.Localization.Strings.GroupTitle_OpenFiles,   openTabsFiltered.Count);
            resultGroupMenu.Header        = string.Format("{0} ({1})", Main.Localization.Strings.GroupTitle_Menu,        menuItems.Length - recentlyUsed.Where(hi => hi is MenuItem).Count());
            resultGroupPreferences.Header = string.Format("{0} ({1})", Main.Localization.Strings.GroupTitle_Preferences, prefDialogItems.Length - recentlyUsed.Where(hi => hi is DialogItem).Count());

            foreach (var hi in recentlyUsed)
            {
                ListViewItem item = new ListViewItem();
                item.Tag = hi;
                item.Text = hi + "";
                item.Group = resultGroupRecentlyUsed;
                viewResults.Items.Add(item);
#if DEBUG
                item.Text = string.Format("[{1:0.0000}] {0}", hi, hi.MatchingSimilarity(words));
#endif
            }

            int i = 0;
            foreach (var item in menuItems)
            {
                if (recentlyUsed.Contains(item))
                    continue;

                if (i++ == MaxMenuResults)
                    break;

                ListViewItem lvitem = new ListViewItem()
                {
                    Tag = item,
                    Text = item.ToString(),
                    Group = resultGroupMenu,
                };
                viewResults.Items.Add(lvitem);
#if DEBUG
                lvitem.Text = string.Format("[{1:0.0000}] {0}", item, item.MatchingSimilarity(words));
#endif
            }

            foreach (var item in openTabsFiltered)
            {
                viewResults.Items.Add(new ListViewItem()
                {
                    Tag = item,
                    Text = item.ToString(),
                    ToolTipText = item.ToolTipText,
                    Group = resultGroupTabs,
                });
            }

            i = 0;
            foreach (var item in prefDialogItems)
            {
                if (recentlyUsed.Contains(item))
                    continue;

                if (i++ == MaxPreferencesResults)
                    break;

                ListViewItem lvitem = new ListViewItem()
                {
                    Tag = item,
                    Text = item.ToString(),
                    Group = resultGroupPreferences,
                };
                viewResults.Items.Add(lvitem);
#if DEBUG
                lvitem.Text = string.Format("[{1}] {0}", item, item.MatchingSimilarity(words));
#endif
            }

            if (viewResults.Items.Count > 0)
                viewResults.Items[0].Selected = true;
        }

        void OwnerTextBox_TextChanged(object sender, EventArgs e)
        {
            MaxMenuResults = DefaultMaxMenuResults;
            MaxPreferencesResults = DefaultMaxPreferencesResults;
            lblHelp.Visible = true;

            RebuildResultsList();
        }

        void ItemSelected()
        {
            if (viewResults.SelectedItems.Count == 0)
                return;

            MenuItem menuItem = viewResults.SelectedItems[0].Tag as MenuItem;
            if (menuItem != null)
            {
                var recentCmd = new UniqueControlIdx(menuItem.CommandId, 0);
                Main.RecentlyUsedCommands.Remove(recentCmd);
                Main.RecentlyUsedCommands.AddFirst(recentCmd);

                //Console.WriteLine("Selected {0}", item.CommandId);
                Win32.SendMessage(PluginBase.nppData._nppHandle, (NppMsg)Win32.WM_COMMAND, (int)menuItem.CommandId, 0);
                Hide();
                OwnerTextBox.Text = "";

                if (OwnerTextBox.Focused)
                {
                    Win32.SetFocus(PluginBase.GetCurrentScintilla());
                }

                Main.RecalcRepeatLastCommandMenuItem();
                OnFinished();
                return;
            }

            DialogItem dialogItem = viewResults.SelectedItems[0].Tag as DialogItem;
            if (dialogItem != null)
            {
                var recentCmd = dialogItem.CtrlIdx;
                Main.RecentlyUsedCommands.Remove(recentCmd);
                Main.RecentlyUsedCommands.AddFirst(recentCmd);

                OpenPreferences(dialogItem.CtrlIdx);
                Hide();
                OwnerTextBox.Text = "";

                OnFinished();
                return;
            }

            TabItem tabItem = viewResults.SelectedItems[0].Tag as TabItem;
            if (tabItem != null)
            {
                int viewNumber = tabItem.ViewNumber;
                int index = tabItem.Index;
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_ACTIVATEDOC, viewNumber, index);

                Hide();
                OwnerTextBox.Text = "";

                OnFinished();
                return;
            }
        }

        public void OnFinished()
        {
            if (Finished != null)
                Finished(this, new EventArgs());
        }

        public void Highlight(IntPtr hwnd)
        {
            int counter = 2 * BlinkRepeat;

            EventHandler tick = null;
            tick = (sender, e) =>
            {
                if (--counter == 0 || !Win32.IsWindowVisible(hwnd))
                {
                    ((Timer)sender).Stop();
                    ((Timer)sender).Tick -= tick;
                }

                RECT rect;
                Win32.GetClientRect(hwnd, out rect);
                IntPtr hdc = Win32.GetWindowDC(hwnd);
                {
                    Win32.PatBlt(hdc,
                        rect.Left,
                        rect.Top,
                        rect.Right - rect.Left,
                        rect.Bottom - rect.Top,
                        Win32.DSTINVERT);
                }
                Win32.ReleaseDC(hwnd, hdc);
            };

            timerBlink.Tick += tick;
            timerBlink.Start();
        }


        static IntPtr hwndPreferences = IntPtr.Zero;
        public static IntPtr FindPreferencesDialog()
        {
            if (hwndPreferences != IntPtr.Zero)
                return hwndPreferences;

            List<IntPtr> hwndClosebutton;
            hwndPreferences = FindDialogByChildControlId(6001, true, out hwndClosebutton);
            return hwndPreferences;
        }

        // Collects all controls from different pages (child dialogs) where the control ID == `controlID`.
        // If `onlyFirst` is `true`, returns the very first control with the given `controlID`.
        public static IntPtr FindDialogByChildControlId(uint controlId, bool onlyFirst, out List<IntPtr> hwndControls)
        {
            IntPtr form = Win32.GetActiveWindow();//Win32.GetForegroundWindow();

            var controls = new List<IntPtr>();

            if (controlId == 0)
            {
                hwndControls = controls;
                return form;
            }

            Predicate<IntPtr> callback = hwndChild =>
            {
                if (Win32.GetDlgCtrlID(hwndChild) == controlId)
                {
                    controls.Add(hwndChild);
                    if (onlyFirst)
                        return false;
                }
                return true;
            };

            Win32.EnumChildWindows(form, callback);

            hwndControls = controls;
            return form;
        }

        public void OpenPreferences(UniqueControlIdx destinationCtrlIdx)
        {
            /* WM_TIMER messages have the lowest priority, so the following EventHandler will be called 
			 * (immediately) after the Preferences Dialog is shown [becuase we use a tick count of 1ms]
			 * 
			 * This does not work when the Preferences window is already visible, because it wont be 
			 * activated by Notepad++
			 */
            EventHandler tick = null;
            tick = (timer, ev) =>
            {
                ((Timer)timer).Stop();
                ((Timer)timer).Tick -= tick;

                List<IntPtr> hwndDestinationControls;
                IntPtr hwndPreferences = FindDialogByChildControlId(destinationCtrlIdx.ControlId, false, out hwndDestinationControls);

                if (hwndDestinationControls.Count != 0)
                {
                    IntPtr hwndCtrl = DialogHelper.NavigateToChild(hwndPreferences, hwndDestinationControls, destinationCtrlIdx.PageIdx);
                    if (Win32.IsWindowVisible(hwndCtrl))
                    {
                        Win32.SetFocus(hwndCtrl);
                        Highlight(hwndCtrl);
                    }
                }
            };

            timerIdle.Tick += tick;

            timerIdle.Start();
            Win32.SendMessage(PluginBase.nppData._nppHandle, (NppMsg)Win32.WM_COMMAND, (int)NppMenuCmd.IDM_SETTING_PREFERECE, 0);
        }

        private void viewResults_Resize(object sender, EventArgs e)
        {
            viewResults.TileSize = new Size(Math.Max(20, viewResults.ClientSize.Width - 20), viewResults.TileSize.Height);
        }

        private void viewResults_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            Color backgroundColor;
            Color foregroundColor;

            if (e.Item.Selected)
            {
                backgroundColor = DarkMode.SelectedItemBackColor;
                foregroundColor = DarkMode.SelectedItemForeColor;
            }
            else
            {
                backgroundColor = DarkMode.TextBackColor;
                foregroundColor = DarkMode.TextForeColor;
            }

            using (Brush background = new SolidBrush(backgroundColor))
            using (Brush foreground = new SolidBrush(foregroundColor))
            {
                Rectangle bounds = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top, e.Bounds.Width - 10, e.Bounds.Height);
                Rectangle textBounds = new Rectangle(bounds.Left + bounds.Height + 4, bounds.Top, bounds.Width - bounds.Height - 4, bounds.Height);

                e.Graphics.FillRectangle(background, bounds);

                StringFormat format = new StringFormat(
                    StringFormatFlags.NoWrap | StringFormatFlags.NoClip | StringFormatFlags.FitBlackBox);
                format.SetTabStops(20f, new float[] { 20f });
                if (e.Item.Tag is DialogItem)
                {
                    e.Graphics.DrawImage(
                        e.Item.Selected ? DarkMode.SelectedGearIcon : DarkMode.GearIcon,
                        bounds.Left,
                        bounds.Top);
                }
                else if (e.Item.Tag is MenuItem mi)
                {
                    if (mi.NativeIcon != IntPtr.Zero) // todo: and no special icon constant
                    {
                        try
                        {
                            WithNativeIcon(mi.NativeIcon, bmp => e.Graphics.DrawImage(bmp, bounds.Left, bounds.Top));
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Console.WriteLine(ex);
#endif
                        }
                    }
                    else if (Main.ToolbarSearchForm != null && Main.ToolbarSearchForm.HwndToolbar != IntPtr.Zero)
                    {
                        Win32.TBBUTTONINFO tbi = new Win32.TBBUTTONINFO();
                        tbi.cbSize = Win32.TBBUTTONINFO.Size;
                        tbi.dwMask = Win32.TBIF_IMAGE;
                        IntPtr index = Win32.SendMessage(Main.ToolbarSearchForm.HwndToolbar, Win32.TB_GETBUTTONINFOW, (int)mi.CommandId, ref tbi);
                        if (index != (IntPtr)(-1))
                        {
                            IntPtr hImgList = Win32.SendMessage(Main.ToolbarSearchForm.HwndToolbar, Win32.TB_GETIMAGELIST, 0, 0);
                            IntPtr hdc = e.Graphics.GetHdc();
                            try
                            {
                                Win32.ImageList_Draw(
                                    hImgList, tbi.iImage, hdc,
                                    bounds.Left, bounds.Top,
                                    //bounds.Height, bounds.Height,
                                    //Win32.CLR_NONE, Win32.CLR_NONE,
                                    Win32.ImageListDrawingStyle.Transparent);
                            }
                            finally
                            {
                                e.Graphics.ReleaseHdc(hdc);
                            }
                        }
                    }
                }

                e.Graphics.DrawString(
                    e.Item.Text.Replace('\n', ' ').Replace("\r", ""),
                    e.Item.Font ?? e.Item.ListView.Font,
                    foreground,
                    textBounds.Location,
                    format);
            }
        }

        private static void WithNativeIcon(IntPtr hBitmap, Action<Bitmap> draw)
        {
            using (var bmp = Bitmap.FromHbitmap(hBitmap))
            {
                if (bmp.PixelFormat == PixelFormat.Format32bppRgb)
                {
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
                    try
                    {
                        if (UsesAlphaChannel(bmpData))
                        {
                            using (var alphaBmp = new Bitmap(bmpData.Width, bmpData.Height, bmpData.Stride, PixelFormat.Format32bppArgb, bmpData.Scan0))
                            {
                                draw(alphaBmp);
                                return;
                            }
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(bmpData);
                    }
                }

                draw(bmp);
                return;
            }
        }

        private static bool UsesAlphaChannel(BitmapData bmpData)
        {
            for (int y = 0; y <= bmpData.Height - 1; y++)
            {
                for (int x = 0; x <= bmpData.Width - 1; x++)
                {
                    byte alpha = Marshal.ReadByte(bmpData.Scan0, (bmpData.Stride * y) + (4 * x) + 3);
                    if (alpha > 0)
                        return true;
                }
            }
            return false;
        }

        private void viewResults_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ItemSelected();
                return;
            }

            //OwnerTextBox.Focus();
        }

        private void menuGotoShortcutDefinition_Click(object sender, EventArgs e)
        {
            if (viewResults.SelectedItems.Count == 0)
                return;

            MenuItem menuItem = viewResults.SelectedItems[0].Tag as MenuItem;
            if (menuItem != null)
            {
                OpenShortcutMapper(menuItem);

                OnFinished();
                return;
            }

            Win32.MessageBeep(Win32.BeepType.MB_ICONERROR);
        }

        private void OpenShortcutMapper(MenuItem menuItem)
        {
#if DEBUG
            Console.WriteLine("search shortcut for {0} ({1})", menuItem.CommandId, menuItem);
#endif
            Hide();
            OwnerTextBox.Text = "";

            EventHandler tick = null;
            tick = (timer, ev) =>
            {
                ((Timer)timer).Stop();
                ((Timer)timer).Tick -= tick;

                List<IntPtr> hwndGrid;
                IntPtr hwndShortcutMapper = FindDialogByChildControlId(ShortcutMapperUtil.IDD_BABYGRID_ID1, true, out hwndGrid);

                if (hwndShortcutMapper != IntPtr.Zero && hwndGrid.Count != 0)
                {
                    if (ShortcutMapperUtil.GotoGridItem(hwndShortcutMapper, hwndGrid[0], menuItem))
                        return;
                }

                Win32.MessageBeep(Win32.BeepType.MB_ICONERROR);
            };

            timerIdle.Tick += tick;

            timerIdle.Start();
            Win32.SendMessage(PluginBase.nppData._nppHandle, (NppMsg)Win32.WM_COMMAND, (int)NppMenuCmd.IDM_SETTING_SHORTCUT_MAPPER, 0);
        }

        private void popupMenu_Popup(object sender, EventArgs e)
        {
            menuGotoShortcutDefinition.Enabled = false;
            menuOpenDialog.Visible = false;
            menuSelectTab.Visible = false;

            if (viewResults.SelectedItems.Count > 0)
            {
                if (viewResults.SelectedItems[0].Tag is MenuItem)
                {
                    menuGotoShortcutDefinition.Enabled = true;
                }

                if (viewResults.SelectedItems[0].Tag is DialogItem)
                {
                    menuOpenDialog.Visible = true;
                }
                else if (viewResults.SelectedItems[0].Tag is TabItem)
                {
                    menuSelectTab.Visible = true;
                }

                menuExecute.Enabled = true;
            }
            else
                menuExecute.Enabled = false;

            menuExecute.Visible = !menuOpenDialog.Visible && !menuSelectTab.Visible;
        }

        private void menuExecute_Click(object sender, EventArgs e)
        {
            ItemSelected();
        }

        private void ResultsPopup_SizeChanged(object sender, EventArgs e)
        {
            if (!Visible)
                return;

            LineBreakHelpText();
            Main.PreferredResultsWindowSize = Size;
        }

        private void LineBreakHelpText()
        {
            lblHelp.MaximumSize = new Size(ClientSize.Width, ClientSize.Height / 4);
        }
    }
}