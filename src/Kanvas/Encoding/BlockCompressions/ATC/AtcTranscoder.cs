using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.BlockCompressions.ATC.Models;
using Kanvas.Encoding.BlockCompressions.BCn;
using Kanvas.Encoding.BlockCompressions.BCn.Models;

namespace Kanvas.Encoding.BlockCompressions.ATC
{
    class AtcTranscoder
    {
        private readonly AtcFormat _format;

        public AtcTranscoder(AtcFormat format)
        {
            _format = format;
        }

        public IEnumerable<Color> DecodeBlocks(ulong block1, ulong block2)
        {
            switch (_format)
            {
                case AtcFormat.ATC:
                    return AtcBlockDecoder.Instance.Process(block1);

                case AtcFormat.ATCA_Exp:
                    var alphas = BC2AlphaBlockDecoder.Instance.Process(block1);
                    var colors = AtcBlockDecoder.Instance.Process(block2);

                    return Zip(alphas, colors).Select(c => Color.FromArgb(c.First, c.Second));

                case AtcFormat.ATCA_Int:
                    var alphas1 = BC4BlockDecoder.Instance.Process(block1);
                    var colors1 = AtcBlockDecoder.Instance.Process(block2);

                    return Zip(alphas1, colors1).Select(c => Color.FromArgb(c.First, c.Second));
            }

            return Array.Empty<Color>();
        }

        public AtcBlockData EncodeColors(IList<Color> colors)
        {
            switch (_format)
            {
                case AtcFormat.ATC:
                    return new AtcBlockData { Block1 = AtcBlockEncoder.Instance.Process(colors) };

                case AtcFormat.ATCA_Exp:
                    var outAlpha = BC2AlphaBlockEncoder.Instance.Process(colors);
                    var outColor = AtcBlockEncoder.Instance.Process(colors);

                    return new AtcBlockData { Block1 = outAlpha, Block2 = outColor };

                case AtcFormat.ATCA_Int:
                    var bc4Block = BC4BlockEncoder.Instance.LoadBlock(colors, Bc4Component.A);
                    var outAlpha1 = BC4BlockEncoder.Instance.EncodeUnsigned(bc4Block).PackedValue;

                    var outColor1 = AtcBlockEncoder.Instance.Process(colors);

                    return new AtcBlockData { Block1 = outAlpha1, Block2 = outColor1 };
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
