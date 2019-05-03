using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Kanvas.Interface;
using Kanvas.Models;
using Kontract.Interfaces.Intermediate;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces;

namespace plugin_rgba
{
    [Export(typeof(IPlugin))]
    [Property(nameof(Red), typeof(byte), 8)]
    [Property(nameof(Green), typeof(byte), 8)]
    [Property(nameof(Blue), typeof(byte), 8)]
    [Property(nameof(Alpha), typeof(byte), 8)]
    public class RgbaAdapter : IColorEncodingAdapter
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
        public byte Blue { get; set; }
        public byte Alpha { get; set; }

        public string Name => "RGBA";
        public IImageSwizzleAdapter Swizzle { get; set; }

        public int CalculateLength(int width, int height)
        {
            var bitDepth = Red + Green + Blue + Alpha;
            var paddedBitDepth = bitDepth + (8 - bitDepth % 8);
            return width * height * paddedBitDepth;
        }

        public Bitmap Decode(byte[] imgData, int width, int height)
        {
            var settings = new ImageSettings(new RGBA(Red, Green, Blue, Alpha), width, height)
            {
                Swizzle = GetSwizzle()
            };
            return Kanvas.Kolors.Load(imgData, settings);
        }

        public byte[] Encode(System.Drawing.Bitmap img)
        {
            throw new NotImplementedException();
        }
    }
}
