namespace Kompression.Specialized.SlimeMoriMori.Obfuscators
{
    class SlimeMode2Obfuscator : ISlimeObfuscator
    {
        public void Obfuscate(byte[] input)
        {
            var position = 0;
            var seed = 0;
            while (position < input.Length)
            {
                var byte2 = input[position];
                var byte1 = input[position+1];

                var byte1New = byte1 - byte2;
                var byte2New = byte2 - seed;
                seed = byte1;

                input[position++] = (byte) byte2New;
                input[position++] = (byte)byte1New;
            }
        }
    }
}
