using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Format.ETC1.Models;

namespace Kanvas.Format.ETC1
{
    internal class Decoder
    {
        private readonly bool _zOrdered;
        private readonly Queue<Color> _queue;

        public Decoder(bool zOrdered)
        {
            _zOrdered = zOrdered;
            _queue = new Queue<Color>();
        }

        public Color Get(Func<PixelData> func)
        {
            if (_queue.Any())
                return _queue.Dequeue();

            var data = func();
            var basec0 = data.Block.Color0.Scale(data.Block.ColorDepth);
            var basec1 = data.Block.Color1.Scale(data.Block.ColorDepth);

            int flipbitmask = data.Block.FlipBit ? 2 : 8;
            foreach (var i in _zOrdered ? Constants.ZOrder : Constants.NormalOrder)
            {
                var basec = (i & flipbitmask) == 0 ? basec0 : basec1;
                var mod = Constants.Modifiers[(i & flipbitmask) == 0 ? data.Block.Table0 : data.Block.Table1];
                var c = basec + mod[data.Block[i]];
                _queue.Enqueue(Color.FromArgb((int)((data.Alpha >> (4 * i)) % 16 * 17), c.R, c.G, c.B));
            }
            return _queue.Dequeue();
        }
    }
}
