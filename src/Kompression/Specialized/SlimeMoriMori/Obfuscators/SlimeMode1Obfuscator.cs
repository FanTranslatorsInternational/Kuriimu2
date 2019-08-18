using System.IO;

namespace Kompression.Specialized.SlimeMoriMori.Obfuscators
{
    class SlimeMode1Obfuscator : ISlimeObfuscator
    {
        public void Obfuscate(Stream input)
        {
            var seed = 0;
            while (input.Position < input.Length)
            {
                var byte2 = input.ReadByte();
                var byte1 = input.ReadByte();

                var nibble4 = ((byte1 >> 4) - (byte2 & 0xF)) & 0xF;
                var nibble3 = ((byte1 & 0xF) - (byte1 >> 4)) & 0xF;
                var byte2New = (nibble4 << 4) | nibble3;

                var nibble2 = ((byte2 >> 4) - seed) & 0xF;
                var nibble1 = ((byte2 & 0xF) - (byte2 >> 4)) & 0xF;
                var byte1New = (nibble2 << 4) | nibble1;

                seed = byte1 & 0xF;

                input.Position -= 2;
                input.WriteByte((byte)byte2New);
                input.WriteByte((byte)byte1New);
            }
        }
    }
}
