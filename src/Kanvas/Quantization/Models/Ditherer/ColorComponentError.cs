using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Models.Ditherer
{
    class ColorComponentError
    {
        public int RedError { get; set; }
        public int GreenError { get; set; }
        public int BlueError { get; set; }

        public ColorComponentError()
        {
            RedError = 0;
            GreenError = 0;
            BlueError = 0;
        }

        public ColorComponentError(int redError, int greenError, int blueError)
        {
            RedError = redError;
            GreenError = greenError;
            BlueError = blueError;
        }
    }
}
