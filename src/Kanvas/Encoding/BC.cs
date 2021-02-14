using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;

namespace Kanvas.Encoding
{
    // TODO: Implement BC with BCnEncoder nuget package
    public class Bc : IColorEncoding
    {
        /// <inheritdoc cref="BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public int BitsPerValue { get; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public int ColorsPerValue => 16;

        /// <inheritdoc cref="FormatName"/>
        public string FormatName { get; }

        public Bc(BcFormat format)
        {
            var hasSecondBlock = HasSecondBlock(format);

            BitsPerValue = hasSecondBlock ? 128 : 64;
            BitDepth = hasSecondBlock ? 8 : 4;

            FormatName = format.ToString();
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

        private bool HasSecondBlock(BcFormat format)
        {
            return format == BcFormat.Bc2 ||
                   format == BcFormat.Bc3 ||
                   format == BcFormat.Bc5 ||
                   //format == BcFormat.Bc6 ||
                   format == BcFormat.Bc7;
        }
    }

    /// <summary>
    /// The format identifier for BCs.
    /// </summary>
    /// <remarks>
    /// The WiiU contains non-standardized implementations for BC4 and BC5.<para />
    /// The WiiU implements BC4 with target Alpha or Luminance (RGB channels), instead of Red.<para />
    /// The WiiU implements BC5 with target Alpha/Luminance, instead of Red/Green.
    /// </remarks>
    public enum BcFormat
    {
        Bc1,
        Bc2,
        Bc3,
        Bc4,
        Bc5,
        //Bc6,
        Bc7,

        Dxt1 = Bc1,
        Dxt3,
        Dxt5,

        Ati1 = Bc4,
        Ati2
    }
}
