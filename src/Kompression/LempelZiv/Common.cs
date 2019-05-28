using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("KompressionUnitTests")]

namespace Kompression.LempelZiv
{
    internal static class Common
    {
        // TODO: Parallelize occurrence search
        // TODO: Use streams
        public static unsafe List<LzResult> FindOccurrences(byte[] input, int windowSize, int minOccurenceSize, int maxOccurenceSize, int postDiscrepancySize = 0)
        {
            var result = new List<LzResult>();

            fixed (byte* ptr = input)
            {
                var position = ptr;
                position += minOccurenceSize;

                while (position - ptr < input.Length)
                {
                    var displacementPtr = position - Math.Min(position - ptr, windowSize);

                    var displacement = -1L;
                    var length = -1;
                    byte[] discrepancy = null;
                    while (displacementPtr < position)
                    {
                        if (length >= maxOccurenceSize)
                            break;

                        #region Find max occurence from displacementPtr onwards

                        var walk = 0;
                        while (*(displacementPtr + walk) == *(position + walk))
                        {
                            walk++;
                            if (walk >= maxOccurenceSize || position - ptr + walk >= input.Length)
                                break;
                        }

                        if (walk >= minOccurenceSize && walk > length)
                        {
                            length = walk;
                            displacement = position - displacementPtr;
                            discrepancy = new byte[postDiscrepancySize];
                            for (int i = 0; i < discrepancy.Length; i++)
                                discrepancy[i] = *(position + walk + i);
                        }

                        #endregion

                        displacementPtr++;
                    }

                    if (length >= minOccurenceSize)
                    {
                        result.Add(new LzResult(position - ptr, displacement, length, discrepancy));
                        position += length + postDiscrepancySize;
                    }
                    else
                    {
                        position++;
                    }
                }
            }

            return result;
        }
    }
}
