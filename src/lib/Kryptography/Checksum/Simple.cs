using System.Buffers.Binary;

namespace Kryptography.Checksum
{
    public class Simple(uint magic) : Checksum<uint>
    {
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
                result = result * magic + value;
        }

        protected override byte[] ConvertResult(uint result)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, result);

            return buffer;
        }
    }
}
