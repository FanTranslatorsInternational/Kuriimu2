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
    class LaPixelDescriptor : IPixelDescriptor
    {
        private int[] _indexTable;
        private int[] _componentIndexTable;
        private int[] _depthTable;
        private int[] _shiftTable;
        private int[] _maskTable;

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
            var componentLetters = new[] { "L", "A" };

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

            colorBuffer[_indexTable[0]] = ReadComponent(value, _shiftTable[0], _maskTable[0], _depthTable[0]);
            colorBuffer[_indexTable[1]] = ReadComponent(value, _shiftTable[1], _maskTable[1], _depthTable[1]);

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
            WriteComponent(colorBuffer[_indexTable[index]], _shiftTable[index], _maskTable[index], _depthTable[index], ref result);

            index = _componentIndexTable[1];
            WriteComponent(colorBuffer[_indexTable[index]], _shiftTable[index], _maskTable[index], _depthTable[index], ref result);

            return result;
        }

        private void AssertValidOrder(string componentOrder)
        {
            ContractAssertions.IsNotNull(componentOrder, nameof(componentOrder));
            ContractAssertions.IsInRange(componentOrder.Length, nameof(componentOrder), 1, 2);

            if (!Regex.IsMatch(componentOrder, "^[la]{1,2}$"))
                throw new InvalidOperationException($"'{componentOrder}' contains invalid characters.");

            if (componentOrder.Distinct().Count() != componentOrder.Length)
                throw new InvalidOperationException($"'{componentOrder}' contains duplicated characters.");
        }

        private void AssertBitDepth(int bitDepth)
        {
            ContractAssertions.IsInRange(bitDepth, "bitDepth", 1, 32);

            if (!IsPowerOf2(bitDepth))
                throw new InvalidOperationException("Bit depth has to be a power of 2.");
        }

        private bool IsPowerOf2(int value)
        {
            return value != 0 && (value & (value - 1)) == 0;
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

                shiftValue += depth;
            }

            // Index lookup table holds the indices to the depth Values in order of reading
            _indexTable = new int[2];

            // Depth lookup table holds depth of components in order of reading
            _depthTable = new int[2];

            // Depth index table holds index into depth table in order LA
            _componentIndexTable = new int[2];

            // Shift lookup table holds the shift Values for each depth in order of reading
            _shiftTable = new int[2];

            // Mask lookup table holds the bit mask to AND the shifted value with in order of reading
            _maskTable = new int[2];

            var shift = 0;
            var length = componentOrder.Length;
            bool lSet = false, aSet = false;

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
                }
            }

            if (!lSet) SetTableValues(length++, 0, 0, ref shift);
            if (!aSet) SetTableValues(length, 1, 0, ref shift);
        }

        private int ReadComponent(long value, int shift, int mask, int depth)
        {
            return Conversion.ChangeBitDepth((int)((value >> shift) & mask), depth, 8);
        }

        private void WriteComponent(int value, int shift, int mask, int depth, ref long result)
        {
            result |= (long)(Conversion.ChangeBitDepth(value, 8, depth) & mask) << shift;
        }
    }
}
