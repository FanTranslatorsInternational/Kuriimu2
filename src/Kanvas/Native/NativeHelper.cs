using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Kanvas.Native
{
    static class NativeHelper
    {
        private static bool _delegateSet;

        public static void SetDllImportResolver()
        {
            // Set delegate once
            if (_delegateSet)
                return;

            NativeLibrary.SetDllImportResolver(typeof(NativeHelper).Assembly, NativeHelper.ResolveImport);
            _delegateSet = true;
        }

        public static IntPtr ResolveImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            // If resolve is requested by any other assembly than Kanvas
            var currentAssembly = typeof(NativeHelper).Assembly;
            if (assembly != currentAssembly)
                return NativeLibrary.Load(libraryName, assembly, searchPath);

            var platform = GetPlatformMonicker();
            var architecture = platform == "osx" ? "64" : GetArchitecture();
            var extension = GetExtension();

            // Try get resource depending on the platform
            var resourceName = $"Kanvas.Resources.{platform}_x{architecture}.{libraryName}.{extension}";
            var resourceStream = currentAssembly.GetManifestResourceStream(resourceName);

            if (resourceStream == null)
                throw new InvalidOperationException($"The resource '{resourceName}' could not be found.");

            // Extract resource to disk
            var libraryPath = Path.GetTempFileName();
            var libraryFile = File.Create(libraryPath);

            resourceStream.CopyTo(libraryFile);

            libraryFile.Close();

            // Load extracted library
            return NativeLibrary.Load(libraryPath);
        }

        public static GCHandle PinObject(object obj)
        {
            return GCHandle.Alloc(obj, GCHandleType.Pinned);
        }

        public static void FreePinnedObject(GCHandle handle)
        {
            handle.Free();
        }

        public static IntPtr MarshalObject(object obj)
        {
            var objSize = Marshal.SizeOf(obj);
            var ptr = Marshal.AllocHGlobal(objSize);
            Marshal.StructureToPtr(obj, ptr, true);

            return ptr;
        }

        public static void FreeObject(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        private static string GetPlatformMonicker()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "lin";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "osx";

            throw new InvalidOperationException($"Unsupported platform {RuntimeInformation.OSDescription}.");
        }

        private static string GetArchitecture()
        {
            if (RuntimeInformation.OSArchitecture.HasFlag(Architecture.X64))
                return "64";

            if (RuntimeInformation.OSArchitecture.HasFlag(Architecture.X86))
                return "32";

            throw new InvalidOperationException($"Unsupported architecture {RuntimeInformation.OSArchitecture}.");
        }

        private static string GetExtension()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "dll";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "so";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "dylib";

            throw new InvalidOperationException($"Unsupported platform {RuntimeInformation.OSDescription}.");
        }
    }
}
