using System.Buffers.Binary;

namespace Kryptography.Checksum
{
    public class Simple : Checksum<uint>
    {
        private readonly uint _magic;

        public Simple(uint magic)
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

        public override void ComputeBlock(Span<byte> input, ref uint result)
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
