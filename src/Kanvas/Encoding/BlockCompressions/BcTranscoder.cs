using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.BlockCompressions.BCn;
using Kanvas.Encoding.BlockCompressions.BCn.Models;

namespace Kanvas.Encoding.BlockCompressions
{
    class BcTranscoder
    {
        private readonly BcFormat _format;

        public BcTranscoder(BcFormat format)
        {
            _format = format;
        }

        public IEnumerable<Color> DecodeBlocks(ulong block1, ulong block2)
        {
            switch (_format)
            {
                case BcFormat.BC1:
                    return BC1BlockDecoder.Instance.Process(block1);

                case BcFormat.BC2:
                    var alphas = BC2AlphaBlockDecoder.Instance.Process(block1);
                    var colors = BC1BlockDecoder.Instance.Process(block2);

                    return Zip(alphas, colors).Select(c => Color.FromArgb(c.First, c.Second));

                case BcFormat.BC3:
                    var alphas1 = BC4BlockDecoder.Instance.Process(block1);
                    var colors1 = BC1BlockDecoder.Instance.Process(block2);

                    return Zip(alphas1, colors1).Select(c => Color.FromArgb(c.First, c.Second));

                case BcFormat.BC4:
                    var reds = BC4BlockDecoder.Instance.Process(block1);

                    return reds.Select(r => Color.FromArgb(r, 0, 0));

                case BcFormat.BC5:
                    var reds1 = BC4BlockDecoder.Instance.Process(block1);
                    var greens = BC4BlockDecoder.Instance.Process(block2);

                    return Zip(reds1, greens).Select(c => Color.FromArgb(c.First, c.Second, 0));

                case BcFormat.ATI1A_WiiU:
                    var alphas2 = BC4BlockDecoder.Instance.Process(block1);

                    return alphas2.Select(a => Color.FromArgb(a, 0, 0, 0));

                case BcFormat.ATI1L_WiiU:
                    var lums = BC4BlockDecoder.Instance.Process(block1);

                    return lums.Select(l => Color.FromArgb(l, l, l));

                case BcFormat.ATI2_WiiU:
                    var lums1 = BC4BlockDecoder.Instance.Process(block1);
                    var alphas3 = BC4BlockDecoder.Instance.Process(block2);

                    return Zip(lums1, alphas3).Select(c => Color.FromArgb(c.Second, c.First, c.First, c.First));
            }

            return Array.Empty<Color>();
        }

        public BcPixelData EncodeColors(IList<Color> colors)
        {
            var bc1Encoder = BC1BlockEncoder.Instance;
            var bc4Encoder = BC4BlockEncoder.Instance;

            Bc1BlockData bc1Block;
            ulong outColor;
            ulong outAlpha;

            switch (_format)
            {
                case BcFormat.BC1:
                    bc1Block = bc1Encoder.LoadBlock(colors);
                    outColor = bc1Encoder.Encode(bc1Block).PackedValue;

                    return new BcPixelData { Block1 = outColor };

                case BcFormat.BC2:
                    bc1Block = bc1Encoder.LoadBlock(colors, false);
                    outColor = bc1Encoder.Encode(bc1Block).PackedValue;

                    outAlpha = BC2AlphaBlockEncoder.Instance.Process(colors);

                    return new BcPixelData { Block1 = outAlpha, Block2 = outColor };

                case BcFormat.BC3:
                    var bc4Block = bc4Encoder.LoadBlock(colors, Bc4Component.A);
                    outAlpha = bc4Encoder.EncodeUnsigned(bc4Block).PackedValue;

                    bc1Block = bc1Encoder.LoadBlock(colors, false);
                    outColor = bc1Encoder.Encode(bc1Block).PackedValue;

                    return new BcPixelData { Block1 = outAlpha, Block2 = outColor };

                case BcFormat.BC4:
                    var bc4Block1 = bc4Encoder.LoadBlock(colors, Bc4Component.R);
                    var outRed = bc4Encoder.EncodeUnsigned(bc4Block1).PackedValue;

                    return new BcPixelData { Block1 = outRed };

                case BcFormat.BC5:
                    var bc4Block2 = bc4Encoder.LoadBlock(colors, Bc4Component.R);
                    var outRed1 = bc4Encoder.EncodeUnsigned(bc4Block2).PackedValue;

                    var bc4Block3 = bc4Encoder.LoadBlock(colors, Bc4Component.G);
                    var outGreen = bc4Encoder.EncodeUnsigned(bc4Block3).PackedValue;

                    return new BcPixelData { Block1 = outRed1, Block2 = outGreen };

                case BcFormat.ATI1A_WiiU:
                    var bc4Block4 = bc4Encoder.LoadBlock(colors, Bc4Component.A);
                    var outAlpha1 = bc4Encoder.EncodeUnsigned(bc4Block4).PackedValue;

                    return new BcPixelData { Block1 = outAlpha1 };

                case BcFormat.ATI1L_WiiU:
                    var bc4Block5 = bc4Encoder.LoadBlock(colors, Bc4Component.L);
                    var outLum = bc4Encoder.EncodeUnsigned(bc4Block5).PackedValue;

                    return new BcPixelData { Block1 = outLum };

                case BcFormat.ATI2_WiiU:
                    var bc4Block6 = bc4Encoder.LoadBlock(colors, Bc4Component.R);
                    var outLum1 = bc4Encoder.EncodeUnsigned(bc4Block6).PackedValue;

                    var bc4Block7 = bc4Encoder.LoadBlock(colors, Bc4Component.G);
                    var outAlpha2 = bc4Encoder.EncodeUnsigned(bc4Block7).PackedValue;

                    return new BcPixelData { Block1 = outLum1, Block2 = outAlpha2 };
            }

            return default;
        }

        // TODO: Remove when targeting only netcoreapp31
        private IEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
#if NET_CORE_31
            return first.Zip(second);
#else
            return first.Zip(second, (f, s) => (f, s));
#endif
        }
    }
}
