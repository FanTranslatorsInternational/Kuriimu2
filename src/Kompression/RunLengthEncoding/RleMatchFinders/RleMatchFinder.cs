using System;
using System.Collections.Generic;
using Kompression.LempelZiv.MatchFinder;

namespace Kompression.RunLengthEncoding.RleMatchFinders
{
    public class RleMatchFinder : IAllMatchFinder, ILongestMatchFinder
    {
        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public DataType UnitLength { get; }

        public RleMatchFinder(int minMatchSize, int maxMatchSize)
        {
            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            UnitLength = DataType.Byte;
        }

        public IEnumerable<Match> FindAllMatches(byte[] input, int position, int limit = -1)
        {
            if (limit == 0 || input.Length - position < MinMatchSize)
                yield break;

            var found = 0;
            for (var i = 1; i < Math.Min(MaxMatchSize, input.Length - position); i++)
            {
                if (input[position] == input[position + i] && i >= MinMatchSize)
                {
                    found++;
                    yield return new Match(position, 0, i + 1);
                    if (limit > 0 && found == limit)
                        break;
                }
                else
                {
                    break;
                }
            }
        }

        public Match FindLongestMatch(byte[] input, int position)
        {
            if (input.Length - position < MinMatchSize)
                return null;

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

            return new Match(position, 0, repetitions);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
