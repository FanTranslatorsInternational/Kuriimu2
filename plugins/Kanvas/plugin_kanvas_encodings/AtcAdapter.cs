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
using Kanvas.Encoding.Support.ATC.Models;
using Kanvas.Interface;
using Kanvas.Models;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces;
using Kontract.Interfaces.Intermediate;
using Kontract.Models;

namespace plugin_kanvas_encodings
{
    [Export(typeof(IPlugin))]
    [Property(nameof(AlphaMode), typeof(AlphaMode), AlphaMode.None)]
    public class AtcAdapter : IColorEncodingAdapter
    {
        private IImageSwizzle GetSwizzle()
        {
            if (Swizzle == null)
                return null;

            var props = Swizzle.GetType().GetProperties();
            var swizzleProp = props.FirstOrDefault(x => x.PropertyType == typeof(IImageSwizzle));
            return (IImageSwizzle)swizzleProp?.GetValue(Swizzle);
        }

        public AlphaMode AlphaMode { get; set; }

        public string Name => "ATC";
        public IImageSwizzleAdapter Swizzle { get; set; }

        public int CalculateLength(int width, int height)
        {
            var bitDepth = AlphaMode != AlphaMode.None ? 8 : 4;
            return Align(width, 4) * Align(height, 4) * bitDepth / 8;
        }

        public Task<Bitmap> Decode(byte[] imgData, int width, int height, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new ATC(AlphaMode), width, height)
            {
                Swizzle = GetSwizzle(),
                PadWidth = Align(width, 4),
                PadHeight = Align(height, 4)
            };
            return Task.Factory.StartNew(() => Kanvas.Kolors.Load(imgData, settings));
        }

        public Task<byte[]> Encode(Bitmap img, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new ATC(AlphaMode), img.Width, img.Height)
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
