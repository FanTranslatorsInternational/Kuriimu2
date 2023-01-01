using System;

namespace Kore.Exceptions.KPal
{
    class UnsupportedKPalVersionException : Exception
    {
        public UnsupportedKPalVersionException(int version) : base($"Unsupported Kuriimu Palette version: {version}.")
        {

        }
    }
}
