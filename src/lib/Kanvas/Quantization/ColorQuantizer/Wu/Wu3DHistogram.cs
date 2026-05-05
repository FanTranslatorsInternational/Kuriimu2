using Kanvas.Contract.DataClasses;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorQuantizer.Wu
{
    internal class Wu3DHistogram
    {
        public int IndexRedBits { get; }
        public int IndexGreenBits { get; }
        public int IndexBlueBits { get; }
        public int IndexAlphaBits { get; }
        public int IndexRedCount { get; }
        public int IndexGreenCount { get; }
        public int IndexBlueCount { get; }
        public int IndexAlphaCount { get; }

        /// <summary>
        /// Moment of <c>P(c)</c>.
        /// </summary>
        public long[] Vwt { get; }

        /// <summary>
        /// Moment of <c>r*P(c)</c>.
        /// </summary>
        public long[] Vmr { get; }

        /// <summary>
        /// Moment of <c>g*P(c)</c>.
        /// </summary>
        public long[] Vmg { get; }

        /// <summary>
        /// Moment of <c>b*P(c)</c>.
        /// </summary>
        public long[] Vmb { get; }

        /// <summary>
        /// Moment of <c>a*P(c)</c>.
        /// </summary>
        public long[] Vma { get; }

        /// <summary>
        /// Moment of <c>c^2*P(c)</c>.
        /// </summary>
        public double[] M2 { get; }

        /// <summary>
        /// Creates a 3-dimensional color histogram.
        /// </summary>
        /// <param name="bitDepths"></param>
        public Wu3DHistogram(ColorChannelBitDepths bitDepths)
        {
            IndexRedBits = bitDepths.Red;
            IndexGreenBits = bitDepths.Green;
            IndexBlueBits = bitDepths.Blue;
            IndexAlphaBits = bitDepths.Alpha;
            IndexRedCount = (1 << bitDepths.Red) + 1;
            IndexGreenCount = (1 << bitDepths.Green) + 1;
            IndexBlueCount = (1 << bitDepths.Blue) + 1;
            IndexAlphaCount = (1 << bitDepths.Alpha) + 1;

            var tableLength = IndexRedCount * IndexGreenCount * IndexBlueCount * IndexAlphaCount;

            Vwt = new long[tableLength];
            Vmr = new long[tableLength];
            Vmg = new long[tableLength];
            Vmb = new long[tableLength];
            Vma = new long[tableLength];
            M2 = new double[tableLength];
        }

        public void Create(IList<Rgba32> colors)
        {
            Create(colors, null);
        }

        public void Create(IList<Rgba32> colors, IList<Rgba32>? biasPalette)
        {
            ClearTables();

            FillTables(colors, biasPalette);

            CalculateMoments();
        }

        private void ClearTables()
        {
            Array.Clear(Vwt);
            Array.Clear(Vmr);
            Array.Clear(Vmg);
            Array.Clear(Vmb);
            Array.Clear(Vma);
            Array.Clear(M2);
        }

        private void FillTables(IList<Rgba32> colors, IList<Rgba32>? biasPalette)
        {
            foreach (var color in colors)
            {
                if (biasPalette?.Contains(color) ?? false)
                    continue;

                int a = color.A;
                int r = color.R;
                int g = color.G;
                int b = color.B;

                int inr = r >> 8 - IndexRedBits;
                int ing = g >> 8 - IndexGreenBits;
                int inb = b >> 8 - IndexBlueBits;
                int ina = a >> 8 - IndexAlphaBits;

                AddWeightedSample(inr, ing, inb, ina, r, g, b, a, 1);
            }
        }

        private void AddWeightedSample(int inr, int ing, int inb, int ina, int r, int g, int b, int a, int weight)
        {
            int ind = WuCommon.GetIndex(inr + 1, ing + 1, inb + 1, ina + 1, IndexGreenCount, IndexBlueCount, IndexAlphaCount);

            Vwt[ind] += weight;
            Vmr[ind] += weight * r;
            Vmg[ind] += weight * g;
            Vmb[ind] += weight * b;
            Vma[ind] += weight * a;
            M2[ind] += weight * (r * r + g * g + b * b + a * a); // Euclidean distance as moment
        }

        private void CalculateMoments()
        {
            long[] volume = new long[IndexBlueCount * IndexAlphaCount];
            long[] volumeR = new long[IndexBlueCount * IndexAlphaCount];
            long[] volumeG = new long[IndexBlueCount * IndexAlphaCount];
            long[] volumeB = new long[IndexBlueCount * IndexAlphaCount];
            long[] volumeA = new long[IndexBlueCount * IndexAlphaCount];
            double[] volume2 = new double[IndexBlueCount * IndexAlphaCount];

            long[] area = new long[IndexAlphaCount];
            long[] areaR = new long[IndexAlphaCount];
            long[] areaG = new long[IndexAlphaCount];
            long[] areaB = new long[IndexAlphaCount];
            long[] areaA = new long[IndexAlphaCount];
            double[] area2 = new double[IndexAlphaCount];

            for (int r = 1; r < IndexRedCount; r++)
            {
                Array.Clear(volume, 0, IndexBlueCount * IndexAlphaCount);
                Array.Clear(volumeR, 0, IndexBlueCount * IndexAlphaCount);
                Array.Clear(volumeG, 0, IndexBlueCount * IndexAlphaCount);
                Array.Clear(volumeB, 0, IndexBlueCount * IndexAlphaCount);
                Array.Clear(volumeA, 0, IndexBlueCount * IndexAlphaCount);
                Array.Clear(volume2, 0, IndexBlueCount * IndexAlphaCount);

                for (int g = 1; g < IndexGreenCount; g++)
                {
                    Array.Clear(area, 0, IndexAlphaCount);
                    Array.Clear(areaR, 0, IndexAlphaCount);
                    Array.Clear(areaG, 0, IndexAlphaCount);
                    Array.Clear(areaB, 0, IndexAlphaCount);
                    Array.Clear(areaA, 0, IndexAlphaCount);
                    Array.Clear(area2, 0, IndexAlphaCount);

                    for (int b = 1; b < IndexBlueCount; b++)
                    {
                        long line = 0;
                        long lineR = 0;
                        long lineG = 0;
                        long lineB = 0;
                        long lineA = 0;
                        double line2 = 0;

                        for (int a = 1; a < IndexAlphaCount; a++)
                        {
                            int ind1 = WuCommon.GetIndex(r, g, b, a, IndexGreenCount, IndexBlueCount, IndexAlphaCount);

                            line += Vwt[ind1];
                            lineR += Vmr[ind1];
                            lineG += Vmg[ind1];
                            lineB += Vmb[ind1];
                            lineA += Vma[ind1];
                            line2 += M2[ind1];

                            area[a] += line;
                            areaR[a] += lineR;
                            areaG[a] += lineG;
                            areaB[a] += lineB;
                            areaA[a] += lineA;
                            area2[a] += line2;

                            int inv = b * IndexAlphaCount + a;

                            volume[inv] += area[a];
                            volumeR[inv] += areaR[a];
                            volumeG[inv] += areaG[a];
                            volumeB[inv] += areaB[a];
                            volumeA[inv] += areaA[a];
                            volume2[inv] += area2[a];

                            int ind2 = ind1 - WuCommon.GetIndex(1, 0, 0, 0, IndexGreenCount, IndexBlueCount, IndexAlphaCount);

                            Vwt[ind1] = Vwt[ind2] + volume[inv];
                            Vmr[ind1] = Vmr[ind2] + volumeR[inv];
                            Vmg[ind1] = Vmg[ind2] + volumeG[inv];
                            Vmb[ind1] = Vmb[ind2] + volumeB[inv];
                            Vma[ind1] = Vma[ind2] + volumeA[inv];
                            M2[ind1] = M2[ind2] + volume2[inv];
                        }
                    }
                }
            }
        }
    }
}
