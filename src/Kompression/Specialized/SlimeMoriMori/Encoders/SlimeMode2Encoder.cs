using System.IO;
using System.Linq;
using Kompression.IO;
using Kompression.LempelZiv;
using Kompression.Specialized.SlimeMoriMori.ValueWriters;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    class SlimeMode2Encoder : SlimeEncoder
    {
        private IValueWriter _valueWriter;

        public SlimeMode2Encoder(IValueWriter valueWriter)
        {
            _valueWriter = valueWriter;
        }

        public override void Encode(Stream input, BitWriter bw, LzMatch[] matches)
        {
            CreateDisplacementTable(matches.Select(x => x.Displacement).ToArray(), 7);
            WriteDisplacementTable(bw);
            ;
        }
    }
}
