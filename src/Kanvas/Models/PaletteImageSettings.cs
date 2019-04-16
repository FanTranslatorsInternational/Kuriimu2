using Kanvas.Interface;
using Kanvas.Models;
using Kanvas.Quantization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Models
{
    public class PaletteImageSettings : ImageSettings
    {
        public IPaletteImageFormat PaletteFormat { get; }

        public IColorQuantizer Quantizer { get; }

        public PaletteImageSettings(IImageFormat format, IPaletteImageFormat paletteFormat, IColorQuantizer quantizer, int width, int height) : base(format, width, height)
        {
            PaletteFormat = paletteFormat;
            Quantizer = quantizer;
        }
    }
}
