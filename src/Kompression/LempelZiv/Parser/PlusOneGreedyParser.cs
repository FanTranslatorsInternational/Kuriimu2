using System.Collections.Generic;
using System.Threading.Tasks;
using Kompression.LempelZiv.MatchFinder;

namespace Kompression.LempelZiv.Parser
{
    /// <summary>
    /// Searches for longest pattern matches at position n and n+1 in one loop execution.
    /// </summary>
    public class PlusOneGreedyParser : ILzParser
    {
        private readonly ILongestMatchFinder _finder;

        /// <summary>
        /// Creates a new instance of <see cref="PlusOneGreedyParser"/>.
        /// </summary>
        /// <param name="finder">The <see cref="ILongestMatchFinder"/> to find a longest pattern match at a given position.</param>
        public PlusOneGreedyParser(ILongestMatchFinder finder)
        {
            _finder = finder;
        }

        /// <inheritdoc cref="Parse"/>
        public LzMatch[] Parse(byte[] input, int startPosition)
        {
            var results = new List<LzMatch>();

            var positionOffset = 0;
            while (startPosition + positionOffset < input.Length)
            {
                // Find longest match at position i and position i+1
                var offset = positionOffset;
                var positionTask = Task.Factory.StartNew(() => _finder.FindLongestMatch(input, startPosition + offset));
                var positionPlusOneTask = Task.Factory.StartNew(() => _finder.FindLongestMatch(input, startPosition + offset + 1));
                Task.WaitAll(positionTask, positionPlusOneTask);

                // If legit match was found
                if (positionTask.Result != null)
                {
                    var match = positionTask.Result;

                    // If legit 2nd match was found
                    if (positionPlusOneTask.Result != null)
                    {
                        var matchPlusOne = positionPlusOneTask.Result;

                        // If 2nd match is longer than 1st
                        if (match.Length + 1 < matchPlusOne.Length)
                        {
                            results.Add(matchPlusOne);
                            positionOffset += matchPlusOne.Length;
                            continue;
                        }
                    }

                    // If 2nd match was not legit or not better
                    results.Add(match);
                    positionOffset += match.Length;
                    continue;
                }

                // If no legit matches were found
                positionOffset++;
            }

            return results.ToArray();
        }

        public void Dispose()
        {
            _finder.Dispose();
        }
    }
}
