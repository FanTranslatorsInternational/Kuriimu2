using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Format.DXT.Models;
using Kanvas.Support.BCn;

namespace Kanvas.Format.DXT
{
    internal class Encoder
    {
        private readonly List<Color> _queue;
        private readonly DxtFormat _format;

        public Encoder(DxtFormat format)
        {
            _queue = new List<Color>();
            _format = format;
        }

        public void Set(Color c, Action<(ulong alpha, ulong block)> func)
        {
            _queue.Add(c);
            if (_queue.Count != 16)
                return;

            // Alpha
            ulong outAlpha = 0;
            if (_format == DxtFormat.DXT5)
            {
                var alphaEncoder = new BC4BlockEncoder();
                alphaEncoder.LoadBlock(_queue.Select(clr => clr.A / 255f).ToArray());
                outAlpha = alphaEncoder.EncodeUnsigned().PackedValue;
            }

            // Color
            var colorEncoder = new BC1BlockEncoder();
            colorEncoder.LoadBlock(
                _queue.Select(clr => clr.R / 255f).ToArray(),
                _queue.Select(clr => clr.G / 255f).ToArray(),
                _queue.Select(clr => clr.B / 255f).ToArray()
            );
            var outColor = colorEncoder.Encode().PackedValue;

            func((outAlpha, outColor));
            _queue.Clear();
        }
    }
}
