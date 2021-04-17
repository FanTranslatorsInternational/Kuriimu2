using System;
using System.Buffers.Binary;

namespace Kryptography.Hash.Fnv
{
    public class Fnv1 : BaseHash<uint>
    {
        private const uint Initial = 0x811c9dc5;
        private const uint Prime = 0x1000193;

        public static Fnv1 Create()
        {
            return new Fnv1();
        }

        private Fnv1()
        {
        }

        protected override uint CreateInitialValue()
        {
            return Initial;
        }

        protected override void FinalizeResult(ref uint result)
        {
        }

        protected override void ComputeInternal(Span<byte> input, ref uint result)
        {
            foreach (var value in input)
                result = (result * Prime) ^ value;
        }

        protected override byte[] ConvertResult(uint result)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer,result);

            return buffer;
        }
    }
}
