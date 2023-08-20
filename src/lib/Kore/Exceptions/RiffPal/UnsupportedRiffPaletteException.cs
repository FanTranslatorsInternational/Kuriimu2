using System;

namespace Kore.Exceptions.RiffPal
{
    class UnsupportedRiffPaletteException : Exception
    {
        public UnsupportedRiffPaletteException(string palType) : base($"Unsupported RIFF Palette type: {palType}")
        {

        }
    }
}
