using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Komponent.Utilities;
using Kontract;
using Kontract.Kanvas;

namespace Kanvas.Encoding.Descriptors
{
    public class IndexPixelDescriptor : IPixelIndexDescriptor
    {
        private int[] _indexTable;
        private int[] _componentIndexTable;
        private int[] _depthTable;
        private int[] _shiftTable;
        private int[] _maskTable;

        private Func<long, int>[] _readActions;
        private Func<long, int, long>[] _writeActions;

        public IndexPixelDescriptor(string componentOrder, int i, int a)
        {
            AssertValidOrder(componentOrder.ToLower());
            AssertBitDepth(i + a);

            SetupLookupTables(componentOrder, i, a);
        }

        public string GetPixelName()
        {
            var componentBuilder = new StringBuilder();
            var depthBuilder = new StringBuilder();
            var componentLetters = new[] { "I", "A", "X" };

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

        public Color GetColor(long value, IList<Color> palette)
        {
            var colorBuffer = new int[3];

            colorBuffer[_indexTable[0]] = _readActions[0](value);
            colorBuffer[_indexTable[1]] = _readActions[1](value);

            // If alpha depth 0 then return color from palette
            if (_depthTable[_componentIndexTable[1]] == 0)
                return palette[colorBuffer[0]];

            var paletteColor = palette[colorBuffer[0]];
            return Color.FromArgb(colorBuffer[1], paletteColor.R, paletteColor.G, paletteColor.B);
        }

        public long GetValue(int index, IList<Color> palette)
        {
            var result = 0L;
            var colorBuffer = new[] { index, palette[index].A, 0 };

            var componentIndex = _componentIndexTable[0];
            result = _writeActions[componentIndex](result, colorBuffer[_indexTable[componentIndex]]);

            componentIndex = _componentIndexTable[1];
            result = _writeActions[componentIndex](result, colorBuffer[_indexTable[componentIndex]]);

            return result;
        }

        private void AssertValidOrder(string componentOrder)
        {
            ContractAssertions.IsNotNull(componentOrder, nameof(componentOrder));
            ContractAssertions.IsInRange(componentOrder.Length, nameof(componentOrder), 1, 2);

            if (!Regex.IsMatch(componentOrder, "^[iax]{1,2}$"))
                throw new InvalidOperationException($"'{componentOrder}' contains invalid characters.");

            if (componentOrder.Distinct().Count() != componentOrder.Length)
                throw new InvalidOperationException($"'{componentOrder}' contains duplicated characters.");
        }

        private void AssertBitDepth(int bitDepth)
        {
            ContractAssertions.IsInRange(bitDepth, nameof(bitDepth), 1, 16);
        }

        private void SetupLookupTables(string componentOrder, int i, int a)
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
            _indexTable = new int[3];

            // Depth lookup table holds depth of components in order of reading
            _depthTable = new int[3];

            // Depth index table holds index into depth table in order IAX
            _componentIndexTable = new int[3];

            // Shift lookup table holds the shift Values for each depth in order of reading
            _shiftTable = new int[3];

            // Mask lookup table holds the bit mask to AND the shifted value with in order of reading
            _maskTable = new int[3];

            // Table for the read/write action per index lookup
            _readActions = new Func<long, int>[3];
            _writeActions = new Func<long, int, long>[3];

            bool iSet = false, aSet = false, xSet = false;
            var shift = 0;
            var length = componentOrder.Length;
            for (var j = length - 1; j >= 0; j--)
            {
                var tableIndex = length - j - 1;
                switch (componentOrder[j])
                {
                    case 'i':
                    case 'I':
                        _readActions[tableIndex] = value =>
                            ReadIndexComponent(value, _shiftTable[tableIndex], _maskTable[tableIndex]);
                        _writeActions[tableIndex] = (result, value) =>
                            WriteIndexComponent(value, _shiftTable[tableIndex], _maskTable[tableIndex], ref result);

                        SetTableValues(tableIndex, 0, i, ref shift);
                        iSet = true;
                        break;

                    case 'a':
                    case 'A':
                        _readActions[tableIndex] = value =>
                            ReadComponent(value, _shiftTable[tableIndex], _maskTable[tableIndex], _depthTable[tableIndex]);
                        _writeActions[tableIndex] = (result, value) =>
                            WriteComponent(value, _shiftTable[tableIndex], _maskTable[tableIndex], _depthTable[tableIndex], ref result);

                        SetTableValues(tableIndex, 1, a, ref shift);
                        aSet = true;
                        break;

                    case 'x':
                    case 'X':
                        _readActions[tableIndex] = value =>
                            ReadComponent(value, _shiftTable[tableIndex], _maskTable[tableIndex], _depthTable[tableIndex]);
                        _writeActions[tableIndex] = (result, value) =>
                            WriteComponent(value, _shiftTable[tableIndex], _maskTable[tableIndex], _depthTable[tableIndex], ref result);

                        SetTableValues(tableIndex, 2, GetBitDepthOfMissingComponent(componentOrder, i, a), ref shift);
                        xSet = true;
                        break;
                }
            }

            if (!iSet) SetTableValues(length++, 0, 0, ref shift);
            if (!aSet) SetTableValues(length, 1, 0, ref shift);
            if (!xSet) SetTableValues(length, 2, 0, ref shift);
        }

        private int ReadComponent(long value, int shift, int mask, int depth)
        {
            return Conversion.ChangeBitDepth((int)((value >> shift) & mask), depth, 8);
        }

        private int ReadIndexComponent(long value, int shift, int mask)
        {
            return (int)((value >> shift) & mask);
        }

        private long WriteComponent(int value, int shift, int mask, int depth, ref long result)
        {
            return result |= (long)(Conversion.ChangeBitDepth(value, 8, depth) & mask) << shift;
        }

        private long WriteIndexComponent(int value, int shift, int mask, ref long result)
        {
            return result |= (long)(value & mask) << shift;
        }

        private int GetBitDepthOfMissingComponent(string componentOrder, int i, int a)
        {
            bool iSet = false, aSet = false;
            foreach (var component in componentOrder)
            {
                switch (component)
                {
                    case 'i':
                    case 'I':
                        iSet = true;
                        break;

                    case 'a':
                    case 'A':
                        aSet = true;
                        break;
                }
            }

            if (!iSet) return i;
            if (!aSet) return a;

            // HINT: This case should never occur!
            throw new InvalidOperationException("No color component was marked as missing, but a missing color component was expected.");
        }
    }
}
