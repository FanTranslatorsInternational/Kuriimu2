using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Format.ATI.Models;

namespace Kanvas.Format.ATI
{
    public class Decoder
    {
        private readonly Queue<Color> _queue;
        private readonly AtiFormat _format;

        private static int Interpolate(int a, int b, int num, int den) => (num * a + (den - num) * b + den / 2) / den;

        public Decoder(AtiFormat format)
        {
            _queue = new Queue<Color>();
            _format = format;
        }

        public Color Get(Func<(ulong block, ulong block2)> func)
        {
            if (_queue.Any())
                return _queue.Dequeue();

            var (block, block2) = func();

            var p_0 = block & 0xFF;
            var p_1 = block >> 8 & 0xFF;

            switch (_format)
            {
                case AtiFormat.ATI1A:
                case AtiFormat.ATI1L:
                    for (int i = 0; i < 16; i++)
                    {
                        var code = (int)(block >> 16 + 3 * i) & 7;
                        var value = (code == 0) ? (int)p_0
                            : code == 1 ? (int)p_1
                            : p_0 > p_1 ? Interpolate((int)p_0, (int)p_1, 8 - code, 7)
                            : code < 6 ? Interpolate((int)p_0, (int)p_1, 6 - code, 5)
                            : (code - 6) * 255;

                        _queue.Enqueue((_format == AtiFormat.ATI1A) ?
                            Color.FromArgb(value, 255, 255, 255) :
                            Color.FromArgb(255, value, value, value));
                    }
                    break;
                case AtiFormat.ATI2:
                    var a_0 = block2 & 0xFF;
                    var a_1 = block2 >> 8 & 0xFF;

                    for (var i = 0; i < 16; i++)
                    {
                        var codeL = (int)(block >> 16 + 3 * i) & 7;
                        var codeA = (int)(block2 >> 16 + 3 * i) & 7;

                        var lum = (codeL == 0) ? (int)p_0
                            : codeL == 1 ? (int)p_1
                            : p_0 > p_1 ? Interpolate((int)p_0, (int)p_1, 8 - codeL, 7)
                            : codeL < 6 ? Interpolate((int)p_0, (int)p_1, 6 - codeL, 5)
                            : (codeL - 6) * 255;

                        var alpha = (codeA == 0) ? (int)a_0
                            : codeA == 1 ? (int)a_1
                            : a_0 > a_1 ? Interpolate((int)a_0, (int)a_1, 8 - codeA, 7)
                            : codeA < 6 ? Interpolate((int)a_0, (int)a_1, 6 - codeA, 5)
                            : (codeA - 6) * 255;

                        _queue.Enqueue(Color.FromArgb(alpha, lum, lum, lum));
                    }
                    break;
            }

            return _queue.Dequeue();
        }
    }
}
