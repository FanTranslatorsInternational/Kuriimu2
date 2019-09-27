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

        public static KompressionConfiguration Lz40
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(modes => new Lz40Decoder()).
                    EncodeWith((parser, builder, modes) => new Lz40Encoder(parser));
                config.WithMatchOptions(options =>
                    options.WithinLimitations(() => new FindLimitations(0x3, 0x1010F, 1, 0xFFF)));

                return config;
            }
        }

        public static KompressionConfiguration Lz60
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(modes => new Lz60Decoder()).
                    EncodeWith((parser, builder, modes) => new Lz60Encoder(parser));
                config.WithMatchOptions(options =>
                    options.WithinLimitations(() => new FindLimitations(0x3, 0x1010F, 1, 0xFFF)));

                return config;
            }
        }

        public static KompressionConfiguration Lz77
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(modes => new Lz77Decoder()).
                    EncodeWith((parser, builder, modes) => new Lz77Encoder(parser));
                config.WithMatchOptions(options =>
                    options.SkipUnitsAfterMatch(1).
                        WithinLimitations(() => new FindLimitations(0x1, 0xFF, 1, 0xFF)));

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

        public static KompressionConfiguration LzEcd
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(modes => new LzEcdDecoder(0x3BE)).
                    EncodeWith((parser, builder, modes) => new LzEcdEncoder(parser));
                config.WithMatchOptions(options =>
                    options.WithPreBufferSize(0x3BE).
                        WithinLimitations(() => new FindLimitations(3, 0x42, 1, 0x400)));

                return config;
            }
        }

        public static KompressionConfiguration Lze
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(modes => new LzeDecoder()).
                    EncodeWith((parser, builder, modes) => new LzeEncoder(parser));
                config.WithMatchOptions(options =>
                    options.WithinLimitations(() => new FindLimitations(0x3, 0x12, 5, 0x1004)).
                        WithinLimitations(() => new FindLimitations(0x2, 0x41, 1, 4)));

                return config;
            }
        }
    }
}
