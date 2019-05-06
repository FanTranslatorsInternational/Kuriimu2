using System;
using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Models.Image
{
    public class TranscodeResult
    {
        public bool Result { get; }
        public Bitmap TranscodedImage { get; }
        public IList<Color> Palette { get; set; }
        public Exception Exception { get; }

        private TranscodeResult(bool result)
        {
            Result = result;
        }

        public TranscodeResult(bool result, Exception exc) : this(result)
        {
            Exception = exc;
        }

        public TranscodeResult(bool result, Bitmap image) : this(result)
        {
            TranscodedImage = image;
        }
    }
}
