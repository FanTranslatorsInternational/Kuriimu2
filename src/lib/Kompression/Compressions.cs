using K4os.Compression.LZ4.Encoders;
using Komponent.Contract.Enums;
using Kompression.Configuration;
using Kompression.Contract.Configuration;
using Kompression.Contract.Enums.Encoder.Huffman;
using Kompression.Decoder;
using Kompression.Decoder.Headerless;
using Kompression.Decoder.Level5;
using Kompression.Decoder.Nintendo;
using Kompression.Encoder;
using Kompression.Encoder.Headerless;
using Kompression.Encoder.Level5;
using Kompression.Encoder.Nintendo;

namespace Kompression
{
    public static class Compressions
    {
        public static class Nintendo
        {
            public static ICompressionConfigurationBuilder Lz10 =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Decoder.Nintendo.Lz10Decoder())
                    .Encode.With(() => new Encoder.Nintendo.Lz10Encoder());

            public static ICompressionConfigurationBuilder Lz11 =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Lz11Decoder())
                    .Encode.With(() => new Lz11Encoder());

            public static ICompressionConfigurationBuilder Lz40 =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Lz40Decoder())
                    .Encode.With(() => new Lz40Encoder());

            public static ICompressionConfigurationBuilder Lz60 =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Lz60Decoder())
                    .Encode.With(() => new Lz60Encoder());

            public static ICompressionConfigurationBuilder BackwardLz77 =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new BackwardLz77Decoder(ByteOrder.LittleEndian))
                    .Encode.With(() => new BackwardLz77Encoder(ByteOrder.LittleEndian));

            public static ICompressionConfigurationBuilder Huffman4Bit =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Decoder.Nintendo.HuffmanDecoder(4, NibbleOrder.HighNibbleFirst))
                    .Encode.With(() => new Encoder.Nintendo.HuffmanEncoder(4, NibbleOrder.HighNibbleFirst));

            public static ICompressionConfigurationBuilder Huffman8Bit =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Decoder.Nintendo.HuffmanDecoder(8, NibbleOrder.HighNibbleFirst))
                    .Encode.With(() => new Encoder.Nintendo.HuffmanEncoder(8, NibbleOrder.HighNibbleFirst));

            public static ICompressionConfigurationBuilder Rle =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Decoder.Nintendo.RleDecoder())
                    .Encode.With(() => new Encoder.Nintendo.RleEncoder());

            public static ICompressionConfigurationBuilder Mio0Le =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Mio0Decoder(ByteOrder.LittleEndian))
                    .Encode.With(() => new Mio0Encoder(ByteOrder.LittleEndian));

            public static ICompressionConfigurationBuilder Mio0Be =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Mio0Decoder(ByteOrder.BigEndian))
                    .Encode.With(() => new Mio0Encoder(ByteOrder.BigEndian));

            public static ICompressionConfigurationBuilder Yay0Le =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Yay0Decoder(ByteOrder.LittleEndian))
                    .Encode.With(() => new Yay0Encoder(ByteOrder.LittleEndian));

            public static ICompressionConfigurationBuilder Yay0Be =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Yay0Decoder(ByteOrder.BigEndian))
                    .Encode.With(() => new Yay0Encoder(ByteOrder.BigEndian));

            public static ICompressionConfigurationBuilder Yaz0Le =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Yaz0Decoder(ByteOrder.LittleEndian))
                    .Encode.With(() => new Yaz0Encoder(ByteOrder.LittleEndian));

            public static ICompressionConfigurationBuilder Yaz0Be =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Yaz0Decoder(ByteOrder.BigEndian))
                    .Encode.With(() => new Yaz0Encoder(ByteOrder.BigEndian));
        }

        public static class Level5
        {
            public static ICompressionConfigurationBuilder Lz10 =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Decoder.Level5.Lz10Decoder())
                    .Encode.With(() => new Encoder.Level5.Lz10Encoder());

            public static ICompressionConfigurationBuilder Huffman4Bit =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Decoder.Level5.HuffmanDecoder(4, NibbleOrder.LowNibbleFirst))
                    .Encode.With(() => new Encoder.Level5.HuffmanEncoder(4, NibbleOrder.LowNibbleFirst));

            public static ICompressionConfigurationBuilder Huffman8Bit =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Decoder.Level5.HuffmanDecoder(8, NibbleOrder.LowNibbleFirst))
                    .Encode.With(() => new Encoder.Level5.HuffmanEncoder(8, NibbleOrder.LowNibbleFirst));

            public static ICompressionConfigurationBuilder Rle =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new Decoder.Level5.RleDecoder())
                    .Encode.With(() => new Encoder.Level5.RleEncoder());

            public static ICompressionConfigurationBuilder Inazuma3Lzss =>
                new CompressionConfigurationBuilder()
                    .Decode.With(() => new InazumaLzssDecoder())
                    .Encode.With(() => new InazumaLzssEncoder());
        }

        public static ICompressionConfigurationBuilder Lz77 =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new Lz77Decoder())
                .Encode.With(() => new Lz77Encoder());

        public static ICompressionConfigurationBuilder LzEcd =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new LzEcdDecoder())
                .Encode.With(() => new LzEcdEncoder());

        public static ICompressionConfigurationBuilder Lze =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new LzeDecoder())
                .Encode.With(() => new LzeEncoder());

        /* Is more LZSS, described by wikipedia, through the flag denoting if following data is compressed or raw.
           Though the format is denoted as LZ77 with the magic num? (Issue 517) */
        public static ICompressionConfigurationBuilder Lzss =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new LzssDecoder())
                .Encode.With(() => new LzssEncoder());

        public static ICompressionConfigurationBuilder LzssVlc =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new LzssVlcDecoder())
                .Encode.With(() => new LzssVlcEncoder());

        public static ICompressionConfigurationBuilder TaikoLz80 =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new TaikoLz80Decoder())
                .Encode.With(() => new TaikoLz80Encoder());

        public static ICompressionConfigurationBuilder TaikoLz81 =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new TaikoLz81Decoder())
                .Encode.With(() => new TaikoLz81Encoder());

        public static ICompressionConfigurationBuilder Wp16 =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new Wp16Decoder())
                .Encode.With(() => new Wp16Encoder());

        public static ICompressionConfigurationBuilder TalesOf01 =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new TalesOf01Decoder())
                .Encode.With(() => new TalesOf01Encoder());

        public static ICompressionConfigurationBuilder TalesOf03 =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new TalesOf03Decoder())
                .Encode.With(() => new TalesOf03Encoder());

        public static ICompressionConfigurationBuilder LzEnc =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new LzEncDecoder())
                .Encode.With(() => new LzEncEncoder());

        public static ICompressionConfigurationBuilder ShadeLz =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new ShadeLzDecoder())
                .Encode.With(() => new ShadeLzEncoder());

        public static ICompressionConfigurationBuilder ShadeLzHeaderless =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new ShadeLzHeaderlessDecoder())
                .Encode.With(() => new ShadeLzHeaderlessEncoder());

        // TODO: Find better naming, seemingly used on PS2 in multiple games
        public static ICompressionConfigurationBuilder PsLz =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new PsLzDecoder())
                .Encode.With(() => new PsLzEncoder());

        public static ICompressionConfigurationBuilder Deflate =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new DeflateDecoder())
                .Encode.With(() => new DeflateEncoder());

        public static ICompressionConfigurationBuilder ZLib =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new ZLibDecoder())
                .Encode.With(() => new ZlibEncoder());

        public static ICompressionConfigurationBuilder IrLz =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new IrLzHeaderlessDecoder())
                .Encode.With(() => new IrLzHeaderlessEncoder());

        public static ICompressionConfigurationBuilder Crilayla =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new CrilaylaDecoder())
                .Encode.With(() => new CrilaylaEncoder());

        public static ICompressionConfigurationBuilder Iecp =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new IecpDecoder())
                .Encode.With(() => new IecpEncoder());

        public static ICompressionConfigurationBuilder Lz4Headerless =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new Lz4HeaderlessDecoder())
                .Encode.With(() => new Lz4HeaderlessEncoder());

        public static ICompressionConfigurationBuilder Danganronpa3 =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new Dr3Decoder())
                .Encode.With(() => new Dr3Encoder());

        public static ICompressionConfigurationBuilder StingLz =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new StingLzDecoder())
                .Encode.With(() => new StingLzEncoder());

        /* Story of Seasons Switch/PC */
        public static ICompressionConfigurationBuilder SosLz3 =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new SosLz3Decoder())
                .Encode.With(() => new SosLz3Encoder());

        public static ICompressionConfigurationBuilder Lzma =>
            new CompressionConfigurationBuilder()
                .Decode.With(() => new LzmaDecoder())
                .Encode.With(() => new LzmaEncoder());
    }
}
