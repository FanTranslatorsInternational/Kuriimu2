using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Kanvas.Interface;

namespace Kanvas.Support
{
    public class Helper
    {
        public static int ChangeBitDepth(int value, int bitDepthFrom, int bitDepthTo)
        {
            if (bitDepthTo < 0 || bitDepthFrom < 0)
                throw new Exception("BitDepths can't be negative!");
            if (bitDepthFrom == 0 || bitDepthTo == 0)
                return 0;
            if (bitDepthFrom == bitDepthTo)
                return value;

            if (bitDepthFrom < bitDepthTo)
            {
                var fromMaxRange = (1 << bitDepthFrom) - 1;
                var toMaxRange = (1 << bitDepthTo) - 1;

                var div = 1;
                while (toMaxRange % fromMaxRange != 0)
                {
                    div <<= 1;
                    toMaxRange = ((toMaxRange + 1) << 1) - 1;
                }

                return value * (toMaxRange / fromMaxRange) / div;
            }
            else
            {
                var fromMax = 1 << bitDepthFrom;
                var toMax = 1 << bitDepthTo;

                var limit = fromMax / toMax;

                return value / limit;
            }
        }
    }
}
