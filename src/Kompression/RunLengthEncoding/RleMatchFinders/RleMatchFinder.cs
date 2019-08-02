using System.Collections.Generic;

namespace Kompression.RunLengthEncoding.RleMatchFinders
{
    public class RleMatchFinder : IRleMatchFinder
    {
        private readonly int _minMatchSize;

        public RleMatchFinder(int minMatchSize)
        {
            _minMatchSize = minMatchSize;
        }

        public IList<RleMatch> FindAllMatches(byte[] input)
        {
            var results = new List<RleMatch>();

            var value = input[0];
            var repetition = 1;
            for (int i = 1; i < input.Length; i++)
            {
                if (input[i] != value)
                {
                    if (repetition >= _minMatchSize)
                        results.Add(new RleMatch(value, i - repetition, repetition));

                    value = input[i];
                    repetition = 1;
                    continue;
                }

                repetition++;
            }

            if (repetition >= _minMatchSize)
                results.Add(new RleMatch(value, input.Length - repetition, repetition));

            return results;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
