using System;

namespace Kanvas.Support.BCn
{
    public class BC2ABlockEncoder
    {
        public bool Dither { get; set; }

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

        public BC2ABlock Encode()
        {
            //clamp the values into range

            for (int i = 0; i < 16; i++)
            {
                var v = values[i];

                if (v < 0) values[i] = 0;
                else if (v > 1) values[i] = 1;
            }

            //encode, optionally dithering as we go

            bool dither = Dither;

            BC2ABlock ret = new BC2ABlock();

            if (dither)
            {
                if (error == null)
                    error = new float[16];
                else
                    Array.Clear(error, 0, 16);
            }

            for (int i = 0; i < values.Length; i++)
            {
                float v = values[i];

                if (dither)
                    v += error[i];

                int u = (int)(v * 15F + 0.5F);

                ret.PackedValue |= (ulong)u << (i * 4);

                if (dither)
                {
                    float d = v - u * (1F / 15F);

                    if ((i & 3) != 3)
                        error[i + 1] += d * (7F / 16F);

                    if (i < 12)
                    {
                        if ((i & 3) != 0)
                            error[i + 3] += d * (3F / 16F);

                        error[i + 4] += d * (5F / 16F);

                        if ((i & 3) != 3)
                            error[i + 5] += d * (1F / 16F);
                    }
                }
            }

            return ret;
        }

        private float[] values = new float[16];
        private float[] error;
    }
}
