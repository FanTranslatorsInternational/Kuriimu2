using System;
using System.Runtime.InteropServices;

namespace Kanvas.Native
{
    partial class PvrTexture
    {
        private const string DllName_ = @"pvr_lib";

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PVRTexLib_CreateTexture(IntPtr pvrHeader, IntPtr data);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PVRTexLib_GetTextureHeader(IntPtr pvrTexture);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PVRTexLib_TranscodeTexture(IntPtr pvrTexture, IntPtr options);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PVRTexLib_GetTextureDataPtr(IntPtr pvrTexture, uint mipLevel = 0, uint arrayMember = 0, uint faceNumber = 0, uint zSlice = 0);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint PVRTexLib_GetTextureDataSize(IntPtr pvrHeader, int mipLevel = 0, bool allSurfaces = false, bool allFaces = false);

        [DllImport(DllName_, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PVRTexLib_DestroyTexture(IntPtr pvrTexture);
    }
}
