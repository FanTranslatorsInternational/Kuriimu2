namespace Kontract.Models.IO
{
    /// <summary>
    /// The byte order in which values should be read.
    /// </summary>
    public enum ByteOrder : ushort
    {
        LittleEndian = 0xFEFF,
        BigEndian = 0xFFFE
    }
}
