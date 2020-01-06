using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;

namespace Kanvas.Quantization.Models.Quantizer.Wu
{
    class Wu3DHistogram
    {
        public int IndexBits { get; }
        public int IndexAlphaBits { get; }
        public int IndexCount { get; }
        public int IndexAlphaCount { get; }

        /// <summary>
        /// Moment of <c>P(c)</c>.
        /// </summary>
        public long[] Vwt { get; private set; }

        /// <summary>
        /// Moment of <c>r*P(c)</c>.
        /// </summary>
        public long[] Vmr { get; private set; }

        /// <summary>
        /// Moment of <c>g*P(c)</c>.
        /// </summary>
        public long[] Vmg { get; private set; }

        /// <summary>
        /// Moment of <c>b*P(c)</c>.
        /// </summary>
        public long[] Vmb { get; private set; }

        /// <summary>
        /// Moment of <c>a*P(c)</c>.
        /// </summary>
        public long[] Vma { get; private set; }

        /// <summary>
        /// Moment of <c>c^2*P(c)</c>.
        /// </summary>
        public double[] M2 { get; private set; }

        /// <summary>
        /// Creates a 3-dimensional color histogram.
        /// </summary>
        /// <param name="colors">The colors to put into the histogram.</param>
        /// <param name="indexBits"></param>
        /// <param name="alphaBits"></param>
        /// <param name="indexCount"></param>
        /// <param name="alphaCount"></param>
        public Wu3DHistogram(int indexBits, int alphaBits, int indexCount, int alphaCount)
        {
            IndexBits = indexBits;
            IndexAlphaBits = alphaBits;
            IndexCount = indexCount;
            IndexAlphaCount = alphaCount;
        }

        public void Create(IList<Color> colors)
        {
            InitializeTables(IndexCount * IndexCount * IndexCount * IndexAlphaCount);

            FillTables(colors);
            CalculateMoments();
        }

        private void InitializeTables(int tableLength)
        {
            Vwt = new long[tableLength];
            Vmr = new long[tableLength];
            Vmg = new long[tableLength];
            Vmb = new long[tableLength];
            Vma = new long[tableLength];
            M2 = new double[tableLength];
        }

        private void FillTables(IList<Color> colors)
        {
            foreach (var color in colors)
            {
                int a = color.A;
                int r = color.R;
                int g = color.G;
                int b = color.B;

                int inr = r >> (8 - IndexBits);
                int ing = g >> (8 - IndexBits);
                int inb = b >> (8 - IndexBits);
                int ina = a >> (8 - IndexAlphaBits);

                int ind = WuCommon.GetIndex(inr + 1, ing + 1, inb + 1, ina + 1, IndexBits, IndexAlphaBits);

                Vwt[ind]++;
                Vmr[ind] += r;
                Vmg[ind] += g;
                Vmb[ind] += b;
                Vma[ind] += a;
                M2[ind] += r * r + g * g + b * b + a * a;      // Euclidean distance as moment
            }
        }

        private void CalculateMoments()
        {
            long[] volume = new long[IndexCount * IndexAlphaCount];
            long[] volumeR = new long[IndexCount * IndexAlphaCount];
            long[] volumeG = new long[IndexCount * IndexAlphaCount];
            long[] volumeB = new long[IndexCount * IndexAlphaCount];
            long[] volumeA = new long[IndexCount * IndexAlphaCount];
            double[] volume2 = new double[IndexCount * IndexAlphaCount];

            long[] area = new long[IndexAlphaCount];
            long[] areaR = new long[IndexAlphaCount];
            long[] areaG = new long[IndexAlphaCount];
            long[] areaB = new long[IndexAlphaCount];
            long[] areaA = new long[IndexAlphaCount];
            double[] area2 = new double[IndexAlphaCount];

            for (int r = 1; r < IndexCount; r++)
            {
                Array.Clear(volume, 0, IndexCount * IndexAlphaCount);
                Array.Clear(volumeR, 0, IndexCount * IndexAlphaCount);
                Array.Clear(volumeG, 0, IndexCount * IndexAlphaCount);
                Array.Clear(volumeB, 0, IndexCount * IndexAlphaCount);
                Array.Clear(volumeA, 0, IndexCount * IndexAlphaCount);
                Array.Clear(volume2, 0, IndexCount * IndexAlphaCount);

                for (int g = 1; g < IndexCount; g++)
                {
                    Array.Clear(area, 0, IndexAlphaCount);
                    Array.Clear(areaR, 0, IndexAlphaCount);
                    Array.Clear(areaG, 0, IndexAlphaCount);
                    Array.Clear(areaB, 0, IndexAlphaCount);
                    Array.Clear(areaA, 0, IndexAlphaCount);
                    Array.Clear(area2, 0, IndexAlphaCount);

                    for (int b = 1; b < IndexCount; b++)
                    {
                        long line = 0;
                        long lineR = 0;
                        long lineG = 0;
                        long lineB = 0;
                        long lineA = 0;
                        double line2 = 0;

                        for (int a = 1; a < IndexAlphaCount; a++)
                        {
                            int ind1 = WuCommon.GetIndex(r, g, b, a, IndexBits, IndexAlphaBits);

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

                            int ind2 = ind1 - WuCommon.GetIndex(1, 0, 0, 0, IndexBits, IndexAlphaBits);

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
