using System;
using System.Collections.Generic;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.Parser
{
    /// <summary>
    /// Naive greedy matching algorithm.
    /// </summary>
    public class NaiveParser : ILzParser
    {
        public int MinMatchingSize { get; }

        public int MaxMatchingSize { get; }

        public int WindowSize { get; }

        public NaiveParser(int minMatching, int maxMatching, int windowSize)
        {
            MinMatchingSize = minMatching;
            MaxMatchingSize = maxMatching;
            WindowSize = windowSize;
        }

        public unsafe LzMatch[] Parse(Span<byte> input)
        {
            var result = new List<LzMatch>();

            fixed (byte* ptr = input)
            {
                var position = ptr;
                position += MinMatchingSize;

                while (position - ptr < input.Length)
                {
                    var displacementPtr = position - Math.Min(position - ptr, WindowSize);

                    var displacement = -1L;
                    var length = -1;
                    while (displacementPtr < position)
                    {
                        if (length >= MaxMatchingSize)
                            break;

                        #region Find max occurence from displacementPtr onwards

                        var walk = 0;
                        while (*(displacementPtr + walk) == *(position + walk))
                        {
                            walk++;
                            if (walk >= MaxMatchingSize || position - ptr + walk >= input.Length)
                                break;
                        }

                        if (walk >= MinMatchingSize && walk > length)
                        {
                            length = walk;
                            displacement = position - displacementPtr;
                        }

                        #endregion

                        displacementPtr++;
                    }

                    if (length >= MinMatchingSize)
                    {
                        result.Add(new LzMatch(position - ptr, displacement, length));
                        position += length;
                    }
                    else
                    {
                        position++;
                    }
                }
            }

            return result.ToArray();
        }

        public void Dispose()
        {
            // Nothing to dispose here
        }
    }
}
