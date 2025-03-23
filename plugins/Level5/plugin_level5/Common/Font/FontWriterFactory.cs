namespace plugin_level5.Common.Font
{
    internal class FontWriterFactory
    {
        public IFontWriter Create(int version)
        {
            switch (version)
            {
                case 0:
                    return new Fnt00Writer();

                case 1:
                    return new Fnt01Writer();

                default:
                    throw new InvalidOperationException($"Unknown font version {version}.");
            }
        }
    }
}
