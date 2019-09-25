namespace Kompression.Specialized.SlimeMoriMori.Obfuscators
{
    class SlimeMode3Obfuscator : ISlimeObfuscator
    {
        public void Obfuscate(byte[] input)
        {
            var position = 0;
            var seed = 0;
            while (position < input.Length)
            {
                var short1 = (input[position] << 8) | input[position + 1];

                var short1New = short1 - seed;
                seed = short1;

                input[position++] = (byte)(short1New >> 8);
                input[position++] = (byte)short1New;
            }
        }
    }
}
