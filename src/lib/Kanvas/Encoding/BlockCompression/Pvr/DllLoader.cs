using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Kanvas.Encoding.BlockCompression.Pvr
{
    public static class DllLoader
    {
        private const string NativePath_ = @"runtimes\{0}-{1}\native";

        public static void PreloadDll(string dllName)
        {
            var dllDirs = GetDirectedDllDirectories();

            // Not using OperatingSystem.Platform.
            // See: https://www.mono-project.com/docs/faq/technical/#how-to-detect-the-execution-platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Win32.LoadDll(dllDirs, dllName);
            }
            else
            {
                Posix.LoadDll(dllDirs, dllName);
            }
        }

        private static string[] GetDirectedDllDirectories()
        {
            return
            [
                AppDomain.CurrentDomain.BaseDirectory,
                GetPlatformDependantDllDirectory()
            ];
        }

        private static string GetPlatformDependantDllDirectory()
        {
            string localDir = AppDomain.CurrentDomain.BaseDirectory;

            string platform;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                platform = "win";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                platform = "linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                platform = "osx";
            else
                throw new NotSupportedException();

            string subDir = Environment.Is64BitProcess ? string.Format(NativePath_, platform, "x64") : string.Format(NativePath_, platform, "x86");

            string directedDllDir = Path.Combine(localDir, subDir);

            return directedDllDir;
        }

        private static class Win32
        {
            internal static void LoadDll(string[] dllDirs, string dllName)
            {
                var dllFileName = $"{dllName}.dll";

                foreach (string dllDir in dllDirs)
                {
                    var directedDllPath = Path.Combine(dllDir, dllFileName);

                    if (!File.Exists(directedDllPath))
                        continue;

                    // Specify SEARCH_DLL_LOAD_DIR to load dependent libraries located in the same platform-specific directory.
                    var hLibrary = LoadLibraryEx(directedDllPath, nint.Zero, LOAD_LIBRARY_SEARCH_DEFAULT_DIRS | LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR);

                    if (hLibrary == nint.Zero)
                    {
                        var errorCode = Marshal.GetLastWin32Error();
                        var exception = new Win32Exception(errorCode);

                        throw new DllNotFoundException(exception.Message, exception);
                    }

                    return;
                }

                throw new DllNotFoundException($"Could not find {dllFileName} in any of the specified directories.");
            }

            // HMODULE LoadLibraryExA(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
            // HMODULE LoadLibraryExW(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern nint LoadLibraryEx(string lpLibFileName, nint hFile, uint dwFlags);

            private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x1000;
            private const uint LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x100;
        }

        private static class Posix
        {
            internal static void LoadDll(string[] dllDirs, string dllName)
            {
                string dllExtension;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    dllExtension = ".so";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    dllExtension = ".dylib";
                }
                else
                {
                    throw new NotSupportedException();
                }

                var dllFileName = $"{dllName}{dllExtension}";

                foreach (string dllDir in dllDirs)
                {
                    var directedDllPath = Path.Combine(dllDir, dllFileName);

                    if (!File.Exists(directedDllPath))
                        continue;

                    const int ldFlags = RTLD_NOW | RTLD_GLOBAL;
                    var hLibrary = DlOpen(directedDllPath, ldFlags);

                    if (hLibrary == nint.Zero)
                    {
                        var pErrStr = DlError();
                        // `PtrToStringAnsi` always uses the specific constructor of `String` (see dotnet/core#2325),
                        // which in turn interprets the byte sequence with system default codepage. On OSX and Linux
                        // the codepage is UTF-8 so the error message should be handled correctly.
                        var errorMessage = Marshal.PtrToStringAnsi(pErrStr);

                        throw new DllNotFoundException(errorMessage);
                    }

                    return;
                }

                throw new DllNotFoundException($"Could not find {dllFileName} in any of the specified directories.");
            }

            // OSX and most Linux OS use LP64 so `int` is still 32-bit even on 64-bit platforms.
            // void *dlopen(const char *filename, int flag);
            [DllImport("libdl", EntryPoint = "dlopen")]
            private static extern nint DlOpen([MarshalAs(UnmanagedType.LPStr)] string fileName, int flags);

            // char *dlerror(void);
            [DllImport("libdl", EntryPoint = "dlerror")]
            private static extern nint DlError();

            private const int RTLD_LAZY = 0x1;
            private const int RTLD_NOW = 0x2;
            private const int RTLD_GLOBAL = 0x100;
        }
    }
}
