using System;
using System.IO;
using Kompression.Specialized.SlimeMoriMori.Decoders;
using Kompression.Specialized.SlimeMoriMori.Deobfuscators;
using Kompression.Specialized.SlimeMoriMori.Huffman;

namespace Kompression.Specialized.SlimeMoriMori
{
    public class SlimeMoriMoriCompression : ICompression
    {
        public void Decompress(Stream input, Stream output)
        {
            var originalInputPosition = input.Position;
            if (input.ReadByte() == 0x70)
            {
                input.Position = 7;
                var identByte = input.ReadByte();

                input.Position = originalInputPosition;
                var huffmanReader = CreateHuffmanReader((identByte >> 3) & 0x3);
                var decoder = CreateDecoder(identByte & 0x7, huffmanReader);
                decoder.Decode(input, output);

                // TODO: Implement deobfuscation
                //var deobfuscator = CreateDeobfuscator((identByte >> 5) & 0x7);
                //if (deobfuscator != null)
                //{
                //    output.Position = 0;
                //    deobfuscator.Deobfuscate(output);
                //}
            }
        }

        public void Compress(Stream input, Stream output)
        {
            throw new NotImplementedException();
        }

        private ISlimeHuffmanReader CreateHuffmanReader(int huffmanMode)
        {
            switch (huffmanMode)
            {
                case 1:
                    return new SlimeHuffman4Reader();
                case 2:
                    return new SlimeHuffman8Reader();
                default:
                    return new DefaultSlimeHuffmanReader();
            }
        }

        private ISlimeDecoder CreateDecoder(int decompMode, ISlimeHuffmanReader huffmanReader)
        {
            switch (decompMode)
            {
                case 1:
                    return new SlimeMode1Decoder(huffmanReader);
                case 2:
                    return new SlimeMode2Decoder(huffmanReader);
                case 3:
                    return new SlimeMode3Decoder(huffmanReader);
                case 4:
                    return new SlimeMode4Decoder(huffmanReader);
                default:
                    return new SlimeMode5Decoder(huffmanReader);
            }
        }

        //private ISlimeDeobfuscator CreateDeobfuscator(int deobfuscateMode)
        //{
        //    switch (deobfuscateMode)
        //    {
        //        case 1:
        //            break;
        //    }

        //    return null;
        //}
    }
}
