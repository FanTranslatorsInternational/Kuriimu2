using System.IO;

namespace Kompression.Specialized.SlimeMoriMori.Deobfuscators
{
    class SlimeMode2Deobfuscator:ISlimeDeobfuscator
    {
        public void Deobfuscate(Stream input)
        {
            var seed = 0;
            while (input.Position < input.Length)
            {
                var byte2 = input.ReadByte();
                var byte1 = input.ReadByte();

                var byte2New = byte2 + seed;
                var byte1New = seed = byte1 + byte2New;

                input.Position -= 2;
                input.WriteByte((byte)byte2New);
                input.WriteByte((byte)byte1New);
            }
        }
    }
}
