using System;
using System.Collections.Generic;

namespace Kompression.PatternMatch.RunLength
{
    public class RleMatchFinder : IMatchFinder
    {
        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public int MinDisplacement { get; } = 0;
        public int MaxDisplacement { get; } = 0;
        public DataType DataType { get; }
        public bool UseLookAhead { get; }

        public RleMatchFinder(int minMatchSize, int maxMatchSize)
        {
            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            DataType = DataType.Byte;
            UseLookAhead = true;
        }

        public IEnumerable<Match> FindMatches(byte[] input, int position)
        {
            if (input.Length - position < MinMatchSize)
                yield break;

            var repetitions = 1;
            for (var i = 1; i < Math.Min(MaxMatchSize, input.Length - position); i++)
            {
                if (input[position] == input[position + i])
                {
                    repetitions++;
                }
                else
                {
                    break;
                }
            }

            yield return new Match(position, 0, repetitions);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
