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
    class RgbaPixelDescriptor : IPixelDescriptor
    {
        private int[] _indexTable;
        private int[] _componentIndexTable;
        private int[] _depthTable;
        private int[] _shiftTable;
        private int[] _maskTable;

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

        public Color GetColor(long value)
        {
            // colorBuffer[4] is reserved for the X color component, and will be ignored when the color is constructed
            var colorBuffer = new int[5];

            colorBuffer[_indexTable[0]] = ReadComponent(value, _shiftTable[0], _maskTable[0], _depthTable[0]);
            colorBuffer[_indexTable[1]] = ReadComponent(value, _shiftTable[1], _maskTable[1], _depthTable[1]);
            colorBuffer[_indexTable[2]] = ReadComponent(value, _shiftTable[2], _maskTable[2], _depthTable[2]);
            colorBuffer[_indexTable[3]] = ReadComponent(value, _shiftTable[3], _maskTable[3], _depthTable[3]);

            // If alpha depth 0 then make color opaque
            if (_depthTable[_componentIndexTable[0]] == 0)
                colorBuffer[_indexTable[_componentIndexTable[0]]] = 255;

            return Color.FromArgb(colorBuffer[0], colorBuffer[1], colorBuffer[2], colorBuffer[3]);
        }

        public long GetValue(Color color)
        {
            // colorBuffer[4] is reserved for the X color component
            var colorBuffer = new[] { color.A, color.R, color.G, color.B, 0 };

            var result = 0L;

            var index = _componentIndexTable[0];
            WriteComponent(colorBuffer[_indexTable[index]], _shiftTable[index], _maskTable[index], _depthTable[index], ref result);

            index = _componentIndexTable[1];
            WriteComponent(colorBuffer[_indexTable[index]], _shiftTable[index], _maskTable[index], _depthTable[index], ref result);

            index = _componentIndexTable[2];
            WriteComponent(colorBuffer[_indexTable[index]], _shiftTable[index], _maskTable[index], _depthTable[index], ref result);

            index = _componentIndexTable[3];
            WriteComponent(colorBuffer[_indexTable[index]], _shiftTable[index], _maskTable[index], _depthTable[index], ref result);

            return result;
        }

        private void AssertValidOrder(string componentOrder)
        {
            ContractAssertions.IsNotNull(componentOrder, nameof(componentOrder));
            ContractAssertions.IsInRange(componentOrder.Length, nameof(componentOrder), 1, 4);

            if (!Regex.IsMatch(componentOrder, "^[rgbax]{1,4}$"))
                throw new InvalidOperationException($"'{componentOrder}' contains invalid characters.");

            if (componentOrder.Distinct().Count() != componentOrder.Length)
                throw new InvalidOperationException($"'{componentOrder}' contains duplicated characters.");
        }

        private void AssertBitDepth(int bitDepth)
        {
            ContractAssertions.IsInRange(bitDepth, "bitDepth", 4, 32);
        }

        private void SetupLookupTables(string componentOrder, int r, int g, int b, int a)
        {
            void SetTableValues(int tableIndex, int colorBufferIndex, int depth, ref int shiftValue)
            {
                _indexTable[tableIndex] = colorBufferIndex;
                _depthTable[tableIndex] = depth;
                _componentIndexTable[colorBufferIndex] = tableIndex;
                _shiftTable[tableIndex] = shiftValue;
                _maskTable[tableIndex] = (1 << depth) - 1;

                shiftValue += depth;
            }

            // Index lookup table holds the indices to the depth Values in order of reading
            _indexTable = new int[5];

            // Depth lookup table holds depth of components in order of reading
            _depthTable = new int[5];

            // Depth index table holds index into depth table in order ARGBX
            _componentIndexTable = new int[5];

            // Shift lookup table holds the shift Values for each depth in order of reading
            _shiftTable = new int[5];

            // Mask lookup table holds the bit mask to AND the shifted value with in order of reading
            _maskTable = new int[5];

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

        private int ReadComponent(long value, int shift, int mask, int depth)
        {
            return Conversion.ChangeBitDepth((int)((value >> shift) & mask), depth, 8);
        }

        private void WriteComponent(int value, int shift, int mask, int depth, ref long result)
        {
            result |= (long)(Conversion.ChangeBitDepth(value, 8, depth) & mask) << shift;
        }

        private int GetBitDepthOfMissingComponent(string componentOrder, int r, int g, int b, int a)
        {
            bool rSet = false, bSet = false, gSet = false, aSet = false, xSet = false;
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
    }
}
