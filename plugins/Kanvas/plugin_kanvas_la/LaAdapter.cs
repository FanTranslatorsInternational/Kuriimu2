using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Kanvas.Interface;
using Kanvas.Models;
using Kontract.Interfaces.Intermediate;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces;
using Kontract.Models;

namespace plugin_kanvas_la
{
    [Export(typeof(IPlugin))]
    [Property(nameof(Luminence), typeof(byte), 8)]
    [Property(nameof(Alpha), typeof(byte), 8)]
    public class LaAdapter : IColorEncodingAdapter
    {
        private IImageSwizzle GetSwizzle()
        {
            if (Swizzle == null)
                return null;

            var props = Swizzle.GetType().GetProperties();
            var swizzleProp = props.FirstOrDefault(x => x.PropertyType == typeof(IImageSwizzle));
            return (IImageSwizzle)swizzleProp?.GetValue(Swizzle);
        }

        public byte Luminence { get; set; }
        public byte Alpha { get; set; }

        public string Name => "LA";
        public IImageSwizzleAdapter Swizzle { get; set; }

        public int CalculateLength(int width, int height)
        {
            var bitDepth = Luminence + Alpha;
            var byteDepth = bitDepth + (8 - bitDepth % 8) / 8;
            return width * height *byteDepth;
        }

        public Task<Bitmap> Decode(byte[] imgData, int width, int height, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new LA(Luminence, Alpha), width, height)
            {
                Swizzle = GetSwizzle()
            };
            return Task.Factory.StartNew(() => Kanvas.Kolors.Load(imgData, settings));
        }

        public Task<byte[]> Encode(Bitmap img, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new LA(Luminence, Alpha), img.Width, img.Height)
            {
                Swizzle = GetSwizzle()
            };
            return Task.Factory.StartNew(() => Kanvas.Kolors.Save(img, settings));
        }
    }
}
