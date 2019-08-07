using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.LempelZiv.Encoders
{
    public class BackwardLz77Encoder : ILzEncoder
    {
        private readonly ByteOrder _byteOrder;

        public BackwardLz77Encoder(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public void Encode(Stream input, Stream output, LzMatch[] matches)
        {
            // Matches are backward relative to the end of input
            // That means matches[0] relates to the first match found from the end of input
            foreach (var match in matches.Reverse())    // We reverse the matches to work from the beginning of the file as usual
            {

            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
