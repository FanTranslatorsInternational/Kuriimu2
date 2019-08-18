using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.LempelZiv;
using Kompression.Specialized.SlimeMoriMori.ValueWriters;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    class SlimeMode2Encoder : ISlimeEncoder
    {
        private IValueWriter _valueWriter;

        public SlimeMode2Encoder(IValueWriter valueWriter)
        {
            _valueWriter = valueWriter;
        }

        public void Encode(Stream input, Stream output, LzMatch[] matches)
        {

        }
    }
}
