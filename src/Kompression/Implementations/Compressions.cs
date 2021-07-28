using Kompression.Configuration;
using Kompression.Implementations.Decoders;
using Kompression.Implementations.Decoders.Headerless;
using Kompression.Implementations.Decoders.Nintendo;
using Kompression.Implementations.Encoders;
using Kompression.Implementations.Encoders.Headerless;
using Kompression.Implementations.Encoders.Nintendo;
using Kompression.PatternMatch.MatchParser;
using Kontract.Kompression.Configuration;
using Kontract.Models.IO;

namespace Kompression.Implementations
{
    public static class Compressions
    {
        private static KompressionConfiguration NewKompressionConfiguration =>
            new KompressionConfiguration();

        public static class Nintendo
        {
            public static IKompressionConfiguration Lz10 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Lz10Decoder())
                    .EncodeWith(() => new Lz10Encoder());

            public static IKompressionConfiguration Lz11 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Lz11Decoder())
                    .EncodeWith(() => new Lz11Encoder());

            public static IKompressionConfiguration Lz40 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Lz40Decoder())
                    .EncodeWith(() => new Lz40Encoder());

            public static IKompressionConfiguration Lz60 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Lz60Decoder())
                    .EncodeWith(() => new Lz60Encoder());

            public static IKompressionConfiguration BackwardLz77 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new BackwardLz77Decoder(ByteOrder.LittleEndian))
                    .EncodeWith(() => new BackwardLz77Encoder(ByteOrder.LittleEndian));

            public static IKompressionConfiguration Huffman4Bit =>
                NewKompressionConfiguration
                    .DecodeWith(() => new HuffmanDecoder(4, NibbleOrder.HighNibbleFirst))
                    .EncodeWith(() => new HuffmanEncoder(4, NibbleOrder.HighNibbleFirst));

            public static IKompressionConfiguration Huffman8Bit =>
                NewKompressionConfiguration
                    .DecodeWith(() => new HuffmanDecoder(8, NibbleOrder.HighNibbleFirst))
                    .EncodeWith(() => new HuffmanEncoder(8, NibbleOrder.HighNibbleFirst));

            public static IKompressionConfiguration Rle =>
                NewKompressionConfiguration
                    .DecodeWith(() => new RleDecoder())
                    .EncodeWith(() => new RleEncoder());

            public static IKompressionConfiguration Mio0Le =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Mio0Decoder(ByteOrder.LittleEndian))
                    .EncodeWith(() => new Mio0Encoder(ByteOrder.LittleEndian));

            public static IKompressionConfiguration Mio0Be =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Mio0Decoder(ByteOrder.BigEndian))
                    .EncodeWith(() => new Mio0Encoder(ByteOrder.BigEndian));

            public static IKompressionConfiguration Yay0Le =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Yay0Decoder(ByteOrder.LittleEndian))
                    .EncodeWith(() => new Yay0Encoder(ByteOrder.LittleEndian));

            public static IKompressionConfiguration Yay0Be =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Yay0Decoder(ByteOrder.BigEndian))
                    .EncodeWith(() => new Yay0Encoder(ByteOrder.BigEndian));

            public static IKompressionConfiguration Yaz0Le =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Yaz0Decoder(ByteOrder.LittleEndian))
                    .EncodeWith(() => new Yaz0Encoder(ByteOrder.LittleEndian));

            public static IKompressionConfiguration Yaz0Be =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Yaz0Decoder(ByteOrder.BigEndian))
                    .EncodeWith(() => new Yaz0Encoder(ByteOrder.BigEndian));
        }

        public static class Level5
        {
            public static IKompressionConfiguration Lz10 =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Decoders.Level5.Lz10Decoder())
                    .EncodeWith(() => new Encoders.Level5.Lz10Encoder());

            public static IKompressionConfiguration Huffman4Bit =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Decoders.Level5.HuffmanDecoder(4, NibbleOrder.LowNibbleFirst))
                    .EncodeWith(() => new Encoders.Level5.HuffmanEncoder(4, NibbleOrder.LowNibbleFirst));

            public static IKompressionConfiguration Huffman8Bit =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Decoders.Level5.HuffmanDecoder(8, NibbleOrder.LowNibbleFirst))
                    .EncodeWith(() => new Encoders.Level5.HuffmanEncoder(8, NibbleOrder.LowNibbleFirst));

            public static IKompressionConfiguration Rle =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Decoders.Level5.RleDecoder())
                    .EncodeWith(() => new Encoders.Level5.RleEncoder());

            public static IKompressionConfiguration Inazuma3Lzss =>
                NewKompressionConfiguration
                    .DecodeWith(() => new Decoders.Level5.InazumaLzssDecoder())
                    .EncodeWith(() => new Encoders.Level5.InazumaLzssEncoder());
        }

        public static IKompressionConfiguration Lz77 =>
            NewKompressionConfiguration
                .DecodeWith(() => new Lz77Decoder())
                .EncodeWith(() => new Lz77Encoder());

        public static IKompressionConfiguration LzEcd =>
            NewKompressionConfiguration
                .DecodeWith(() => new LzEcdDecoder())
                .EncodeWith(() => new LzEcdEncoder());

        public static IKompressionConfiguration Lze =>
            NewKompressionConfiguration
                .DecodeWith(() => new LzeDecoder())
                .EncodeWith(() => new LzeEncoder());

        /* Is more LZSS, described by wikipedia, through the flag denoting if following data is compressed or raw.
           Though the format is denoted as LZ77 with the magic num? (Issue 517) */
        public static IKompressionConfiguration Lzss =>
            NewKompressionConfiguration
                .DecodeWith(() => new LzssDecoder())
                .EncodeWith(() => new LzssEncoder());

        public static IKompressionConfiguration LzssVlc =>
            NewKompressionConfiguration
                .DecodeWith(() => new LzssVlcDecoder())
                .EncodeWith(() => new LzssVlcEncoder());

        public static IKompressionConfiguration TaikoLz80 =>
            NewKompressionConfiguration
                .DecodeWith(() => new TaikoLz80Decoder())
                .EncodeWith(() => new TaikoLz80Encoder());

        public static IKompressionConfiguration TaikoLz81 =>
            NewKompressionConfiguration
                .DecodeWith(() => new TaikoLz81Decoder())
                .EncodeWith(() => new TaikoLz81Encoder());

        public static IKompressionConfiguration Wp16 =>
            NewKompressionConfiguration
                .DecodeWith(() => new Wp16Decoder())
                .EncodeWith(() => new Wp16Encoder());

        public static IKompressionConfiguration TalesOf01 =>
            NewKompressionConfiguration
                .DecodeWith(() => new TalesOf01Decoder())
                .EncodeWith(() => new TalesOf01Encoder());

        public static IKompressionConfiguration TalesOf03 =>
            NewKompressionConfiguration
                .DecodeWith(() => new TalesOf03Decoder())
                .EncodeWith(() => new TalesOf03Encoder());

        public static IKompressionConfiguration LzEnc =>
            NewKompressionConfiguration
                .DecodeWith(() => new LzEncDecoder())
                .EncodeWith(() => new LzEncEncoder());

        public static IKompressionConfiguration ShadeLz =>
            NewKompressionConfiguration
                .DecodeWith(() => new ShadeLzDecoder())
                .EncodeWith(() => new ShadeLzEncoder());

        public static IKompressionConfiguration ShadeLzHeaderless =>
            NewKompressionConfiguration
                .DecodeWith(() => new ShadeLzHeaderlessDecoder())
                .EncodeWith(() => new ShadeLzHeaderlessEncoder());

        // TODO: Find better naming, seemingly used on PS2 in multiple games
        public static IKompressionConfiguration PsLz =>
            NewKompressionConfiguration
                .DecodeWith(() => new PsLzDecoder())
                .EncodeWith(() => new PsLzEncoder());

        public static IKompressionConfiguration Deflate =>
            NewKompressionConfiguration
                .DecodeWith(() => new DeflateDecoder())
                .EncodeWith(() => new DeflateEncoder());

        public static IKompressionConfiguration ZLib =>
            NewKompressionConfiguration
                .DecodeWith(() => new ZLibDecoder())
                .EncodeWith(() => new ZlibEncoder());

        public static IKompressionConfiguration IrLz =>
            NewKompressionConfiguration
                .DecodeWith(() => new IrLzHeaderlessDecoder())
                .EncodeWith(() => new IrLzHeaderlessEncoder());

        public static IKompressionConfiguration Crilayla =>
            NewKompressionConfiguration
                .DecodeWith(() => new CrilaylaDecoder())
                .EncodeWith(() => new CrilaylaEncoder());

        public static IKompressionConfiguration Iecp =>
            NewKompressionConfiguration
                .DecodeWith(() => new IecpDecoder())
                .EncodeWith(() => new IecpEncoder());

        public static IKompressionConfiguration Lz4Headerless =>
            NewKompressionConfiguration
                .DecodeWith(() => new Lz4HeaderlessDecoder())
                .EncodeWith(() => new Lz4HeaderlessEncoder());

        public static IKompressionConfiguration Danganronpa3 =>
            NewKompressionConfiguration
                .DecodeWith(() => new Dr3Decoder())
                .EncodeWith(() => new Dr3Encoder());

        public static IKompressionConfiguration StingLz =>
            NewKompressionConfiguration
                .DecodeWith(() => new StingLzDecoder())
                .EncodeWith(() => new StingLzEncoder());
    }
}
