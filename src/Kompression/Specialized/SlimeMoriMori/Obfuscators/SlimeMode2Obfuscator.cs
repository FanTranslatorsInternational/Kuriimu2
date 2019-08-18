using System.IO;

namespace Kompression.Specialized.SlimeMoriMori.Obfuscators
{
    class SlimeMode2Obfuscator : ISlimeObfuscator
    {
        public void Obfuscate(Stream input)
        {
            var seed = 0;
            while (input.Position < input.Length)
            {
                var byte2 = input.ReadByte();
                var byte1 = input.ReadByte();

                var byte1New = byte1 - byte2;
                var byte2New = byte2 - seed;
                seed = byte1;

                input.Position -= 2;
                input.WriteByte((byte)byte2New);
                input.WriteByte((byte)byte1New);
            }
        }
    }
}
