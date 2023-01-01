using System;
using System.Buffers.Binary;
using System.IO;

namespace Kryptography.Hash
{
    public class SimpleHash : BaseHash<uint>
    {
        private readonly uint _magic;

        public SimpleHash(uint magic)
        {
            _magic = magic;
        }

        protected override uint CreateInitialValue()
        {
            return 0;
        }

        protected override void FinalizeResult(ref uint result)
        {
        }

        protected override void ComputeInternal(Span<byte> input, ref uint result)
        {
            foreach (var value in input)
                result = result * _magic + value;
        }

        protected override byte[] ConvertResult(uint result)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, result);

            return buffer;
        }
    }
}
