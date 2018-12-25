using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kuriimu2_WinForms.Tools
{
    public static class Win32
    {
        // TreeView
        public const uint TV_FIRST = 0x1100;
        public const uint TVM_SETIMAGELIST = (TV_FIRST + 9);

        public const uint TVSIL_NORMAL = 0;
        public const uint TVSIL_STATE = 2;

        // ListView
        public const uint LVM_FIRST = 0x1000;
        public const uint LVM_GETIMAGELIST = (LVM_FIRST + 2);
        public const uint LVM_SETIMAGELIST = (LVM_FIRST + 3);

        public const uint LVSIL_NORMAL = 0;
        public const uint LVSIL_SMALL = 1;
        public const uint LVSIL_STATE = 2;
        public const uint LVSIL_GROUPHEADER = 3;

        // Shell
        public const uint SHGFI_DISPLAYNAME = 0x200;
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;
        public const uint SHGFI_SMALLICON = 0x1;
        public const uint SHGFI_SYSICONINDEX = 0x4000;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        // Structs
        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260 /* MAX_PATH */)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        // Methods
        [DllImport("shell32")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, int cbSizeFileInfo, uint uFlags);

        [DllImport("User32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, uint wParam, IntPtr lParam);

        [DllImport("uxtheme", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        // FileSize
        [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
        public static extern int StrFormatByteSize(long fileSize, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer, int bufferSize);
    }
}
