namespace plugin_level5.Common.Font
{
    public class FontReaderFactory
    {
        public IFontReader Create(int version)
        {
            switch (version)
            {
                case 0:
                    return new Fnt00Reader();

                case 1:
                    return new Fnt01Reader();
                
                default:
                    throw new InvalidOperationException($"Unknown font version {version}.");
            }
        }
    }
}
