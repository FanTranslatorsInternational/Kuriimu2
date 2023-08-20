using System;
using System.Runtime.InteropServices;

namespace Kanvas.Native
{
    partial class PvrHeader
    {
        private const string DllName_ = @"pvr_lib";

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PVRTexLib_CreateTextureHeader(PvrCreateParams attributes);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        private static extern PixelFormat PVRTexLib_GetTexturePixelFormat(IntPtr pvrHeader);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint PVRTexLib_GetTextureBitsPerPixel(IntPtr pvrHeader);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        private static extern ChannelType PVRTexLib_GetTextureChannelType(IntPtr pvrHeader);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        private static extern ColorSpace PVRTexLib_GetTextureColourSpace(IntPtr pvrHeader);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint PVRTexLib_GetTextureWidth(IntPtr pvrHeader, uint mipLevel);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint PVRTexLib_GetTextureHeight(IntPtr pvrHeader, uint mipLevel);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PVRTexLib_DestroyTextureHeader(IntPtr pvrHeader);
    }
}
