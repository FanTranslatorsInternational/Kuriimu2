namespace Komponent.Extensions
{
    static class ByteArrayExtensions
    {
        public static string Stringify(this byte[] input, int length = -1)
        {
            var result = string.Empty;
            for (var i = 0; i < (length < 0 ? input.Length : length); i++)
                result += input[i].ToString("X2");
            return result;
        }
    }
}
