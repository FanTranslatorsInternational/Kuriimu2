using System;

namespace Kanvas.Support.BCn
{

    /// <summary>
    /// Encodes BC3(alpha)/BC4/BС5 blocks.
    /// </summary>
    /// <remarks>
    /// To use the encoder, you must first load a block to encode
    /// using one of the <see cref="LoadBlock"/> overloads, after
    /// which you call either <see cref="EncodeSigned"/> or
    /// <see cref="EncodeUnsigned"/>. Note that encoding a block
    /// alters the loaded values in place - call <c>LoadBlock</c>
    /// before calling one of the encode methods again.
    /// </remarks>
    public class BC4BlockEncoder
    {
        /// <summary>
        /// Loads a block of values for subsequent encoding.
        /// </summary>
        /// <param name="values">The values to encode.</param>
        /// <param name="index">The index to start reading values.</param>
        /// <param name="rowPitch">The pitch between rows of values.</param>
        /// <param name="colPitch">The pitch between subsequent values within a row.</param>
        public void LoadBlock(float[] values, int index = 0,
            int rowPitch = 4, int colPitch = 1)
        {
            var target = this.values;

            if (rowPitch == 4 && colPitch == 1)
            {
                //get the fast case out of the way
                Array.Copy(values, index, target, 0, 16);
                return;
            }

            int i = index;

            target[0] = values[i];
            target[1] = values[i += colPitch];
            target[2] = values[i += colPitch];
            target[3] = values[i += colPitch];

            i = index += rowPitch;

            target[4] = values[i];
            target[5] = values[i += colPitch];
            target[6] = values[i += colPitch];
            target[7] = values[i += colPitch];

            i = index += rowPitch;

            target[8] = values[i];
            target[9] = values[i += colPitch];
            target[10] = values[i += colPitch];
            target[11] = values[i += colPitch];

            i = index + rowPitch;

            target[12] = values[i];
            target[13] = values[i += colPitch];
            target[14] = values[i += colPitch];
            target[15] = values[i += colPitch];
        }

        /// <summary>
        /// Encode a block of signed values.
        /// </summary>
        /// <returns></returns>
        public BC4SBlock EncodeSigned()
        {
            //load the input and scan for the boundary condition

            ClampAndFindRange(-1F, 1F);

            bool hasEndPoint = minValue == -1F || maxValue == 1F;

            //find a span across the space

            float r0, r1;
            SpanValues(out r0, out r1, hasEndPoint, true);

            //roundtrip it through integer format

            var ret = new BC4SBlock();

            ret.R0 = Helpers.FloatToSNorm(r0);
            ret.R1 = Helpers.FloatToSNorm(r1);

            ret.GetPalette(interpValues);

            ret.PackedValue |= FindClosest();

            return ret;
        }

        /// <summary>
        /// Encode a block of unsigned values.
        /// </summary>
        /// <returns></returns>
        public BC4UBlock EncodeUnsigned()
        {
            //load the input and scan for the boundary condition

            ClampAndFindRange(0F, 1F);

            bool hasEndPoint = minValue == 0F || maxValue == 1F;

            //find a span across the space

            float r0, r1;
            SpanValues(out r0, out r1, hasEndPoint, false);

            //roundtrip it through integer format

            var ret = new BC4UBlock();

            ret.R0 = Helpers.FloatToUNorm(r0);
            ret.R1 = Helpers.FloatToUNorm(r1);

            ret.GetPalette(interpValues);

            ret.PackedValue |= FindClosest();

            return ret;
        }

        private void ClampAndFindRange(float clampMin, float clampMax)
        {
            var target = this.values;

            var v0 = target[0];

            if (v0 < clampMin) target[0] = v0 = clampMin;
            else if (v0 > clampMax) target[0] = v0 = clampMax;

            minValue = maxValue = v0;

            for (int i = 1; i < target.Length; i++)
            {
                var v = target[i];

                if (v < clampMin) target[i] = v = clampMin;
                else if (v > clampMax) target[i] = v = clampMax;

                if (v < minValue) minValue = v;
                else if (v > maxValue) maxValue = v;
            }
        }

        private float[] values = new float[16];
        private float[] interpValues = new float[8];
        private float minValue, maxValue;

        private static readonly float[] pC6 = { 5.0f / 5.0f, 4.0f / 5.0f, 3.0f / 5.0f, 2.0f / 5.0f, 1.0f / 5.0f, 0.0f / 5.0f };
        private static readonly float[] pD6 = { 0.0f / 5.0f, 1.0f / 5.0f, 2.0f / 5.0f, 3.0f / 5.0f, 4.0f / 5.0f, 5.0f / 5.0f };
        private static readonly float[] pC8 = { 7.0f / 7.0f, 6.0f / 7.0f, 5.0f / 7.0f, 4.0f / 7.0f, 3.0f / 7.0f, 2.0f / 7.0f, 1.0f / 7.0f, 0.0f / 7.0f };
        private static readonly float[] pD8 = { 0.0f / 7.0f, 1.0f / 7.0f, 2.0f / 7.0f, 3.0f / 7.0f, 4.0f / 7.0f, 5.0f / 7.0f, 6.0f / 7.0f, 7.0f / 7.0f };

        private void SpanValues(out float r0, out float r1, bool isSixPointInterp, bool isSigned)
        {
            //pulled from the original OptimizeAlpha code in the D3DX sample code

            float[] pC, pD;
            if (isSixPointInterp)
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
            if (isSixPointInterp)
            {
                vMin = rangeMax;
                vMax = rangeMin;

                for (int i = 0; i < values.Length; i++)
                {
                    var v = values[i];

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
                vMin = minValue;
                vMax = maxValue;
            }

            // Use Newton's Method to find local minima of sum-of-squares error.

            int numSteps = isSixPointInterp ? 6 : 8;
            float fSteps = (float)(numSteps - 1);

            for (int iteration = 0; iteration < 8; iteration++)
            {
                if ((vMax - vMin) < (1.0f / 256.0f))
                    break;

                float fScale = fSteps / (vMax - vMin);

                // Calculate new steps

                for (int i = 0; i < numSteps; i++)
                    interpValues[i] = pC[i] * vMin + pD[i] * vMax;

                if (isSixPointInterp)
                {
                    interpValues[6] = rangeMin;
                    interpValues[7] = rangeMax;
                }

                // Evaluate function, and derivatives
                float dX = 0F;
                float dY = 0F;
                float d2X = 0F;
                float d2Y = 0F;

                for (int iPoint = 0; iPoint < values.Length; iPoint++)
                {
                    float dot = (values[iPoint] - vMin) * fScale;

                    int iStep;
                    if (dot <= 0.0f)
                        iStep = ((6 == numSteps) && (values[iPoint] <= vMin * 0.5f)) ? 6 : 0;
                    else if (dot >= fSteps)
                        iStep = ((6 == numSteps) && (values[iPoint] >= (vMax + 1.0f) * 0.5f)) ? 7 : (numSteps - 1);
                    else
                        iStep = (int)(dot + 0.5f);


                    if (iStep < numSteps)
                    {
                        // D3DX had this computation backwards (pPoints[iPoint] - pSteps[iStep])
                        // this fix improves RMS of the alpha component
                        float fDiff = interpValues[iStep] - values[iPoint];

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

            if (isSixPointInterp)
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

        private ulong FindClosest()
        {
            ulong ret = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                var v = values[i];

                int iBest = 0;
                float bestDelta = Math.Abs(interpValues[0] - v);

                for (int j = 1; j < interpValues.Length; j++)
                {
                    float delta = Math.Abs(interpValues[j] - v);

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
