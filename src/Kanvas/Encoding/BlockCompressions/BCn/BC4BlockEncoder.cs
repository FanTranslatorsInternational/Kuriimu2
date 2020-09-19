using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.BlockCompressions.BCn.Models;

namespace Kanvas.Encoding.BlockCompressions.BCn
{

    /// <summary>
    /// Encodes BC3(alpha)/BC4/BС5 blocks.
    /// </summary>
    /// <remarks>
    /// To use the encoder, you must first load a block to encode
    /// using one of the <see cref="LoadBlock"/> overloads, after
    /// which you call either <see cref="EncodeSigned"/> or
    /// <see cref="EncodeUnsigned"/>. Note that encoding a block
    /// alters the loaded Values in place - call <c>LoadBlock</c>
    /// before calling one of the encode methods again.
    /// </remarks>
    public class BC4BlockEncoder
    {
        private static readonly float[] pC6 = { 5.0f / 5.0f, 4.0f / 5.0f, 3.0f / 5.0f, 2.0f / 5.0f, 1.0f / 5.0f, 0.0f / 5.0f };
        private static readonly float[] pD6 = { 0.0f / 5.0f, 1.0f / 5.0f, 2.0f / 5.0f, 3.0f / 5.0f, 4.0f / 5.0f, 5.0f / 5.0f };
        private static readonly float[] pC8 = { 7.0f / 7.0f, 6.0f / 7.0f, 5.0f / 7.0f, 4.0f / 7.0f, 3.0f / 7.0f, 2.0f / 7.0f, 1.0f / 7.0f, 0.0f / 7.0f };
        private static readonly float[] pD8 = { 0.0f / 7.0f, 1.0f / 7.0f, 2.0f / 7.0f, 3.0f / 7.0f, 4.0f / 7.0f, 5.0f / 7.0f, 6.0f / 7.0f, 7.0f / 7.0f };

        private static readonly Lazy<BC4BlockEncoder> Lazy = new Lazy<BC4BlockEncoder>(() => new BC4BlockEncoder());
        public static BC4BlockEncoder Instance => Lazy.Value;

        /// <summary>
        /// Loads a block of Values for subsequent encoding.
        /// </summary>
        /// <param name="values">The Values to encode.</param>
        /// <param name="index">The index to start reading Values.</param>
        /// <param name="rowPitch">The pitch between rows of Values.</param>
        /// <param name="colPitch">The pitch between subsequent Values within a row.</param>
        public Bc4BlockData LoadBlock(float[] values, int index = 0,
            int rowPitch = 4, int colPitch = 1)
        {
            var target = new float[16];

            if (rowPitch == 4 && colPitch == 1)
            {
                //get the fast case out of the way
                Array.Copy(values, index, target, 0, 16);

                return new Bc4BlockData { Values = target };
            }

            var i = index;

            target[0] = values[i];
            target[1] = values[i += colPitch];
            target[2] = values[i += colPitch];
            target[3] = values[i + colPitch];

            i = index += rowPitch;

            target[4] = values[i];
            target[5] = values[i += colPitch];
            target[6] = values[i += colPitch];
            target[7] = values[i + colPitch];

            i = index += rowPitch;

            target[8] = values[i];
            target[9] = values[i += colPitch];
            target[10] = values[i += colPitch];
            target[11] = values[i + colPitch];

            i = index + rowPitch;

            target[12] = values[i];
            target[13] = values[i += colPitch];
            target[14] = values[i += colPitch];
            target[15] = values[i + colPitch];

            return new Bc4BlockData { Values = target };
        }

        public Bc4BlockData LoadBlock(IList<Color> colors, Bc4Component component,
            int rowPitch = 4, int colPitch = 1)
        {
            switch (component)
            {
                case Bc4Component.R:
                    return LoadBlock(colors.Select(clr => clr.R / 255f).ToArray(),
                        rowPitch, colPitch);

                case Bc4Component.G:
                    return LoadBlock(colors.Select(clr => clr.G / 255f).ToArray(),
                        rowPitch, colPitch);

                case Bc4Component.B:
                    return LoadBlock(colors.Select(clr => clr.B / 255f).ToArray(),
                        rowPitch, colPitch);

                case Bc4Component.A:
                    return LoadBlock(colors.Select(clr => clr.A / 255f).ToArray(),
                        rowPitch, colPitch);

                default:
                    return LoadBlock(colors.Select(clr => clr.GetBrightness()).ToArray(),
                        rowPitch, colPitch);
            }
        }

        /// <summary>
        /// Encode a block of signed Values.
        /// </summary>
        /// <returns></returns>
        public BC4SBlock EncodeSigned(Bc4BlockData data)
        {
            //load the input and scan for the boundary condition

            ClampAndFindRange(data, -1F, 1F);

            bool hasEndPoint = data.MinValue == -1F || data.MaxValue == 1F;

            //find a span across the space

            SpanValues(hasEndPoint, true, data, out var r0, out var r1);

            //roundtrip it through integer format

            var ret = new BC4SBlock
            {
                R0 = Helpers.FloatToSNorm(r0),
                R1 = Helpers.FloatToSNorm(r1)
            };


            ret.GetPalette(data.InterpretedValues);

            ret.PackedValue |= FindClosest(data);

            return ret;
        }

