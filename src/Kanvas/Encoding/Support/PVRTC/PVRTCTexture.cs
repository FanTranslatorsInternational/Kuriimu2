using System;
using System.Runtime.InteropServices;
using Kanvas.Encoding.Support.PVRTC.Models;

namespace Kanvas.Encoding.Support.PVRTC
{
    internal class PVRTexture : IDisposable
    {
        private IntPtr _pvrTexturePointer;

        public bool IsDisposed { get; private set; }

        public PVRTexture(string filePath)
        {
            _pvrTexturePointer = NativeCalls.CreateTexture(filePath);
        }

        public static PVRTexture CreateTexture<T>(T[] data, uint width, uint height, uint depth, PixelFormat ptFormat, bool preMultiplied, VariableType channelType, ColorSpace colorspace) where T : struct
        {
            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var pvrTexture = NativeCalls.CreateTexture(gcHandle.AddrOfPinnedObject(), width, height, depth, ptFormat, preMultiplied, channelType, colorspace);
            gcHandle.Free();

            return new PVRTexture(pvrTexture);
        }

        internal PVRTexture(IntPtr pvrTexture)
        {
            _pvrTexturePointer = pvrTexture;
        }

        public bool SaveTexture(string filePath)
        {
            if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
            return NativeCalls.SaveTexture(_pvrTexturePointer, filePath);
        }

        public bool Resize(uint u32NewWidth, uint u32NewHeight, uint u32NewDepth, ResizeMode eResizeMode)
        {
            if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
            return NativeCalls.Resize(_pvrTexturePointer, u32NewWidth, u32NewHeight, u32NewDepth, eResizeMode);
        }

        public bool GenerateMIPMaps(ResizeMode eFilterMode, uint uiMIPMapsToDo = int.MaxValue)
        {
            if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
            return NativeCalls.GenerateMIPMaps(_pvrTexturePointer, eFilterMode, uiMIPMapsToDo);
        }

        public bool Transcode(PixelFormat ptFormat, VariableType eChannelType, ColorSpace eColourspace, CompressorQuality eQuality = CompressorQuality.PVRTCNormal, bool bDoDither = false)
        {
            if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
            return NativeCalls.Transcode(_pvrTexturePointer, ptFormat, eChannelType, eColourspace, eQuality, bDoDither);
        }

        public uint GetTextureDataSize(int iMIPLevel = -1)
        {
            if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
            return NativeCalls.GetTextureDataSize(_pvrTexturePointer, iMIPLevel);
        }

        public void GetTextureData<T>(T[] data, uint dataSize, uint uiMIPLevel = 0) where T : struct
        {
            if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            NativeCalls.GetTextureData(_pvrTexturePointer, gcHandle.AddrOfPinnedObject(), dataSize, uiMIPLevel);
            gcHandle.Free();
        }

        ~PVRTexture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                // release other disposable objects
            }

            // free unmanaged resources
            NativeCalls.DestroyTexture(_pvrTexturePointer);
            _pvrTexturePointer = IntPtr.Zero;

            IsDisposed = true;
        }
    }
}
