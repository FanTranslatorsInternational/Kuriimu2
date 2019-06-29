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
    [Export(typeof(IPlugin))]
    [Property(nameof(Red), typeof(byte), 8)]
    [Property(nameof(Green), typeof(byte), 8)]
    public class HiloAdapter : IColorEncodingAdapter
    {
        private IImageSwizzle GetSwizzle()
        {
            if (Swizzle == null)
                return null;

            var props = Swizzle.GetType().GetProperties();
            var swizzleProp = props.FirstOrDefault(x => x.PropertyType == typeof(IImageSwizzle));
            return (IImageSwizzle)swizzleProp?.GetValue(Swizzle);
        }

        public byte Red { get; set; }
        public byte Green { get; set; }

        public string Name => "HILO";
        public IImageSwizzleAdapter Swizzle { get; set; }

        public int CalculateLength(int width, int height)
        {
            var bitDepth = Red + Green;
            var byteDepth = bitDepth + (8 - bitDepth % 8) / 8;
            return width * height * byteDepth;
        }

        public Task<Bitmap> Decode(byte[] imgData, int width, int height, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new HILO(Red, Green), width, height)
            {
                Swizzle = GetSwizzle()
            };
            return Task.Factory.StartNew(() => Kanvas.Kolors.Load(imgData, settings));
        }

        public Task<byte[]> Encode(Bitmap img, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new HILO(Red, Green), img.Width, img.Height)
            {
                Swizzle = GetSwizzle()
            };
            return Task.Factory.StartNew(() => Kanvas.Kolors.Save(img, settings));
        }
    }
}
