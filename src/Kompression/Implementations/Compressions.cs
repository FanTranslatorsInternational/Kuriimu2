using Kompression.Configuration;
using Kompression.Huffman;
using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch.MatchFinders;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model;
using Kontract.Models.IO;

namespace Kompression.Implementations
{
    public static class Compressions
    {
        public static IKompressionConfiguration Lz10
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Lz10Decoder()).EncodeWith((parser, builder) => new Lz10Encoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x3, 0x12, 1, 0x1000))
                    .CalculatePricesWith(() => new Lz10PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Lz11
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Lz11Decoder()).
                    EncodeWith((parser, builder) => new Lz11Encoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x10110, 1, 0x1000))
                    .CalculatePricesWith(() => new Lz11PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Lz40
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Lz40Decoder()).
                    EncodeWith((parser, builder) => new Lz40Encoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x3, 0x1010F, 1, 0xFFF))
                    .CalculatePricesWith(() => new Lz40PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Lz60
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Lz60Decoder()).
                    EncodeWith((parser, builder) => new Lz60Encoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x3, 0x1010F, 1, 0xFFF))
                    .CalculatePricesWith(() => new Lz60PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Lz77
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Lz77Decoder()).
                    EncodeWith((parser, builder) => new Lz77Encoder(parser));
                config.WithMatchOptions(options => options
                    .SkipUnitsAfterMatch(1)
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x1, 0xFF, 1, 0xFF))
                    .CalculatePricesWith(() => new Lz77PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration BackwardLz77
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new BackwardLz77Decoder(ByteOrder.LittleEndian)).
                    EncodeWith((parser, builder) => new BackwardLz77Encoder(parser, ByteOrder.LittleEndian));
                config.WithMatchOptions(options => options
                    .FindInBackwardOrder()
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x12, 3, 0x1002))
                    .CalculatePricesWith(() => new BackwardLz77PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration LzEcd
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new LzEcdDecoder(0x3BE)).
                    EncodeWith((parser, builder) => new LzEcdEncoder(parser));
                config.WithMatchOptions(options => options
                    .WithPreBufferSize(0x3BE)
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x42, 1, 0x400))
                    .CalculatePricesWith(() => new LzEcdPriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Lze
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new LzeDecoder()).
                    EncodeWith((parser, builder) => new LzeEncoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x3, 0x12, 5, 0x1004))
                    .AndWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x2, 0x41, 1, 4))
                    .CalculatePricesWith(() => new LzePriceCalculator()));

                return config;
            }
        }

        /* Is more LZSS, described by wikipedia, through the flag denoting if following data is compressed or raw.
           Though the format is denoted as LZ77 with the magic num? (Issue 517) */
        public static IKompressionConfiguration Lzss
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new LzssDecoder()).
                    EncodeWith((parser, builder) => new LzssEncoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x3, 0x12, 1, 0x1000))
                    .CalculatePricesWith(() => new LzssPriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration LzssVlc
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new LzssVlcDecoder()).
                    EncodeWith((parser, builder) => new LzssVlcEncoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(1, -1))
                    .CalculatePricesWith(() => new LzssVlcPriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration NintendoHuffman4BitLe
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new NintendoHuffmanDecoder(4, ByteOrder.LittleEndian)).
                    EncodeWith((parser, builder) => new NintendoHuffmanEncoder(4, ByteOrder.LittleEndian, builder));
                config.WithHuffmanOptions(options => options.BuildTreeWith(() => new HuffmanTreeBuilder()));

                return config;
            }
        }

        public static IKompressionConfiguration NintendoHuffman4BitBe
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new NintendoHuffmanDecoder(4, ByteOrder.BigEndian)).
                    EncodeWith((parser, builder) => new NintendoHuffmanEncoder(4, ByteOrder.BigEndian, builder));
                config.WithHuffmanOptions(options => options.BuildTreeWith(() => new HuffmanTreeBuilder()));

                return config;
            }
        }

        public static IKompressionConfiguration NintendoHuffman8Bit
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new NintendoHuffmanDecoder(8, ByteOrder.LittleEndian)).
                    EncodeWith((parser, builder) => new NintendoHuffmanEncoder(8, ByteOrder.LittleEndian, builder));
                config.WithHuffmanOptions(options => options.BuildTreeWith(() => new HuffmanTreeBuilder()));

                return config;
            }
        }

        public static IKompressionConfiguration NintendoRle
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new NintendoRleDecoder()).
                    EncodeWith((parser, builder) => new NintendoRleEncoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWith((limits, findOptions) => new RleMatchFinder(limits, findOptions))
                    .WithinLimitations(() => new FindLimitations(0x3, 0x82))
                    .CalculatePricesWith(() => new NintendoRlePriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Mio0Le
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Mio0Decoder(ByteOrder.LittleEndian)).
                    EncodeWith((parser, builder) => new Mio0Encoder(ByteOrder.LittleEndian, parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x12, 1, 0x1000))
                    .CalculatePricesWith(() => new Mio0PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Mio0Be
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Mio0Decoder(ByteOrder.BigEndian)).
                    EncodeWith((parser, builder) => new Mio0Encoder(ByteOrder.BigEndian, parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x12, 1, 0x1000))
                    .CalculatePricesWith(() => new Mio0PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Yay0Le
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Yay0Decoder(ByteOrder.LittleEndian)).
                    EncodeWith((parser, builder) => new Yay0Encoder(ByteOrder.LittleEndian, parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x111, 1, 0x1000))
                    .CalculatePricesWith(() => new Yay0PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Yay0Be
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Yay0Decoder(ByteOrder.BigEndian)).
                    EncodeWith((parser, builder) => new Yay0Encoder(ByteOrder.BigEndian, parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x111, 1, 0x1000))
                    .CalculatePricesWith(() => new Yay0PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Yaz0Le
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Yaz0Decoder(ByteOrder.LittleEndian)).
                    EncodeWith((parser, builder) => new Yaz0Encoder(ByteOrder.LittleEndian, parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x111, 1, 0x1000))
                    .CalculatePricesWith(() => new Yaz0PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Yaz0Be
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Yaz0Decoder(ByteOrder.BigEndian)).
                    EncodeWith((parser, builder) => new Yaz0Encoder(ByteOrder.BigEndian, parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x111, 1, 0x1000))
                    .CalculatePricesWith(() => new Yaz0PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration TaikoLz80
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new TaikoLz80Decoder()).
                    EncodeWith((parser, builder) => new TaikoLz80Encoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(2, 5, 1, 0x10))
                    .AndWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x12, 1, 0x400))
                    .AndWithDefault()
                    .WithinLimitations(() => new FindLimitations(4, 0x83, 1, 0x8000))
                    .CalculatePricesWith(() => new TaikoLz80PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration TaikoLz81
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new TaikoLz81Decoder()).
                    EncodeWith((parser, builder) => new TaikoLz81Encoder(parser, builder));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(1, 0x102, 2, 0x8000))
                    .CalculatePricesWith(() => new TaikoLz81PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration Wp16
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new Wp16Decoder()).
                    EncodeWith((parser, builder) => new Wp16Encoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(4, 0x42, 2, 0xFFE))
                    .CalculatePricesWith(() => new Wp16PriceCalculator())
                    .WithUnitSize(UnitSize.Short));

                return config;
            }
        }

        public static IKompressionConfiguration TalesOf01
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new TalesOf01Decoder(0xFEE)).
                    EncodeWith((parser, builder) => new TalesOf01Encoder(parser));
                config.WithMatchOptions(options => options
                    .WithPreBufferSize(0xFEE)
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x12, 1, 0x1000))
                    .CalculatePricesWith(() => new TalesOf01PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration TalesOf03
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new TalesOf03Decoder(0xFEF)).
                    EncodeWith((parser, builder) => new TalesOf03Encoder(parser));
                config.WithMatchOptions(options => options
                    .WithPreBufferSize(0xFEF)
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x11, 1, 0x1000))
                    .AndWith((limits, findOptions) => new RleMatchFinder(limits, findOptions))
                    .WithinLimitations(() => new FindLimitations(0x4, 0x112))
                    .CalculatePricesWith(() => new TalesOf03PriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration LzEnc
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new LzEncDecoder()).
                    EncodeWith((parser, builder) => new LzEncEncoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, -1, 1, 0xBFFF))
                    .CalculatePricesWith(() => new LzEncPriceCalculator()));

                return config;
            }
        }

        public static IKompressionConfiguration SpikeChunsoft
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new SpikeChunsoftDecoder()).
                    EncodeWith((parser, builder) => new SpikeChunsoftEncoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(4, -1, 1, 0x1FFF))
                    .AndWith((limits, findOptions) => new RleMatchFinder(limits, findOptions))
                    .WithinLimitations(() => new FindLimitations(4, 0x1003))
                    .CalculatePricesWith(() => new SpikeChunsoftPriceCalculator()));

                return config;
            }
        }

        // TODO: Find better naming, seemingly used on PS2 in multiple games
        public static IKompressionConfiguration PsLz
        {
            get
            {
                var config = new KompressionConfiguration();

                config.DecodeWith(() => new PsLzDecoder()).
                    EncodeWith((parser, builder) => new PsLzEncoder(parser));
                config.WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(1, 0xFFFF, 1, 0xFFFF))
                    .AndWith((limits, findOptions) => new StaticValueRleMatchFinder(0, limits, findOptions))
                    .WithinLimitations(() => new FindLimitations(1, 0xFFFF))
                    .CalculatePricesWith(() => new PsLzPriceCalculator()));

                return config;
            }
        }
    }
}
