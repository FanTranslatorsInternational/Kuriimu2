using Kompression.Configuration;
using Kompression.Huffman;
using Kompression.Implementations.Decoders;
using Kompression.Implementations.Decoders.Headerless;
using Kompression.Implementations.Decoders.Nintendo;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.Encoders.Headerless;
using Kompression.Implementations.Encoders.Nintendo;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch.MatchFinders;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model;
using Kontract.Models.IO;

namespace Kompression.Implementations
{
    public static class Compressions
    {
        private static IKompressionConfiguration NewKompressionConfiguration =>
            new KompressionConfiguration();

        public static class Nintendo
        {
            public static IKompressionConfiguration Lz10 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Lz10Decoder())
                    .EncodeWith((parser, builder) => new Lz10Encoder(parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(0x3, 0x12, 1, 0x1000))
                        .CalculatePricesWith(() => new Lz10PriceCalculator()));

            public static IKompressionConfiguration Lz11 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Lz11Decoder())
                    .EncodeWith((parser, builder) => new Lz11Encoder(parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(3, 0x10110, 1, 0x1000))
                        .CalculatePricesWith(() => new Lz11PriceCalculator()));

            public static IKompressionConfiguration Lz40 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Lz40Decoder())
                    .EncodeWith((parser, builder) => new Lz40Encoder(parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(0x3, 0x1010F, 1, 0xFFF))
                        .CalculatePricesWith(() => new Lz40PriceCalculator()));

            public static IKompressionConfiguration Lz60 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Lz60Decoder())
                    .EncodeWith((parser, builder) => new Lz60Encoder(parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(0x3, 0x1010F, 1, 0xFFF))
                        .CalculatePricesWith(() => new Lz60PriceCalculator()));

            public static IKompressionConfiguration BackwardLz77 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new BackwardLz77Decoder(ByteOrder.LittleEndian))
                    .EncodeWith((parser, builder) => new BackwardLz77Encoder(parser, ByteOrder.LittleEndian))
                    .WithMatchOptions(options => options
                        .FindInBackwardOrder()
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(3, 0x12, 3, 0x1002))
                        .CalculatePricesWith(() => new BackwardLz77PriceCalculator()));

            public static IKompressionConfiguration Huffman4Bit =>
                NewKompressionConfiguration
                    .DecodeWith(() => new HuffmanDecoder(4, NibbleOrder.HighNibbleFirst))
                    .EncodeWith((parser, builder) => new HuffmanEncoder(4, builder))
                    .WithHuffmanOptions(options => options
                        .BuildTreeWith(() => new HuffmanTreeBuilder()));

            public static IKompressionConfiguration Huffman8Bit =>
                NewKompressionConfiguration
                    .DecodeWith(() => new HuffmanDecoder(8, NibbleOrder.HighNibbleFirst))
                    .EncodeWith((parser, builder) => new HuffmanEncoder(8, builder))
                    .WithHuffmanOptions(options => options
                        .BuildTreeWith(() => new HuffmanTreeBuilder()));

            public static IKompressionConfiguration Rle =>
                NewKompressionConfiguration
                    .DecodeWith(() => new RleDecoder())
                    .EncodeWith((parser, builder) => new RleEncoder(parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWith((limits, findOptions) => new RleMatchFinder(limits, findOptions))
                        .WithinLimitations(() => new FindLimitations(0x3, 0x82))
                        .CalculatePricesWith(() => new NintendoRlePriceCalculator()));

            public static IKompressionConfiguration Mio0Le =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Mio0Decoder(ByteOrder.LittleEndian))
                    .EncodeWith((parser, builder) => new Mio0Encoder(ByteOrder.LittleEndian, parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(3, 0x12, 1, 0x1000))
                        .CalculatePricesWith(() => new Mio0PriceCalculator()));

            public static IKompressionConfiguration Mio0Be =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Mio0Decoder(ByteOrder.BigEndian))
                    .EncodeWith((parser, builder) => new Mio0Encoder(ByteOrder.BigEndian, parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(3, 0x12, 1, 0x1000))
                        .CalculatePricesWith(() => new Mio0PriceCalculator()));

            public static IKompressionConfiguration Yay0Le =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Yay0Decoder(ByteOrder.LittleEndian))
                    .EncodeWith((parser, builder) => new Yay0Encoder(ByteOrder.LittleEndian, parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(3, 0x111, 1, 0x1000))
                        .CalculatePricesWith(() => new Yay0PriceCalculator()));

            public static IKompressionConfiguration Yay0Be =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Yay0Decoder(ByteOrder.BigEndian))
                    .EncodeWith((parser, builder) => new Yay0Encoder(ByteOrder.BigEndian, parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(3, 0x111, 1, 0x1000))
                        .CalculatePricesWith(() => new Yay0PriceCalculator()));

            public static IKompressionConfiguration Yaz0Le =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Yaz0Decoder(ByteOrder.LittleEndian))
                    .EncodeWith((parser, builder) => new Yaz0Encoder(ByteOrder.LittleEndian, parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(3, 0x111, 1, 0x1000))
                        .CalculatePricesWith(() => new Yaz0PriceCalculator()));

            public static IKompressionConfiguration Yaz0Be =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Yaz0Decoder(ByteOrder.BigEndian))
                    .EncodeWith((parser, builder) => new Yaz0Encoder(ByteOrder.BigEndian, parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(3, 0x111, 1, 0x1000))
                        .CalculatePricesWith(() => new Yaz0PriceCalculator()));
        }

        public static class Level5
        {
            public static IKompressionConfiguration Lz10 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Decoders.Level5.Lz10Decoder())
                    .EncodeWith((parser, builder) => new Encoders.Level5.Lz10Encoder(parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(0x3, 0x12, 1, 0x1000))
                        .CalculatePricesWith(() => new Lz10PriceCalculator()));

            public static IKompressionConfiguration Huffman4Bit =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Decoders.Level5.HuffmanDecoder(4, NibbleOrder.LowNibbleFirst))
                    .EncodeWith((parser, builder) => new Encoders.Level5.HuffmanEncoder(4, builder, NibbleOrder.LowNibbleFirst))
                    .WithHuffmanOptions(options => options
                        .BuildTreeWith(() => new HuffmanTreeBuilder()));

            public static IKompressionConfiguration Huffman8Bit =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Decoders.Level5.HuffmanDecoder(8, NibbleOrder.LowNibbleFirst))
                    .EncodeWith((parser, builder) => new Encoders.Level5.HuffmanEncoder(8, builder, NibbleOrder.LowNibbleFirst))
                    .WithHuffmanOptions(options => options
                        .BuildTreeWith(() => new HuffmanTreeBuilder()));

            public static IKompressionConfiguration Rle =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Decoders.Level5.RleDecoder())
                    .EncodeWith((parser, builder) => new Encoders.Level5.RleEncoder(parser))
                    .WithMatchOptions(options => options
                        .FindMatchesWith((limits, findOptions) => new RleMatchFinder(limits, findOptions))
                        .WithinLimitations(() => new FindLimitations(0x3, 0x82))
                        .CalculatePricesWith(() => new NintendoRlePriceCalculator()));

            public static IKompressionConfiguration Inazuma3Lzss =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Decoders.Level5.InazumaLzssDecoder(0xFEE))
                    .EncodeWith((parser, builder) => new Encoders.Level5.InazumaLzssEncoder(parser))
                    .WithMatchOptions(options => options
                        .WithPreBufferSize(0xFEE)
                        .FindMatchesWithDefault()
                        .WithinLimitations(() => new FindLimitations(3, 0x12, 1, 0x1000))
                        .CalculatePricesWith(() => new Lzss01PriceCalculator()));
        }

        public static IKompressionConfiguration Lz77 =>
            NewKompressionConfiguration
                .DecodeWith(() => new Lz77Decoder())
                .EncodeWith((parser, builder) => new Lz77Encoder(parser))
                .WithMatchOptions(options => options
                    .SkipUnitsAfterMatch(1)
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x1, 0xFF, 1, 0xFF))
                    .CalculatePricesWith(() => new Lz77PriceCalculator()));

        public static IKompressionConfiguration LzEcd =>
            NewKompressionConfiguration
                .DecodeWith(() => new LzEcdDecoder(0x3BE))
                .EncodeWith((parser, builder) => new LzEcdEncoder(parser))
                .WithMatchOptions(options => options
                    .WithPreBufferSize(0x3BE)
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x42, 1, 0x400))
                    .CalculatePricesWith(() => new LzEcdPriceCalculator()));

        public static IKompressionConfiguration Lze =>
            NewKompressionConfiguration
                .DecodeWith(() => new LzeDecoder())
                .EncodeWith((parser, builder) => new LzeEncoder(parser))
                .WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x3, 0x12, 5, 0x1004))
                    .AndWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x2, 0x41, 1, 4))
                    .CalculatePricesWith(() => new LzePriceCalculator()));

        /* Is more LZSS, described by wikipedia, through the flag denoting if following data is compressed or raw.
           Though the format is denoted as LZ77 with the magic num? (Issue 517) */
        public static IKompressionConfiguration Lzss =>
            NewKompressionConfiguration
                .DecodeWith(() => new LzssDecoder())
                .EncodeWith((parser, builder) => new LzssEncoder(parser))
                .WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(0x3, 0x12, 1, 0x1000))
                    .CalculatePricesWith(() => new LzssPriceCalculator()));

        public static IKompressionConfiguration LzssVlc =>
            NewKompressionConfiguration
                .DecodeWith(() => new LzssVlcDecoder())
                .EncodeWith((parser, builder) => new LzssVlcEncoder(parser))
                .WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(1, -1))
                    .CalculatePricesWith(() => new LzssVlcPriceCalculator()));

        public static IKompressionConfiguration TaikoLz80 =>
            NewKompressionConfiguration
                .DecodeWith(() => new TaikoLz80Decoder())
                .EncodeWith((parser, builder) => new TaikoLz80Encoder(parser))
                .WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(2, 5, 1, 0x10))
                    .AndWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x12, 1, 0x400))
                    .AndWithDefault()
                    .WithinLimitations(() => new FindLimitations(4, 0x83, 1, 0x8000))
                    .CalculatePricesWith(() => new TaikoLz80PriceCalculator()));

        public static IKompressionConfiguration TaikoLz81 =>
            NewKompressionConfiguration
                .DecodeWith(() => new TaikoLz81Decoder())
                .EncodeWith((parser, builder) => new TaikoLz81Encoder(parser, builder))
                .WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(1, 0x102, 2, 0x8000))
                    .CalculatePricesWith(() => new TaikoLz81PriceCalculator()));

        public static IKompressionConfiguration Wp16 =>
            NewKompressionConfiguration
                .DecodeWith(() => new Wp16Decoder(0xFFE))
                .EncodeWith((parser, builder) => new Wp16Encoder(parser))
                .WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(4, 0x42, 2, 0xFFE))
                    .WithPreBufferSize(0xFFE)
                    .CalculatePricesWith(() => new Wp16PriceCalculator())
                    .WithUnitSize(UnitSize.Short));

        public static IKompressionConfiguration TalesOf01 =>
            NewKompressionConfiguration
                .DecodeWith(() => new TalesOf01Decoder(0xFEE))
                .EncodeWith((parser, builder) => new TalesOf01Encoder(parser))
                .WithMatchOptions(options => options
                    .WithPreBufferSize(0xFEE)
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x12, 1, 0x1000))
                    .CalculatePricesWith(() => new Lzss01PriceCalculator()));

        public static IKompressionConfiguration TalesOf03 =>
            NewKompressionConfiguration
                .DecodeWith(() => new TalesOf03Decoder(0xFEF))
                .EncodeWith((parser, builder) => new TalesOf03Encoder(parser))
                .WithMatchOptions(options => options
                    .WithPreBufferSize(0xFEF)
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, 0x11, 1, 0x1000))
                    .AndWith((limits, findOptions) => new RleMatchFinder(limits, findOptions))
                    .WithinLimitations(() => new FindLimitations(0x4, 0x112))
                    .CalculatePricesWith(() => new TalesOf03PriceCalculator()));

        public static IKompressionConfiguration LzEnc =>
            NewKompressionConfiguration
                .DecodeWith(() => new LzEncDecoder())
                .EncodeWith((parser, builder) => new LzEncEncoder(parser))
                .WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(3, -1, 1, 0xBFFF))
                    .CalculatePricesWith(() => new LzEncPriceCalculator()));

        public static IKompressionConfiguration SpikeChunsoft =>
            NewKompressionConfiguration
                .DecodeWith(() => new SpikeChunsoftDecoder())
                .EncodeWith((parser, builder) => new SpikeChunsoftEncoder(parser))
                .WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(4, -1, 1, 0x1FFF))
                    .AndWith((limits, findOptions) => new RleMatchFinder(limits, findOptions))
                    .WithinLimitations(() => new FindLimitations(4, 0x1003))
                    .CalculatePricesWith(() => new SpikeChunsoftPriceCalculator()));

        public static IKompressionConfiguration SpikeChunsoftHeaderless =>
            NewKompressionConfiguration
                .DecodeWith(() => new SpikeChunsoftHeaderlessDecoder())
                .EncodeWith((parser, builder) => new SpikeChunsoftHeaderlessEncoder(parser))
                .WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(4, -1, 1, 0x1FFF))
                    .AndWith((limits, findOptions) => new RleMatchFinder(limits, findOptions))
                    .WithinLimitations(() => new FindLimitations(4, 0x1003))
                    .CalculatePricesWith(() => new SpikeChunsoftPriceCalculator()));

        // TODO: Find better naming, seemingly used on PS2 in multiple games
        public static IKompressionConfiguration PsLz =>
            NewKompressionConfiguration
                .DecodeWith(() => new PsLzDecoder())
                .EncodeWith((parser, builder) => new PsLzEncoder(parser))
                .WithMatchOptions(options => options
                    .FindMatchesWithDefault()
                    .WithinLimitations(() => new FindLimitations(1, 0xFFFF, 1, 0xFFFF))
                    .AndWith((limits, findOptions) => new StaticValueRleMatchFinder(0, limits, findOptions))
                    .WithinLimitations(() => new FindLimitations(1, 0xFFFF))
                    .AndWith((limits, findOptions) => new RleMatchFinder(limits, findOptions))
                    .WithinLimitations(() => new FindLimitations(1, 0xFFFF))
                    .CalculatePricesWith(() => new PsLzPriceCalculator()));

        public static IKompressionConfiguration ZLib =>
            NewKompressionConfiguration
                .DecodeWith(() => new ZLibDecoder())
                .EncodeWith((parser, builder) => new ZlibEncoder());
    }
}
