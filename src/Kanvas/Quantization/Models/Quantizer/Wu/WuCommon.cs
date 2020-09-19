namespace Kanvas.Quantization.Models.Quantizer.Wu
{
    static class WuCommon
    {
        public static int GetIndex(int r, int g, int b, int a, int indexBits, int indexAlphaBits)
        {
            return (r << (indexBits * 2 + indexAlphaBits))
                   + (r << (indexBits + indexAlphaBits + 1))
                   + (g << (indexBits + indexAlphaBits))
                   + (r << (indexBits * 2))
                   + (r << (indexBits + 1))
                   + (g << indexBits)
                   + ((r + g + b) << indexAlphaBits)
                   + r + g + b + a;
        }

        public static bool Cut(WuColorBox set1, WuColorBox set2)
        {
            double wholeR = set1.GetPartialVolume(1);
            double wholeG = set1.GetPartialVolume(2);
            double wholeB = set1.GetPartialVolume(3);
            double wholeA = set1.GetPartialVolume(4);
            double wholeW = set1.GetPartialVolume(5);

            double maxr = set1.Maximize(3, set1.R0 + 1, set1.R1, out var cutr, wholeR, wholeG, wholeB, wholeA, wholeW);
            double maxg = set1.Maximize(2, set1.G0 + 1, set1.G1, out var cutg, wholeR, wholeG, wholeB, wholeA, wholeW);
            double maxb = set1.Maximize(1, set1.B0 + 1, set1.B1, out var cutb, wholeR, wholeG, wholeB, wholeA, wholeW);
            double maxa = set1.Maximize(0, set1.A0 + 1, set1.A1, out var cuta, wholeR, wholeG, wholeB, wholeA, wholeW);

            int dir;

            if ((maxr >= maxg) && (maxr >= maxb) && (maxr >= maxa))
            {
                dir = 3;

                if (cutr < 0)
                {
                    return false;
                }
            }
            else if ((maxg >= maxr) && (maxg >= maxb) && (maxg >= maxa))
            {
                dir = 2;
            }
            else if ((maxb >= maxr) && (maxb >= maxg) && (maxb >= maxa))
            {
                dir = 1;
            }
            else
            {
                dir = 0;
            }

            set2.R1 = set1.R1;
            set2.G1 = set1.G1;
            set2.B1 = set1.B1;
            set2.A1 = set1.A1;

            switch (dir)
            {
                // Red
                case 3:
                    set2.R0 = set1.R1 = cutr;
                    set2.G0 = set1.G0;
                    set2.B0 = set1.B0;
                    set2.A0 = set1.A0;
                    break;

                // Green
                case 2:
                    set2.G0 = set1.G1 = cutg;
                    set2.R0 = set1.R0;
                    set2.B0 = set1.B0;
                    set2.A0 = set1.A0;
                    break;

                // Blue
                case 1:
                    set2.B0 = set1.B1 = cutb;
                    set2.R0 = set1.R0;
                    set2.G0 = set1.G0;
                    set2.A0 = set1.A0;
                    break;

                // Alpha
                case 0:
                    set2.A0 = set1.A1 = cuta;
                    set2.R0 = set1.R0;
                    set2.G0 = set1.G0;
                    set2.B0 = set1.B0;
                    break;
            }

            set1.Volume = (set1.R1 - set1.R0) * (set1.G1 - set1.G0) * (set1.B1 - set1.B0) * (set1.A1 - set1.A0);
            set2.Volume = (set2.R1 - set2.R0) * (set2.G1 - set2.G0) * (set2.B1 - set2.B0) * (set2.A1 - set2.A0);

            return true;
        }
    }
}
