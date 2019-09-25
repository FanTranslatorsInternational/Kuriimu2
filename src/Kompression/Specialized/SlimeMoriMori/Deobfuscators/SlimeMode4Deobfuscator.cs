using System.IO;

namespace Kompression.Specialized.SlimeMoriMori.Deobfuscators
{
    class SlimeMode4Deobfuscator:ISlimeDeobfuscator
    {
        public void Deobfuscate(Stream input)
        {
            var seed = 0;
            var seed2 = 0;
            while (input.Position < input.Length)
            {
                var byte2 = input.ReadByte();
                var byte1 = input.ReadByte();

                var byte2New = seed = (byte2 + seed) & 0xFF;
                var byte1New = seed2 = (byte1 + seed2) & 0xFF;

                input.Position -= 2;
                input.WriteByte((byte)byte2New);
                input.WriteByte((byte)byte1New);
            }
        }
    }
}
