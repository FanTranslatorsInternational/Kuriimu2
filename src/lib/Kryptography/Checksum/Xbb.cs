using System.Buffers.Binary;

namespace Kryptography.Checksum
{
    /// <summary>
    /// The XBB hash implementation.
    /// </summary>
    /// <remarks>This hash implementation is not thread-safe.</remarks>
    public class Xbb : Checksum<uint>
    {
        private int _seed;

        protected override uint CreateInitialValue()
        {
            _seed = 0;
            return 0;
        }

        protected override void FinalizeResult(ref uint result)
        {
        }

        public override void ComputeBlock(Span<byte> input, ref uint result)
        {
            foreach (var value in input)
            {
                _seed += value;
                result += (uint)(value << _seed | value >> -_seed);
            }
        }

        protected override byte[] ConvertResult(uint result)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, result);

            return buffer;
        }
    }
}
