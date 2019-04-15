using Kanvas.Interface;
using Kanvas.Models;
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

        public PaletteImageSettings(IImageFormat format, IPaletteImageFormat paletteFormat, int width, int height) : base(format, width, height)
        {
            PaletteFormat = paletteFormat;
        }
    }
}
