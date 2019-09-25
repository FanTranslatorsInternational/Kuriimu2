namespace Kompression.Specialized.SlimeMoriMori.Obfuscators
{
    class SlimeMode1Obfuscator : ISlimeObfuscator
    {
        public void Obfuscate(byte[] input)
        {
            var position = 0;
            var seed = 0;
            while (position < input.Length)
            {
                var byte2 = input[position];
                var byte1 = input[position + 1];

                var nibble4 = ((byte1 >> 4) - (byte2 & 0xF)) & 0xF;
                var nibble3 = ((byte1 & 0xF) - (byte1 >> 4)) & 0xF;
                var byte2New = (nibble4 << 4) | nibble3;

                var nibble2 = ((byte2 >> 4) - seed) & 0xF;
                var nibble1 = ((byte2 & 0xF) - (byte2 >> 4)) & 0xF;
                var byte1New = (nibble2 << 4) | nibble1;

                seed = byte1 & 0xF;

                input[position++] = (byte)byte2New;
                input[position++] = (byte)byte1New;
            }
        }
    }
}
