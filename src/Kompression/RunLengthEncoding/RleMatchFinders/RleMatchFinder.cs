using System;
using System.Collections.Generic;
using Kompression.LempelZiv.MatchFinder;

namespace Kompression.RunLengthEncoding.RleMatchFinders
{
    public class RleMatchFinder : IAllMatchFinder, ILongestMatchFinder
    {
        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public int UnitLength { get; }

        public RleMatchFinder(int minMatchSize, int maxMatchSize)
        {
            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            UnitLength = 1;
        }

        public IEnumerable<IMatch> FindAllMatches(byte[] input, int position, int limit = -1)
        {
            if (limit == 0 || input.Length - position < MinMatchSize)
                yield break;

            var found = 0;
            for (var i = 0; i < Math.Min(MaxMatchSize, input.Length - position); i++)
            {
                if (input[position] == input[position + i] && i >= MinMatchSize)
                {
                    found++;
                    yield return new RleMatch(input[position], position, i);
                    if (limit > 0 && found == limit)
                        break;
                }
                else
                {
                    break;
                }
            }

            //var value = input[0];
            //var repetition = 1;
            //var found = 0;
            //for (var i = 1; i < Math.Min(MaxMatchSize, input.Length - position); i++)
            //{
            //    if (input[i] != value)
            //    {
            //        if (repetition >= MinMatchSize)
            //        {
            //            found++;
            //            yield return new RleMatch(value, i - repetition, repetition);
            //            if (found == limit)
            //                yield break;
            //        }

            //        value = input[i];
            //        repetition = 1;
            //        continue;
            //    }

            //    repetition++;
            //}

            //if (repetition >= MinMatchSize)
            //    yield return new RleMatch(value, input.Length - repetition, repetition);
        }

        public IMatch FindLongestMatch(byte[] input, int position)
        {
            if (input.Length - position < MinMatchSize)
                return null;

            var repetitions = 0;
            for (var i = 0; i < Math.Min(MaxMatchSize, input.Length - position); i++)
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

            return new RleMatch(input[position], position, repetitions);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
