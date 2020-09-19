using Komponent.IO;

namespace Kanvas.Encoding.BlockCompressions.ASTC_CS.Models
{
    class BlockMode
    {
        public bool IsVoidExtent { get; }
        public bool IsHdr { get; }

        public bool UsesReserved { get; }

        public bool IsHpr { get; }
        public bool IsDualPlane { get; }
        public int Width { get; }
        public int Height { get; }
        public int QuantizationMode { get; }
        public int WeightCount => Width * Height * (IsDualPlane ? 2 : 1);
        public int WeightBitCount => IntegerSequenceEncoding.ComputeBitCount(WeightCount, QuantizationMode);

        public static BlockMode Create(BitReader br)
        {
            return new BlockMode(br.ReadBits<short>(11));
        }

        private BlockMode(short modeValue)
        {
            if ((modeValue & 0x1FF) == 0x1FC)
            {
                IsVoidExtent = true;
                IsHdr = (modeValue & 0x200) != 0;
                return;
            }

            if ((modeValue & 0xF) == 0 || (modeValue & 0x1C3) == 0x1C0)
            {
                UsesReserved = true;
                return;
            }

            IsHpr = ((modeValue >> 9) & 1) == 1;
            IsDualPlane = ((modeValue >> 10) & 1) == 1;
            var a = (modeValue >> 5) & 3;

            var baseQuantizationMode = (modeValue >> 4) & 1;
            if ((modeValue & 0x3) != 0)
            {
                baseQuantizationMode |= (modeValue & 3) << 1;
                var b = (modeValue >> 7) & 3;

                switch ((modeValue >> 2) & 3)
                {
                    case 0:
                        Width = b + 4;
                        Height = a + 2;
                        break;

                    case 1:
                        Width = b + 8;
                        Height = a + 2;
                        break;

                    case 2:
                        Width = a + 2;
                        Height = b + 8;
                        break;

                    case 3:
                        if (b >> 1 == 1)
                        {
                            Width = (b & 1) + 2;
                            Height = a + 2;
                        }
                        else
                        {
                            Width = a + 2;
                            Height = (b & 1) + 6;
                        }
                        break;
                }
            }
            else
            {
                baseQuantizationMode |= (modeValue & 0xC) >> 1;

                switch ((modeValue >> 7) & 3)
                {
                    case 0:
                        Width = 12;
                        Height = a + 2;
                        break;

                    case 1:
                        Width = a + 2;
                        Height = 12;
                        break;

                    case 2:
                        var b = modeValue >> 9;

                        Width = a + 6;
                        Height = b + 6;

                        IsHpr = false;
                        IsDualPlane = false;
                        break;

                    case 3:
                        if (((modeValue >> 5) & 1) == 0)
                        {
                            Width = 6;
                            Height = 10;
                        }
                        else
                        {
                            Width = 10;
                            Height = 6;
                        }
                        break;
                }
            }

            QuantizationMode = baseQuantizationMode - 2 + (IsHpr ? 6 : 0);
        }
    }
}
