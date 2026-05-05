using System.Text;

namespace Komponent.DataClasses
{
    public class LengthInfo(int length, Encoding encoding)
    {
        public int Length { get; } = length;
        public Encoding Encoding { get; } = encoding;
    }
}
