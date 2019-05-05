using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Kanvas.Encoding.Support.ATC.Models;
using Kanvas.Interface;
using Kanvas.Models;
using Kontract.Interfaces.Intermediate;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces;
using Kontract.Models;

namespace plugin_kanvas_atc
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
            var byteDepth = AlphaMode != AlphaMode.None ? 16 : 8;
            return (Align(width, 4) / 4) * (Align(height, 4) / 4) * byteDepth;
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
            return value + (align - value % align);
        }
    }
}
