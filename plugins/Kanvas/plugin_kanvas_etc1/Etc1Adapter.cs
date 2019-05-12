﻿using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Kanvas.Encoding;
using Kanvas.Encoding.Support.PVRTC.Models;
using Kanvas.Interface;
using Kanvas.Models;
using Kontract.Interfaces.Intermediate;
using Kontract.Attributes.Intermediate;
using Kontract.Interfaces;
using Kontract.Models;

namespace plugin_kanvas_etc1
{
    [Export(typeof(IPlugin))]
    [Property(nameof(Alpha), typeof(bool), false)]
    public class Etc1Adapter : IColorEncodingAdapter
    {
        private IImageSwizzle GetSwizzle()
        {
            if (Swizzle == null)
                return null;

            var props = Swizzle.GetType().GetProperties();
            var swizzleProp = props.FirstOrDefault(x => x.PropertyType == typeof(IImageSwizzle));
            return (IImageSwizzle)swizzleProp?.GetValue(Swizzle);
        }

        public bool Alpha { get; set; }

        public string Name => "ETC1";
        public IImageSwizzleAdapter Swizzle { get; set; }

        public int CalculateLength(int width, int height)
        {
            var byteDepth = Alpha ? 16 : 8;
            return (Align(width, 4) / 4) * (Align(height, 4) / 4) * byteDepth;
        }

        public Task<Bitmap> Decode(byte[] imgData, int width, int height, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new ETC1(Alpha), width, height)
            {
                Swizzle = GetSwizzle(),
                PadWidth = Align(width, 4),
                PadHeight = Align(height, 4)
            };
            return Task.Factory.StartNew(() => Kanvas.Kolors.Load(imgData, settings));
        }

        public Task<byte[]> Encode(Bitmap img, IProgress<ProgressReport> progress)
        {
            var settings = new ImageSettings(new ETC1(Alpha), img.Width, img.Height)
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