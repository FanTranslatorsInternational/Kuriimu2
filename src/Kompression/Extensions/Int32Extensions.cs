namespace Kompression.Extensions
{
    static class Int32Extensions
    {
        public static byte[] GetArrayLittleEndian(this int value)
        {
            return new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
        }

        public static byte[] GetArrayBigEndian(this int value)
        {
            return new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
        }
    }
}
