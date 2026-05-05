using System.Text;
using System.Text.RegularExpressions;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding.Descriptor;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Encoding.Descriptors
{
    public partial class RgbaPixelDescriptor : IPixelDescriptor
    {
        [GeneratedRegex("^[rgbax]{1,4}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex ComponentOrderRegex();

        // Index lookup table holds the indices to the depth Values in order of reading
        private readonly int[] _indexTable = new int[5];
        // Depth index table holds index into depth table in order ARGBX
        private readonly int[] _componentIndexTable = new int[5];
        // Depth lookup table holds depth of components in order of reading
        private readonly int[] _depthTable = new int[5];
        // Shift lookup table holds the shift Values for each depth in order of reading
        private readonly int[] _shiftTable = new int[5];
        // Mask lookup table holds the bit mask to AND the shifted value with in order of reading
        private readonly int[] _maskTable = new int[5];

        // Delegates to convert from one bit depth to another
        // Based on input and output bit depth, certain conditions can optimize the process
        private readonly Func<int, int>[] _readBitDepthDelegates = new Func<int, int>[5];
        private readonly Func<int, int>[] _writeBitDepthDelegates = new Func<int, int>[5];

        public RgbaPixelDescriptor(string componentOrder, int r, int g, int b, int a)
        {
            AssertValidOrder(componentOrder.ToLower());
            AssertBitDepth(r + g + b + a);

            SetupLookupTables(componentOrder, r, g, b, a);
        }

        public string GetPixelName()
        {
            var componentBuilder = new StringBuilder();
            var depthBuilder = new StringBuilder();
            var componentLetters = new[] { "A", "R", "G", "B", "X" };

            void AppendComponent(int level)
            {
                if (_depthTable[level] == 0)
                    return;

                componentBuilder.Append(componentLetters[_indexTable[level]]);
                depthBuilder.Append(_depthTable[level]);
            }

            AppendComponent(3);
            AppendComponent(2);
            AppendComponent(1);
            AppendComponent(0);

            return componentBuilder.ToString() + depthBuilder;
        }

        public int GetBitDepth()
        {
            return _depthTable[0] + _depthTable[1] + _depthTable[2] + _depthTable[3];
        }

        public ColorChannelBitDepths GetColorChannelBitDepths()
        {
            return new ColorChannelBitDepths(
                GetComponentDepth(1),
                GetComponentDepth(2),
                GetComponentDepth(3),
                GetComponentDepth(0));
        }

        public Rgba32 GetColor(long value)
        {
            // colorBuffer[4] is reserved for the X color component, and will be ignored when the color is constructed
            var colorBuffer = new int[5];

            colorBuffer[_indexTable[0]] = _readBitDepthDelegates[0](ReadComponent(value, _shiftTable[0], _maskTable[0]));
            colorBuffer[_indexTable[1]] = _readBitDepthDelegates[1](ReadComponent(value, _shiftTable[1], _maskTable[1]));
            colorBuffer[_indexTable[2]] = _readBitDepthDelegates[2](ReadComponent(value, _shiftTable[2], _maskTable[2]));
            colorBuffer[_indexTable[3]] = _readBitDepthDelegates[3](ReadComponent(value, _shiftTable[3], _maskTable[3]));

            // If alpha depth 0 then make color opaque
            if (_depthTable[_componentIndexTable[0]] == 0)
                colorBuffer[_indexTable[_componentIndexTable[0]]] = 255;

            return new Rgba32((byte)colorBuffer[1], (byte)colorBuffer[2], (byte)colorBuffer[3], (byte)colorBuffer[0]);
        }

        public long GetValue(Rgba32 color)
        {
            // colorBuffer[4] is reserved for the X color component
            var colorBuffer = new[] { color.A, color.R, color.G, color.B, 0 };

            var result = 0L;

            var index = _componentIndexTable[0];
            WriteComponent(_writeBitDepthDelegates[index](colorBuffer[_indexTable[index]]), _shiftTable[index], _maskTable[index], ref result);

            index = _componentIndexTable[1];
            WriteComponent(_writeBitDepthDelegates[index](colorBuffer[_indexTable[index]]), _shiftTable[index], _maskTable[index], ref result);

            index = _componentIndexTable[2];
            WriteComponent(_writeBitDepthDelegates[index](colorBuffer[_indexTable[index]]), _shiftTable[index], _maskTable[index], ref result);

            index = _componentIndexTable[3];
            WriteComponent(_writeBitDepthDelegates[index](colorBuffer[_indexTable[index]]), _shiftTable[index], _maskTable[index], ref result);

            return result;
        }

        private static void AssertValidOrder(string componentOrder)
        {
            if (componentOrder.Length is < 1 or > 4)
                throw new ArgumentOutOfRangeException(nameof(componentOrder), "Value needs to be in range 1..4");

            if (!ComponentOrderRegex().IsMatch(componentOrder))
                throw new InvalidOperationException($"'{componentOrder}' contains invalid characters.");

            if (componentOrder.Distinct().Count() != componentOrder.Length)
                throw new InvalidOperationException($"'{componentOrder}' contains duplicated characters.");
        }

        private static void AssertBitDepth(int bitDepth)
        {
            if (bitDepth is < 4 or > 32)
                throw new ArgumentOutOfRangeException(nameof(bitDepth), "Value needs to be in range 4..32");
            
        }

        private void SetupLookupTables(string componentOrder, int r, int g, int b, int a)
        {
            var shift = 0;
            var length = componentOrder.Length;
            bool rSet = false, bSet = false, gSet = false, aSet = false, xSet = false;

            for (var i = length - 1; i >= 0; i--)
            {
                switch (componentOrder[i])
                {
                    case 'r':
                    case 'R':
                        SetTableValues(length - i - 1, 1, r, ref shift);
                        rSet = true;
                        break;

                    case 'g':
                    case 'G':
                        SetTableValues(length - i - 1, 2, g, ref shift);
                        gSet = true;
                        break;

                    case 'b':
                    case 'B':
                        SetTableValues(length - i - 1, 3, b, ref shift);
                        bSet = true;
                        break;

                    case 'a':
                    case 'A':
                        SetTableValues(length - i - 1, 0, a, ref shift);
                        aSet = true;
                        break;

                    case 'x':
                    case 'X':
                        if (componentOrder.Length != 4)
                            throw new InvalidOperationException("Ignoring a component by X, can only be done if 4 color components are given.");

                        SetTableValues(length - i - 1, 4, GetBitDepthOfMissingComponent(componentOrder, r, g, b, a), ref shift);
                        xSet = true;
                        break;
                }
            }

            if (!rSet) SetTableValues(length++, 1, 0, ref shift);
            if (!gSet) SetTableValues(length++, 2, 0, ref shift);
            if (!bSet) SetTableValues(length++, 3, 0, ref shift);
            if (!aSet) SetTableValues(length++, 0, 0, ref shift);
            if (!xSet) SetTableValues(length, 4, 0, ref shift);
        }

        private void SetTableValues(int tableIndex, int colorBufferIndex, int depth, ref int shiftValue)
        {
            _indexTable[tableIndex] = colorBufferIndex;
            _depthTable[tableIndex] = depth;
            _componentIndexTable[colorBufferIndex] = tableIndex;
            _shiftTable[tableIndex] = shiftValue;
            _maskTable[tableIndex] = (1 << depth) - 1;

            if (depth <= 8)
            {
                if (depth == 0)
                {
                    _readBitDepthDelegates[tableIndex] = _ => 0;
                    _writeBitDepthDelegates[tableIndex] = _ => 0;
                }
                else
                {
                    _readBitDepthDelegates[tableIndex] = value => Conversion.UpscaleBitDepth(value, depth);
                    _writeBitDepthDelegates[tableIndex] = value => Conversion.DownscaleBitDepth(value, depth);
                }
            }
            else
            {
                _readBitDepthDelegates[tableIndex] = value => Conversion.DownscaleBitDepth(value, depth, 8);
                _writeBitDepthDelegates[tableIndex] = value => Conversion.UpscaleBitDepth(value, 8, depth);
            }

            shiftValue += depth;
        }

        private static int ReadComponent(long value, int shift, int mask)
        {
            return (int) ((value >> shift) & mask);
        }

        private static void WriteComponent(int value, int shift, int mask, ref long result)
        {
            result |= (long)(value & mask) << shift;
        }

        private static int GetBitDepthOfMissingComponent(string componentOrder, int r, int g, int b, int a)
        {
            bool rSet = false, bSet = false, gSet = false, aSet = false;
            foreach (var component in componentOrder)
            {
                switch (component)
                {
                    case 'r':
                    case 'R':
                        rSet = true;
                        break;

                    case 'g':
                    case 'G':
                        gSet = true;
                        break;

                    case 'b':
                    case 'B':
                        bSet = true;
                        break;

                    case 'a':
                    case 'A':
                        aSet = true;
                        break;
                }
            }

            if (!rSet) return r;
            if (!gSet) return g;
            if (!bSet) return b;
            if (!aSet) return a;

            // HINT: This case should never occur!
            throw new InvalidOperationException("No color component was marked as missing, but a missing color component was expected.");
        }

        private int GetComponentDepth(int componentColorBufferIndex)
        {
            int depthIndex = _componentIndexTable[componentColorBufferIndex];
            return _depthTable[depthIndex];
        }
    }
}
