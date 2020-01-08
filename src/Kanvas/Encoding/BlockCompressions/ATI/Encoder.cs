using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.BlockCompressions.ATI.Models;
using Kanvas.Encoding.BlockCompressions.BCn;

namespace Kanvas.Encoding.BlockCompressions.ATI
{
    public class Encoder
    {
        private readonly List<Color> _queue;
        private readonly AtiFormat _format;

        public Encoder(AtiFormat format)
        {
            _queue=new List<Color>();
            _format = format;
        }

        public void Set(Color c, Action<(ulong alpha, ulong block)> func)
        {
            _queue.Add(c);
            if (_queue.Count != 16)
                return;

            // Alpha
            if (_format == AtiFormat.ATI1A)
            {
                ulong outAlpha = 0;
                var alphaEncoder = new BC4BlockEncoder();
                alphaEncoder.LoadBlock(_queue.Select(clr => clr.A / 255f).ToArray());
                outAlpha = alphaEncoder.EncodeUnsigned().PackedValue;

                func((0, outAlpha));
            }
            //Luminance
            else if (_format == AtiFormat.ATI1L)
            {
                ulong outLum = 0;
                var lumEncoder = new BC4BlockEncoder();
                lumEncoder.LoadBlock(_queue.Select(clr => clr.R / 255f).ToArray());
                outLum = lumEncoder.EncodeUnsigned().PackedValue;

                func((0, outLum));
            }
            //ATI2 - Both
            else if (_format == AtiFormat.ATI2)
            {
                var alphaEncoder = new BC4BlockEncoder();
                alphaEncoder.LoadBlock(_queue.Select(clr => clr.A / 255f).ToArray());
                var outAlpha = alphaEncoder.EncodeUnsigned().PackedValue;

                var lumEncoder = new BC4BlockEncoder();
                lumEncoder.LoadBlock(_queue.Select(clr => clr.R / 255f).ToArray());
                var outLum = lumEncoder.EncodeUnsigned().PackedValue;

                func((outAlpha, outLum));
            }

            _queue.Clear();
        }
    }
}