        /// <summary>
        /// Encode a block of unsigned Values.
        /// </summary>
        /// <returns></returns>
        public BC4UBlock EncodeUnsigned(Bc4BlockData data)
        {
            //load the input and scan for the boundary condition

            ClampAndFindRange(data, 0F, 1F);

            var hasEndPoint = data.MinValue == 0F || data.MaxValue == 1F;

            //find a span across the space
            SpanValues(hasEndPoint, false, data, out var r0, out var r1);

            //roundtrip it through integer format

            var ret = new BC4UBlock
            {
                R0 = Helpers.FloatToUNorm(r0),
                R1 = Helpers.FloatToUNorm(r1)
            };


            ret.GetPalette(data.InterpretedValues);

            ret.PackedValue |= FindClosest(data);

            return ret;
        }

        private void ClampAndFindRange(Bc4BlockData data, float clampMin, float clampMax)
        {
            var target = data.Values;

            var v0 = target[0];

            if (v0 < clampMin) target[0] = v0 = clampMin;
            else if (v0 > clampMax) target[0] = v0 = clampMax;

            data.MinValue = data.MaxValue = v0;

            for (int i = 1; i < target.Length; i++)
            {
                var v = target[i];

                if (v < clampMin) target[i] = v = clampMin;
                else if (v > clampMax) target[i] = v = clampMax;

                if (v < data.MinValue) data.MinValue = v;
                else if (v > data.MaxValue) data.MaxValue = v;
            }
        }

        private void SpanValues(bool isSixPointInterpreter, bool isSigned, Bc4BlockData data, out float r0, out float r1)
        {
            //pulled from the original OptimizeAlpha code in the D3DX sample code

            float[] pC, pD;
            if (isSixPointInterpreter)
            {
                pC = pC6;
                pD = pD6;
            }
            else
            {
                pC = pC8;
                pD = pD8;
            }

            float rangeMin = isSigned ? -1F : 0F;
            const float rangeMax = 1F;

            //find min and max points as a starting solution

            float vMin, vMax;
            if (isSixPointInterpreter)
            {
                vMin = rangeMax;
                vMax = rangeMin;

                for (int i = 0; i < data.Values.Length; i++)
                {
                    var v = data.Values[i];

                    if (v == rangeMin || v == rangeMax)
                        continue;

                    if (v < vMin) vMin = v;
                    if (v > vMax) vMax = v;
                }

                if (vMin == vMax)
                    vMax = rangeMax;
            }
            else
            {
                vMin = data.MinValue;
                vMax = data.MaxValue;
            }

            // Use Newton's Method to find local minima of sum-of-squares Error.

            int numSteps = isSixPointInterpreter ? 6 : 8;
            float fSteps = numSteps - 1;

            for (int iteration = 0; iteration < 8; iteration++)
            {
                if ((vMax - vMin) < (1.0f / 256.0f))
                    break;

                float fScale = fSteps / (vMax - vMin);

                // Calculate new steps

                for (int i = 0; i < numSteps; i++)
                    data.InterpretedValues[i] = pC[i] * vMin + pD[i] * vMax;

                if (isSixPointInterpreter)
                {
                    data.InterpretedValues[6] = rangeMin;
                    data.InterpretedValues[7] = rangeMax;
                }

                // Evaluate function, and derivatives
                float dX = 0F;
                float dY = 0F;
                float d2X = 0F;
                float d2Y = 0F;

                for (int iPoint = 0; iPoint < data.Values.Length; iPoint++)
                {
                    float dot = (data.Values[iPoint] - vMin) * fScale;

                    int iStep;
                    if (dot <= 0.0f)
                        iStep = ((6 == numSteps) && (data.Values[iPoint] <= vMin * 0.5f)) ? 6 : 0;
                    else if (dot >= fSteps)
                        iStep = ((6 == numSteps) && (data.Values[iPoint] >= (vMax + 1.0f) * 0.5f)) ? 7 : (numSteps - 1);
                    else
                        iStep = (int)(dot + 0.5f);


                    if (iStep < numSteps)
                    {
                        // D3DX had this computation backwards (pPoints[iPoint] - pSteps[iStep])
                        // this fix improves RMS of the alpha component
                        float fDiff = data.InterpretedValues[iStep] - data.Values[iPoint];

                        dX += pC[iStep] * fDiff;
                        d2X += pC[iStep] * pC[iStep];

                        dY += pD[iStep] * fDiff;
                        d2Y += pD[iStep] * pD[iStep];
                    }
                }

                // Move endpoints

                if (d2X > 0.0f)
                    vMin -= dX / d2X;

                if (d2Y > 0.0f)
                    vMax -= dY / d2Y;

                if (vMin > vMax)
                {
                    float f = vMin; vMin = vMax; vMax = f;
                }

                if ((dX * dX < (1.0f / 64.0f)) && (dY * dY < (1.0f / 64.0f)))
                    break;
            }

            vMin = (vMin < rangeMin) ? rangeMin : (vMin > rangeMax) ? rangeMax : vMin;
            vMax = (vMax < rangeMin) ? rangeMin : (vMax > rangeMax) ? rangeMax : vMax;

            if (isSixPointInterpreter)
            {
                r0 = vMin;
                r1 = vMax;
            }
            else
            {
                r0 = vMax;
                r1 = vMin;
            }
        }

        private ulong FindClosest(Bc4BlockData data)
        {
            ulong ret = 0;

            for (int i = 0; i < data.Values.Length; ++i)
            {
                var v = data.Values[i];

                int iBest = 0;
                float bestDelta = Math.Abs(data.InterpretedValues[0] - v);

                for (int j = 1; j < data.InterpretedValues.Length; j++)
                {
                    float delta = Math.Abs(data.InterpretedValues[j] - v);

                    if (delta < bestDelta)
                    {
                        iBest = j;
                        bestDelta = delta;
                    }
                }

                int shift = 16 + 3 * i;
                ret |= (ulong)iBest << shift;
            }

            return ret;
        }
    }
}
