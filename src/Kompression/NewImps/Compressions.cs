using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Configuration;
using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.MatchFinders;
using Kompression.PatternMatch.LempelZiv;

namespace Kompression.NewImps
{
    public static class Compressions
    {
        public static KompressionConfiguration Lz10
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(modes => new Lz10Decoder()).EncodeWith((parser, builder, modes) => new Lz10Encoder(parser));
                config.WithMatchOptions(options =>
                    options.WithinLimitations(() => new FindLimitations(0x3, 0x12, 1, 0x1000)));

                return config;
            }
        }
    }
}
