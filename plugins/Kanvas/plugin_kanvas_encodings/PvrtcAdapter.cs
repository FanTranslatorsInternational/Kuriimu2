using System;
#if NET_CORE_21
using System.Composition;
#else
using System.ComponentModel.Composition;
#endif
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Kanvas.Encoding.Support.PVRTC.Models;
using Kanvas.Interface;
using Kanvas.Models;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces;
using Kontract.Interfaces.Intermediate;
using Kontract.Models;

namespace plugin_kanvas_encodings
{
    [Export(typeof(IPlugin))]
    [Property(nameof(Format), typeof(PvrtcFormat), PvrtcFormat.PVRTC_4bpp)]
    public class PvrtcAdapter : IColorEncodingAdapter
    {
        private IImageSwizzle GetSwizzle()
        {
            if (Swizzle == null)
                return null;

            var props = Swizzle.GetType().GetProperties();
            var swizzleProp = props.FirstOrDefault(x => x.PropertyType == typeof(IImageSwizzle));
            return (IImageSwizzle)swizzleProp?.GetValue(Swizzle);
        }

        public PvrtcFormat Format { get; set; }

        public string Name => "PVRTC";
        public IImageSwizzleAdapter Swizzle { get; set; }

        public int CalculateLength(int width, int height)
        {
            var bitDepth = Format == PvrtcFormat.PVRTC2_4bpp ||
                Format == PvrtcFormat.PVRTCA_4bpp ||
                Format == PvrtcFormat.PVRTC_4bpp ? 4 : 2;
            return Align(width, 4) * Align(height, 4) * bitDepth / 8;
        }

        public Task<Bitmap> Decode(byte[] imgData, int width, int height, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new PVRTC(Format), width, height)
            {
                Swizzle = GetSwizzle(),
                PadWidth = Align(width, 4),
                PadHeight = Align(height, 4)
            };
            return Task.Factory.StartNew(() => Kanvas.Kolors.Load(imgData, settings));
        }

        public Task<byte[]> Encode(Bitmap img, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new PVRTC(Format), img.Width, img.Height)
            {
                Swizzle = GetSwizzle(),
                PadWidth = Align(img.Width, 4),
                PadHeight = Align(img.Height, 4)
            };
            return Task.Factory.StartNew(() => Kanvas.Kolors.Save(img, settings));
        }

        private int Align(int value, int align)
        {
            if (value % align == 0)
                return value;
            return value + (align - value % align);
        }
    }
}
