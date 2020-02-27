namespace Kontract.Models.Images
{
    /// <summary>
    /// Extended <see cref="ImageInfo"/> to add paletteData information.
    /// </summary>
    public class IndexImageInfo : ImageInfo
    {
        /// <inheritdoc />
        public override bool IsIndexed => true;

        /// <summary>
        /// The paletteData of the main image.
        /// </summary>
        public byte[] PaletteData { get; set; }

        /// <summary>
        /// The format in which the paletteData is encoded.
        /// </summary>
        public int PaletteFormat { get; set; }
    }
}
