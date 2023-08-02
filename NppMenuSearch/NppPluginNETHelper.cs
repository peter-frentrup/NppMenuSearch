using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NppPluginNET
{
    #region " Notepad++ "
    [StructLayout(LayoutKind.Sequential)]
    public struct NppData
    {
        public IntPtr _nppHandle;
        public IntPtr _scintillaMainHandle;
        public IntPtr _scintillaSecondHandle;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void NppFuncItemDelegate();

    [StructLayout(LayoutKind.Sequential)]
    public struct ShortcutKey
    {
        public ShortcutKey(bool isCtrl, bool isAlt, bool isShift, Keys key)
        {
            // the types 'bool' and 'char' have a size of 1 byte only!
            _isCtrl = Convert.ToByte(isCtrl);
            _isAlt = Convert.ToByte(isAlt);
            _isShift = Convert.ToByte(isShift);
            _key = Convert.ToByte(key);
        }
        public byte _isCtrl;
        public byte _isAlt;
        public byte _isShift;
        public byte _key;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FuncItem
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string _itemName;
        public NppFuncItemDelegate _pFunc;
        public int _cmdID;
        public bool _init2Check;
        public ShortcutKey _pShKey;
    }

    public class FuncItems : IDisposable
    {
        List<FuncItem> _funcItems;
        int _sizeFuncItem;
        List<IntPtr> _shortCutKeys;
        IntPtr _nativePointer;
        bool _disposed = false;

        public FuncItems()
        {
            _funcItems = new List<FuncItem>();
            _sizeFuncItem = Marshal.SizeOf(typeof(FuncItem));
            _shortCutKeys = new List<IntPtr>();
        }

        [DllImport("kernel32")]
        static extern void RtlMoveMemory(IntPtr Destination, IntPtr Source, int Length);
        public void Add(FuncItem funcItem)
        {
            int oldSize = _funcItems.Count * _sizeFuncItem;
            _funcItems.Add(funcItem);
            int newSize = _funcItems.Count * _sizeFuncItem;
            IntPtr newPointer = Marshal.AllocHGlobal(newSize);

            if (_nativePointer != IntPtr.Zero)
            {
                RtlMoveMemory(newPointer, _nativePointer, oldSize);
                Marshal.FreeHGlobal(_nativePointer);
            }
            IntPtr ptrPosNewItem = (IntPtr)(newPointer.ToInt64() + oldSize);
            byte[] aB = Encoding.Unicode.GetBytes(funcItem._itemName + "\0");
            Marshal.Copy(aB, 0, ptrPosNewItem, aB.Length);
            ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + 128);
            IntPtr p = (funcItem._pFunc != null) ? Marshal.GetFunctionPointerForDelegate(funcItem._pFunc) : IntPtr.Zero;
            Marshal.WriteIntPtr(ptrPosNewItem, p);
            ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + IntPtr.Size);
            Marshal.WriteInt32(ptrPosNewItem, funcItem._cmdID);
            ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + 4);
            Marshal.WriteInt32(ptrPosNewItem, Convert.ToInt32(funcItem._init2Check));
            ptrPosNewItem = (IntPtr)(ptrPosNewItem.ToInt64() + 4);
            if (funcItem._pShKey._key != 0)
            {
                IntPtr newShortCutKey = Marshal.AllocHGlobal(4);
                Marshal.StructureToPtr(funcItem._pShKey, newShortCutKey, false);
                Marshal.WriteIntPtr(ptrPosNewItem, newShortCutKey);
            }
            else Marshal.WriteIntPtr(ptrPosNewItem, IntPtr.Zero);

            _nativePointer = newPointer;
        }

        public void RefreshItems()
        {
            IntPtr ptrPosItem = _nativePointer;
            for (int i = 0; i < _funcItems.Count; i++)
            {
                FuncItem updatedItem = new FuncItem();
                updatedItem._itemName = _funcItems[i]._itemName;
                ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + 128);
                updatedItem._pFunc = _funcItems[i]._pFunc;
                ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + IntPtr.Size);
                updatedItem._cmdID = Marshal.ReadInt32(ptrPosItem);
                ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + 4);
                updatedItem._init2Check = _funcItems[i]._init2Check;
                ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + 4);
                updatedItem._pShKey = _funcItems[i]._pShKey;
                ptrPosItem = (IntPtr)(ptrPosItem.ToInt64() + IntPtr.Size);

                _funcItems[i] = updatedItem;
            }
        }

        public IntPtr NativePointer { get { return _nativePointer; } }
        public List<FuncItem> Items { get { return _funcItems; } }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (IntPtr ptr in _shortCutKeys) Marshal.FreeHGlobal(ptr);
                if (_nativePointer != IntPtr.Zero) Marshal.FreeHGlobal(_nativePointer);
                _disposed = true;
            }
        }
        ~FuncItems()
        {
            Dispose();
        }
    }

    [Flags]
    public enum NppTbMsg : uint
    {
        // styles for containers
        //CAPTION_TOP                = 1,
        //CAPTION_BOTTOM            = 0,

        // defines for docking manager
        CONT_LEFT = 0,
        CONT_RIGHT = 1,
        CONT_TOP = 2,
        CONT_BOTTOM = 3,
        DOCKCONT_MAX = 4,

        // mask params for plugins of internal dialogs
        DWS_ICONTAB = 0x00000001,            // Icon for tabs are available
        DWS_ICONBAR = 0x00000002,            // Icon for icon bar are available (currently not supported)
        DWS_ADDINFO = 0x00000004,            // Additional information are in use
        DWS_PARAMSALL = (DWS_ICONTAB | DWS_ICONBAR | DWS_ADDINFO),

        // default docking values for first call of plugin
        DWS_DF_CONT_LEFT = (CONT_LEFT << 28),    // default docking on left
        DWS_DF_CONT_RIGHT = (CONT_RIGHT << 28),    // default docking on right
        DWS_DF_CONT_TOP = (CONT_TOP << 28),        // default docking on top
        DWS_DF_CONT_BOTTOM = (CONT_BOTTOM << 28),    // default docking on bottom
        DWS_DF_FLOATING = 0x80000000            // default state is floating
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NppTbData
    {
        public IntPtr hClient;            // HWND: client Window Handle
        public string pszName;            // TCHAR*: name of plugin (shown in window)
        public int dlgID;                // int: a funcItem provides the function pointer to start a dialog. Please parse here these ID
                                         // user modifications
        public NppTbMsg uMask;                // UINT: mask params: look to above defines
        public uint hIconTab;            // HICON: icon for tabs
        public string pszAddInfo;        // TCHAR*: for plugin to display additional informations
                                         // internal data, do not use !!!
        public RECT rcFloat;            // RECT: floating position
        public int iPrevCont;           // int: stores the privious container (toggling between float and dock)
        public string pszModuleName;    // const TCHAR*: it's the plugin file name. It's used to identify the plugin
    }

    public enum LangType
    {
        L_TEXT, L_PHP, L_C, L_CPP, L_CS, L_OBJC, L_JAVA, L_RC,
        L_HTML, L_XML, L_MAKEFILE, L_PASCAL, L_BATCH, L_INI, L_ASCII, L_USER,
        L_ASP, L_SQL, L_VB, L_JS, L_CSS, L_PERL, L_PYTHON, L_LUA,
        L_TEX, L_FORTRAN, L_BASH, L_FLASH, L_NSIS, L_TCL, L_LISP, L_SCHEME,
        L_ASM, L_DIFF, L_PROPS, L_PS, L_RUBY, L_SMALLTALK, L_VHDL, L_KIX, L_AU3,
        L_CAML, L_ADA, L_VERILOG, L_MATLAB, L_HASKELL, L_INNO, L_SEARCHRESULT,
        L_CMAKE, L_YAML, L_COBOL, L_GUI4CLI, L_D, L_POWERSHELL, L_R, L_JSP,
        // The end of enumated language type, so it should be always at the end
        L_EXTERNAL
    }

    [Flags]
    public enum NppMsg : uint
    {
        NOTEPADPLUS_USER_INTERNAL = (0x400/*WM_USER*/ + 0000),

        NPPM_INTERNAL_GETMENU = (NOTEPADPLUS_USER_INTERNAL + 14),
        NPPM_INTERNAL_RELOADNATIVELANG = (NOTEPADPLUS_USER_INTERNAL + 25),

        //Here you can find how to use these messages : http://notepad-plus.sourceforge.net/uk/plugins-HOWTO.php 
        NPPMSG = (0x400/*WM_USER*/ + 1000),

        NPPM_GETCURRENTSCINTILLA = (NPPMSG + 4),
        NPPM_GETCURRENTLANGTYPE = (NPPMSG + 5),
        NPPM_SETCURRENTLANGTYPE = (NPPMSG + 6),

        NPPM_GETNBOPENFILES = (NPPMSG + 7),
        ALL_OPEN_FILES = 0,
        PRIMARY_VIEW = 1,
        SECOND_VIEW = 2,

        NPPM_GETOPENFILENAMES = (NPPMSG + 8),

        NPPM_MODELESSDIALOG = (NPPMSG + 12),
        MODELESSDIALOGADD = 0,
        MODELESSDIALOGREMOVE = 1,

        NPPM_GETNBSESSIONFILES = (NPPMSG + 13),
        NPPM_GETSESSIONFILES = (NPPMSG + 14),
        NPPM_SAVESESSION = (NPPMSG + 15),
        NPPM_SAVECURRENTSESSION = (NPPMSG + 16),
        //struct sessionInfo {
        //    TCHAR* sessionFilePathName;
        //    int nbFile;
        //    TCHAR** files;
        //};

        NPPM_GETOPENFILENAMESPRIMARY = (NPPMSG + 17),
        NPPM_GETOPENFILENAMESSECOND = (NPPMSG + 18),

        NPPM_CREATESCINTILLAHANDLE = (NPPMSG + 20),
        NPPM_DESTROYSCINTILLAHANDLE = (NPPMSG + 21),
        NPPM_GETNBUSERLANG = (NPPMSG + 22),

        NPPM_GETCURRENTDOCINDEX = (NPPMSG + 23),
        MAIN_VIEW = 0,
        SUB_VIEW = 1,

        NPPM_SETSTATUSBAR = (NPPMSG + 24),
        STATUSBAR_DOC_TYPE = 0,
        STATUSBAR_DOC_SIZE = 1,
        STATUSBAR_CUR_POS = 2,
        STATUSBAR_EOF_FORMAT = 3,
        STATUSBAR_UNICODE_TYPE = 4,
        STATUSBAR_TYPING_MODE = 5,

        NPPM_GETMENUHANDLE = (NPPMSG + 25),
        NPPPLUGINMENU = 0,

        NPPM_ENCODESCI = (NPPMSG + 26),
        //ascii file to unicode
        //int NPPM_ENCODESCI(MAIN_VIEW/SUB_VIEW, 0)
        //return new unicodeMode

        NPPM_DECODESCI = (NPPMSG + 27),
        //unicode file to ascii
        //int NPPM_DECODESCI(MAIN_VIEW/SUB_VIEW, 0)
        //return old unicodeMode

        NPPM_ACTIVATEDOC = (NPPMSG + 28),
        //void NPPM_ACTIVATEDOC(int view, int index2Activate)

        NPPM_LAUNCHFINDINFILESDLG = (NPPMSG + 29),
        //void NPPM_LAUNCHFINDINFILESDLG(TCHAR * dir2Search, TCHAR * filtre)

        NPPM_DMMSHOW = (NPPMSG + 30),
        NPPM_DMMHIDE = (NPPMSG + 31),
        NPPM_DMMUPDATEDISPINFO = (NPPMSG + 32),
        //void NPPM_DMMxxx(0, tTbData->hClient)

        NPPM_DMMREGASDCKDLG = (NPPMSG + 33),
        //void NPPM_DMMREGASDCKDLG(0, &tTbData)

        NPPM_LOADSESSION = (NPPMSG + 34),
        //void NPPM_LOADSESSION(0, const TCHAR* file name)

        NPPM_DMMVIEWOTHERTAB = (NPPMSG + 35),
        //void WM_DMM_VIEWOTHERTAB(0, tTbData->pszName)

        NPPM_RELOADFILE = (NPPMSG + 36),
        //BOOL NPPM_RELOADFILE(BOOL withAlert, TCHAR *filePathName2Reload)

        NPPM_SWITCHTOFILE = (NPPMSG + 37),
        //BOOL NPPM_SWITCHTOFILE(0, TCHAR *filePathName2switch)

        NPPM_SAVECURRENTFILE = (NPPMSG + 38),
        //BOOL NPPM_SAVECURRENTFILE(0, 0)

        NPPM_SAVEALLFILES = (NPPMSG + 39),
        //BOOL NPPM_SAVEALLFILES(0, 0)

        NPPM_SETMENUITEMCHECK = (NPPMSG + 40),
        //void WM_PIMENU_CHECK(UINT    funcItem[X]._cmdID, TRUE/FALSE)

        NPPM_ADDTOOLBARICON = (NPPMSG + 41),
        //void WM_ADDTOOLBARICON(UINT funcItem[X]._cmdID, toolbarIcons icon)
        //struct toolbarIcons {
        //    HBITMAP    hToolbarBmp;
        //    HICON    hToolbarIcon;
        //};

        NPPM_GETWINDOWSVERSION = (NPPMSG + 42),
        //winVer NPPM_GETWINDOWSVERSION(0, 0)

        NPPM_DMMGETPLUGINHWNDBYNAME = (NPPMSG + 43),
        //HWND WM_DMM_GETPLUGINHWNDBYNAME(const TCHAR *windowName, const TCHAR *moduleName)
        // if moduleName is NULL, then return value is NULL
        // if windowName is NULL, then the first found window handle which matches with the moduleName will be returned

        NPPM_MAKECURRENTBUFFERDIRTY = (NPPMSG + 44),
        //BOOL NPPM_MAKECURRENTBUFFERDIRTY(0, 0)

        NPPM_GETENABLETHEMETEXTUREFUNC = (NPPMSG + 45),
        //BOOL NPPM_GETENABLETHEMETEXTUREFUNC(0, 0)

        NPPM_GETPLUGINSCONFIGDIR = (NPPMSG + 46),
        //void NPPM_GETPLUGINSCONFIGDIR(int strLen, TCHAR *str)

        NPPM_MSGTOPLUGIN = (NPPMSG + 47),
        //BOOL NPPM_MSGTOPLUGIN(TCHAR *destModuleName, CommunicationInfo *info)
        // return value is TRUE when the message arrive to the destination plugins.
        // if destModule or info is NULL, then return value is FALSE
        //struct CommunicationInfo {
        //    long internalMsg;
        //    const TCHAR * srcModuleName;
        //    void * info; // defined by plugin
        //};

        NPPM_MENUCOMMAND = (NPPMSG + 48),
        //void NPPM_MENUCOMMAND(0, int cmdID)
        // uncomment //#include "menuCmdID.h"
        // in the beginning of this file then use the command symbols defined in "menuCmdID.h" file
        // to access all the Notepad++ menu command items

        NPPM_TRIGGERTABBARCONTEXTMENU = (NPPMSG + 49),
        //void NPPM_TRIGGERTABBARCONTEXTMENU(int view, int index2Activate)

        NPPM_GETNPPVERSION = (NPPMSG + 50),
        // int NPPM_GETNPPVERSION(0, 0)
        // return version 
        // ex : v4.6
        // HIWORD(version) == 4
        // LOWORD(version) == 6

        NPPM_HIDETABBAR = (NPPMSG + 51),
        // BOOL NPPM_HIDETABBAR(0, BOOL hideOrNot)
        // if hideOrNot is set as TRUE then tab bar will be hidden
        // otherwise it'll be shown.
        // return value : the old status value

        NPPM_ISTABBARHIDDEN = (NPPMSG + 52),
        // BOOL NPPM_ISTABBARHIDDEN(0, 0)
        // returned value : TRUE if tab bar is hidden, otherwise FALSE

        NPPM_GETPOSFROMBUFFERID = (NPPMSG + 57),
        // INT NPPM_GETPOSFROMBUFFERID(INT bufferID, 0)
        // Return VIEW|INDEX from a buffer ID. -1 if the bufferID non existing
        //
        // VIEW takes 2 highest bits and INDEX (0 based) takes the rest (30 bits) 
        // Here's the values for the view :
        //  MAIN_VIEW 0
        //  SUB_VIEW  1

        NPPM_GETFULLPATHFROMBUFFERID = (NPPMSG + 58),
        // INT NPPM_GETFULLPATHFROMBUFFERID(INT bufferID, TCHAR *fullFilePath)
        // Get full path file name from a bufferID. 
        // Return -1 if the bufferID non existing, otherwise the number of TCHAR copied/to copy
        // User should call it with fullFilePath be NULL to get the number of TCHAR (not including the nul character),
        // allocate fullFilePath with the return values + 1, then call it again to get  full path file name

        NPPM_GETBUFFERIDFROMPOS = (NPPMSG + 59),
        //wParam: Position of document
        //lParam: View to use, 0 = Main, 1 = Secondary
        //Returns 0 if invalid

        NPPM_GETCURRENTBUFFERID = (NPPMSG + 60),
        //Returns active Buffer

        NPPM_RELOADBUFFERID = (NPPMSG + 61),
        //Reloads Buffer
        //wParam: Buffer to reload
        //lParam: 0 if no alert, else alert


        NPPM_GETBUFFERLANGTYPE = (NPPMSG + 64),
        //wParam: BufferID to get LangType from
        //lParam: 0
        //Returns as int, see LangType. -1 on error

        NPPM_SETBUFFERLANGTYPE = (NPPMSG + 65),
        //wParam: BufferID to set LangType of
        //lParam: LangType
        //Returns TRUE on success, FALSE otherwise
        //use int, see LangType for possible values
        //L_USER and L_EXTERNAL are not supported

        NPPM_GETBUFFERENCODING = (NPPMSG + 66),
        //wParam: BufferID to get encoding from
        //lParam: 0
        //returns as int, see UniMode. -1 on error

        NPPM_SETBUFFERENCODING = (NPPMSG + 67),
        //wParam: BufferID to set encoding of
        //lParam: format
        //Returns TRUE on success, FALSE otherwise
        //use int, see UniMode
        //Can only be done on new, unedited files

        NPPM_GETBUFFERFORMAT = (NPPMSG + 68),
        //wParam: BufferID to get format from
        //lParam: 0
        //returns as int, see formatType. -1 on error

        NPPM_SETBUFFERFORMAT = (NPPMSG + 69),
        //wParam: BufferID to set format of
        //lParam: format
        //Returns TRUE on success, FALSE otherwise
        //use int, see formatType

        /*
        NPPM_ADDREBAR = (NPPMSG + 57),
        // BOOL NPPM_ADDREBAR(0, REBARBANDINFO *)
        // Returns assigned ID in wID value of struct pointer
        NPPM_UPDATEREBAR = (NPPMSG + 58),
        // BOOL NPPM_ADDREBAR(INT ID, REBARBANDINFO *)
        //Use ID assigned with NPPM_ADDREBAR
        NPPM_REMOVEREBAR = (NPPMSG + 59),
        // BOOL NPPM_ADDREBAR(INT ID, 0)
        //Use ID assigned with NPPM_ADDREBAR
        */

        NPPM_HIDETOOLBAR = (NPPMSG + 70),
        // BOOL NPPM_HIDETOOLBAR(0, BOOL hideOrNot)
        // if hideOrNot is set as TRUE then tool bar will be hidden
        // otherwise it'll be shown.
        // return value : the old status value

        NPPM_ISTOOLBARHIDDEN = (NPPMSG + 71),
        // BOOL NPPM_ISTOOLBARHIDDEN(0, 0)
        // returned value : TRUE if tool bar is hidden, otherwise FALSE

        NPPM_HIDEMENU = (NPPMSG + 72),
        // BOOL NPPM_HIDEMENU(0, BOOL hideOrNot)
        // if hideOrNot is set as TRUE then menu will be hidden
        // otherwise it'll be shown.
        // return value : the old status value

        NPPM_ISMENUHIDDEN = (NPPMSG + 73),
        // BOOL NPPM_ISMENUHIDDEN(0, 0)
        // returned value : TRUE if menu is hidden, otherwise FALSE

        NPPM_HIDESTATUSBAR = (NPPMSG + 74),
        // BOOL NPPM_HIDESTATUSBAR(0, BOOL hideOrNot)
        // if hideOrNot is set as TRUE then STATUSBAR will be hidden
        // otherwise it'll be shown.
        // return value : the old status value

        NPPM_ISSTATUSBARHIDDEN = (NPPMSG + 75),
        // BOOL NPPM_ISSTATUSBARHIDDEN(0, 0)
        // returned value : TRUE if STATUSBAR is hidden, otherwise FALSE

        NPPM_GETSHORTCUTBYCMDID = (NPPMSG + 76),
        // BOOL NPPM_GETSHORTCUTBYCMDID(int cmdID, ShortcutKey *sk)
        // get your plugin command current mapped shortcut into sk via cmdID
        // You may need it after getting NPPN_READY notification
        // returned value : TRUE if this function call is successful and shorcut is enable, otherwise FALSE

        NPPM_DOOPEN = (NPPMSG + 77),
        // BOOL NPPM_DOOPEN(0, const TCHAR *fullPathName2Open)
        // fullPathName2Open indicates the full file path name to be opened.
        // The return value is TRUE (1) if the operation is successful, otherwise FALSE (0).

        NPPM_SAVECURRENTFILEAS = (NPPMSG + 78),
        // BOOL NPPM_SAVECURRENTFILEAS (BOOL asCopy, const TCHAR* filename)

        NPPM_GETCURRENTNATIVELANGENCODING = (NPPMSG + 79),
        // INT NPPM_GETCURRENTNATIVELANGENCODING(0, 0)
        // returned value : the current native language enconding

        NPPM_ALLOCATESUPPORTED = (NPPMSG + 80),
        // returns TRUE if NPPM_ALLOCATECMDID is supported
        // Use to identify if subclassing is necessary

        NPPM_ALLOCATECMDID = (NPPMSG + 81),
        // BOOL NPPM_ALLOCATECMDID(int numberRequested, int* startNumber)
        // sets startNumber to the initial command ID if successful
        // Returns: TRUE if successful, FALSE otherwise. startNumber will also be set to 0 if unsuccessful

        NPPM_ALLOCATEMARKER = (NPPMSG + 82),
        // BOOL NPPM_ALLOCATEMARKER(int numberRequested, int* startNumber)
        // sets startNumber to the initial command ID if successful
        // Allocates a marker number to a plugin
        // Returns: TRUE if successful, FALSE otherwise. startNumber will also be set to 0 if unsuccessful

        RUNCOMMAND_USER = (0x400/*WM_USER*/ + 3000),
        NPPM_GETFULLCURRENTPATH = (RUNCOMMAND_USER + FULL_CURRENT_PATH),
        NPPM_GETCURRENTDIRECTORY = (RUNCOMMAND_USER + CURRENT_DIRECTORY),
        NPPM_GETFILENAME = (RUNCOMMAND_USER + FILE_NAME),
        NPPM_GETNAMEPART = (RUNCOMMAND_USER + NAME_PART),
        NPPM_GETEXTPART = (RUNCOMMAND_USER + EXT_PART),
        NPPM_GETCURRENTWORD = (RUNCOMMAND_USER + CURRENT_WORD),
        NPPM_GETNPPDIRECTORY = (RUNCOMMAND_USER + NPP_DIRECTORY),
        // BOOL NPPM_GETXXXXXXXXXXXXXXXX(size_t strLen, TCHAR *str)
        // where str is the allocated TCHAR array,
        //         strLen is the allocated array size
        // The return value is TRUE when get generic_string operation success
        // Otherwise (allocated array size is too small) FALSE

        NPPM_GETCURRENTLINE = (RUNCOMMAND_USER + CURRENT_LINE),
        // INT NPPM_GETCURRENTLINE(0, 0)
        // return the caret current position line
        NPPM_GETCURRENTCOLUMN = (RUNCOMMAND_USER + CURRENT_COLUMN),
        // INT NPPM_GETCURRENTCOLUMN(0, 0)
        // return the caret current position column
        VAR_NOT_RECOGNIZED = 0,
        FULL_CURRENT_PATH = 1,
        CURRENT_DIRECTORY = 2,
        FILE_NAME = 3,
        NAME_PART = 4,
        EXT_PART = 5,
        CURRENT_WORD = 6,
        NPP_DIRECTORY = 7,
        CURRENT_LINE = 8,
        CURRENT_COLUMN = 9,

        // Notification code
        NPPN_FIRST = 1000,
        NPPN_READY = (NPPN_FIRST + 1), // To notify plugins that all the procedures of launchment of notepad++ are done.
                                       //scnNotification->nmhdr.code = NPPN_READY;
                                       //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                       //scnNotification->nmhdr.idFrom = 0;

        NPPN_TBMODIFICATION = (NPPN_FIRST + 2), // To notify plugins that toolbar icons can be registered
                                                //scnNotification->nmhdr.code = NPPN_TB_MODIFICATION;
                                                //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                                //scnNotification->nmhdr.idFrom = 0;

        NPPN_FILEBEFORECLOSE = (NPPN_FIRST + 3), // To notify plugins that the current file is about to be closed
                                                 //scnNotification->nmhdr.code = NPPN_FILEBEFORECLOSE;
                                                 //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                                 //scnNotification->nmhdr.idFrom = BufferID;

        NPPN_FILEOPENED = (NPPN_FIRST + 4), // To notify plugins that the current file is just opened
                                            //scnNotification->nmhdr.code = NPPN_FILEOPENED;
                                            //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                            //scnNotification->nmhdr.idFrom = BufferID;

        NPPN_FILECLOSED = (NPPN_FIRST + 5), // To notify plugins that the current file is just closed
                                            //scnNotification->nmhdr.code = NPPN_FILECLOSED;
                                            //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                            //scnNotification->nmhdr.idFrom = BufferID;

        NPPN_FILEBEFOREOPEN = (NPPN_FIRST + 6), // To notify plugins that the current file is about to be opened
                                                //scnNotification->nmhdr.code = NPPN_FILEBEFOREOPEN;
                                                //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                                //scnNotification->nmhdr.idFrom = BufferID;

        NPPN_FILEBEFORESAVE = (NPPN_FIRST + 7), // To notify plugins that the current file is about to be saved
                                                //scnNotification->nmhdr.code = NPPN_FILEBEFOREOPEN;
                                                //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                                //scnNotification->nmhdr.idFrom = BufferID;

        NPPN_FILESAVED = (NPPN_FIRST + 8), // To notify plugins that the current file is just saved
                                           //scnNotification->nmhdr.code = NPPN_FILESAVED;
                                           //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                           //scnNotification->nmhdr.idFrom = BufferID;

        NPPN_SHUTDOWN = (NPPN_FIRST + 9), // To notify plugins that Notepad++ is about to be shutdowned.
                                          //scnNotification->nmhdr.code = NPPN_SHUTDOWN;
                                          //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                          //scnNotification->nmhdr.idFrom = 0;

        NPPN_BUFFERACTIVATED = (NPPN_FIRST + 10), // To notify plugins that a buffer was activated (put to foreground).
                                                  //scnNotification->nmhdr.code = NPPN_BUFFERACTIVATED;
                                                  //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                                  //scnNotification->nmhdr.idFrom = activatedBufferID;

        NPPN_LANGCHANGED = (NPPN_FIRST + 11), // To notify plugins that the language in the current doc is just changed.
                                              //scnNotification->nmhdr.code = NPPN_LANGCHANGED;
                                              //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                              //scnNotification->nmhdr.idFrom = currentBufferID;

        NPPN_WORDSTYLESUPDATED = (NPPN_FIRST + 12), // To notify plugins that user initiated a WordStyleDlg change.
                                                    //scnNotification->nmhdr.code = NPPN_WORDSTYLESUPDATED;
                                                    //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                                    //scnNotification->nmhdr.idFrom = currentBufferID;

        NPPN_SHORTCUTREMAPPED = (NPPN_FIRST + 13), // To notify plugins that plugin command shortcut is remapped.
                                                   //scnNotification->nmhdr.code = NPPN_SHORTCUTSREMAPPED;
                                                   //scnNotification->nmhdr.hwndFrom = ShortcutKeyStructurePointer;
                                                   //scnNotification->nmhdr.idFrom = cmdID;
                                                   //where ShortcutKeyStructurePointer is pointer of struct ShortcutKey:
                                                   //struct ShortcutKey {
                                                   //    bool _isCtrl;
                                                   //    bool _isAlt;
                                                   //    bool _isShift;
                                                   //    UCHAR _key;
                                                   //};

        NPPN_FILEBEFORELOAD = (NPPN_FIRST + 14), // To notify plugins that the current file is about to be loaded
                                                 //scnNotification->nmhdr.code = NPPN_FILEBEFOREOPEN;
                                                 //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                                 //scnNotification->nmhdr.idFrom = NULL;

        NPPN_FILELOADFAILED = (NPPN_FIRST + 15),  // To notify plugins that file open operation failed
                                                  //scnNotification->nmhdr.code = NPPN_FILEOPENFAILED;
                                                  //scnNotification->nmhdr.hwndFrom = hwndNpp;
                                                  //scnNotification->nmhdr.idFrom = BufferID;

        NPPN_READONLYCHANGED = (NPPN_FIRST + 16),  // To notify plugins that current document change the readonly status,
                                                   //scnNotification->nmhdr.code = NPPN_READONLYCHANGED;
                                                   //scnNotification->nmhdr.hwndFrom = bufferID;
                                                   //scnNotification->nmhdr.idFrom = docStatus;
                                                   // where bufferID is BufferID
                                                   //       docStatus can be combined by DOCSTAUS_READONLY and DOCSTAUS_BUFFERDIRTY

        DOCSTAUS_READONLY = 1,
        DOCSTAUS_BUFFERDIRTY = 2,

        NPPN_DOCORDERCHANGED = (NPPN_FIRST + 16)  // To notify plugins that document order is changed
                                                  //scnNotification->nmhdr.code = NPPN_DOCORDERCHANGED;
                                                  //scnNotification->nmhdr.hwndFrom = newIndex;
                                                  //scnNotification->nmhdr.idFrom = BufferID;
    }

    public enum NppMenuCmd : uint
    {
        ID_MACRO = 20000,
        ID_MACRO_LIMIT = 20200,

        ID_USER_CMD = 21000,
        ID_USER_CMD_LIMIT = 21200,

        ID_PLUGINS_CMD = 22000,
        ID_PLUGINS_CMD_LIMIT = 22500,
        ID_PLUGINS_CMD_DYNAMIC = 23000,
        ID_PLUGINS_CMD_DYNAMIC_LIMIT = 24999,


        IDM = 40000,

        IDM_FILE = (IDM + 1000),
        IDM_FILE_NEW = (IDM_FILE + 1),
        IDM_FILE_OPEN = (IDM_FILE + 2),
        IDM_FILE_CLOSE = (IDM_FILE + 3),
        IDM_FILE_CLOSEALL = (IDM_FILE + 4),
        IDM_FILE_CLOSEALL_BUT_CURRENT = (IDM_FILE + 5),
        IDM_FILE_SAVE = (IDM_FILE + 6),
        IDM_FILE_SAVEALL = (IDM_FILE + 7),
        IDM_FILE_SAVEAS = (IDM_FILE + 8),
        //IDM_FILE_ASIAN_LANG              = (IDM_FILE + 9), 
        IDM_FILE_PRINT = (IDM_FILE + 10),
        IDM_FILE_PRINTNOW = 1001,
        IDM_FILE_EXIT = (IDM_FILE + 11),
        IDM_FILE_LOADSESSION = (IDM_FILE + 12),
        IDM_FILE_SAVESESSION = (IDM_FILE + 13),
        IDM_FILE_RELOAD = (IDM_FILE + 14),
        IDM_FILE_SAVECOPYAS = (IDM_FILE + 15),
        IDM_FILE_DELETE = (IDM_FILE + 16),
        IDM_FILE_RENAME = (IDM_FILE + 17),

        // A mettre à jour si on ajoute nouveau menu item dans le menu "File"
        IDM_FILEMENU_LASTONE = IDM_FILE_RENAME,

        IDM_EDIT = (IDM + 2000),
        IDM_EDIT_CUT = (IDM_EDIT + 1),
        IDM_EDIT_COPY = (IDM_EDIT + 2),
        IDM_EDIT_UNDO = (IDM_EDIT + 3),
        IDM_EDIT_REDO = (IDM_EDIT + 4),
        IDM_EDIT_PASTE = (IDM_EDIT + 5),
        IDM_EDIT_DELETE = (IDM_EDIT + 6),
        IDM_EDIT_SELECTALL = (IDM_EDIT + 7),

        IDM_EDIT_INS_TAB = (IDM_EDIT + 8),
        IDM_EDIT_RMV_TAB = (IDM_EDIT + 9),
        IDM_EDIT_DUP_LINE = (IDM_EDIT + 10),
        IDM_EDIT_TRANSPOSE_LINE = (IDM_EDIT + 11),
        IDM_EDIT_SPLIT_LINES = (IDM_EDIT + 12),
        IDM_EDIT_JOIN_LINES = (IDM_EDIT + 13),
        IDM_EDIT_LINE_UP = (IDM_EDIT + 14),
        IDM_EDIT_LINE_DOWN = (IDM_EDIT + 15),
        IDM_EDIT_UPPERCASE = (IDM_EDIT + 16),
        IDM_EDIT_LOWERCASE = (IDM_EDIT + 17),

        // Menu macro
        IDM_MACRO_STARTRECORDINGMACRO = (IDM_EDIT + 18),
        IDM_MACRO_STOPRECORDINGMACRO = (IDM_EDIT + 19),
        IDM_MACRO_PLAYBACKRECORDEDMACRO = (IDM_EDIT + 21),
        //-----------

        IDM_EDIT_BLOCK_COMMENT = (IDM_EDIT + 22),
        IDM_EDIT_STREAM_COMMENT = (IDM_EDIT + 23),
        IDM_EDIT_TRIMTRAILING = (IDM_EDIT + 24),
        IDM_EDIT_TRIMLINEHEAD = (IDM_EDIT + 42),
        IDM_EDIT_TRIM_BOTH = (IDM_EDIT + 43),
        IDM_EDIT_EOL2WS = (IDM_EDIT + 44),
        IDM_EDIT_TRIMALL = (IDM_EDIT + 45),
        IDM_EDIT_TAB2SW = (IDM_EDIT + 46),
        IDM_EDIT_SW2TAB = (IDM_EDIT + 47),

        // Menu macro
        IDM_MACRO_SAVECURRENTMACRO = (IDM_EDIT + 25),
        //-----------

        IDM_EDIT_RTL = (IDM_EDIT + 26),
        IDM_EDIT_LTR = (IDM_EDIT + 27),
        IDM_EDIT_SETREADONLY = (IDM_EDIT + 28),
        IDM_EDIT_FULLPATHTOCLIP = (IDM_EDIT + 29),
        IDM_EDIT_FILENAMETOCLIP = (IDM_EDIT + 30),
        IDM_EDIT_CURRENTDIRTOCLIP = (IDM_EDIT + 31),

        // Menu macro
        IDM_MACRO_RUNMULTIMACRODLG = (IDM_EDIT + 32),
        //-----------

        IDM_EDIT_CLEARREADONLY = (IDM_EDIT + 33),
        IDM_EDIT_COLUMNMODE = (IDM_EDIT + 34),
        IDM_EDIT_BLOCK_COMMENT_SET = (IDM_EDIT + 35),
        IDM_EDIT_BLOCK_UNCOMMENT = (IDM_EDIT + 36),

        IDM_EDIT_AUTOCOMPLETE = (50000 + 0),
        IDM_EDIT_AUTOCOMPLETE_CURRENTFILE = (50000 + 1),
        IDM_EDIT_FUNCCALLTIP = (50000 + 2),

        //Belong to MENU FILE
        IDM_OPEN_ALL_RECENT_FILE = (IDM_EDIT + 40),
        IDM_CLEAN_RECENT_FILE_LIST = (IDM_EDIT + 41),

        IDM_SEARCH = (IDM + 3000),

        IDM_SEARCH_FIND = (IDM_SEARCH + 1),
        IDM_SEARCH_FINDNEXT = (IDM_SEARCH + 2),
        IDM_SEARCH_REPLACE = (IDM_SEARCH + 3),
        IDM_SEARCH_GOTOLINE = (IDM_SEARCH + 4),
        IDM_SEARCH_TOGGLE_BOOKMARK = (IDM_SEARCH + 5),
        IDM_SEARCH_NEXT_BOOKMARK = (IDM_SEARCH + 6),
        IDM_SEARCH_PREV_BOOKMARK = (IDM_SEARCH + 7),
        IDM_SEARCH_CLEAR_BOOKMARKS = (IDM_SEARCH + 8),
        IDM_SEARCH_GOTOMATCHINGBRACE = (IDM_SEARCH + 9),
        IDM_SEARCH_FINDPREV = (IDM_SEARCH + 10),
        IDM_SEARCH_FINDINCREMENT = (IDM_SEARCH + 11),
        IDM_SEARCH_FINDINFILES = (IDM_SEARCH + 13),
        IDM_SEARCH_VOLATILE_FINDNEXT = (IDM_SEARCH + 14),
        IDM_SEARCH_VOLATILE_FINDPREV = (IDM_SEARCH + 15),
        IDM_SEARCH_CUTMARKEDLINES = (IDM_SEARCH + 18),
        IDM_SEARCH_COPYMARKEDLINES = (IDM_SEARCH + 19),
        IDM_SEARCH_PASTEMARKEDLINES = (IDM_SEARCH + 20),
        IDM_SEARCH_DELETEMARKEDLINES = (IDM_SEARCH + 21),
        IDM_SEARCH_MARKALLEXT1 = (IDM_SEARCH + 22),
        IDM_SEARCH_UNMARKALLEXT1 = (IDM_SEARCH + 23),
        IDM_SEARCH_MARKALLEXT2 = (IDM_SEARCH + 24),
        IDM_SEARCH_UNMARKALLEXT2 = (IDM_SEARCH + 25),
        IDM_SEARCH_MARKALLEXT3 = (IDM_SEARCH + 26),
        IDM_SEARCH_UNMARKALLEXT3 = (IDM_SEARCH + 27),
        IDM_SEARCH_MARKALLEXT4 = (IDM_SEARCH + 28),
        IDM_SEARCH_UNMARKALLEXT4 = (IDM_SEARCH + 29),
        IDM_SEARCH_MARKALLEXT5 = (IDM_SEARCH + 30),
        IDM_SEARCH_UNMARKALLEXT5 = (IDM_SEARCH + 31),
        IDM_SEARCH_CLEARALLMARKS = (IDM_SEARCH + 32),

        IDM_SEARCH_GOPREVMARKER1 = (IDM_SEARCH + 33),
        IDM_SEARCH_GOPREVMARKER2 = (IDM_SEARCH + 34),
        IDM_SEARCH_GOPREVMARKER3 = (IDM_SEARCH + 35),
        IDM_SEARCH_GOPREVMARKER4 = (IDM_SEARCH + 36),
        IDM_SEARCH_GOPREVMARKER5 = (IDM_SEARCH + 37),
        IDM_SEARCH_GOPREVMARKER_DEF = (IDM_SEARCH + 38),

        IDM_SEARCH_GONEXTMARKER1 = (IDM_SEARCH + 39),
        IDM_SEARCH_GONEXTMARKER2 = (IDM_SEARCH + 40),
        IDM_SEARCH_GONEXTMARKER3 = (IDM_SEARCH + 41),
        IDM_SEARCH_GONEXTMARKER4 = (IDM_SEARCH + 42),
        IDM_SEARCH_GONEXTMARKER5 = (IDM_SEARCH + 43),
        IDM_SEARCH_GONEXTMARKER_DEF = (IDM_SEARCH + 44),

        IDM_FOCUS_ON_FOUND_RESULTS = (IDM_SEARCH + 45),
        IDM_SEARCH_GOTONEXTFOUND = (IDM_SEARCH + 46),
        IDM_SEARCH_GOTOPREVFOUND = (IDM_SEARCH + 47),

        IDM_SEARCH_SETANDFINDNEXT = (IDM_SEARCH + 48),
        IDM_SEARCH_SETANDFINDPREV = (IDM_SEARCH + 49),
        IDM_SEARCH_INVERSEMARKS = (IDM_SEARCH + 50),

        IDM_VIEW = (IDM + 4000),
        //IDM_VIEW_TOOLBAR_HIDE            = (IDM_VIEW + 1),
        IDM_VIEW_TOOLBAR_REDUCE = (IDM_VIEW + 2),
        IDM_VIEW_TOOLBAR_ENLARGE = (IDM_VIEW + 3),
        IDM_VIEW_TOOLBAR_STANDARD = (IDM_VIEW + 4),
        IDM_VIEW_REDUCETABBAR = (IDM_VIEW + 5),
        IDM_VIEW_LOCKTABBAR = (IDM_VIEW + 6),
        IDM_VIEW_DRAWTABBAR_TOPBAR = (IDM_VIEW + 7),
        IDM_VIEW_DRAWTABBAR_INACIVETAB = (IDM_VIEW + 8),
        IDM_VIEW_POSTIT = (IDM_VIEW + 9),
        IDM_VIEW_TOGGLE_FOLDALL = (IDM_VIEW + 10),
        IDM_VIEW_USER_DLG = (IDM_VIEW + 11),
        IDM_VIEW_LINENUMBER = (IDM_VIEW + 12),
        IDM_VIEW_SYMBOLMARGIN = (IDM_VIEW + 13),
        IDM_VIEW_FOLDERMAGIN = (IDM_VIEW + 14),
        IDM_VIEW_FOLDERMAGIN_SIMPLE = (IDM_VIEW + 15),
        IDM_VIEW_FOLDERMAGIN_ARROW = (IDM_VIEW + 16),
        IDM_VIEW_FOLDERMAGIN_CIRCLE = (IDM_VIEW + 17),
        IDM_VIEW_FOLDERMAGIN_BOX = (IDM_VIEW + 18),
        IDM_VIEW_ALL_CHARACTERS = (IDM_VIEW + 19),
        IDM_VIEW_INDENT_GUIDE = (IDM_VIEW + 20),
        IDM_VIEW_CURLINE_HILITING = (IDM_VIEW + 21),
        IDM_VIEW_WRAP = (IDM_VIEW + 22),
        IDM_VIEW_ZOOMIN = (IDM_VIEW + 23),
        IDM_VIEW_ZOOMOUT = (IDM_VIEW + 24),
        IDM_VIEW_TAB_SPACE = (IDM_VIEW + 25),
        IDM_VIEW_EOL = (IDM_VIEW + 26),
        IDM_VIEW_EDGELINE = (IDM_VIEW + 27),
        IDM_VIEW_EDGEBACKGROUND = (IDM_VIEW + 28),
        IDM_VIEW_TOGGLE_UNFOLDALL = (IDM_VIEW + 29),
        IDM_VIEW_FOLD_CURRENT = (IDM_VIEW + 30),
        IDM_VIEW_UNFOLD_CURRENT = (IDM_VIEW + 31),
        IDM_VIEW_FULLSCREENTOGGLE = (IDM_VIEW + 32),
        IDM_VIEW_ZOOMRESTORE = (IDM_VIEW + 33),
        IDM_VIEW_ALWAYSONTOP = (IDM_VIEW + 34),
        IDM_VIEW_SYNSCROLLV = (IDM_VIEW + 35),
        IDM_VIEW_SYNSCROLLH = (IDM_VIEW + 36),
        IDM_VIEW_EDGENONE = (IDM_VIEW + 37),
        IDM_VIEW_DRAWTABBAR_CLOSEBOTTUN = (IDM_VIEW + 38),
        IDM_VIEW_DRAWTABBAR_DBCLK2CLOSE = (IDM_VIEW + 39),
        IDM_VIEW_REFRESHTABAR = (IDM_VIEW + 40),
        IDM_VIEW_WRAP_SYMBOL = (IDM_VIEW + 41),
        IDM_VIEW_HIDELINES = (IDM_VIEW + 42),
        IDM_VIEW_DRAWTABBAR_VERTICAL = (IDM_VIEW + 43),
        IDM_VIEW_DRAWTABBAR_MULTILINE = (IDM_VIEW + 44),
        IDM_VIEW_DOCCHANGEMARGIN = (IDM_VIEW + 45),
        IDM_VIEW_LWDEF = (IDM_VIEW + 46),
        IDM_VIEW_LWALIGN = (IDM_VIEW + 47),
        IDM_VIEW_LWINDENT = (IDM_VIEW + 48),
        IDM_VIEW_SUMMARY = (IDM_VIEW + 49),

        IDM_VIEW_FOLD = (IDM_VIEW + 50),
        IDM_VIEW_FOLD_1 = (IDM_VIEW_FOLD + 1),
        IDM_VIEW_FOLD_2 = (IDM_VIEW_FOLD + 2),
        IDM_VIEW_FOLD_3 = (IDM_VIEW_FOLD + 3),
        IDM_VIEW_FOLD_4 = (IDM_VIEW_FOLD + 4),
        IDM_VIEW_FOLD_5 = (IDM_VIEW_FOLD + 5),
        IDM_VIEW_FOLD_6 = (IDM_VIEW_FOLD + 6),
        IDM_VIEW_FOLD_7 = (IDM_VIEW_FOLD + 7),
        IDM_VIEW_FOLD_8 = (IDM_VIEW_FOLD + 8),

        IDM_VIEW_UNFOLD = (IDM_VIEW + 60),
        IDM_VIEW_UNFOLD_1 = (IDM_VIEW_UNFOLD + 1),
        IDM_VIEW_UNFOLD_2 = (IDM_VIEW_UNFOLD + 2),
        IDM_VIEW_UNFOLD_3 = (IDM_VIEW_UNFOLD + 3),
        IDM_VIEW_UNFOLD_4 = (IDM_VIEW_UNFOLD + 4),
        IDM_VIEW_UNFOLD_5 = (IDM_VIEW_UNFOLD + 5),
        IDM_VIEW_UNFOLD_6 = (IDM_VIEW_UNFOLD + 6),
        IDM_VIEW_UNFOLD_7 = (IDM_VIEW_UNFOLD + 7),
        IDM_VIEW_UNFOLD_8 = (IDM_VIEW_UNFOLD + 8),

        IDM_VIEW_GOTO_ANOTHER_VIEW = 10001,
        IDM_VIEW_CLONE_TO_ANOTHER_VIEW = 10002,
        IDM_VIEW_GOTO_NEW_INSTANCE = 10003,
        IDM_VIEW_LOAD_IN_NEW_INSTANCE = 10004,

        IDM_VIEW_SWITCHTO_OTHER_VIEW = (IDM_VIEW + 72),

        IDM_FORMAT = (IDM + 5000),
        IDM_FORMAT_TODOS = (IDM_FORMAT + 1),
        IDM_FORMAT_TOUNIX = (IDM_FORMAT + 2),
        IDM_FORMAT_TOMAC = (IDM_FORMAT + 3),
        IDM_FORMAT_ANSI = (IDM_FORMAT + 4),
        IDM_FORMAT_UTF_8 = (IDM_FORMAT + 5),
        IDM_FORMAT_UCS_2BE = (IDM_FORMAT + 6),
        IDM_FORMAT_UCS_2LE = (IDM_FORMAT + 7),
        IDM_FORMAT_AS_UTF_8 = (IDM_FORMAT + 8),
        IDM_FORMAT_CONV2_ANSI = (IDM_FORMAT + 9),
        IDM_FORMAT_CONV2_AS_UTF_8 = (IDM_FORMAT + 10),
        IDM_FORMAT_CONV2_UTF_8 = (IDM_FORMAT + 11),
        IDM_FORMAT_CONV2_UCS_2BE = (IDM_FORMAT + 12),
        IDM_FORMAT_CONV2_UCS_2LE = (IDM_FORMAT + 13),

        IDM_FORMAT_ENCODE = (IDM_FORMAT + 20),
        IDM_FORMAT_WIN_1250 = (IDM_FORMAT_ENCODE + 0),
        IDM_FORMAT_WIN_1251 = (IDM_FORMAT_ENCODE + 1),
        IDM_FORMAT_WIN_1252 = (IDM_FORMAT_ENCODE + 2),
        IDM_FORMAT_WIN_1253 = (IDM_FORMAT_ENCODE + 3),
        IDM_FORMAT_WIN_1254 = (IDM_FORMAT_ENCODE + 4),
        IDM_FORMAT_WIN_1255 = (IDM_FORMAT_ENCODE + 5),
        IDM_FORMAT_WIN_1256 = (IDM_FORMAT_ENCODE + 6),
        IDM_FORMAT_WIN_1257 = (IDM_FORMAT_ENCODE + 7),
        IDM_FORMAT_WIN_1258 = (IDM_FORMAT_ENCODE + 8),
        IDM_FORMAT_ISO_8859_1 = (IDM_FORMAT_ENCODE + 9),
        IDM_FORMAT_ISO_8859_2 = (IDM_FORMAT_ENCODE + 10),
        IDM_FORMAT_ISO_8859_3 = (IDM_FORMAT_ENCODE + 11),
        IDM_FORMAT_ISO_8859_4 = (IDM_FORMAT_ENCODE + 12),
        IDM_FORMAT_ISO_8859_5 = (IDM_FORMAT_ENCODE + 13),
        IDM_FORMAT_ISO_8859_6 = (IDM_FORMAT_ENCODE + 14),
        IDM_FORMAT_ISO_8859_7 = (IDM_FORMAT_ENCODE + 15),
        IDM_FORMAT_ISO_8859_8 = (IDM_FORMAT_ENCODE + 16),
        IDM_FORMAT_ISO_8859_9 = (IDM_FORMAT_ENCODE + 17),
        IDM_FORMAT_ISO_8859_10 = (IDM_FORMAT_ENCODE + 18),
        IDM_FORMAT_ISO_8859_11 = (IDM_FORMAT_ENCODE + 19),
        IDM_FORMAT_ISO_8859_13 = (IDM_FORMAT_ENCODE + 20),
        IDM_FORMAT_ISO_8859_14 = (IDM_FORMAT_ENCODE + 21),
        IDM_FORMAT_ISO_8859_15 = (IDM_FORMAT_ENCODE + 22),
        IDM_FORMAT_ISO_8859_16 = (IDM_FORMAT_ENCODE + 23),
        IDM_FORMAT_DOS_437 = (IDM_FORMAT_ENCODE + 24),
        IDM_FORMAT_DOS_720 = (IDM_FORMAT_ENCODE + 25),
        IDM_FORMAT_DOS_737 = (IDM_FORMAT_ENCODE + 26),
        IDM_FORMAT_DOS_775 = (IDM_FORMAT_ENCODE + 27),
        IDM_FORMAT_DOS_850 = (IDM_FORMAT_ENCODE + 28),
        IDM_FORMAT_DOS_852 = (IDM_FORMAT_ENCODE + 29),
        IDM_FORMAT_DOS_855 = (IDM_FORMAT_ENCODE + 30),
        IDM_FORMAT_DOS_857 = (IDM_FORMAT_ENCODE + 31),
        IDM_FORMAT_DOS_858 = (IDM_FORMAT_ENCODE + 32),
        IDM_FORMAT_DOS_860 = (IDM_FORMAT_ENCODE + 33),
        IDM_FORMAT_DOS_861 = (IDM_FORMAT_ENCODE + 34),
        IDM_FORMAT_DOS_862 = (IDM_FORMAT_ENCODE + 35),
        IDM_FORMAT_DOS_863 = (IDM_FORMAT_ENCODE + 36),
        IDM_FORMAT_DOS_865 = (IDM_FORMAT_ENCODE + 37),
        IDM_FORMAT_DOS_866 = (IDM_FORMAT_ENCODE + 38),
        IDM_FORMAT_DOS_869 = (IDM_FORMAT_ENCODE + 39),
        IDM_FORMAT_BIG5 = (IDM_FORMAT_ENCODE + 40),
        IDM_FORMAT_GB2312 = (IDM_FORMAT_ENCODE + 41),
        IDM_FORMAT_SHIFT_JIS = (IDM_FORMAT_ENCODE + 42),
        IDM_FORMAT_KOREAN_WIN = (IDM_FORMAT_ENCODE + 43),
        IDM_FORMAT_EUC_KR = (IDM_FORMAT_ENCODE + 44),
        IDM_FORMAT_TIS_620 = (IDM_FORMAT_ENCODE + 45),
        IDM_FORMAT_MAC_CYRILLIC = (IDM_FORMAT_ENCODE + 46),
        IDM_FORMAT_KOI8U_CYRILLIC = (IDM_FORMAT_ENCODE + 47),
        IDM_FORMAT_KOI8R_CYRILLIC = (IDM_FORMAT_ENCODE + 48),
        IDM_FORMAT_ENCODE_END = IDM_FORMAT_KOI8R_CYRILLIC,

        //#define    IDM_FORMAT_CONVERT            200

        IDM_LANG = (IDM + 6000),
        IDM_LANGSTYLE_CONFIG_DLG = (IDM_LANG + 1),
        IDM_LANG_C = (IDM_LANG + 2),
        IDM_LANG_CPP = (IDM_LANG + 3),
        IDM_LANG_JAVA = (IDM_LANG + 4),
        IDM_LANG_HTML = (IDM_LANG + 5),
        IDM_LANG_XML = (IDM_LANG + 6),
        IDM_LANG_JS = (IDM_LANG + 7),
        IDM_LANG_PHP = (IDM_LANG + 8),
        IDM_LANG_ASP = (IDM_LANG + 9),
        IDM_LANG_CSS = (IDM_LANG + 10),
        IDM_LANG_PASCAL = (IDM_LANG + 11),
        IDM_LANG_PYTHON = (IDM_LANG + 12),
        IDM_LANG_PERL = (IDM_LANG + 13),
        IDM_LANG_OBJC = (IDM_LANG + 14),
        IDM_LANG_ASCII = (IDM_LANG + 15),
        IDM_LANG_TEXT = (IDM_LANG + 16),
        IDM_LANG_RC = (IDM_LANG + 17),
        IDM_LANG_MAKEFILE = (IDM_LANG + 18),
        IDM_LANG_INI = (IDM_LANG + 19),
        IDM_LANG_SQL = (IDM_LANG + 20),
        IDM_LANG_VB = (IDM_LANG + 21),
        IDM_LANG_BATCH = (IDM_LANG + 22),
        IDM_LANG_CS = (IDM_LANG + 23),
        IDM_LANG_LUA = (IDM_LANG + 24),
        IDM_LANG_TEX = (IDM_LANG + 25),
        IDM_LANG_FORTRAN = (IDM_LANG + 26),
        IDM_LANG_BASH = (IDM_LANG + 27),
        IDM_LANG_FLASH = (IDM_LANG + 28),
        IDM_LANG_NSIS = (IDM_LANG + 29),
        IDM_LANG_TCL = (IDM_LANG + 30),
        IDM_LANG_LISP = (IDM_LANG + 31),
        IDM_LANG_SCHEME = (IDM_LANG + 32),
        IDM_LANG_ASM = (IDM_LANG + 33),
        IDM_LANG_DIFF = (IDM_LANG + 34),
        IDM_LANG_PROPS = (IDM_LANG + 35),
        IDM_LANG_PS = (IDM_LANG + 36),
        IDM_LANG_RUBY = (IDM_LANG + 37),
        IDM_LANG_SMALLTALK = (IDM_LANG + 38),
        IDM_LANG_VHDL = (IDM_LANG + 39),
        IDM_LANG_CAML = (IDM_LANG + 40),
        IDM_LANG_KIX = (IDM_LANG + 41),
        IDM_LANG_ADA = (IDM_LANG + 42),
        IDM_LANG_VERILOG = (IDM_LANG + 43),
        IDM_LANG_AU3 = (IDM_LANG + 44),
        IDM_LANG_MATLAB = (IDM_LANG + 45),
        IDM_LANG_HASKELL = (IDM_LANG + 46),
        IDM_LANG_INNO = (IDM_LANG + 47),
        IDM_LANG_CMAKE = (IDM_LANG + 48),
        IDM_LANG_YAML = (IDM_LANG + 49),
        IDM_LANG_COBOL = (IDM_LANG + 50),
        IDM_LANG_D = (IDM_LANG + 51),
        IDM_LANG_GUI4CLI = (IDM_LANG + 52),
        IDM_LANG_POWERSHELL = (IDM_LANG + 53),
        IDM_LANG_R = (IDM_LANG + 54),
        IDM_LANG_JSP = (IDM_LANG + 55),

        IDM_LANG_EXTERNAL = (IDM_LANG + 65),
        IDM_LANG_EXTERNAL_LIMIT = (IDM_LANG + 79),

        IDM_LANG_USER = (IDM_LANG + 80),     //46080
        IDM_LANG_USER_LIMIT = (IDM_LANG + 110),    //46110


        IDM_ABOUT = (IDM + 7000),
        IDM_HOMESWEETHOME = (IDM_ABOUT + 1),
        IDM_PROJECTPAGE = (IDM_ABOUT + 2),
        IDM_ONLINEHELP = (IDM_ABOUT + 3),
        IDM_FORUM = (IDM_ABOUT + 4),
        IDM_PLUGINSHOME = (IDM_ABOUT + 5),
        IDM_UPDATE_NPP = (IDM_ABOUT + 6),
        IDM_WIKIFAQ = (IDM_ABOUT + 7),
        IDM_HELP = (IDM_ABOUT + 8),


        IDM_SETTING = (IDM + 8000),
        IDM_SETTING_TAB_SIZE = (IDM_SETTING + 1),
        IDM_SETTING_TAB_REPLCESPACE = (IDM_SETTING + 2),
        IDM_SETTING_HISTORY_SIZE = (IDM_SETTING + 3),
        IDM_SETTING_EDGE_SIZE = (IDM_SETTING + 4),
        IDM_SETTING_IMPORTPLUGIN = (IDM_SETTING + 5),
        IDM_SETTING_IMPORTSTYLETHEMS = (IDM_SETTING + 6),
        IDM_SETTING_TRAYICON = (IDM_SETTING + 8),
        IDM_SETTING_SHORTCUT_MAPPER = (IDM_SETTING + 9),
        IDM_SETTING_REMEMBER_LAST_SESSION = (IDM_SETTING + 10),
        IDM_SETTING_PREFERECE = (IDM_SETTING + 11),
        IDM_SETTING_AUTOCNBCHAR = (IDM_SETTING + 15),
        IDM_SETTING_SHORTCUT_MAPPER_MACRO = (IDM_SETTING + 16),
        IDM_SETTING_SHORTCUT_MAPPER_RUN = (IDM_SETTING + 17),
        IDM_SETTING_EDITCONTEXTMENU = (IDM_SETTING + 18),

        IDM_EXECUTE = (IDM + 9000),

        IDM_SYSTRAYPOPUP = (IDM + 3100),
        IDM_SYSTRAYPOPUP_ACTIVATE = (IDM_SYSTRAYPOPUP + 1),
        IDM_SYSTRAYPOPUP_NEWDOC = (IDM_SYSTRAYPOPUP + 2),
        IDM_SYSTRAYPOPUP_NEW_AND_PASTE = (IDM_SYSTRAYPOPUP + 3),
        IDM_SYSTRAYPOPUP_OPENFILE = (IDM_SYSTRAYPOPUP + 4),
        IDM_SYSTRAYPOPUP_CLOSE = (IDM_SYSTRAYPOPUP + 5)
    }

    public enum NppResources : int
    {
        IDD_REGEXT_BOX = 4000,

        IDD_PREFERENCE_BOX = 6000,
        IDC_BUTTON_CLOSE = IDD_PREFERENCE_BOX + 1,
        IDC_LIST_DLGTITLE = IDD_PREFERENCE_BOX + 2,

        IDD_PREFERENCE_SUB_GENRAL = 6100,

        IDD_PREFERENCE_SUB_MULTIINSTANCE = 6150,

        IDD_PREFERENCE_SUB_EDITING = 6200,

        IDD_PREFERENCE_SUB_DELIMITER = 6250,
        
        IDD_PREFERENCE_SUB_CLOUD_LINK = 6260,

        IDD_PREFERENCE_SUB_SEARCHENGINE = 6270,

        IDD_PREFERENCE_SUB_MARGING_BORDER_EDGE = 6290,

        IDD_PREFERENCE_SUB_MISC = 6300,

        IDD_PREFERENCE_SUB_NEWDOCUMENT = 6400,

        IDD_PREFERENCE_SUB_DEFAULTDIRECTORY = 6450,

        IDD_PREFERENCE_SUB_RECENTFILESHISTORY = 6460,

        IDD_PREFERENCE_SUB_LANGUAGE = 6500,

        IDD_PREFERENCE_SUB_HIGHLIGHTING = 6550,

        IDD_PREFERENCE_SUB_PRINT = 6600,

        /// <summary>
        ///  now part of <see cref="IDD_PREFERENCE_SUB_PRINT"/>
        /// </summary>
        IDD_PREFERENCE_PRINT2_BOX = 6700,

        IDD_PREFERENCE_SUB_BACKUP = 6800,

        IDD_PREFERENCE_SUB_AUTOCOMPLETION = 6850,

        IDD_PREFERENCE_SUB_SEARCHING = 6900,

        IDD_PREFERENCE_SUB_DARKMODE = 7100,
    };

    [Flags]
    public enum DockMgrMsg : uint
    {
        IDB_CLOSE_DOWN = 137,
        IDB_CLOSE_UP = 138,
        IDD_CONTAINER_DLG = 139,

        IDC_TAB_CONT = 1027,
        IDC_CLIENT_TAB = 1028,
        IDC_BTN_CAPTION = 1050,

        DMM_MSG = 0x5000,
        DMM_CLOSE = (DMM_MSG + 1),
        DMM_DOCK = (DMM_MSG + 2),
        DMM_FLOAT = (DMM_MSG + 3),
        DMM_DOCKALL = (DMM_MSG + 4),
        DMM_FLOATALL = (DMM_MSG + 5),
        DMM_MOVE = (DMM_MSG + 6),
        DMM_UPDATEDISPINFO = (DMM_MSG + 7),
        DMM_GETIMAGELIST = (DMM_MSG + 8),
        DMM_GETICONPOS = (DMM_MSG + 9),
        DMM_DROPDATA = (DMM_MSG + 10),
        DMM_MOVE_SPLITTER = (DMM_MSG + 11),
        DMM_CANCEL_MOVE = (DMM_MSG + 12),
        DMM_LBUTTONUP = (DMM_MSG + 13),

        DMN_FIRST = 1050,
        DMN_CLOSE = (DMN_FIRST + 1),
        //nmhdr.code = DWORD(DMN_CLOSE, 0));
        //nmhdr.hwndFrom = hwndNpp;
        //nmhdr.idFrom = ctrlIdNpp;

        DMN_DOCK = (DMN_FIRST + 2),
        DMN_FLOAT = (DMN_FIRST + 3)
        //nmhdr.code = DWORD(DMN_XXX, int newContainer);
        //nmhdr.hwndFrom = hwndNpp;
        //nmhdr.idFrom = ctrlIdNpp;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct toolbarIcons
    {
        public IntPtr hToolbarBmp;
        public IntPtr hToolbarIcon;
    }


    public enum BabyGridMsg : uint
    {
        BABYGRID_USER = (0x400/*WM_USER*/ + 7000),

        BGM_GETCELLDATA = BABYGRID_USER + 4,
        BGM_GETROWS = BABYGRID_USER + 23,
        BGM_GETROW = BABYGRID_USER + 27
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct _BGCELL
    {
        public int Row;
        public int Column;
    };

    #endregion

    #region " Scintilla "
    [StructLayout(LayoutKind.Sequential)]
    public struct Sci_NotifyHeader
    {
        /* Compatible with Windows NMHDR.
         * hwndFrom is really an environment specific window handle or pointer
         * but most clients of Scintilla.h do not have this type visible. */
        public IntPtr hwndFrom;
        public IntPtr idFrom;
        public uint code;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SCNotification
    {
        public Sci_NotifyHeader nmhdr;
        public int position;            /* SCN_STYLENEEDED, SCN_MODIFIED, SCN_DWELLSTART, SCN_DWELLEND */
        public int ch;                    /* SCN_CHARADDED, SCN_KEY */
        public int modifiers;            /* SCN_KEY */
        public int modificationType;    /* SCN_MODIFIED */
        public IntPtr text;                /* SCN_MODIFIED, SCN_USERLISTSELECTION, SCN_AUTOCSELECTION */
        public int length;                /* SCN_MODIFIED */
        public int linesAdded;            /* SCN_MODIFIED */
        public int message;                /* SCN_MACRORECORD */
        public IntPtr wParam;                /* SCN_MACRORECORD */
        public IntPtr lParam;                /* SCN_MACRORECORD */
        public int line;                /* SCN_MODIFIED */
        public int foldLevelNow;        /* SCN_MODIFIED */
        public int foldLevelPrev;        /* SCN_MODIFIED */
        public int margin;                /* SCN_MARGINCLICK */
        public int listType;            /* SCN_USERLISTSELECTION */
        public int x;                    /* SCN_DWELLSTART, SCN_DWELLEND */
        public int y;                    /* SCN_DWELLSTART, SCN_DWELLEND */
        public int token;                /* SCN_MODIFIED with SC_MOD_CONTAINER */
        public int annotationLinesAdded;/* SC_MOD_CHANGEANNOTATION */
    }

    [Flags]
    public enum SciMsg : uint
    {
        INVALID_POSITION = 0xFFFFFFFF,
        SCI_START = 2000,
        SCI_OPTIONAL_START = 3000,
        SCI_LEXER_START = 4000,
        SCI_ADDTEXT = 2001,
        SCI_ADDSTYLEDTEXT = 2002,
        SCI_INSERTTEXT = 2003,
        SCI_CLEARALL = 2004,
        SCI_CLEARDOCUMENTSTYLE = 2005,
        SCI_GETLENGTH = 2006,
        SCI_GETCHARAT = 2007,
        SCI_GETCURRENTPOS = 2008,
        SCI_GETANCHOR = 2009,
        SCI_GETSTYLEAT = 2010,
        SCI_REDO = 2011,
        SCI_SETUNDOCOLLECTION = 2012,
        SCI_SELECTALL = 2013,
        SCI_SETSAVEPOINT = 2014,
        SCI_GETSTYLEDTEXT = 2015,
        SCI_CANREDO = 2016,
        SCI_MARKERLINEFROMHANDLE = 2017,
        SCI_MARKERDELETEHANDLE = 2018,
        SCI_GETUNDOCOLLECTION = 2019,
        SCWS_INVISIBLE = 0,
        SCWS_VISIBLEALWAYS = 1,
        SCWS_VISIBLEAFTERINDENT = 2,
        SCI_GETVIEWWS = 2020,
        SCI_SETVIEWWS = 2021,
        SCI_POSITIONFROMPOINT = 2022,
        SCI_POSITIONFROMPOINTCLOSE = 2023,
        SCI_GOTOLINE = 2024,
        SCI_GOTOPOS = 2025,
        SCI_SETANCHOR = 2026,
        SCI_GETCURLINE = 2027,
        SCI_GETENDSTYLED = 2028,
        SC_EOL_CRLF = 0,
        SC_EOL_CR = 1,
        SC_EOL_LF = 2,
        SCI_CONVERTEOLS = 2029,
        SCI_GETEOLMODE = 2030,
        SCI_SETEOLMODE = 2031,
        SCI_STARTSTYLING = 2032,
        SCI_SETSTYLING = 2033,
        SCI_GETBUFFEREDDRAW = 2034,
        SCI_SETBUFFEREDDRAW = 2035,
        SCI_SETTABWIDTH = 2036,
        SCI_GETTABWIDTH = 2121,
        SC_CP_UTF8 = 65001,
        SC_CP_DBCS = 1,
        SCI_SETCODEPAGE = 2037,
        SCI_SETUSEPALETTE = 2039,
        MARKER_MAX = 31,
        SC_MARK_CIRCLE = 0,
        SC_MARK_ROUNDRECT = 1,
        SC_MARK_ARROW = 2,
        SC_MARK_SMALLRECT = 3,
        SC_MARK_SHORTARROW = 4,
        SC_MARK_EMPTY = 5,
        SC_MARK_ARROWDOWN = 6,
        SC_MARK_MINUS = 7,
        SC_MARK_PLUS = 8,
        SC_MARK_VLINE = 9,
        SC_MARK_LCORNER = 10,
        SC_MARK_TCORNER = 11,
        SC_MARK_BOXPLUS = 12,
        SC_MARK_BOXPLUSCONNECTED = 13,
        SC_MARK_BOXMINUS = 14,
        SC_MARK_BOXMINUSCONNECTED = 15,
        SC_MARK_LCORNERCURVE = 16,
        SC_MARK_TCORNERCURVE = 17,
        SC_MARK_CIRCLEPLUS = 18,
        SC_MARK_CIRCLEPLUSCONNECTED = 19,
        SC_MARK_CIRCLEMINUS = 20,
        SC_MARK_CIRCLEMINUSCONNECTED = 21,
        SC_MARK_BACKGROUND = 22,
        SC_MARK_DOTDOTDOT = 23,
        SC_MARK_ARROWS = 24,
        SC_MARK_PIXMAP = 25,
        SC_MARK_FULLRECT = 26,
        SC_MARK_LEFTRECT = 27,
        SC_MARK_AVAILABLE = 28,
        SC_MARK_UNDERLINE = 29,
        SC_MARK_CHARACTER = 10000,
        SC_MARKNUM_FOLDEREND = 25,
        SC_MARKNUM_FOLDEROPENMID = 26,
        SC_MARKNUM_FOLDERMIDTAIL = 27,
        SC_MARKNUM_FOLDERTAIL = 28,
        SC_MARKNUM_FOLDERSUB = 29,
        SC_MARKNUM_FOLDER = 30,
        SC_MARKNUM_FOLDEROPEN = 31,
        SC_MASK_FOLDERS = 0xFE000000,
        SCI_MARKERDEFINE = 2040,
        SCI_MARKERSETFORE = 2041,
        SCI_MARKERSETBACK = 2042,
        SCI_MARKERADD = 2043,
        SCI_MARKERDELETE = 2044,
        SCI_MARKERDELETEALL = 2045,
        SCI_MARKERGET = 2046,
        SCI_MARKERNEXT = 2047,
        SCI_MARKERPREVIOUS = 2048,
        SCI_MARKERDEFINEPIXMAP = 2049,
        SCI_MARKERADDSET = 2466,
        SCI_MARKERSETALPHA = 2476,
        SC_MARGIN_SYMBOL = 0,
        SC_MARGIN_NUMBER = 1,
        SC_MARGIN_BACK = 2,
        SC_MARGIN_FORE = 3,
        SC_MARGIN_TEXT = 4,
        SC_MARGIN_RTEXT = 5,
        SCI_SETMARGINTYPEN = 2240,
        SCI_GETMARGINTYPEN = 2241,
        SCI_SETMARGINWIDTHN = 2242,
        SCI_GETMARGINWIDTHN = 2243,
        SCI_SETMARGINMASKN = 2244,
        SCI_GETMARGINMASKN = 2245,
        SCI_SETMARGINSENSITIVEN = 2246,
        SCI_GETMARGINSENSITIVEN = 2247,
        STYLE_DEFAULT = 32,
        STYLE_LINENUMBER = 33,
        STYLE_BRACELIGHT = 34,
        STYLE_BRACEBAD = 35,
        STYLE_CONTROLCHAR = 36,
        STYLE_INDENTGUIDE = 37,
        STYLE_CALLTIP = 38,
        STYLE_LASTPREDEFINED = 39,
        STYLE_MAX = 255,
        SC_CHARSET_ANSI = 0,
        SC_CHARSET_DEFAULT = 1,
        SC_CHARSET_BALTIC = 186,
        SC_CHARSET_CHINESEBIG5 = 136,
        SC_CHARSET_EASTEUROPE = 238,
        SC_CHARSET_GB2312 = 134,
        SC_CHARSET_GREEK = 161,
        SC_CHARSET_HANGUL = 129,
        SC_CHARSET_MAC = 77,
        SC_CHARSET_OEM = 255,
        SC_CHARSET_RUSSIAN = 204,
        SC_CHARSET_CYRILLIC = 1251,
        SC_CHARSET_SHIFTJIS = 128,
        SC_CHARSET_SYMBOL = 2,
        SC_CHARSET_TURKISH = 162,
        SC_CHARSET_JOHAB = 130,
        SC_CHARSET_HEBREW = 177,
        SC_CHARSET_ARABIC = 178,
        SC_CHARSET_VIETNAMESE = 163,
        SC_CHARSET_THAI = 222,
        SC_CHARSET_8859_15 = 1000,
        SCI_STYLECLEARALL = 2050,
        SCI_STYLESETFORE = 2051,
        SCI_STYLESETBACK = 2052,
        SCI_STYLESETBOLD = 2053,
        SCI_STYLESETITALIC = 2054,
        SCI_STYLESETSIZE = 2055,
        SCI_STYLESETFONT = 2056,
        SCI_STYLESETEOLFILLED = 2057,
        SCI_STYLERESETDEFAULT = 2058,
        SCI_STYLESETUNDERLINE = 2059,
        SC_CASE_MIXED = 0,
        SC_CASE_UPPER = 1,
        SC_CASE_LOWER = 2,
        SCI_STYLEGETFORE = 2481,
        SCI_STYLEGETBACK = 2482,
        SCI_STYLEGETBOLD = 2483,
        SCI_STYLEGETITALIC = 2484,
        SCI_STYLEGETSIZE = 2485,
        SCI_STYLEGETFONT = 2486,
        SCI_STYLEGETEOLFILLED = 2487,
        SCI_STYLEGETUNDERLINE = 2488,
        SCI_STYLEGETCASE = 2489,
        SCI_STYLEGETCHARACTERSET = 2490,
        SCI_STYLEGETVISIBLE = 2491,
        SCI_STYLEGETCHANGEABLE = 2492,
        SCI_STYLEGETHOTSPOT = 2493,
        SCI_STYLESETCASE = 2060,
        SCI_STYLESETCHARACTERSET = 2066,
        SCI_STYLESETHOTSPOT = 2409,
        SCI_SETSELFORE = 2067,
        SCI_SETSELBACK = 2068,
        SCI_GETSELALPHA = 2477,
        SCI_SETSELALPHA = 2478,
        SCI_GETSELEOLFILLED = 2479,
        SCI_SETSELEOLFILLED = 2480,
        SCI_SETCARETFORE = 2069,
        SCI_ASSIGNCMDKEY = 2070,
        SCI_CLEARCMDKEY = 2071,
        SCI_CLEARALLCMDKEYS = 2072,
        SCI_SETSTYLINGEX = 2073,
        SCI_STYLESETVISIBLE = 2074,
        SCI_GETCARETPERIOD = 2075,
        SCI_SETCARETPERIOD = 2076,
        SCI_SETWORDCHARS = 2077,
        SCI_BEGINUNDOACTION = 2078,
        SCI_ENDUNDOACTION = 2079,
        INDIC_PLAIN = 0,
        INDIC_SQUIGGLE = 1,
        INDIC_TT = 2,
        INDIC_DIAGONAL = 3,
        INDIC_STRIKE = 4,
        INDIC_HIDDEN = 5,
        INDIC_BOX = 6,
        INDIC_ROUNDBOX = 7,
        INDIC_MAX = 31,
        INDIC_CONTAINER = 8,
        INDIC0_MASK = 0x20,
        INDIC1_MASK = 0x40,
        INDIC2_MASK = 0x80,
        INDICS_MASK = 0xE0,
        SCI_INDICSETSTYLE = 2080,
        SCI_INDICGETSTYLE = 2081,
        SCI_INDICSETFORE = 2082,
        SCI_INDICGETFORE = 2083,
        SCI_INDICSETUNDER = 2510,
        SCI_INDICGETUNDER = 2511,
        SCI_GETCARETLINEVISIBLEALWAYS = 3095,
        SCI_SETCARETLINEVISIBLEALWAYS = 3096,
        SCI_SETWHITESPACEFORE = 2084,
        SCI_SETWHITESPACEBACK = 2085,
        SCI_SETSTYLEBITS = 2090,
        SCI_GETSTYLEBITS = 2091,
        SCI_SETLINESTATE = 2092,
        SCI_GETLINESTATE = 2093,
        SCI_GETMAXLINESTATE = 2094,
        SCI_GETCARETLINEVISIBLE = 2095,
        SCI_SETCARETLINEVISIBLE = 2096,
        SCI_GETCARETLINEBACK = 2097,
        SCI_SETCARETLINEBACK = 2098,
        SCI_STYLESETCHANGEABLE = 2099,
        SCI_AUTOCSHOW = 2100,
        SCI_AUTOCCANCEL = 2101,
        SCI_AUTOCACTIVE = 2102,
        SCI_AUTOCPOSSTART = 2103,
        SCI_AUTOCCOMPLETE = 2104,
        SCI_AUTOCSTOPS = 2105,
        SCI_AUTOCSETSEPARATOR = 2106,
        SCI_AUTOCGETSEPARATOR = 2107,
        SCI_AUTOCSELECT = 2108,
        SCI_AUTOCSETCANCELATSTART = 2110,
        SCI_AUTOCGETCANCELATSTART = 2111,
        SCI_AUTOCSETFILLUPS = 2112,
        SCI_AUTOCSETCHOOSESINGLE = 2113,
        SCI_AUTOCGETCHOOSESINGLE = 2114,
        SCI_AUTOCSETIGNORECASE = 2115,
        SCI_AUTOCGETIGNORECASE = 2116,
        SCI_USERLISTSHOW = 2117,
        SCI_AUTOCSETAUTOHIDE = 2118,
        SCI_AUTOCGETAUTOHIDE = 2119,
        SCI_AUTOCSETDROPRESTOFWORD = 2270,
        SCI_AUTOCGETDROPRESTOFWORD = 2271,
        SCI_REGISTERIMAGE = 2405,
        SCI_CLEARREGISTEREDIMAGES = 2408,
        SCI_AUTOCGETTYPESEPARATOR = 2285,
        SCI_AUTOCSETTYPESEPARATOR = 2286,
        SCI_AUTOCSETMAXWIDTH = 2208,
        SCI_AUTOCGETMAXWIDTH = 2209,
        SCI_AUTOCSETMAXHEIGHT = 2210,
        SCI_AUTOCGETMAXHEIGHT = 2211,
        SCI_SETINDENT = 2122,
        SCI_GETINDENT = 2123,
        SCI_SETUSETABS = 2124,
        SCI_GETUSETABS = 2125,
        SCI_SETLINEINDENTATION = 2126,
        SCI_GETLINEINDENTATION = 2127,
        SCI_GETLINEINDENTPOSITION = 2128,
        SCI_GETCOLUMN = 2129,
        SCI_SETHSCROLLBAR = 2130,
        SCI_GETHSCROLLBAR = 2131,
        SC_IV_NONE = 0,
        SC_IV_REAL = 1,
        SC_IV_LOOKFORWARD = 2,
        SC_IV_LOOKBOTH = 3,
        SCI_SETINDENTATIONGUIDES = 2132,
        SCI_GETINDENTATIONGUIDES = 2133,
        SCI_SETHIGHLIGHTGUIDE = 2134,
        SCI_GETHIGHLIGHTGUIDE = 2135,
        SCI_GETLINEENDPOSITION = 2136,
        SCI_GETCODEPAGE = 2137,
        SCI_GETCARETFORE = 2138,
        SCI_GETUSEPALETTE = 2139,
        SCI_GETREADONLY = 2140,
        SCI_SETCURRENTPOS = 2141,
        SCI_SETSELECTIONSTART = 2142,
        SCI_GETSELECTIONSTART = 2143,
        SCI_SETSELECTIONEND = 2144,
        SCI_GETSELECTIONEND = 2145,
        SCI_SETPRINTMAGNIFICATION = 2146,
        SCI_GETPRINTMAGNIFICATION = 2147,
        SC_PRINT_NORMAL = 0,
        SC_PRINT_INVERTLIGHT = 1,
        SC_PRINT_BLACKONWHITE = 2,
        SC_PRINT_COLOURONWHITE = 3,
        SC_PRINT_COLOURONWHITEDEFAULTBG = 4,
        SCI_SETPRINTCOLOURMODE = 2148,
        SCI_GETPRINTCOLOURMODE = 2149,
        SCFIND_WHOLEWORD = 2,
        SCFIND_MATCHCASE = 4,
        SCFIND_WORDSTART = 0x00100000,
        SCFIND_REGEXP = 0x00200000,
        SCFIND_POSIX = 0x00400000,
        SCI_FINDTEXT = 2150,
        SCI_FORMATRANGE = 2151,
        SCI_GETFIRSTVISIBLELINE = 2152,
        SCI_GETLINE = 2153,
        SCI_GETLINECOUNT = 2154,
        SCI_SETMARGINLEFT = 2155,
        SCI_GETMARGINLEFT = 2156,
        SCI_SETMARGINRIGHT = 2157,
        SCI_GETMARGINRIGHT = 2158,
        SCI_GETMODIFY = 2159,
        SCI_SETSEL = 2160,
        SCI_GETSELTEXT = 2161,
        SCI_GETTEXTRANGE = 2162,
        SCI_HIDESELECTION = 2163,
        SCI_POINTXFROMPOSITION = 2164,
        SCI_POINTYFROMPOSITION = 2165,
        SCI_LINEFROMPOSITION = 2166,
        SCI_POSITIONFROMLINE = 2167,
        SCI_LINESCROLL = 2168,
        SCI_SCROLLCARET = 2169,
        SCI_REPLACESEL = 2170,
        SCI_SETREADONLY = 2171,
        SCI_NULL = 2172,
        SCI_CANPASTE = 2173,
        SCI_CANUNDO = 2174,
        SCI_EMPTYUNDOBUFFER = 2175,
        SCI_UNDO = 2176,
        SCI_CUT = 2177,
        SCI_COPY = 2178,
        SCI_PASTE = 2179,
        SCI_CLEAR = 2180,
        SCI_SETTEXT = 2181,
        SCI_GETTEXT = 2182,
        SCI_GETTEXTLENGTH = 2183,
        SCI_GETDIRECTFUNCTION = 2184,
        SCI_GETDIRECTPOINTER = 2185,
        SCI_SETOVERTYPE = 2186,
        SCI_GETOVERTYPE = 2187,
        SCI_SETCARETWIDTH = 2188,
        SCI_GETCARETWIDTH = 2189,
        SCI_SETTARGETSTART = 2190,
        SCI_GETTARGETSTART = 2191,
        SCI_SETTARGETEND = 2192,
        SCI_GETTARGETEND = 2193,
        SCI_REPLACETARGET = 2194,
        SCI_REPLACETARGETRE = 2195,
        SCI_SEARCHINTARGET = 2197,
        SCI_SETSEARCHFLAGS = 2198,
        SCI_GETSEARCHFLAGS = 2199,
        SCI_CALLTIPSHOW = 2200,
        SCI_CALLTIPCANCEL = 2201,
        SCI_CALLTIPACTIVE = 2202,
        SCI_CALLTIPPOSSTART = 2203,
        SCI_CALLTIPSETHLT = 2204,
        SCI_CALLTIPSETBACK = 2205,
        SCI_CALLTIPSETFORE = 2206,
        SCI_CALLTIPSETFOREHLT = 2207,
        SCI_CALLTIPUSESTYLE = 2212,
        SCI_VISIBLEFROMDOCLINE = 2220,
        SCI_DOCLINEFROMVISIBLE = 2221,
        SCI_WRAPCOUNT = 2235,
        SC_FOLDLEVELBASE = 0x400,
        SC_FOLDLEVELWHITEFLAG = 0x1000,
        SC_FOLDLEVELHEADERFLAG = 0x2000,
        SC_FOLDLEVELNUMBERMASK = 0x0FFF,
        SCI_SETFOLDLEVEL = 2222,
        SCI_GETFOLDLEVEL = 2223,
        SCI_GETLASTCHILD = 2224,
        SCI_GETFOLDPARENT = 2225,
        SCI_SHOWLINES = 2226,
        SCI_HIDELINES = 2227,
        SCI_GETLINEVISIBLE = 2228,
        SCI_SETFOLDEXPANDED = 2229,
        SCI_GETFOLDEXPANDED = 2230,
        SCI_TOGGLEFOLD = 2231,
        SCI_ENSUREVISIBLE = 2232,
        SC_FOLDFLAG_LINEBEFORE_EXPANDED = 0x0002,
        SC_FOLDFLAG_LINEBEFORE_CONTRACTED = 0x0004,
        SC_FOLDFLAG_LINEAFTER_EXPANDED = 0x0008,
        SC_FOLDFLAG_LINEAFTER_CONTRACTED = 0x0010,
        SC_FOLDFLAG_LEVELNUMBERS = 0x0040,
        SCI_SETFOLDFLAGS = 2233,
        SCI_ENSUREVISIBLEENFORCEPOLICY = 2234,
        SCI_SETTABINDENTS = 2260,
        SCI_GETTABINDENTS = 2261,
        SCI_SETBACKSPACEUNINDENTS = 2262,
        SCI_GETBACKSPACEUNINDENTS = 2263,
        SC_TIME_FOREVER = 10000000,
        SCI_SETMOUSEDWELLTIME = 2264,
        SCI_GETMOUSEDWELLTIME = 2265,
        SCI_WORDSTARTPOSITION = 2266,
        SCI_WORDENDPOSITION = 2267,
        SC_WRAP_NONE = 0,
        SC_WRAP_WORD = 1,
        SC_WRAP_CHAR = 2,
        SCI_SETWRAPMODE = 2268,
        SCI_GETWRAPMODE = 2269,
        SC_WRAPVISUALFLAG_NONE = 0x0000,
        SC_WRAPVISUALFLAG_END = 0x0001,
        SC_WRAPVISUALFLAG_START = 0x0002,
        SCI_SETWRAPVISUALFLAGS = 2460,
        SCI_GETWRAPVISUALFLAGS = 2461,
        SC_WRAPVISUALFLAGLOC_DEFAULT = 0x0000,
        SC_WRAPVISUALFLAGLOC_END_BY_TEXT = 0x0001,
        SC_WRAPVISUALFLAGLOC_START_BY_TEXT = 0x0002,
        SCI_SETWRAPVISUALFLAGSLOCATION = 2462,
        SCI_GETWRAPVISUALFLAGSLOCATION = 2463,
        SCI_SETWRAPSTARTINDENT = 2464,
        SCI_GETWRAPSTARTINDENT = 2465,
        SC_WRAPINDENT_FIXED = 0,
        SC_WRAPINDENT_SAME = 1,
        SC_WRAPINDENT_INDENT = 2,
        SCI_SETWRAPINDENTMODE = 2472,
        SCI_GETWRAPINDENTMODE = 2473,
        SC_CACHE_NONE = 0,
        SC_CACHE_CARET = 1,
        SC_CACHE_PAGE = 2,
        SC_CACHE_DOCUMENT = 3,
        SCI_SETLAYOUTCACHE = 2272,
        SCI_GETLAYOUTCACHE = 2273,
        SCI_SETSCROLLWIDTH = 2274,
        SCI_GETSCROLLWIDTH = 2275,
        SCI_SETSCROLLWIDTHTRACKING = 2516,
        SCI_GETSCROLLWIDTHTRACKING = 2517,
        SCI_TEXTWIDTH = 2276,
        SCI_SETENDATLASTLINE = 2277,
        SCI_GETENDATLASTLINE = 2278,
        SCI_TEXTHEIGHT = 2279,
        SCI_SETVSCROLLBAR = 2280,
        SCI_GETVSCROLLBAR = 2281,
        SCI_APPENDTEXT = 2282,
        SCI_GETTWOPHASEDRAW = 2283,
        SCI_SETTWOPHASEDRAW = 2284,
        SCI_TARGETFROMSELECTION = 2287,
        SCI_LINESJOIN = 2288,
        SCI_LINESSPLIT = 2289,
        SCI_SETFOLDMARGINCOLOUR = 2290,
        SCI_SETFOLDMARGINHICOLOUR = 2291,
        SCI_LINEDOWN = 2300,
        SCI_LINEDOWNEXTEND = 2301,
        SCI_LINEUP = 2302,
        SCI_LINEUPEXTEND = 2303,
        SCI_CHARLEFT = 2304,
        SCI_CHARLEFTEXTEND = 2305,
        SCI_CHARRIGHT = 2306,
        SCI_CHARRIGHTEXTEND = 2307,
        SCI_WORDLEFT = 2308,
        SCI_WORDLEFTEXTEND = 2309,
        SCI_WORDRIGHT = 2310,
        SCI_WORDRIGHTEXTEND = 2311,
        SCI_HOME = 2312,
        SCI_HOMEEXTEND = 2313,
        SCI_LINEEND = 2314,
        SCI_LINEENDEXTEND = 2315,
        SCI_DOCUMENTSTART = 2316,
        SCI_DOCUMENTSTARTEXTEND = 2317,
        SCI_DOCUMENTEND = 2318,
        SCI_DOCUMENTENDEXTEND = 2319,
        SCI_PAGEUP = 2320,
        SCI_PAGEUPEXTEND = 2321,
        SCI_PAGEDOWN = 2322,
        SCI_PAGEDOWNEXTEND = 2323,
        SCI_EDITTOGGLEOVERTYPE = 2324,
        SCI_CANCEL = 2325,
        SCI_DELETEBACK = 2326,
        SCI_TAB = 2327,
        SCI_BACKTAB = 2328,
        SCI_NEWLINE = 2329,
        SCI_FORMFEED = 2330,
        SCI_VCHOME = 2331,
        SCI_VCHOMEEXTEND = 2332,
        SCI_ZOOMIN = 2333,
        SCI_ZOOMOUT = 2334,
        SCI_DELWORDLEFT = 2335,
        SCI_DELWORDRIGHT = 2336,
        SCI_DELWORDRIGHTEND = 2518,
        SCI_LINECUT = 2337,
        SCI_LINEDELETE = 2338,
        SCI_LINETRANSPOSE = 2339,
        SCI_LINEDUPLICATE = 2404,
        SCI_LOWERCASE = 2340,
        SCI_UPPERCASE = 2341,
        SCI_LINESCROLLDOWN = 2342,
        SCI_LINESCROLLUP = 2343,
        SCI_DELETEBACKNOTLINE = 2344,
        SCI_HOMEDISPLAY = 2345,
        SCI_HOMEDISPLAYEXTEND = 2346,
        SCI_LINEENDDISPLAY = 2347,
        SCI_LINEENDDISPLAYEXTEND = 2348,
        SCI_HOMEWRAP = 2349,
        SCI_HOMEWRAPEXTEND = 2450,
        SCI_LINEENDWRAP = 2451,
        SCI_LINEENDWRAPEXTEND = 2452,
        SCI_VCHOMEWRAP = 2453,
        SCI_VCHOMEWRAPEXTEND = 2454,
        SCI_LINECOPY = 2455,
        SCI_MOVECARETINSIDEVIEW = 2401,
        SCI_LINELENGTH = 2350,
        SCI_BRACEHIGHLIGHT = 2351,
        SCI_BRACEBADLIGHT = 2352,
        SCI_BRACEMATCH = 2353,
        SCI_GETVIEWEOL = 2355,
        SCI_SETVIEWEOL = 2356,
        SCI_GETDOCPOINTER = 2357,
        SCI_SETDOCPOINTER = 2358,
        SCI_SETMODEVENTMASK = 2359,
        EDGE_NONE = 0,
        EDGE_LINE = 1,
        EDGE_BACKGROUND = 2,
        SCI_GETEDGECOLUMN = 2360,
        SCI_SETEDGECOLUMN = 2361,
        SCI_GETEDGEMODE = 2362,
        SCI_SETEDGEMODE = 2363,
        SCI_GETEDGECOLOUR = 2364,
        SCI_SETEDGECOLOUR = 2365,
        SCI_SEARCHANCHOR = 2366,
        SCI_SEARCHNEXT = 2367,
        SCI_SEARCHPREV = 2368,
        SCI_LINESONSCREEN = 2370,
        SCI_USEPOPUP = 2371,
        SCI_SELECTIONISRECTANGLE = 2372,
        SCI_SETZOOM = 2373,
        SCI_GETZOOM = 2374,
        SCI_CREATEDOCUMENT = 2375,
        SCI_ADDREFDOCUMENT = 2376,
        SCI_RELEASEDOCUMENT = 2377,
        SCI_GETMODEVENTMASK = 2378,
        SCI_SETFOCUS = 2380,
        SCI_GETFOCUS = 2381,
        SC_STATUS_OK = 0,
        SC_STATUS_FAILURE = 1,
        SC_STATUS_BADALLOC = 2,
        SCI_SETSTATUS = 2382,
        SCI_GETSTATUS = 2383,
        SCI_SETMOUSEDOWNCAPTURES = 2384,
        SCI_GETMOUSEDOWNCAPTURES = 2385,
        SC_CURSORNORMAL = 0xFFFFFFFF,
        SC_CURSORWAIT = 4,
        SCI_SETCURSOR = 2386,
        SCI_GETCURSOR = 2387,
        SCI_SETCONTROLCHARSYMBOL = 2388,
        SCI_GETCONTROLCHARSYMBOL = 2389,
        SCI_WORDPARTLEFT = 2390,
        SCI_WORDPARTLEFTEXTEND = 2391,
        SCI_WORDPARTRIGHT = 2392,
        SCI_WORDPARTRIGHTEXTEND = 2393,
        VISIBLE_SLOP = 0x01,
        VISIBLE_STRICT = 0x04,
        SCI_SETVISIBLEPOLICY = 2394,
        SCI_DELLINELEFT = 2395,
        SCI_DELLINERIGHT = 2396,
        SCI_SETXOFFSET = 2397,
        SCI_GETXOFFSET = 2398,
        SCI_CHOOSECARETX = 2399,
        SCI_GRABFOCUS = 2400,
        CARET_SLOP = 0x01,
        CARET_STRICT = 0x04,
        CARET_JUMPS = 0x10,
        CARET_EVEN = 0x08,
        SCI_SETXCARETPOLICY = 2402,
        SCI_SETYCARETPOLICY = 2403,
        SCI_SETPRINTWRAPMODE = 2406,
        SCI_GETPRINTWRAPMODE = 2407,
        SCI_SETHOTSPOTACTIVEFORE = 2410,
        SCI_GETHOTSPOTACTIVEFORE = 2494,
        SCI_SETHOTSPOTACTIVEBACK = 2411,
        SCI_GETHOTSPOTACTIVEBACK = 2495,
        SCI_SETHOTSPOTACTIVEUNDERLINE = 2412,
        SCI_GETHOTSPOTACTIVEUNDERLINE = 2496,
        SCI_SETHOTSPOTSINGLELINE = 2421,
        SCI_GETHOTSPOTSINGLELINE = 2497,
        SCI_PARADOWN = 2413,
        SCI_PARADOWNEXTEND = 2414,
        SCI_PARAUP = 2415,
        SCI_PARAUPEXTEND = 2416,
        SCI_POSITIONBEFORE = 2417,
        SCI_POSITIONAFTER = 2418,
        SCI_COPYRANGE = 2419,
        SCI_COPYTEXT = 2420,
        SC_SEL_STREAM = 0,
        SC_SEL_RECTANGLE = 1,
        SC_SEL_LINES = 2,
        SC_SEL_THIN = 3,
        SCI_SETSELECTIONMODE = 2422,
        SCI_GETSELECTIONMODE = 2423,
        SCI_GETLINESELSTARTPOSITION = 2424,
        SCI_GETLINESELENDPOSITION = 2425,
        SCI_LINEDOWNRECTEXTEND = 2426,
        SCI_LINEUPRECTEXTEND = 2427,
        SCI_CHARLEFTRECTEXTEND = 2428,
        SCI_CHARRIGHTRECTEXTEND = 2429,
        SCI_HOMERECTEXTEND = 2430,
        SCI_VCHOMERECTEXTEND = 2431,
        SCI_LINEENDRECTEXTEND = 2432,
        SCI_PAGEUPRECTEXTEND = 2433,
        SCI_PAGEDOWNRECTEXTEND = 2434,
        SCI_STUTTEREDPAGEUP = 2435,
        SCI_STUTTEREDPAGEUPEXTEND = 2436,
        SCI_STUTTEREDPAGEDOWN = 2437,
        SCI_STUTTEREDPAGEDOWNEXTEND = 2438,
        SCI_WORDLEFTEND = 2439,
        SCI_WORDLEFTENDEXTEND = 2440,
        SCI_WORDRIGHTEND = 2441,
        SCI_WORDRIGHTENDEXTEND = 2442,
        SCI_SETWHITESPACECHARS = 2443,
        SCI_SETCHARSDEFAULT = 2444,
        SCI_AUTOCGETCURRENT = 2445,
        SCI_ALLOCATE = 2446,
        SCI_TARGETASUTF8 = 2447,
        SCI_SETLENGTHFORENCODE = 2448,
        SCI_ENCODEDFROMUTF8 = 2449,
        SCI_FINDCOLUMN = 2456,
        SCI_GETCARETSTICKY = 2457,
        SCI_SETCARETSTICKY = 2458,
        SCI_TOGGLECARETSTICKY = 2459,
        SCI_SETPASTECONVERTENDINGS = 2467,
        SCI_GETPASTECONVERTENDINGS = 2468,
        SCI_SELECTIONDUPLICATE = 2469,
        SC_ALPHA_TRANSPARENT = 0,
        SC_ALPHA_OPAQUE = 255,
        SC_ALPHA_NOALPHA = 256,
        SCI_SETCARETLINEBACKALPHA = 2470,
        SCI_GETCARETLINEBACKALPHA = 2471,
        CARETSTYLE_INVISIBLE = 0,
        CARETSTYLE_LINE = 1,
        CARETSTYLE_BLOCK = 2,
        SCI_SETCARETSTYLE = 2512,
        SCI_GETCARETSTYLE = 2513,
        SCI_SETINDICATORCURRENT = 2500,
        SCI_GETINDICATORCURRENT = 2501,
        SCI_SETINDICATORVALUE = 2502,
        SCI_GETINDICATORVALUE = 2503,
        SCI_INDICATORFILLRANGE = 2504,
        SCI_INDICATORCLEARRANGE = 2505,
        SCI_INDICATORALLONFOR = 2506,
        SCI_INDICATORVALUEAT = 2507,
        SCI_INDICATORSTART = 2508,
        SCI_INDICATOREND = 2509,
        SCI_SETPOSITIONCACHE = 2514,
        SCI_GETPOSITIONCACHE = 2515,
        SCI_COPYALLOWLINE = 2519,
        SCI_GETCHARACTERPOINTER = 2520,
        SCI_SETKEYSUNICODE = 2521,
        SCI_GETKEYSUNICODE = 2522,
        SCI_INDICSETALPHA = 2523,
        SCI_INDICGETALPHA = 2524,
        SCI_SETEXTRAASCENT = 2525,
        SCI_GETEXTRAASCENT = 2526,
        SCI_SETEXTRADESCENT = 2527,
        SCI_GETEXTRADESCENT = 2528,
        SCI_MARKERSYMBOLDEFINED = 2529,
        SCI_MARGINSETTEXT = 2530,
        SCI_MARGINGETTEXT = 2531,
        SCI_MARGINSETSTYLE = 2532,
        SCI_MARGINGETSTYLE = 2533,
        SCI_MARGINSETSTYLES = 2534,
        SCI_MARGINGETSTYLES = 2535,
        SCI_MARGINTEXTCLEARALL = 2536,
        SCI_MARGINSETSTYLEOFFSET = 2537,
        SCI_MARGINGETSTYLEOFFSET = 2538,
        SCI_ANNOTATIONSETTEXT = 2540,
        SCI_ANNOTATIONGETTEXT = 2541,
        SCI_ANNOTATIONSETSTYLE = 2542,
        SCI_ANNOTATIONGETSTYLE = 2543,
        SCI_ANNOTATIONSETSTYLES = 2544,
        SCI_ANNOTATIONGETSTYLES = 2545,
        SCI_ANNOTATIONGETLINES = 2546,
        SCI_ANNOTATIONCLEARALL = 2547,
        ANNOTATION_HIDDEN = 0,
        ANNOTATION_STANDARD = 1,
        ANNOTATION_BOXED = 2,
        SCI_ANNOTATIONSETVISIBLE = 2548,
        SCI_ANNOTATIONGETVISIBLE = 2549,
        SCI_ANNOTATIONSETSTYLEOFFSET = 2550,
        SCI_ANNOTATIONGETSTYLEOFFSET = 2551,
        UNDO_MAY_COALESCE = 1,
        SCI_ADDUNDOACTION = 2560,
        SCI_CHARPOSITIONFROMPOINT = 2561,
        SCI_CHARPOSITIONFROMPOINTCLOSE = 2562,
        SCI_SETMULTIPLESELECTION = 2563,
        SCI_GETMULTIPLESELECTION = 2564,
        SCI_SETADDITIONALSELECTIONTYPING = 2565,
        SCI_GETADDITIONALSELECTIONTYPING = 2566,
        SCI_SETADDITIONALCARETSBLINK = 2567,
        SCI_GETADDITIONALCARETSBLINK = 2568,
        SCI_GETSELECTIONS = 2570,
        SCI_CLEARSELECTIONS = 2571,
        SCI_SETSELECTION = 2572,
        SCI_ADDSELECTION = 2573,
        SCI_SETMAINSELECTION = 2574,
        SCI_GETMAINSELECTION = 2575,
        SCI_SETSELECTIONNCARET = 2576,
        SCI_GETSELECTIONNCARET = 2577,
        SCI_SETSELECTIONNANCHOR = 2578,
        SCI_GETSELECTIONNANCHOR = 2579,
        SCI_SETSELECTIONNCARETVIRTUALSPACE = 2580,
        SCI_GETSELECTIONNCARETVIRTUALSPACE = 2581,
        SCI_SETSELECTIONNANCHORVIRTUALSPACE = 2582,
        SCI_GETSELECTIONNANCHORVIRTUALSPACE = 2583,
        SCI_SETSELECTIONNSTART = 2584,
        SCI_GETSELECTIONNSTART = 2585,
        SCI_SETSELECTIONNEND = 2586,
        SCI_GETSELECTIONNEND = 2587,
        SCI_SETRECTANGULARSELECTIONCARET = 2588,
        SCI_GETRECTANGULARSELECTIONCARET = 2589,
        SCI_SETRECTANGULARSELECTIONANCHOR = 2590,
        SCI_GETRECTANGULARSELECTIONANCHOR = 2591,
        SCI_SETRECTANGULARSELECTIONCARETVIRTUALSPACE = 2592,
        SCI_GETRECTANGULARSELECTIONCARETVIRTUALSPACE = 2593,
        SCI_SETRECTANGULARSELECTIONANCHORVIRTUALSPACE = 2594,
        SCI_GETRECTANGULARSELECTIONANCHORVIRTUALSPACE = 2595,
        SCVS_NONE = 0,
        SCVS_RECTANGULARSELECTION = 1,
        SCVS_USERACCESSIBLE = 2,
        SCI_SETVIRTUALSPACEOPTIONS = 2596,
        SCI_GETVIRTUALSPACEOPTIONS = 2597,
        SCI_SETRECTANGULARSELECTIONMODIFIER = 2598,
        SCI_GETRECTANGULARSELECTIONMODIFIER = 2599,
        SCI_SETADDITIONALSELFORE = 2600,
        SCI_SETADDITIONALSELBACK = 2601,
        SCI_SETADDITIONALSELALPHA = 2602,
        SCI_GETADDITIONALSELALPHA = 2603,
        SCI_SETADDITIONALCARETFORE = 2604,
        SCI_GETADDITIONALCARETFORE = 2605,
        SCI_ROTATESELECTION = 2606,
        SCI_SWAPMAINANCHORCARET = 2607,
        SCI_STARTRECORD = 3001,
        SCI_STOPRECORD = 3002,
        SCI_SETLEXER = 4001,
        SCI_GETLEXER = 4002,
        SCI_COLOURISE = 4003,
        SCI_SETPROPERTY = 4004,
        KEYWORDSET_MAX = 8,
        SCI_SETKEYWORDS = 4005,
        SCI_SETLEXERLANGUAGE = 4006,
        SCI_LOADLEXERLIBRARY = 4007,
        SCI_GETPROPERTY = 4008,
        SCI_GETPROPERTYEXPANDED = 4009,
        SCI_GETPROPERTYINT = 4010,
        SCI_GETSTYLEBITSNEEDED = 4011,
        SC_MOD_INSERTTEXT = 0x1,
        SC_MOD_DELETETEXT = 0x2,
        SC_MOD_CHANGESTYLE = 0x4,
        SC_MOD_CHANGEFOLD = 0x8,
        SC_PERFORMED_USER = 0x10,
        SC_PERFORMED_UNDO = 0x20,
        SC_PERFORMED_REDO = 0x40,
        SC_MULTISTEPUNDOREDO = 0x80,
        SC_LASTSTEPINUNDOREDO = 0x100,
        SC_MOD_CHANGEMARKER = 0x200,
        SC_MOD_BEFOREINSERT = 0x400,
        SC_MOD_BEFOREDELETE = 0x800,
        SC_MULTILINEUNDOREDO = 0x1000,
        SC_STARTACTION = 0x2000,
        SC_MOD_CHANGEINDICATOR = 0x4000,
        SC_MOD_CHANGELINESTATE = 0x8000,
        SC_MOD_CHANGEMARGIN = 0x10000,
        SC_MOD_CHANGEANNOTATION = 0x20000,
        SC_MOD_CONTAINER = 0x40000,
        SC_MODEVENTMASKALL = 0x7FFFF,
        SC_SEARCHRESULT_LINEBUFFERMAXLENGTH = 1024,
        SCEN_CHANGE = 768,
        SCEN_SETFOCUS = 512,
        SCEN_KILLFOCUS = 256,
        SCK_DOWN = 300,
        SCK_UP = 301,
        SCK_LEFT = 302,
        SCK_RIGHT = 303,
        SCK_HOME = 304,
        SCK_END = 305,
        SCK_PRIOR = 306,
        SCK_NEXT = 307,
        SCK_DELETE = 308,
        SCK_INSERT = 309,
        SCK_ESCAPE = 7,
        SCK_BACK = 8,
        SCK_TAB = 9,
        SCK_RETURN = 13,
        SCK_ADD = 310,
        SCK_SUBTRACT = 311,
        SCK_DIVIDE = 312,
        SCK_WIN = 313,
        SCK_RWIN = 314,
        SCK_MENU = 315,
        SCMOD_NORM = 0,
        SCMOD_SHIFT = 1,
        SCMOD_CTRL = 2,
        SCMOD_ALT = 4,
        SCMOD_SUPER = 8,
        SCN_STYLENEEDED = 2000,
        SCN_CHARADDED = 2001,
        SCN_SAVEPOINTREACHED = 2002,
        SCN_SAVEPOINTLEFT = 2003,
        SCN_MODIFYATTEMPTRO = 2004,
        SCN_KEY = 2005,
        SCN_DOUBLECLICK = 2006,
        SCN_UPDATEUI = 2007,
        SCN_MODIFIED = 2008,
        SCN_MACRORECORD = 2009,
        SCN_MARGINCLICK = 2010,
        SCN_NEEDSHOWN = 2011,
        SCN_PAINTED = 2013,
        SCN_USERLISTSELECTION = 2014,
        SCN_URIDROPPED = 2015,
        SCN_DWELLSTART = 2016,
        SCN_DWELLEND = 2017,
        SCN_ZOOM = 2018,
        SCN_HOTSPOTCLICK = 2019,
        SCN_HOTSPOTDOUBLECLICK = 2020,
        SCN_CALLTIPCLICK = 2021,
        SCN_AUTOCSELECTION = 2022,
        SCN_INDICATORCLICK = 2023,
        SCN_INDICATORRELEASE = 2024,
        SCN_AUTOCCANCELLED = 2025,
        SCN_AUTOCCHARDELETED = 2026,
        SCN_SCROLLED = 2080
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Sci_CharacterRange
    {
        public Sci_CharacterRange(int cpmin, int cpmax) { cpMin = cpmin; cpMax = cpmax; }
        public int cpMin;
        public int cpMax;
    }

    public class Sci_TextRange : IDisposable
    {
        _Sci_TextRange _sciTextRange;
        IntPtr _ptrSciTextRange;
        bool _disposed = false;

        public Sci_TextRange(Sci_CharacterRange chrRange, int stringCapacity)
        {
            _sciTextRange.chrg = chrRange;
            _sciTextRange.lpstrText = Marshal.AllocHGlobal(stringCapacity);
        }
        public Sci_TextRange(int cpmin, int cpmax, int stringCapacity)
        {
            _sciTextRange.chrg.cpMin = cpmin;
            _sciTextRange.chrg.cpMax = cpmax;
            _sciTextRange.lpstrText = Marshal.AllocHGlobal(stringCapacity);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct _Sci_TextRange
        {
            public Sci_CharacterRange chrg;
            public IntPtr lpstrText;
        }

        public IntPtr NativePointer { get { _initNativeStruct(); return _ptrSciTextRange; } }
        public string lpstrText { get { _readNativeStruct(); return Marshal.PtrToStringAnsi(_sciTextRange.lpstrText); } }
        public Sci_CharacterRange chrg { get { _readNativeStruct(); return _sciTextRange.chrg; } set { _sciTextRange.chrg = value; _initNativeStruct(); } }
        void _initNativeStruct()
        {
            if (_ptrSciTextRange == IntPtr.Zero)
                _ptrSciTextRange = Marshal.AllocHGlobal(Marshal.SizeOf(_sciTextRange));
            Marshal.StructureToPtr(_sciTextRange, _ptrSciTextRange, false);
        }
        void _readNativeStruct()
        {
            if (_ptrSciTextRange != IntPtr.Zero)
                _sciTextRange = (_Sci_TextRange)Marshal.PtrToStructure(_ptrSciTextRange, typeof(_Sci_TextRange));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_sciTextRange.lpstrText != IntPtr.Zero) Marshal.FreeHGlobal(_sciTextRange.lpstrText);
                if (_ptrSciTextRange != IntPtr.Zero) Marshal.FreeHGlobal(_ptrSciTextRange);
                _disposed = true;
            }
        }
        ~Sci_TextRange()
        {
            Dispose();
        }
    }

    public class Sci_TextToFind : IDisposable
    {
        _Sci_TextToFind _sciTextToFind;
        IntPtr _ptrSciTextToFind;
        bool _disposed = false;

        public Sci_TextToFind(Sci_CharacterRange chrRange, string searchText)
        {
            _sciTextToFind.chrg = chrRange;
            _sciTextToFind.lpstrText = Marshal.StringToHGlobalAnsi(searchText);
        }
        public Sci_TextToFind(int cpmin, int cpmax, string searchText)
        {
            _sciTextToFind.chrg.cpMin = cpmin;
            _sciTextToFind.chrg.cpMax = cpmax;
            _sciTextToFind.lpstrText = Marshal.StringToHGlobalAnsi(searchText);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct _Sci_TextToFind
        {
            public Sci_CharacterRange chrg;
            public IntPtr lpstrText;
            public Sci_CharacterRange chrgText;
        }

        public IntPtr NativePointer { get { _initNativeStruct(); return _ptrSciTextToFind; } }
        public string lpstrText { set { _freeNativeString(); _sciTextToFind.lpstrText = Marshal.StringToHGlobalAnsi(value); } }
        public Sci_CharacterRange chrg { get { _readNativeStruct(); return _sciTextToFind.chrg; } set { _sciTextToFind.chrg = value; _initNativeStruct(); } }
        public Sci_CharacterRange chrgText { get { _readNativeStruct(); return _sciTextToFind.chrgText; } }
        void _initNativeStruct()
        {
            if (_ptrSciTextToFind == IntPtr.Zero)
                _ptrSciTextToFind = Marshal.AllocHGlobal(Marshal.SizeOf(_sciTextToFind));
            Marshal.StructureToPtr(_sciTextToFind, _ptrSciTextToFind, false);
        }
        void _readNativeStruct()
        {
            if (_ptrSciTextToFind != IntPtr.Zero)
                _sciTextToFind = (_Sci_TextToFind)Marshal.PtrToStructure(_ptrSciTextToFind, typeof(_Sci_TextToFind));
        }
        void _freeNativeString()
        {
            if (_sciTextToFind.lpstrText != IntPtr.Zero) Marshal.FreeHGlobal(_sciTextToFind.lpstrText);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _freeNativeString();
                if (_ptrSciTextToFind != IntPtr.Zero) Marshal.FreeHGlobal(_ptrSciTextToFind);
                _disposed = true;
            }
        }
        ~Sci_TextToFind()
        {
            Dispose();
        }
    }
    #endregion

    #region " Platform "
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public RECT(int left, int top, int right, int bottom)
        {
            Left = left; Top = top; Right = right; Bottom = bottom;
        }
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public class Win32
    {
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, NppMsg Msg, int wParam, NppMenuCmd lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, NppMsg Msg, int wParam, IntPtr lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, NppMsg Msg, int wParam, int lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, NppMsg Msg, int wParam, out int lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, NppMsg Msg, IntPtr wParam, int lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, NppMsg Msg, int wParam, ref LangType lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, NppMsg Msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, NppMsg Msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, NppMsg Msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, BabyGridMsg Msg, int wParam, int lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, BabyGridMsg Msg, ref _BGCELL wParam, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lParam);

        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, SciMsg Msg, int wParam, IntPtr lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, SciMsg Msg, int wParam, string lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, SciMsg Msg, int wParam, [MarshalAs(UnmanagedType.LPStr)] StringBuilder lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, SciMsg Msg, int wParam, int lParam);

        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, ref REBARBANDINFO lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, ref NMHDR lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, ref TBBUTTONINFO lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        public const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        public const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

        [DllImport("kernel32.dll")]
        public static extern int FormatMessage(int dwFlags, int lpSource,
            int dwMessageId, int dwLanguageId, ref String lpBuffer, int nSize, int Arguments);

        public static string GetErrorMessage(int errorCode)
        {
            int messageSize = 255;
            string lpMsgBuf = "";
            int dwFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS;

            int retVal = FormatMessage(dwFlags, 0, errorCode, 0, ref lpMsgBuf, messageSize, 0);
            if (0 == retVal)
            {
                return null;
            }
            else
            {
                return lpMsgBuf;
            }
        }

        public const int MAX_PATH = 260;
        [DllImport("kernel32")]
        public static extern int GetPrivateProfileInt(string lpAppName, string lpKeyName, int nDefault, string lpFileName);
        [DllImport("kernel32")]
        public static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

        public const uint MF_BYCOMMAND = 0x00000000;
        public const uint MF_BYPOSITION = 0x00000400;
        public const uint MF_CHECKED = 0x00000008;
        public const uint MF_UNCHECKED = 0x00000000;
        public const uint MF_ENABLED = 0x00000000;
        public const uint MF_GRAYED = 0x00000001;
        public const uint MF_DISABLED = 0x00000002;

        [DllImport("user32")]
        public static extern IntPtr GetMenu(IntPtr hWnd);
        [DllImport("user32")]
        public static extern int CheckMenuItem(IntPtr hmenu, int uIDCheckItem, int uCheck);

        public const int MA_ACTIVATE = 1;
        public const int MA_ACTIVATEANDEAT = 2;
        public const int MA_NOACTIVATE = 3;
        public const int MA_NOACTIVATEANDEAT = 4;

        public const int WA_INACTIVE = 0;
        public const int WA_ACTIVE = 1;
        public const int WA_CLICKACTIVE = 2;

        public const int WM_CREATE = 0x0001;
        public const int WM_DESTROY = 0x0002;
        public const int WM_ACTIVATE = 0x0006;
        public const int WM_ACTIVATEAPP = 0x001C;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int WM_NOTIFY = 0x004E;
        public const int EM_SETMARGINS = 0x00D3;
        public const int EM_GETMARGINS = 0x00D3;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int WM_UNICHAR = 0x0109;
        public const int WM_INITDIALOG = 0x0110;
        public const int WM_COMMAND = 0x0111;

        public const int WM_REFLECT = 0x2000;

        public const int EM_SETCUEBANNER = 0x1501;

        public const int EC_LEFTMARGIN = 1;
        public const int EC_RIGHTMARGIN = 2;

        public const int TB_GETBITMAP = 0x0400 + 44;
        public const int TB_GETIMAGELIST = 0x0400 + 49;
        public const int TB_GETBUTTONSIZE = 0x0400 + 58;
        public const int TB_GETBUTTONINFOW = 0x0400 + 63;

        public const uint TBIF_IMAGE = 0x1;
        public const uint TBIF_TEXT = 0x2;
        public const uint TBIF_STATE = 0x4;
        public const uint TBIF_STYLE = 0x8;
        public const uint TBIF_LPARAM = 0x10;
        public const uint TBIF_COMMAND = 0x20;
        public const uint TBIF_SIZE = 0x40;
        public const uint TBIF_BYINDEX = 0x80000000u;

        [DllImport("user32.dll")]
        public static extern bool GetComboBoxInfo(IntPtr hwnd, ref COMBOBOXINFO pcbi);

        [StructLayout(LayoutKind.Sequential)]
        public struct NMHDR
        {
            public IntPtr hwndFrom;
            public UIntPtr idFrom;
            public uint code;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COMBOBOXINFO
        {
            public int cbSize;
            public RECT rcItem;
            public RECT rcButton;
            public IntPtr stateButton;
            public IntPtr hwndCombo;
            public IntPtr hwndItem;
            public IntPtr hwndList;
        }

        [Flags]
        public enum ImageListDrawingStyle : int
        {
            Normal = 0x00000000,
            Transparent = 0x00000001,
            Blend25 = 0x00000002,
            Blend50 = 0x00000004,
            Mask = 0x00000010,
            Image = 0x00000020,
            Rop = 0x00000040,
            OverlayMask = 0x00000F00,
            PreserveAlpha = 0x00001000, // This preserves the alpha channel in dest
            Scale = 0x00002000, // Causes the image to be scaled to cx, cy instead of clipped
            DpiScale = 0x00004000,
            Async = 0x00008000,
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        //[DllImport("user32.dll")]
        //public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        public static string GetWindowText(IntPtr hWnd)
        {
            int len = GetWindowTextLength(hWnd);
            if (len <= 0)
                return "";

            StringBuilder sb = new StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);

            return sb.ToString();
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hwnd, String lpString);

        public const int SW_HIDE = 0;
        public const int SW_SHOWNOACTIVATE = 4;

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;
        public static IntPtr HWND_TOP = (IntPtr)0;
        public static IntPtr HWND_TOPMOST = (IntPtr)(-1);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref System.Drawing.Point lpPoint);

        public const int GWL_WNDPROC = -4;
        public const int GWL_HINSTANCE = -6;
        public const int GWL_HWNDPARENT = -8;
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int GWL_USERDATA = -21;
        public const int GWL_ID = -12;

        public const int WS_CLIPCHILDREN = 0x02000000;
        public const int WS_CHILD = 0x40000000;
        public const int BS_PUSHBUTTON = 0x00000000;
        public const int BS_DEFPUSHBUTTON = 0x00000001;
        public const int BS_CHECKBOX = 0x00000002;
        public const int BS_AUTOCHECKBOX = 0x00000003;
        public const int BS_RADIOBUTTON = 0x00000004;
        public const int BS_3STATE = 0x00000005;
        public const int BS_AUTO3STATE = 0x00000006;
        public const int BS_GROUPBOX = 0x00000007;
        public const int BS_USERBUTTON = 0x00000008;
        public const int BS_AUTORADIOBUTTON = 0x00000009;
        public const int BS_PUSHBOX = 0x0000000A;
        public const int BS_OWNERDRAW = 0x0000000B;
        public const int BS_TYPEMASK = 0x0000000F;
        public const int BS_LEFTTEXT = 0x00000020;
        public const int BS_TEXT = 0x00000000;
        public const int BS_ICON = 0x00000040;
        public const int BS_BITMAP = 0x00000080;
        public const int BS_LEFT = 0x00000100;
        public const int BS_RIGHT = 0x00000200;
        public const int BS_CENTER = 0x00000300;
        public const int BS_TOP = 0x00000400;
        public const int BS_BOTTOM = 0x00000800;
        public const int BS_VCENTER = 0x00000C00;
        public const int BS_PUSHLIKE = 0x00001000;
        public const int BS_MULTILINE = 0x00002000;
        public const int BS_NOTIFY = 0x00004000;
        public const int BS_FLAT = 0x00008000;
        public const int BS_RIGHTBUTTON = BS_LEFTTEXT;

        public const int WS_EX_DLGMODALFRAME = 0x00000001;
        public const int WS_EX_NOPARENTNOTIFY = 0x00000004;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_ACCEPTFILES = 0x00000010;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_MDICHILD = 0x00000040;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_WINDOWEDGE = 0x00000100;
        public const int WS_EX_CLIENTEDGE = 0x00000200;
        public const int WS_EX_CONTEXTHELP = 0x00000400;
        public const int WS_EX_RIGHT = 0x00001000;
        public const int WS_EX_LEFT = 0x00000000;
        public const int WS_EX_RTLREADING = 0x00002000;
        public const int WS_EX_LTRREADING = 0x00000000;
        public const int WS_EX_LEFTSCROLLBAR = 0x00004000;
        public const int WS_EX_RIGHTSCROLLBAR = 0x00000000;
        public const int WS_EX_CONTROLPARENT = 0x00010000;
        public const int WS_EX_STATICEDGE = 0x00020000;
        public const int WS_EX_APPWINDOW = 0x00040000;
        public const int WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE);
        public const int WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetWindowLong(hWnd, nIndex));
        }

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        public const uint MIIM_BITMAP = 0x00000080;
        public const uint MIIM_CHECKMARKS = 0x00000008;
        public const uint MIIM_DATA = 0x00000020;
        public const uint MIIM_FTYPE = 0x00000100;
        public const uint MIIM_ID = 0x00000002;
        public const uint MIIM_STATE = 0x00000001;
        public const uint MIIM_STRING = 0x00000040;
        public const uint MIIM_SUBMENU = 0x00000004;
        public const uint MIIM_TYPE = 0x00000010;

        public const uint MFT_BITMAP = 0x00000004;
        public const uint MFT_MENUBARBREAK = 0x00000020;
        public const uint MFT_MENUBREAK = 0x00000040;
        public const uint MFT_OWNERDRAW = 0x00000100;
        public const uint MFT_RADIOCHECK = 0x00000200;
        public const uint MFT_RIGHTJUSTIFY = 0x00004000;
        public const uint MFT_RIGHTORDER = 0x00002000;
        public const uint MFT_SEPARATOR = 0x00000800;
        public const uint MFT_STRING = 0x00000000;

        public const uint MFS_CHECKED = 0x00000008;
        public const uint MFS_DEFAULT = 0x00001000;
        public const uint MFS_DISABLED = 0x00000003;
        public const uint MFS_ENABLED = 0x00000000;
        public const uint MFS_GRAYED = 0x00000003;
        public const uint MFS_HILITE = 0x00000080;
        public const uint MFS_UNCHECKED = 0x00000000;
        public const uint MFS_UNHILITE = 0x00000000;

        [StructLayout(LayoutKind.Sequential)]
        public struct MENUITEMINFO
        {
            public uint cbSize;
            public uint fMask;
            public uint fType;
            public uint fState;
            public uint wID;
            public IntPtr hSubMenu;
            public IntPtr hbmpChecked;
            public IntPtr hbmpUnchecked;
            public IntPtr dwItemData;
            public IntPtr dwTypeData;
            public uint cch;
            public IntPtr hbmpItem;

            // return the size of the structure
            public static uint Size
            {
                get { return (uint)Marshal.SizeOf(typeof(MENUITEMINFO)); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TBBUTTONINFO
        {
            public uint cbSize;
            public uint dwMask;
            public int idCommand;
            public int iImage;
            public byte fsState;
            public byte fsStyle;
            public short cx;
            public IntPtr lParam;
            public IntPtr pszText;
            public int cchText;

            // return the size of the structure
            public static uint Size
            {
                get { return (uint)Marshal.SizeOf(typeof(TBBUTTONINFO)); }
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetMenuItemInfoW(IntPtr hMenu, uint uItem, bool fByPosition, ref MENUITEMINFO lpmii);

        [DllImport("user32.dll")]
        public static extern bool SetMenuItemInfoW(IntPtr hMenu, uint uItem, bool fByPosition, [In] ref MENUITEMINFO lpmii);

        [DllImport("user32.dll")]
        public static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        // return null if item is no string item
        public static string GetMenuItemString(IntPtr hMenu, uint uItem, bool fByPosition)
        {
            Win32.MENUITEMINFO info = new Win32.MENUITEMINFO();
            info.cbSize = Win32.MENUITEMINFO.Size;
            info.fMask = Win32.MIIM_FTYPE;
            Win32.GetMenuItemInfoW(hMenu, uItem, fByPosition, ref info);

            string s = null;
            if ((info.fType & Win32.MFT_SEPARATOR) == 0)
            {
                info.dwTypeData = IntPtr.Zero;
                info.cch = 0;
                info.fMask = Win32.MIIM_STRING;
                Win32.GetMenuItemInfoW(hMenu, uItem, fByPosition, ref info);

                int len = (int)info.cch;
                info.cch++;
                IntPtr sPtr = Marshal.AllocHGlobal((len + 1) * 2);
                info.dwTypeData = sPtr;
                info.fMask = Win32.MIIM_STRING | Win32.MIIM_FTYPE;
                Win32.GetMenuItemInfoW(hMenu, uItem, fByPosition, ref info);

                s = Marshal.PtrToStringUni(sPtr, len);
                Marshal.FreeHGlobal(sPtr);
            }

            return s;
        }

        public static IntPtr GetMenuItemBitmap(IntPtr hMenu, uint uItem, bool fByPosition)
        {
            Win32.MENUITEMINFO info = new Win32.MENUITEMINFO();
            info.cbSize = Win32.MENUITEMINFO.Size;
            info.fMask = Win32.MIIM_BITMAP | Win32.MIIM_CHECKMARKS;
            Win32.GetMenuItemInfoW(hMenu, uItem, fByPosition, ref info);
            if(info.hbmpItem != IntPtr.Zero)
                return info.hbmpItem;

            if (info.hbmpUnchecked == info.hbmpChecked)
                return info.hbmpUnchecked;

            if (info.hbmpChecked == IntPtr.Zero)
                return info.hbmpUnchecked;

            return IntPtr.Zero;
        }

        public static IntPtr GetSubMenu(IntPtr hMenu, uint uItem, bool fByPosition)
        {
            Win32.MENUITEMINFO info = new Win32.MENUITEMINFO();
            info.cbSize = Win32.MENUITEMINFO.Size;
            info.fMask = Win32.MIIM_SUBMENU;
            Win32.GetMenuItemInfoW(hMenu, uItem, fByPosition, ref info);
            return info.hSubMenu;
        }

        public static uint GetMenuItemId(IntPtr hMenu, uint uItem, bool fByPosition)
        {
            Win32.MENUITEMINFO info = new Win32.MENUITEMINFO();
            info.cbSize = Win32.MENUITEMINFO.Size;
            info.fMask = Win32.MIIM_ID;
            Win32.GetMenuItemInfoW(hMenu, uItem, fByPosition, ref info);
            return info.wID;
        }

        [DllImport("user32.dll")]
        public static extern int GetMenuItemCount(IntPtr hMenu);

        [DllImport("comctl32.dll", SetLastError = true)]
        public static extern bool ImageList_GetIconSize(IntPtr himl, out int cx, out int cy);

        [DllImport("comctl32.dll", SetLastError = true)]
        public static extern bool ImageList_Draw(IntPtr himl, int i, IntPtr hdcDst, int x, int y, ImageListDrawingStyle fStyle);

        public const uint CLR_NONE = 0xFFFFFFFFu;
        public const uint CLR_DEFAULT = 0xFF000000u;

        [DllImport("comctl32.dll", SetLastError = true)]
        public static extern bool ImageList_DrawEx(IntPtr himl, int i, IntPtr hdcDst, int x, int y, int dx, int dy, uint rgbBk, uint rgbFg, ImageListDrawingStyle fStyle);

        [DllImport("user32.dll")]
        public static extern int GetDlgCtrlID(IntPtr hwndCtl);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public static bool EnumChildWindows(IntPtr hwndParent, Predicate<IntPtr> callback)
        {
            bool result = false;
            GCHandle callbackHandle = GCHandle.Alloc(callback);

            try
            {
                EnumWindowsProc childProc = new EnumWindowsProc(EnumChildWindowsCallback);
                EnumChildWindows(hwndParent, childProc, GCHandle.ToIntPtr(callbackHandle));
            }
            finally
            {
                if (callbackHandle.IsAllocated)
                    callbackHandle.Free();
            }

            return result;
        }

        private static bool EnumChildWindowsCallback(IntPtr handle, IntPtr pointerToPredicate)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointerToPredicate);
            Predicate<IntPtr> callback = gch.Target as Predicate<IntPtr>;
            if (callback == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as Predicate<IntPtr>");
            }
            return callback(handle);
        }

        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                EnumWindowsProc childProc = new EnumWindowsProc(GetChildWindowsCallback);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        private static bool GetChildWindowsCallback(IntPtr handle, IntPtr pointerToList)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointerToList);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            return true;
        }

        public enum BeepType : uint
        {
            SimpleBeep = 0xFFFFFFFF,
            MB_OK = 0x00,
            MB_ICONERROR = 0x10,
            MB_ICONQUESTION = 0x20,
            MB_ICONEXCLAMATION = 0x30,
            MB_ICONASTERISK = 0x40,
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MessageBeep(BeepType uType);

        [DllImport("kernel32")]
        public static extern bool AllocConsole();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public static string GetClassName(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            Win32.GetClassName(hWnd, sb, sb.Capacity);

            return sb.ToString();
        }

        public const int RBS_TOOLTIPS = 256;
        public const int RBS_VARHEIGHT = 512;
        public const int RBS_BANDBORDERS = 1024;
        public const int RBS_FIXEDORDER = 2048;
        public const int RBBS_BREAK = 0x0001;
        public const int RBBS_FIXEDSIZE = 0x0002;
        public const int RBBS_CHILDEDGE = 0x0004;
        public const int RBBS_HIDDEN = 0x0008;
        public const int RBBS_NOVERT = 0x0010;
        public const int RBBS_FIXEDBMP = 0x0020;
        public const int RBBS_VARIABLEHEIGHT = 0x0040;
        public const int RBBS_GRIPPERALWAYS = 0x0080;
        public const int RBBS_NOGRIPPER = 0x0100;
        public const int RBBS_USECHEVRON = 0x0200;
        public const int RBBS_HIDETITLE = 0x0400;
        public const int RBBS_TOPALIGN = 0x0800;
        public const int RBBIM_STYLE = 1;
        public const int RBBIM_COLORS = 2;
        public const int RBBIM_TEXT = 4;
        public const int RBBIM_IMAGE = 8;
        public const int RBBIM_CHILD = 16;
        public const int RBBIM_CHILDSIZE = 32;
        public const int RBBIM_SIZE = 64;
        public const int RBBIM_BACKGROUND = 128;
        public const int RBBIM_ID = 256;
        public const int RBBIM_IDEALSIZE = 512;
        public const int RBBIM_LPARAM = 1024;
        public const int RBBIM_HEADERSIZE = 2048;
        public const int RB_INSERTBANDA = 0x400/*WM_USER*/+ 1;
        public const int RB_INSERTBANDW = 0x400/*WM_USER*/+ 10;
        public const int RB_DELETEBAND = 0x400/*WM_USER*/+ 2;
        public const int RB_GETBARINFO = 0x400/*WM_USER*/+ 3;
        public const int RB_SETBARINFO = 0x400/*WM_USER*/+ 4;
        public const int RB_SETBANDINFOA = 0x400/*WM_USER*/+ 6;
        public const int RB_SETBANDINFOW = 0x400/*WM_USER*/+ 11;
        public const int RB_SETPARENT = 0x400/*WM_USER*/+ 7;
        public const int RB_HITTEST = 0x400/*WM_USER*/+ 8;
        public const int RB_GETRECT = 0x400/*WM_USER*/+ 9;
        public const int RB_GETBANDCOUNT = 0x400/*WM_USER*/+ 12;
        public const int RB_GETROWCOUNT = 0x400/*WM_USER*/+ 13;
        public const int RB_GETROWHEIGHT = 0x400/*WM_USER*/+ 14;
        public const int RB_IDTOINDEX = 0x400/*WM_USER*/+ 16;
        public const int RB_GETTOOLTIPS = 0x400/*WM_USER*/+ 17;
        public const int RB_SETTOOLTIPS = 0x400/*WM_USER*/+ 18;
        public const int RB_SETBKCOLOR = 0x400/*WM_USER*/+ 19;
        public const int RB_GETBKCOLOR = 0x400/*WM_USER*/+ 20;
        public const int RB_SETTEXTCOLOR = 0x400/*WM_USER*/+ 21;
        public const int RB_GETTEXTCOLOR = 0x400/*WM_USER*/+ 22;
        public const int RB_SIZETORECT = 0x400/*WM_USER*/+ 23;
        public const int RB_BEGINDRAG = 0x400/*WM_USER*/+ 24;
        public const int RB_ENDDRAG = 0x400/*WM_USER*/+ 25;
        public const int RB_DRAGMOVE = 0x400/*WM_USER*/+ 26;
        public const int RB_GETBARHEIGHT = 0x400/*WM_USER*/+ 27;
        public const int RB_GETBANDINFOW = 0x400/*WM_USER*/+ 28;
        public const int RB_GETBANDINFOA = 0x400/*WM_USER*/+ 29;
        public const int RB_MINIMIZEBAND = 0x400/*WM_USER*/+ 30;
        public const int RB_MAXIMIZEBAND = 0x400/*WM_USER*/+ 31;
        public const int RB_GETBANDBORDERS = 0x400/*WM_USER*/+ 34;
        public const int RB_SHOWBAND = 0x400/*WM_USER*/+ 35;
        public const int RB_SETPALETTE = 0x400/*WM_USER*/+ 37;
        public const int RB_GETPALETTE = 0x400/*WM_USER*/+ 38;
        public const int RB_MOVEBAND = 0x400/*WM_USER*/+ 39;
        public const int RB_GETBANDMARGINS = 0x400/*WM_USER*/+ 40;
        public const int RB_PUSHCHEVRON = 0x400/*WM_USER*/+ 43;
        public const int RB_SETBANDWIDTH = 0x400/*WM_USER*/+ 44;

        [StructLayout(LayoutKind.Sequential)]
        public struct REBARBANDINFO
        {
            public int cbSize;
            public int fMask;
            public int fStyle;
            public int clrFore;
            public int clrBack;
            public IntPtr lpText;
            public int cch;
            public int iImage;
            public IntPtr hwndChild;
            public int cxMinChild;
            public int cyMinChild;
            public int cx;
            public IntPtr hbmBack;
            public int wID;
            public int cyChild;
            public int cyMaxChild;
            public int cyIntegral;
            public int cxIdeal;
            public IntPtr lParam;
            public int cxHeader;
        }

        public const int TCM_FIRST = 0x1300;
        public const int TCM_GETIMAGELIST = (TCM_FIRST + 2);
        public const int TCM_SETIMAGELIST = (TCM_FIRST + 3);
        public const int TCM_GETITEMCOUNT = (TCM_FIRST + 4);
        public const int TCM_GETITEMA = (TCM_FIRST + 5);
        public const int TCM_GETITEMW = (TCM_FIRST + 60);
        public const int TCM_SETITEMA = (TCM_FIRST + 6);
        public const int TCM_SETITEMW = (TCM_FIRST + 61);
        public const int TCM_INSERTITEMA = (TCM_FIRST + 7);
        public const int TCM_INSERTITEMW = (TCM_FIRST + 62);
        public const int TCM_DELETEITEM = (TCM_FIRST + 8);
        public const int TCM_DELETEALLITEMS = (TCM_FIRST + 9);
        public const int TCM_GETITEMRECT = (TCM_FIRST + 10);
        public const int TCM_GETCURSEL = (TCM_FIRST + 11);
        public const int TCM_SETCURSEL = (TCM_FIRST + 12);
        public const int TCM_HITTEST = (TCM_FIRST + 13);
        public const int TCM_SETITEMEXTRA = (TCM_FIRST + 14);
        public const int TCM_ADJUSTRECT = (TCM_FIRST + 40);
        public const int TCM_SETITEMSIZE = (TCM_FIRST + 41);
        public const int TCM_REMOVEIMAGE = (TCM_FIRST + 42);
        public const int TCM_SETPADDING = (TCM_FIRST + 43);
        public const int TCM_GETROWCOUNT = (TCM_FIRST + 44);
        public const int TCM_GETCURFOCUS = (TCM_FIRST + 47);
        public const int TCM_SETCURFOCUS = (TCM_FIRST + 48);
        public const int TCM_SETMINTABWIDTH = (TCM_FIRST + 49);
        public const int TCM_DESELECTALL = (TCM_FIRST + 50);
        public const int TCM_HIGHLIGHTITEM = (TCM_FIRST + 51);
        public const int TCM_SETEXTENDEDSTYLE = (TCM_FIRST + 52);
        public const int TCM_GETEXTENDEDSTYLE = (TCM_FIRST + 53);

        public const int TCN_SELCHANGE = -551;


        public const int LB_ADDSTRING = 0x0180;
        public const int LB_INSERTSTRING = 0x0181;
        public const int LB_DELETESTRING = 0x0182;
        public const int LB_SELITEMRANGEEX = 0x0183;
        public const int LB_RESETCONTENT = 0x0184;
        public const int LB_SETSEL = 0x0185;
        public const int LB_SETCURSEL = 0x0186;
        public const int LB_GETSEL = 0x0187;
        public const int LB_GETCURSEL = 0x0188;
        public const int LB_GETTEXT = 0x0189;
        public const int LB_GETTEXTLEN = 0x018A;
        public const int LB_GETCOUNT = 0x018B;
        public const int LB_SELECTSTRING = 0x018C;
        public const int LB_DIR = 0x018D;
        public const int LB_GETTOPINDEX = 0x018E;
        public const int LB_FINDSTRING = 0x018F;
        public const int LB_GETSELCOUNT = 0x0190;
        public const int LB_GETSELITEMS = 0x0191;
        public const int LB_SETTABSTOPS = 0x0192;
        public const int LB_GETHORIZONTALEXTENT = 0x0193;
        public const int LB_SETHORIZONTALEXTENT = 0x0194;
        public const int LB_SETCOLUMNWIDTH = 0x0195;
        public const int LB_ADDFILE = 0x0196;
        public const int LB_SETTOPINDEX = 0x0197;
        public const int LB_GETITEMRECT = 0x0198;
        public const int LB_GETITEMDATA = 0x0199;
        public const int LB_SETITEMDATA = 0x019A;
        public const int LB_SELITEMRANGE = 0x019B;
        public const int LB_SETANCHORINDEX = 0x019C;
        public const int LB_GETANCHORINDEX = 0x019D;
        public const int LB_SETCARETINDEX = 0x019E;
        public const int LB_GETCARETINDEX = 0x019F;
        public const int LB_SETITEMHEIGHT = 0x01A0;
        public const int LB_GETITEMHEIGHT = 0x01A1;
        public const int LB_FINDSTRINGEXACT = 0x01A2;
        public const int LB_SETLOCALE = 0x01A5;
        public const int LB_GETLOCALE = 0x01A6;
        public const int LB_SETCOUNT = 0x01A7;
        public const int LB_INITSTORAGE = 0x01A8;
        public const int LB_ITEMFROMPOINT = 0x01A9;

        public const int CBN_SELCHANGE = 1;

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        public const int DSTINVERT = 0x00550009;

        [DllImport("gdi32.dll")]
        public static extern bool PatBlt(IntPtr hdc, int nXLeft, int nYLeft, int nWidth, int nHeight, uint dwRop);


        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    public class ClikeStringArray : IDisposable
    {
        IntPtr _nativeArray;
        List<IntPtr> _nativeItems;
        bool _disposed = false;

        public ClikeStringArray(int num, int stringCapacity)
        {
            _nativeArray = Marshal.AllocHGlobal((num + 1) * IntPtr.Size);
            _nativeItems = new List<IntPtr>();
            for (int i = 0; i < num; i++)
            {
                IntPtr item = Marshal.AllocHGlobal(stringCapacity);
                Marshal.WriteIntPtr((IntPtr)((Int64)_nativeArray + (i * IntPtr.Size)), item);
                _nativeItems.Add(item);
            }
            Marshal.WriteIntPtr((IntPtr)((Int64)_nativeArray + (num * IntPtr.Size)), IntPtr.Zero);
        }

        public IntPtr NativePointer { get { return _nativeArray; } }
        public List<string> ManagedStringsAnsi { get { return _getManagedItems(false); } }
        public List<string> ManagedStringsUnicode { get { return _getManagedItems(true); } }
        List<string> _getManagedItems(bool unicode)
        {
            List<string> _managedItems = new List<string>();
            for (int i = 0; i < _nativeItems.Count; i++)
            {
                if (unicode) _managedItems.Add(Marshal.PtrToStringUni(_nativeItems[i]));
                else _managedItems.Add(Marshal.PtrToStringAnsi(_nativeItems[i]));
            }
            return _managedItems;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                for (int i = 0; i < _nativeItems.Count; i++)
                    if (_nativeItems[i] != IntPtr.Zero) Marshal.FreeHGlobal(_nativeItems[i]);
                if (_nativeArray != IntPtr.Zero) Marshal.FreeHGlobal(_nativeArray);
                _disposed = true;
            }
        }
        ~ClikeStringArray()
        {
            Dispose();
        }
    }
    #endregion
}