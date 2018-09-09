using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Kanvas.Support.ASTC
{
    internal class NativeCalls
    {
        const string astcDll = @"Libraries\astcenc.dll";

        internal enum Method : int
        {
            Compression = 0,
            Decompression = 1,
            DoBoth = 2,
            Compare = 4
        }

        [DllImport(astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateContext")]
        internal extern static IntPtr CreateContext();

        [DllImport(astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetMethod")]
        private extern static IntPtr SetMethod(IntPtr ctx, int method);

        internal static IntPtr SetMethod(IntPtr ctx, Method method) => SetMethod(ctx, (int)method);

        [DllImport(astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetDecodeMode")]
        private extern static IntPtr SetDecodeMode(IntPtr ctx, int mode);

        internal static IntPtr SetDecodeMode(IntPtr ctx, DecodeMode mode) => SetDecodeMode(ctx, (int)mode);

        [DllImport(astcDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "SetInputFile")]
        internal extern static IntPtr SetInputFile(IntPtr ctx, string input);

        [DllImport(astcDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "SetOutputFile")]
        internal extern static IntPtr SetOutputFile(IntPtr ctx, string input);

        [DllImport(astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetBlockMode")]
        private extern static IntPtr SetBlockMode(IntPtr ctx, int blockSize);

        internal static IntPtr SetBlockMode(IntPtr ctx, BlockMode blockSize) => SetBlockMode(ctx, (int)blockSize);

        [DllImport(astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetSpeedMode")]
        private extern static IntPtr SetSpeedMode(IntPtr ctx, int speedMode);

        internal static IntPtr SetSpeedMode(IntPtr ctx, SpeedMode speedMode) => SetSpeedMode(ctx, (int)speedMode);

        [DllImport(astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetThreadCount")]
        internal extern static IntPtr SetThreadCount(IntPtr ctx, int threadCount);

        [DllImport(astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ConvertImage")]
        internal extern static int ConvertImage(IntPtr ctx);

        [DllImport(astcDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "DisposeContext")]
        internal extern static int DisposeContext(IntPtr ctx);
    }
}
