namespace Kuriimu2.WinForms.Extensions
{
    static class StringExtensions
    {
        public static byte[] Hexlify(this string hex, int length = -1)
        {
            var numberChars = hex.Length;
            var bytes = new byte[(length < 0) ? numberChars / 2 : (length + 1) & ~1];
            for (var i = 0; i < ((length < 0) ? numberChars : length * 2); i += 2)
                bytes[i / 2] = System.Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
