using System.IO;

namespace Kompression.Specialized.SlimeMoriMori.Deobfuscators
{
    class SlimeMode3Deobfuscator:ISlimeDeobfuscator
    {
        public void Deobfuscate(Stream input)
        {
            var seed = 0;
            while (input.Position < input.Length)
            {
                var short1 = (input.ReadByte() << 8) | input.ReadByte();

                var short1New = short1 + seed;
                seed = short1;

                input.Position -= 2;
                input.WriteByte((byte)(short1New >> 8));
                input.WriteByte((byte)short1New);
            }
        }
    }
}
