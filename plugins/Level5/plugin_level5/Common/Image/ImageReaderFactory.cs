namespace plugin_level5.Common.Image
{
    internal class ImageReaderFactory
    {
        public IImageReader Create(int version)
        {
            switch (version)
            {
                case 0:
                    return new Img00Reader();

                default:
                    throw new InvalidOperationException($"Unknown image version {version}.");
            }
        }
    }
}
