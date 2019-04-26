namespace Kanvas.Format.PVRTC.Models
{
    public enum PvrtcFormat : ulong
    {
        PVRTC_2bpp = PixelFormat.PVRTCI_2bpp_RGB,
        PVRTCA_2bpp = PixelFormat.PVRTCI_2bpp_RGBA,
        PVRTC_4bpp = PixelFormat.PVRTCI_4bpp_RGB,
        PVRTCA_4bpp = PixelFormat.PVRTCI_4bpp_RGBA,
        PVRTC2_2bpp = PixelFormat.PVRTCII_2bpp,
        PVRTC2_4bpp = PixelFormat.PVRTCII_4bpp,
    }
}
