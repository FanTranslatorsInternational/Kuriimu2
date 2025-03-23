namespace plugin_level5.Common.Image
{
    internal class ImageWriterFactory
    {
        public IImageWriter Create(int version)
        {
            switch (version)
            {
                case 0:
                    return new Img00Writer();

                default:
                    throw new InvalidOperationException($"Unknown font version {version}.");
            }
        }
    }
}
