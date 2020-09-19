using System.Collections.Generic;

namespace Kanvas.Quantization.Models.Quantizer.Wu
{
    class WuColorCube
    {
        public IReadOnlyList<WuColorBox> Boxes { get; }

        public int ColorCount { get; }

        private WuColorCube(WuColorBox[] boxes, int colorCount)
        {
            Boxes = boxes;
            ColorCount = colorCount;
        }

        public static WuColorCube Create(Wu3DHistogram histogram, int colorCount)
        {
            var boxes = new WuColorBox[colorCount];
            double[] vv = new double[colorCount];

            for (int i = 0; i < colorCount; i++)
            {
                boxes[i] = new WuColorBox(histogram);
            }

            boxes[0].R0 = boxes[0].G0 = boxes[0].B0 = boxes[0].A0 = 0;
            boxes[0].R1 = boxes[0].G1 = boxes[0].B1 = histogram.IndexCount - 1;
            boxes[0].A1 = histogram.IndexAlphaCount - 1;

            int next = 0;

            for (int i = 1; i < colorCount; i++)
            {
                if (WuCommon.Cut(boxes[next], boxes[i]))
                {
                    vv[next] = boxes[next].Volume > 1 ? boxes[next].GetVariance() : 0.0;
                    vv[i] = boxes[i].Volume > 1 ? boxes[i].GetVariance() : 0.0;
                }
                else
                {
                    vv[next] = 0.0;
                    i--;
                }

                next = 0;

                double temp = vv[0];
                for (int k = 1; k <= i; k++)
                {
                    if (vv[k] > temp)
                    {
                        temp = vv[k];
                        next = k;
                    }
                }

                if (temp <= 0.0)
                {
                    colorCount = i + 1;
                    break;
                }
            }

            return new WuColorCube(boxes, colorCount);
        }
    }
}
