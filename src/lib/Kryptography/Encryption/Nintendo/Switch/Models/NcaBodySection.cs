namespace Kryptography.Encryption.Nintendo.Switch.Models
{
    public class NcaBodySection
    {
        public NcaBodySection(long mediaOffset, long mediaLength, NcaSectionCrypto sectionCrypto, byte[] baseSectionCtr)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(mediaOffset, 6);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(mediaLength, 0);
            ArgumentNullException.ThrowIfNull(baseSectionCtr);

            if (baseSectionCtr.Length != 0x10)
                throw new InvalidOperationException("Base section ctr needs a length of 0x10 bytes.");

            MediaOffset = mediaOffset;
            MediaLength = mediaLength;
            SectionCrypto = sectionCrypto;
            BaseSectionCtr = new byte[0x10];
            Array.Copy(baseSectionCtr, BaseSectionCtr, 0x10);
        }

        /// <summary>
        /// The offset of the section in media units
        /// </summary>
        public long MediaOffset { get; }

        /// <summary>
        /// The length of the section in media units
        /// </summary>
        public long MediaLength { get; }

        /// <summary>
        /// The cipher context to use for the section
        /// </summary>
        public NcaSectionCrypto SectionCrypto { get; set; }

        /// <summary>
        /// The counter to calculate the actual Ctr from for this section
        /// </summary>
        public byte[] BaseSectionCtr { get; }
    }
}
