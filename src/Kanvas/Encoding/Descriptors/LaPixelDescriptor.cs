using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Komponent.Utilities;
using Kontract;
using Kontract.Kanvas;

namespace Kanvas.Encoding.Descriptors
{
    public class LaPixelDescriptor : IPixelDescriptor
    {
        private int[] _indexTable;
        private int[] _componentIndexTable;
        private int[] _depthTable;
        private int[] _shiftTable;
        private int[] _maskTable;

        private Func<int, int>[] _readBitDepthDelegates;
        private Func<int, int>[] _writeBitDepthDelegates;

        public LaPixelDescriptor(string componentOrder, int l, int a)
        {
            AssertValidOrder(componentOrder.ToLower());
            AssertBitDepth(l + a);

            SetupLookupTables(componentOrder, l, a);
        }

        public string GetPixelName()
        {
            var componentBuilder = new StringBuilder();
            var depthBuilder = new StringBuilder();
            var componentLetters = new[] { "L", "A", "X" };

            void AppendComponent(int level)
            {
                if (_depthTable[level] == 0)
                    return;

                componentBuilder.Append(componentLetters[_indexTable[level]]);
                depthBuilder.Append(_depthTable[level]);
            }

            AppendComponent(1);
            AppendComponent(0);

            return componentBuilder.ToString() + depthBuilder;
        }

        public int GetBitDepth()
        {
            return _depthTable[0] + _depthTable[1];
        }

        public Color GetColor(long value)
        {
            var colorBuffer = new int[2];

            colorBuffer[_indexTable[0]] = _readBitDepthDelegates[0](ReadComponent(value, _shiftTable[0], _maskTable[0]));
            colorBuffer[_indexTable[1]] = _readBitDepthDelegates[1](ReadComponent(value, _shiftTable[1], _maskTable[1]));

            // If luminance depth 0 then make color white
            if (_depthTable[_componentIndexTable[0]] == 0)
                colorBuffer[_indexTable[_componentIndexTable[0]]] = 255;

            // If alpha depth 0 then make color opaque
            if (_depthTable[_componentIndexTable[1]] == 0)
                colorBuffer[_indexTable[_componentIndexTable[1]]] = 255;

            return Color.FromArgb(colorBuffer[1], colorBuffer[0], colorBuffer[0], colorBuffer[0]);
        }

        public long GetValue(Color color)
        {
            var result = 0L;
            var colorBuffer = new[] { (int)(color.GetBrightness() * 255), color.A };

            var index = _componentIndexTable[0];
            WriteComponent(_writeBitDepthDelegates[index](colorBuffer[_indexTable[index]]), _shiftTable[index], _maskTable[index], ref result);

            index = _componentIndexTable[1];
            WriteComponent(_writeBitDepthDelegates[index](colorBuffer[_indexTable[index]]), _shiftTable[index], _maskTable[index], ref result);

            return result;
        }

        private void AssertValidOrder(string componentOrder)
        {
            ContractAssertions.IsNotNull(componentOrder, nameof(componentOrder));
            ContractAssertions.IsInRange(componentOrder.Length, nameof(componentOrder), 1, 2);

            if (!Regex.IsMatch(componentOrder, "^[lax]{1,2}$"))
                throw new InvalidOperationException($"'{componentOrder}' contains invalid characters.");

            if (componentOrder.Distinct().Count() != componentOrder.Length)
                throw new InvalidOperationException($"'{componentOrder}' contains duplicated characters.");
        }

        private void AssertBitDepth(int bitDepth)
        {
            ContractAssertions.IsInRange(bitDepth, nameof(bitDepth), 1, 32);
        }

        private void SetupLookupTables(string componentOrder, int l, int a)
        {
            void SetTableValues(int tableIndex, int colorBufferIndex, int depth, ref int shiftValue)
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
                        _readBitDepthDelegates[tableIndex] = value => 0;
                        _writeBitDepthDelegates[tableIndex] = value => 0;
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

            // Index lookup table holds the indices to the depth Values in order of reading
            _indexTable = new int[3];

            // Depth lookup table holds depth of components in order of reading
            _depthTable = new int[3];

            // Depth index table holds index into depth table in order LAX
            _componentIndexTable = new int[3];

            // Shift lookup table holds the shift Values for each depth in order of reading
            _shiftTable = new int[3];

            // Mask lookup table holds the bit mask to AND the shifted value with in order of reading
            _maskTable = new int[3];

            // Delegates to convert from one bit depth to another
            // Based on input and output bit depth, certain conditions can optimize the process
            _readBitDepthDelegates = new Func<int, int>[3];
            _writeBitDepthDelegates = new Func<int, int>[3];

            var shift = 0;
            var length = componentOrder.Length;
            bool lSet = false, aSet = false, xSet = false;

            for (var i = length - 1; i >= 0; i--)
            {
                switch (componentOrder[i])
                {
                    case 'l':
                    case 'L':
                        SetTableValues(length - i - 1, 0, l, ref shift);
                        lSet = true;
                        break;

                    case 'a':
                    case 'A':
                        SetTableValues(length - i - 1, 1, a, ref shift);
                        aSet = true;
                        break;

                    case 'x':
                    case 'X':
                        if (componentOrder.Length != 2)
                            throw new InvalidOperationException("Ignoring a component by X, can only be done if 2 components are given.");

                        SetTableValues(length - i - 1, 2, GetBitDepthOfMissingComponent(componentOrder, l, a), ref shift);
                        xSet = true;
                        break;
                }
            }

            if (!lSet) SetTableValues(length++, 0, 0, ref shift);
            if (!aSet) SetTableValues(length++, 1, 0, ref shift);
            if (!xSet) SetTableValues(length, 2, 0, ref shift);
        }

        private int ReadComponent(long value, int shift, int mask)
        {
            return (int)((value >> shift) & mask);
        }

        private void WriteComponent(int value, int shift, int mask, ref long result)
        {
            result |= (long)(value & mask) << shift;
        }

        private int GetBitDepthOfMissingComponent(string componentOrder, int l, int a)
        {
            bool lSet = false, aSet = false;
            foreach (var component in componentOrder)
            {
                switch (component)
                {
                    case 'l':
                    case 'L':
                        lSet = true;
                        break;

                    case 'a':
                    case 'A':
                        aSet = true;
                        break;
                }
            }

            if (!lSet) return l;
            if (!aSet) return a;

            // HINT: This case should never occur!
            throw new InvalidOperationException("No color component was marked as missing, but a missing color component was expected.");
        }
    }
}
