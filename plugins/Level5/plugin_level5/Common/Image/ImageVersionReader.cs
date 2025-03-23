namespace plugin_level5.Common.Image
{
    internal class ImageVersionReader
    {
        public int Read(Stream input)
        {
            if (input.Length < 6)
                throw new InvalidOperationException("Stream needs to be at least 6 bytes long.");

            input.Position = 4;

            var buffer = new byte[2];
            int _ = input.Read(buffer);

            return int.Parse($"{(char)buffer[0]}{(char)buffer[1]}");
        }

        public int Peek(Stream input)
        {
            var bkPos = input.Position;
            var type = Read(input);

            input.Position = bkPos;
            return type;
        }
    }
}
