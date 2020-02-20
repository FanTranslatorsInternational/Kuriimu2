namespace Kanvas.Encoding.BlockCompressions.ASTC_CS.Partitions
{
    static class PartitionSelection
    {
        public static int SelectPartition(uint seed, int x, int y, int z, int partitions, bool smallBlock)
        {
            if (smallBlock)
            {
                x <<= 1;
                y <<= 1;
                z <<= 1;
            }

            seed += (uint)((partitions - 1) * 1024);

            uint rnum = Hash52(seed);

            var seed1 = rnum & 0xF;
            var seed2 = (rnum >> 4) & 0xF;
            var seed3 = (rnum >> 8) & 0xF;
            var seed4 = (rnum >> 12) & 0xF;
            var seed5 = (rnum >> 16) & 0xF;
            var seed6 = (rnum >> 20) & 0xF;
            var seed7 = (rnum >> 24) & 0xF;
            var seed8 = (rnum >> 28) & 0xF;
            var seed9 = (rnum >> 18) & 0xF;
            var seed10 = (rnum >> 22) & 0xF;
            var seed11 = (rnum >> 26) & 0xF;
            var seed12 = ((rnum >> 30) | (rnum << 2)) & 0xF;

            // squaring all the seeds in order to bias their distribution
            // towards lower values.
            seed1 *= seed1;
            seed2 *= seed2;
            seed3 *= seed3;
            seed4 *= seed4;
            seed5 *= seed5;
            seed6 *= seed6;
            seed7 *= seed7;
            seed8 *= seed8;
            seed9 *= seed9;
            seed10 *= seed10;
            seed11 *= seed11;
            seed12 *= seed12;

            int sh1, sh2, sh3;
            if ((seed & 1) != 0)
            {
                sh1 = ((seed & 2) != 0 ? 4 : 5);
                sh2 = (partitions == 3 ? 6 : 5);
            }
            else
            {
                sh1 = (partitions == 3 ? 6 : 5);
                sh2 = ((seed & 2) != 0 ? 4 : 5);
            }

            sh3 = (seed & 0x10)!=0 ? sh1 : sh2;

            seed1 >>= sh1;
            seed2 >>= sh2;
            seed3 >>= sh1;
            seed4 >>= sh2;
            seed5 >>= sh1;
            seed6 >>= sh2;
            seed7 >>= sh1;
            seed8 >>= sh2;

            seed9 >>= sh3;
            seed10 >>= sh3;
            seed11 >>= sh3;
            seed12 >>= sh3;

            var a = seed1 * x + seed2 * y + seed11 * z + (rnum >> 14);
            var b = seed3 * x + seed4 * y + seed12 * z + (rnum >> 10);
            var c = seed5 * x + seed6 * y + seed9 * z + (rnum >> 6);
            var d = seed7 * x + seed8 * y + seed10 * z + (rnum >> 2);

            // apply the saw
            a &= 0x3F;
            b &= 0x3F;
            c &= 0x3F;
            d &= 0x3F;

            // remove some of the components if we are to output < 4 partitions.
            if (partitions <= 3)
                d = 0;
            if (partitions <= 2)
                c = 0;
            if (partitions <= 1)
                b = 0;

            if (a >= b && a >= c && a >= d)
                return 0;
            else if (b >= c && b >= d)
                return 1;
            else if (c >= d)
                return 2;
            else
                return 3;
        }

        private static uint Hash52(uint inp)
        {
            inp ^= inp >> 15;

            inp *= 0xEEDE0891; // (2^4+1)*(2^7+1)*(2^17-1)
            inp ^= inp >> 5;
            inp += inp << 16;
            inp ^= inp >> 7;
            inp ^= inp >> 3;
            inp ^= inp << 6;
            inp ^= inp >> 17;

            return inp;
        }
    }
}
