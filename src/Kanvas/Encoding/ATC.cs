using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;

namespace Kanvas.Encoding
{
    // TODO: Implement ATC with BCnEncoder nuget package.
    public class Atc : IColorEncoding
    {
        /// <inheritdoc cref="BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public int BitsPerValue { get; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public int ColorsPerValue => 16;

        /// <inheritdoc cref="FormatName"/>
        public string FormatName { get; }

        public Atc(AtcFormat format)
        {
            var hasSecondBlock = HasSecondBlock(format);

            BitDepth = BitsPerValue = hasSecondBlock ? 128 : 64;

            FormatName = format.ToString().Replace("_", " ");
        }

        /// <inheritdoc cref="Load"/>
        public IEnumerable<Color> Load(byte[] input, EncodingLoadContext loadContext)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="Save"/>
        public byte[] Save(IEnumerable<Color> colors, EncodingSaveContext saveContext)
        {
            throw new System.NotImplementedException();
        }

        private bool HasSecondBlock(AtcFormat format)
        {
            return format == AtcFormat.Atc_Explicit ||
                   format == AtcFormat.Atc_Interpolated;
        }
    }

    public enum AtcFormat
    {
        Atc,
        Atc_Explicit,
        Atc_Interpolated
    }
}
