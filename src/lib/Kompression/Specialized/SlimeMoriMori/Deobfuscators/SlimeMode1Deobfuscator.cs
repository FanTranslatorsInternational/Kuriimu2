using System.IO;

namespace Kompression.Specialized.SlimeMoriMori.Deobfuscators
{
    class SlimeMode1Deobfuscator:ISlimeDeobfuscator
    {
        public void Deobfuscate(Stream input)
        {
            var seed = 0;
            while (input.Position < input.Length)
            {
                var byte2 = input.ReadByte();
                var byte1 = input.ReadByte();

                var nibble2 = (seed + (byte2 >> 4)) & 0xF;
                var nibble1 = (nibble2 + (byte2 & 0xF)) & 0xF;
                var byte2New = (nibble2 << 4) | nibble1;

                var nibble4 = (nibble1 + (byte1 >> 4)) & 0xF;
                var nibble3 = seed = (nibble4 + (byte1 & 0xF)) & 0xF;
                var byte1New = (nibble4 << 4) | nibble3;

                input.Position -= 2;
                input.WriteByte((byte)byte2New);
                input.WriteByte((byte)byte1New);
            }
        }
    }
}
