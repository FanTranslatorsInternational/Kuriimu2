using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Kanvas.Native
{
    class PvrTexture : IDisposable
    {
        private IntPtr _pointer;

        public bool IsDisposed { get; private set; }

        public PvrTexture(PvrHeader header, byte[] data)
        {
            Validate();

            var dataPtr = NativeHelper.PinObject(data);
            _pointer = PVRTexLib_CreateTexture(header.GetAddress(), dataPtr.AddrOfPinnedObject());
            NativeHelper.FreePinnedObject(dataPtr);
        }

        public static PvrTexture Create(byte[] data, uint width, uint height, uint depth, PixelFormat format, ChannelType channelType, ColorSpace colorSpace)
        {
            var attributes = new PvrCreateParams
            {
                pixelFormat = format,
                width = width,
                height = height,
                depth = depth,
                numMipMaps = 1,
                numArrayMembers = 1,
                numFaces = 1,
                colorSpace = colorSpace,
                channelType = channelType,
                preMultiplied = false
            };
            var header = new PvrHeader(attributes);

            return new PvrTexture(header, data);
        }

        public PvrHeader GetHeader()
        {
            Validate();

            return new PvrHeader(PVRTexLib_GetTextureHeader(_pointer));
        }

        public bool Transcode(PixelFormat format, ChannelType channelType, ColorSpace colorSpace, CompressionQuality quality)
        {
            var options =new TranscoderOptions
            {
                sizeOfStruct = 0x2C,
                pixelFormat = format,
                channelTypes = new []{(int)channelType, (int)channelType, (int)channelType, (int)channelType },
                colorSpace = colorSpace,
                compressionQuality = quality,
                doDither = false,
                maxRange = 1
            };
            return Transcode(options);
        }

        public bool Transcode(TranscoderOptions options)
        {
            Validate();

            var optionPtr = NativeHelper.MarshalObject(options);
            var result = PVRTexLib_TranscodeTexture(_pointer, optionPtr);
            NativeHelper.FreeObject(optionPtr);

            return result;
        }

        public byte[] GetData()
        {
            var dataPtr = PVRTexLib_GetTextureDataPtr(_pointer);
            var dataSize = PVRTexLib_GetTextureDataSize(PVRTexLib_GetTextureHeader(_pointer));

            var data = new byte[dataSize];
            Marshal.Copy(dataPtr, data, 0, (int)dataSize);

            return data;
        }

        private void Validate()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_pointer));
            NativeHelper.SetDllImportResolver();
        }

        #region Dispose

        ~PvrTexture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                // Free managed resources
            }

            // Free unmanaged resources
            PVRTexLib_DestroyTexture(_pointer);
            _pointer = IntPtr.Zero;

            IsDisposed = true;
        }

        #endregion

        #region Native calls

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

        #endregion
    }

    class PvrHeader : IDisposable
    {
        private IntPtr _pointer;

        public bool IsDisposed { get; private set; }

        public PvrHeader(PvrCreateParams parameters)
        {
            Validate();
            _pointer = PVRTexLib_CreateTextureHeader(parameters);
        }

        internal PvrHeader(IntPtr pointer)
        {
            _pointer = pointer;
        }

        public IntPtr GetAddress() => _pointer;

        public Size GetSize(uint mipMap = 0)
        {
            Validate();

            return new Size((int)PVRTexLib_GetTextureWidth(_pointer, mipMap), (int)PVRTexLib_GetTextureHeight(_pointer, mipMap));
        }

        public PixelFormat GetPixelFormat()
        {
            Validate();

            return PVRTexLib_GetTexturePixelFormat(_pointer);
        }

        public uint GetBitDepth()
        {
            Validate();

            return PVRTexLib_GetTextureBitsPerPixel(_pointer);
        }

        private void Validate()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_pointer));
            NativeHelper.SetDllImportResolver();
        }

        #region Dispose

        ~PvrHeader()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                // Free managed resources
            }

            // Free unmanaged resources
            PVRTexLib_DestroyTextureHeader(_pointer);
            _pointer = IntPtr.Zero;

            IsDisposed = true;
        }

        #endregion

        #region Native calls

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

        #endregion
    }

    #region Structs

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PvrCreateParams
    {
        public PixelFormat pixelFormat;
        public uint width;
        public uint height;
        public uint depth;
        public uint numMipMaps;
        public uint numArrayMembers;
        public uint numFaces;
        public ColorSpace colorSpace;
        public ChannelType channelType;
        [MarshalAs(UnmanagedType.Bool)]
        public bool preMultiplied;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x2C, Pack = 1)]
    public class TranscoderOptions
    {
        public uint sizeOfStruct;
        public PixelFormat pixelFormat;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = 4)]
        public int[] channelTypes;
        public ColorSpace colorSpace;
        public CompressionQuality compressionQuality;
        [MarshalAs(UnmanagedType.Bool)]
        public bool doDither;
        public float maxRange;
    }

    #endregion

    #region Enums

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
        BC4,
        BC5,

        /* Currently unsupported: */
        BC6,
        BC7,
        /* ~~~~~~~~~~~~~~~~~~ */

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

        ASTC_4x4,
        ASTC_5x4,
        ASTC_5x5,
        ASTC_6x5,
        ASTC_6x6,
        ASTC_8x5,
        ASTC_8x6,
        ASTC_8x8,
        ASTC_10x5,
        ASTC_10x6,
        ASTC_10x8,
        ASTC_10x10,
        ASTC_12x10,
        ASTC_12x12,

        ASTC_3x3x3,
        ASTC_4x3x3,
        ASTC_4x4x3,
        ASTC_4x4x4,
        ASTC_5x4x4,
        ASTC_5x5x4,
        ASTC_5x5x5,
        ASTC_6x5x5,
        ASTC_6x6x5,
        ASTC_6x6x6,

        BASISU_ETC1S,
        BASISU_UASTC,

        RGBM,
        RGBD,

        //Invalid value
        NumCompressedPFs,

        // Component formats
        RGBA8888 = 0x0808080861626772
    }

    public enum ChannelType : int
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
        SignedFloat,
        Float = SignedFloat, //the name Float is now deprecated.
        UnsignedFloat,
        NumVarTypes,

        Invalid = 255
    };

    public enum ColorSpace : int
    {
        Linear,
        sRGB,
        NumSpaces
    };

    public enum CompressionQuality : int
    {
        PVRTCFastest = 0,   //!< PVRTC fastest
        PVRTCFast,          //!< PVRTC fast
        PVRTCLow,           //!< PVRTC low
        PVRTCNormal,        //!< PVRTC normal
        PVRTCHigh,          //!< PVRTC high
        PVRTCVeryHigh,      //!< PVRTC very high
        PVRTCThorough,      //!< PVRTC thorough
        PVRTCBest,          //!< PVRTC best
        NumPVRTCModes,      //!< Number of PVRTC modes

        ETCFast = 0,        //!< ETC fast
        ETCNormal,          //!< ETC normal
        ETCSlow,            //!< ETC slow
        NumETCModes,        //!< Number of ETC modes

        ASTCVeryFast = 0,   //!< ASTC very fast
        ASTCFast,           //!< ASTC fast
        ASTCMedium,         //!< ASTC medium
        ASTCThorough,       //!< ASTC thorough
        ASTCExhaustive,     //!< ASTC exhaustive
        NumASTCModes,       //!< Number of ASTC modes

        BASISULowest = 0,   //!< BASISU lowest quality
        BASISULow,          //!< BASISU low quality
        BASISUNormal,       //!< BASISU normal quality
        BASISUHigh,         //!< BASISU high quality
        BASISUBest,         //!< BASISU best quality
        NumBASISUModes      //!< Number of BASISU modes
    };

    #endregion
}
