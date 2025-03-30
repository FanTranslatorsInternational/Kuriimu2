namespace Komponent.Contract.Enums
{
    public enum ByteOrder : ushort
    {
        BigEndian = 0xFEFF,
        LittleEndian = 0xFFFE,
    }
}
