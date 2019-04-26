using System;
using System.Runtime.InteropServices;
using Kanvas.Format.ASTC.Models;

namespace Kanvas.Format.ASTC
{
    internal static class NativeCalls
    {
        private const string _astcDll = @"Libraries\astcenc.dll";

        [DllImport(_astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateContext")]
        internal static extern IntPtr CreateContext();

        [DllImport(_astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetMethod")]
        private static extern IntPtr SetMethod(IntPtr ctx, int method);

        internal static IntPtr SetMethod(IntPtr ctx, Method method) => SetMethod(ctx, (int)method);

        [DllImport(_astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetDecodeMode")]
        private static extern IntPtr SetDecodeMode(IntPtr ctx, int mode);

        internal static IntPtr SetDecodeMode(IntPtr ctx, DecodeMode mode) => SetDecodeMode(ctx, (int)mode);

        [DllImport(_astcDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "SetInputFile")]
        internal static extern IntPtr SetInputFile(IntPtr ctx, string input);

        [DllImport(_astcDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "SetOutputFile")]
        internal static extern IntPtr SetOutputFile(IntPtr ctx, string input);

        [DllImport(_astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetBlockMode")]
        private static extern IntPtr SetBlockMode(IntPtr ctx, int blockSize);

        internal static IntPtr SetBlockMode(IntPtr ctx, BlockMode blockSize) => SetBlockMode(ctx, (int)blockSize);

        [DllImport(_astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetSpeedMode")]
        private static extern IntPtr SetSpeedMode(IntPtr ctx, int speedMode);

        internal static IntPtr SetSpeedMode(IntPtr ctx, SpeedMode speedMode) => SetSpeedMode(ctx, (int)speedMode);

        [DllImport(_astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetThreadCount")]
        internal static extern IntPtr SetThreadCount(IntPtr ctx, int threadCount);

        [DllImport(_astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ConvertImage")]
        internal static extern int ConvertImage(IntPtr ctx);

        [DllImport(_astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "DisposeContext")]
        internal static extern int DisposeContext(IntPtr ctx);
    }
}
