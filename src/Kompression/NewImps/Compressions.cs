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

        public static KompressionConfiguration Lz11
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(modes => new Lz11Decoder()).
                    EncodeWith((parser, builder, modes) => new Lz11Encoder(parser));
                config.WithMatchOptions(options =>
                    options.WithinLimitations(() => new FindLimitations(3, 0x10110, 1, 0x1000)));

                return config;
            }
        }

        public static KompressionConfiguration BackwardLz77
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(modes => new BackwardLz77Decoder(ByteOrder.LittleEndian)).
                    EncodeWith((parser, builder, modes) => new BackwardLz77Encoder(parser, ByteOrder.LittleEndian));
                config.WithMatchOptions(options =>
                    options.FindInBackwardOrder().
                        WithinLimitations(() => new FindLimitations(3, 0x12, 3, 0x1002)));

                return config;
            }
        }
    }
}
