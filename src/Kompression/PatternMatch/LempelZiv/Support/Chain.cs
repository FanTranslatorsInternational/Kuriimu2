using System;
using System.Text;

namespace Kompression.PatternMatch.LempelZiv.Support
{
    class Chain : IComparable<Chain>
    {
        private readonly byte[] _data;

        public int Head { get; set; }
        public int Length { get; }

        public Chain(byte[] data, int head, int length)
        {
            _data = data;
            Head = head;
            Length = length;
        }

        public int CompareTo(Chain other)
        {
            return Encoding.ASCII.GetString(_data, Head, Length).
                CompareTo(Encoding.ASCII.GetString(_data, other.Head, other.Length));
        }
    }
}
