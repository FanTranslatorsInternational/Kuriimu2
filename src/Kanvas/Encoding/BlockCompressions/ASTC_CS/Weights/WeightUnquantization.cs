namespace Kanvas.Encoding.BlockCompressions.ASTC_CS.Weights
{
    static class WeightUnquantization
    {
        public static readonly byte[][] WeightUnquantizationTable =
        {
            new byte[]
            {
                0, 64
            },
            new byte[]
            {
                0, 32, 64
            },
            new byte[]
            {
                0, 21, 43, 64
            },
            new byte[]
            {
                0, 16, 32, 48, 64
            },
            new byte[]
            {
                0, 64, 12, 52, 25, 39
            },
            new byte[]
            {
                0, 9, 18, 27, 37, 46, 55, 64
            },
            new byte[]
            {
                0, 64, 7, 57, 14, 50, 21, 43, 28, 36
            },
            new byte[]
            {
                0, 64, 17, 47, 5, 59, 23, 41, 11, 53, 28, 36
            },
            new byte[]
            {
                0, 4, 8, 12, 17, 21, 25, 29, 35, 39, 43, 47, 52, 56, 60, 64
            },
            new byte[]
            {
                0, 64, 16, 48, 3, 61, 19, 45, 6, 58, 23, 41, 9, 55, 26, 38, 13, 51,
                29, 35
            },
            new byte[]
            {
                0, 64, 8, 56, 16, 48, 24, 40, 2, 62, 11, 53, 19, 45, 27, 37, 5, 59,
                13, 51, 22, 42, 30, 34
            },
            new byte[]
            {
                0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 34, 36, 38,
                40, 42, 44, 46, 48, 50, 52, 54, 56, 58, 60, 62, 64
            }
        };
    }
}
