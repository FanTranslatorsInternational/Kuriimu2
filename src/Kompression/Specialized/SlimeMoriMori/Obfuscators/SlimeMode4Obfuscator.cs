using System.IO;

namespace Kompression.Specialized.SlimeMoriMori.Obfuscators
{
    class SlimeMode4Obfuscator : ISlimeObfuscator
    {
        public void Obfuscate(Stream input)
        {
            var seed = 0;
            var seed2 = 0;
            while (input.Position < input.Length)
            {
                var byte2 = input.ReadByte();
                var byte1 = input.ReadByte();

                var byte1New = byte1 - seed;
                var byte2New = byte2 - seed2;
                seed = byte1;
                seed2 = byte2;

                input.Position -= 2;
                input.WriteByte((byte)byte2New);
                input.WriteByte((byte)byte1New);
            }
        }
    }
}
