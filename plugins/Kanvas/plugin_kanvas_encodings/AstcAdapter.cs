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
using Kanvas.Interface;
using Kanvas.Models;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces;
using Kontract.Interfaces.Intermediate;
using Kontract.Models;

namespace plugin_kanvas_encodings
{
    // TODO: Fix ASTC to not exit application on errors
    //[Export(typeof(IColorEncodingAdapter))]
    [Property(nameof(XDim), typeof(int), 4)]
    [Property(nameof(YDim), typeof(int), 4)]
    [Property(nameof(ZDim), typeof(int), 1)]
    public class AstcAdapter : IColorEncodingAdapter
    {
        private IImageSwizzle GetSwizzle()
        {
            if (Swizzle == null)
                return null;

            var props = Swizzle.GetType().GetProperties();
            var swizzleProp = props.FirstOrDefault(x => x.PropertyType == typeof(IImageSwizzle));
            return (IImageSwizzle)swizzleProp?.GetValue(Swizzle);
        }

        public int XDim { get; set; }
        public int YDim { get; set; }
        public int ZDim { get; set; }

        public string Name => "ASTC";
        public IImageSwizzleAdapter Swizzle { get; set; }

        public int CalculateLength(int width, int height)
        {
            // TODO: Can't properly calculate length yet
            return width * height;
        }

        public Task<Bitmap> Decode(byte[] imgData, int width, int height, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new ASTC(XDim, YDim, ZDim), width, height)
            {
                Swizzle = GetSwizzle(),
                PadWidth = Align(width, 4),
                PadHeight = Align(height, 4)
            };
            return Task.Factory.StartNew(() => Kanvas.Kolors.Load(imgData, settings));
        }

        public Task<byte[]> Encode(Bitmap img, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new ASTC(XDim, YDim, ZDim), img.Width, img.Height)
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
