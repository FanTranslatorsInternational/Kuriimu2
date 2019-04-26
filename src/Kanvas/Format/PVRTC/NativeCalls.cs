using System;
using System.Runtime.InteropServices;
using Kanvas.Format.PVRTC.Models;

namespace Kanvas.Format.PVRTC
{
    internal static class NativeCalls
    {
        private const string dllName = @"Libraries\PVRTexLibWrapper.dll";

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateTexture(IntPtr data, uint u32Width, uint u32Height, uint u32Depth, PixelFormat ptFormat, bool preMultiplied, VariableType eChannelType, ColorSpace eColourspace);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateTexture(string filePath);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool SaveTexture(IntPtr pPvrTexture, [MarshalAs(UnmanagedType.LPStr)] string filePath);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyTexture(IntPtr pPvrTexture);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Resize(IntPtr pPvrTexture, uint u32NewWidth, uint u32NewHeight, uint u32NewDepth, ResizeMode eResizeMode);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GenerateMIPMaps(IntPtr pPvrTexture, ResizeMode eFilterMode, uint uiMIPMapsToDo = int.MaxValue);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Transcode(IntPtr pPvrTexture, PixelFormat ptFormat, VariableType eChannelType, ColorSpace eColourspace, CompressorQuality eQuality = CompressorQuality.PVRTCNormal, bool bDoDither = false);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetTextureDataSize(IntPtr pPvrTexture, int iMIPLevel = -1);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetTextureData(IntPtr pPvrTexture, IntPtr data, uint dataSize, uint uiMIPLevel = 0);
    }
}
