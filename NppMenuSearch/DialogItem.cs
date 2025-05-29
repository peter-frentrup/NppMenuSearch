using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NppPluginNET;

namespace NppMenuSearch
{
    public class DialogItem : HierarchyItem
    {
        public uint ControlId;
        public uint DlgIdx;

        public DialogItem(string text = "", uint id = 0)
            : base(text)
        {
            ControlId = id;
            DlgIdx = 0;
        }

        public void Translate(IDictionary<uint, string> translations)
        {
            string s;
            if (ControlId != 0 && translations.TryGetValue(ControlId, out s))
                Text = s;

            foreach (var item in this.EnumItems())
            {
                var dialogItem = item as DialogItem;
                if (dialogItem != null)
                    dialogItem.Translate(translations);
            }
        }

        public static DialogItem CreateFromDialogFlat(IntPtr hwndDialog, uint dlgIdx, string title)
        {
            DialogItem dialog = new DialogItem();
            dialog.Text = title;

            Win32.EnumChildWindows(hwndDialog, descendent =>
            {
                if (Win32.GetParent(descendent) == hwndDialog)
                {
                    RECT winRect;
                    Win32.GetWindowRect(descendent, out winRect);

                    Rectangle rect = new Rectangle(winRect.Left, winRect.Top, winRect.Right - winRect.Left, winRect.Bottom - winRect.Top);

                    uint id = (uint)Win32.GetDlgCtrlID(descendent);
                    if (id == 0 || (int)id == -1)
                        return true;

                    string className = Win32.GetClassName(descendent);

                    switch (className)
                    {
                        case "Button":
                        case "Static":
                            {
                                DialogItem item = new DialogItem();
                                item.ControlId = id;
                                item.DlgIdx = dlgIdx;
                                item.Text = Win32.GetWindowText(descendent);

                                dialog.AddItem(item);
                            }
                            break;

                        default:
                            //Console.WriteLine("skip id={0} ({1})", id, className);
                            break;
                    }
                }

                return true;
            });

            return dialog;
        }

        public void ReorderItemsByGroupBoxes(IntPtr hwndDialog)
        {
            var groupBoxes = new Dictionary<DialogItem, Rectangle>();
            var otherItems = new Dictionary<DialogItem, Rectangle>();

            var itToItem = new Dictionary<uint, DialogItem>();
            foreach(var di in EnumItems().Select(hi => hi as DialogItem).Where(di => di != null))
            {
                if(itToItem.ContainsKey(di.ControlId))
                {
                    Console.WriteLine("controlId {0} used twice:\n  {1}\n  {2}", 
                        (int)di.ControlId,
                        itToItem[di.ControlId],
                        di);
                }
                itToItem[di.ControlId] = di;
            }

            Win32.EnumChildWindows(hwndDialog, descendent =>
            {
                if (Win32.GetParent(descendent) == hwndDialog)
                {
                    RECT winRect;
                    Win32.GetWindowRect(descendent, out winRect);

                    Rectangle rect = new Rectangle(winRect.Left, winRect.Top, winRect.Right - winRect.Left, winRect.Bottom - winRect.Top);

                    uint id = (uint)Win32.GetDlgCtrlID(descendent);
                    if (id == 0)
                        return true;

                    DialogItem item;
                    itToItem.TryGetValue(id, out item);

                    if (item == null)
                        return true;

                    if (item.Text != "" &&
                        Win32.BS_GROUPBOX == (Win32.BS_TYPEMASK & Win32.GetWindowLong(descendent, Win32.GWL_STYLE)))
                    {
                        if (Win32.GetClassName(descendent) == "Button")
                        {
                            if(groupBoxes.ContainsKey(item))
                            {
                                Console.WriteLine("group {0} ({1}) already has a rectangle: {2} and {3}",
                                    (int)item.ControlId, 
                                    item,
                                    groupBoxes[item], 
                                    rect);
                            }
                            groupBoxes[item] = rect;
                            return true;
                        }
                    }

                    if(otherItems.ContainsKey(item))
                    {
                        Console.WriteLine("group {0} ({1}) already has a rectangle: {2} and {3}",
                            (int)item.ControlId,
                            item,
                            otherItems[item],
                            rect);
                    }
                    otherItems[item] = rect;
                }

                return true;
            });

            foreach (var innerGroup in groupBoxes.OrderByDescending(kv => kv.Value.Width))
            {
                foreach (var outerGroup in groupBoxes.OrderBy(kv => kv.Value.Width))
                {
                    if (outerGroup.Key == innerGroup.Key)
                        continue;

                    if (outerGroup.Key.Parent != this)
                        continue;

                    if (outerGroup.Value.Contains(innerGroup.Value))
                    {
                        innerGroup.Key.Parent.RemoveItem(innerGroup.Key);
                        outerGroup.Key.AddItem(innerGroup.Key);
                        break;
                    }
                }
            }

            foreach (var other in otherItems)
            {
                foreach (var group in groupBoxes.OrderBy(kv => kv.Value.Width))
                {
                    if (group.Value.Contains(other.Value))
                    {
                        RemoveItem(other.Key);
                        group.Key.AddItem(other.Key);
                        groupBoxes.Remove(other.Key);
                        break;
                    }
                }
            }
        }
    }
}
