using Komponent.IO;

namespace plugin_criware.Archives.Support
{
    public class StringWriter
    {
        private readonly BinaryWriterX _bw;
        private readonly long _stringOffset;
        private readonly IDictionary<string, long> _cachedStrings;

        private long _stringPosition;

        public StringWriter(BinaryWriterX bw, long stringOffset)
        {
            _bw = bw;
            _stringOffset = stringOffset;
            _cachedStrings = new Dictionary<string, long>();
        }

        public long WriteString(string value)
        {
            if (_cachedStrings.ContainsKey(value))
                return _cachedStrings[value];

            _cachedStrings[value] = _stringPosition;

            var bkPos = _bw.BaseStream.Position;
            CpkSupport.WriteString(_bw, _stringOffset + _stringPosition, value);
            _stringPosition = _bw.BaseStream.Position - _stringOffset;
            _bw.BaseStream.Position = bkPos;

            return _cachedStrings[value];
        }
    }
}
