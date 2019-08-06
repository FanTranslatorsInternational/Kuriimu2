using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.LempelZiv.Encoders
{
    public class RevLz77Encoder : ILzEncoder
    {
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
            throw new NotImplementedException();
        }
    }
}
