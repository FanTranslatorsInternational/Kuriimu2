using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.BlockCompressions.ATC.Models;
using Kanvas.Encoding.BlockCompressions.BCn;

namespace Kanvas.Encoding.BlockCompressions.ATC
{
    internal class Encoder
    {
        private readonly List<Color> _queue;
        private readonly AlphaMode _alphaMode;

        private static ushort From565To555(ushort value) => (ushort)(value & 0x1F | (Kanvas.Support.Convert.ChangeBitDepth((value >> 5) & 0x3F, 6, 5) << 5) | (((value >> 11) & 0x1F) << 10));
        private static readonly Dictionary<int, ulong> _remap = new Dictionary<int, ulong>
        {
            [0] = 0,
            [1] = 3,
            [2] = 1,
            [3] = 2
        };

        public Encoder(AlphaMode alphaMode)
        {
            _queue=new List<Color>();
            _alphaMode = alphaMode;
        }

        public void Set(Color c, Action<(ulong alpha, ulong block)> func)
        {
            _queue.Add(c);
            if (_queue.Count != 16)
                return;

            // Alpha
            ulong outAlpha = 0;
            if (_alphaMode == AlphaMode.Interpolated)
            {
                var alphaEncoder = new BC4BlockEncoder();
                alphaEncoder.LoadBlock(_queue.Select(clr => clr.A / 255f).ToArray());
                outAlpha = alphaEncoder.EncodeUnsigned().PackedValue;
            }
            else
            {
                var alphaEncoder = new BC2ABlockEncoder();
                alphaEncoder.LoadBlock(_queue.Select(clr => clr.A / 255f).ToArray());
                outAlpha = alphaEncoder.Encode().PackedValue;
            }

            // Color
            var colorEncoder = new BC1BlockEncoder();
            colorEncoder.LoadBlock(
                _queue.Select(clr => clr.R / 255f).ToArray(),
                _queue.Select(clr => clr.G / 255f).ToArray(),
                _queue.Select(clr => clr.B / 255f).ToArray()
            );
            var outColor = colorEncoder.Encode().PackedValue;

            //ATC specific modifications to BC1
            //according to http://www.guildsoftware.com/papers/2012.Converting.DXTC.to.ATC.pdf
            //change color0 from rgb565 to rgb555 with method 0
            outColor = (outColor & ~0xFFFFUL) | (From565To555((ushort)outColor));

            //change color codes
            for (int i = 0; i < 16; i++)
                outColor = (outColor & ~((ulong)0x3 << (32 + i * 2))) | _remap[(int)((outColor >> (32 + 2 * i)) & 0x3)] << (32 + i * 2);

            func((outAlpha, outColor));
            _queue.Clear();
        }
    }
}
