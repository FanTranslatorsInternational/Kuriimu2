using System;
using System.Runtime.InteropServices;

namespace Kontract.Image.Support
{
    public class PVRTC
    {
        public enum PixelFormat : ulong
        {
            PVRTCI_2bpp_RGB,
            PVRTCI_2bpp_RGBA,
            PVRTCI_4bpp_RGB,
            PVRTCI_4bpp_RGBA,
            PVRTCII_2bpp,
            PVRTCII_4bpp,
            ETC1,
            DXT1,
            DXT2,
            DXT3,
            DXT4,
            DXT5,

            //These formats are identical to some DXT formats.
            BC1 = DXT1,
            BC2 = DXT3,
            BC3 = DXT5,

            //These are currently unsupported:
            BC4,
            BC5,
            BC6,
            BC7,

            //These are supported
            UYVY,
            YUY2,
            BW1bpp,
            SharedExponentR9G9B9E5,
            RGBG8888,
            GRGB8888,
            ETC2_RGB,
            ETC2_RGBA,
            ETC2_RGB_A1,
            EAC_R11,
            EAC_RG11,

            RGB565 = 0x5060561626772,
            RGBA4444 = 0x404040461626772,
            RGBA8888 = 0x808080861626772,
        }

        public enum ResizeMode
        {
            Nearest,
            Linear,
            Cubic,
        }

        public enum VariableType : int
        {
            UnsignedByteNorm,
            SignedByteNorm,
            UnsignedByte,
            SignedByte,
            UnsignedShortNorm,
            SignedShortNorm,
            UnsignedShort,
            SignedShort,
            UnsignedIntegerNorm,
            SignedIntegerNorm,
            UnsignedInteger,
            SignedInteger,
            Float,
        }

        public enum ColourSpace : int
        {
            lRGB,
            sRGB
        }

        public enum CompressorQuality
        {
            PVRTCFast = 0,
            PVRTCNormal,
            PVRTCHigh,
            PVRTCBest,

            ETCFast = 0,
            ETCFastPerceptual,
            ETCMedium,
            ETCMediumPerceptual,
            ETCSlow,
            ETCSlowPerceptual
        }

        public class PVRTexture : IDisposable
        {
            #region Interop
            private const string dllName = "PVRTexLibWrapper.dll";

            [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr CreateTexture(IntPtr data, uint u32Width, uint u32Height, uint u32Depth, PixelFormat ptFormat, bool preMultiplied, VariableType eChannelType, ColourSpace eColourspace);

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
            public static extern bool Transcode(IntPtr pPvrTexture, PixelFormat ptFormat, VariableType eChannelType, ColourSpace eColourspace, CompressorQuality eQuality = CompressorQuality.PVRTCNormal, bool bDoDither = false);

            [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern uint GetTextureDataSize(IntPtr pPvrTexture, int iMIPLevel = -1);

            [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void GetTextureData(IntPtr pPvrTexture, IntPtr data, uint dataSize, uint uiMIPLevel = 0);
            #endregion Interop

            private IntPtr _pPvrTexture = IntPtr.Zero;
            private bool _isDisposed = false;

            public IntPtr PvrTexturePointer { get { return _pPvrTexture; } }
            public bool IsDisposed { get { return _isDisposed; } }

            public PVRTexture(string filePath)
            {
                this._pPvrTexture = CreateTexture(filePath);
            }

            public static PVRTexture CreateTexture<T>(T[] data, uint u32Width, uint u32Height, uint u32Depth, PixelFormat ptFormat, bool preMultiplied, VariableType eChannelType, ColourSpace eColourspace) where T : struct
            {
                var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var pPvrTexture = CreateTexture(gcHandle.AddrOfPinnedObject(), u32Width, u32Height, u32Depth, ptFormat, preMultiplied, eChannelType, eColourspace);
                gcHandle.Free();
                return new PVRTexture(pPvrTexture);
            }

            internal PVRTexture(IntPtr pPvrTexture)
            {
                this._pPvrTexture = pPvrTexture;
            }

            ~PVRTexture()
            {
                Dispose(false);
            }

            public bool SaveTexture(string filePath)
            {
                if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
                return SaveTexture(_pPvrTexture, filePath);
            }

            public bool Resize(uint u32NewWidth, uint u32NewHeight, uint u32NewDepth, ResizeMode eResizeMode)
            {
                if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
                return Resize(_pPvrTexture, u32NewWidth, u32NewHeight, u32NewDepth, eResizeMode);
            }

            public bool GenerateMIPMaps(ResizeMode eFilterMode, uint uiMIPMapsToDo = int.MaxValue)
            {
                if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
                return GenerateMIPMaps(_pPvrTexture, eFilterMode, uiMIPMapsToDo);
            }

            public bool Transcode(PixelFormat ptFormat, VariableType eChannelType, ColourSpace eColourspace, CompressorQuality eQuality = CompressorQuality.PVRTCNormal, bool bDoDither = false)
            {
                if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
                return Transcode(_pPvrTexture, ptFormat, eChannelType, eColourspace, eQuality, bDoDither);
            }

            public uint GetTextureDataSize(int iMIPLevel = -1)
            {
                if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
                return GetTextureDataSize(_pPvrTexture, iMIPLevel);
            }

            public void GetTextureData<T>(T[] data, uint dataSize, uint uiMIPLevel = 0) where T : struct
            {
                if (IsDisposed) throw new ObjectDisposedException("_pPvrTexture");
                var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                GetTextureData(_pPvrTexture, gcHandle.AddrOfPinnedObject(), dataSize, uiMIPLevel);
                gcHandle.Free();
            }

            #region Implement IDisposable & Dispose Pattern
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (IsDisposed) return;

                if (disposing)
                {
                    // release other disposable objects

                }
                // free resources
                DestroyTexture(_pPvrTexture);
                _pPvrTexture = IntPtr.Zero;

                _isDisposed = true;
            }
            #endregion
        }
    }
}
