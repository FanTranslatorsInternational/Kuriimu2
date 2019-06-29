using System;
using System.Linq;
using Kore.Utilities.Models;

namespace Kore.Utilities
{
    public static class Convert
    {
        #region From byte[]
        public static TOut FromByteArray<TOut>(byte[] input, ByteOrder byteOrder)
        {
            if (!typeof(TOut).IsPrimitive)
                throw new InvalidOperationException($"Type is not supported in this method.");

            if (TryToNumber<TOut>(input, byteOrder, out var res))
                return res;

            return default(TOut);
        }

        private static bool TryToNumber<T>(byte[] data, ByteOrder byteOrder, out T result)
        {
            result = default(T);

            var typeCode = Type.GetTypeCode(typeof(T));
            object value;
            byte[] buffer;
            switch (typeCode)
            {
                case TypeCode.Int16:
                    value = UseBitConverter(data, byteOrder, 2, BitConverter.ToInt16);
                    break;
                case TypeCode.UInt16:
                    value = UseBitConverter(data, byteOrder, 2, BitConverter.ToUInt16);
                    break;
                case TypeCode.Int32:
                    value = UseBitConverter(data, byteOrder, 4, BitConverter.ToInt32);
                    break;
                case TypeCode.UInt32:
                    value = UseBitConverter(data, byteOrder, 4, BitConverter.ToUInt32);
                    break;
                case TypeCode.Int64:
                    value = UseBitConverter(data, byteOrder, 8, BitConverter.ToInt64);
                    break;
                case TypeCode.UInt64:
                    value = UseBitConverter(data, byteOrder, 8, BitConverter.ToUInt64);
                    break;
                default:
                    return false;
            }

            result = (T)System.Convert.ChangeType(value, typeof(T));
            return true;
        }

        private static T UseBitConverter<T>(byte[] input, ByteOrder byteOrder, int arraySize, Func<byte[], int, T> func)
        {
            if (input.Length <= 0)
                throw new ArgumentOutOfRangeException(nameof(input));

            byte[] buffer = new byte[arraySize];
            if (byteOrder == ByteOrder.LittleEndian)
            {
                Array.Copy(input, 0, buffer, 0, Math.Min(arraySize, input.Length));
            }
            else
            {
                var lengthToCopy = Math.Min(arraySize, input.Length);
                Array.Copy(input, input.Length - lengthToCopy, buffer, arraySize - lengthToCopy, lengthToCopy);
            }

            if (BitConverter.IsLittleEndian && byteOrder == ByteOrder.LittleEndian ||
                !BitConverter.IsLittleEndian && byteOrder == ByteOrder.BigEndian)
                return func(buffer, 0);

            return func(buffer.Reverse().ToArray(), 0);
        }
        #endregion

        #region To byte[]

        public static byte[] ToByteArray<TIn>(TIn input, int limit, ByteOrder byteOrder)
        {
            var inType = typeof(TIn);

            if (!inType.IsPrimitive)
                throw new InvalidOperationException($"Type is not supported in this method.");

            var typeCode = Type.GetTypeCode(inType);
            switch (typeCode)
            {
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return FromInteger(input, limit, byteOrder);
                default:
                    throw new InvalidOperationException($"{typeCode} is not supported.");
            }
        }

        private static byte[] FromInteger<T>(T input, int limit, ByteOrder byteOrder)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            var result = new byte[limit];

            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.Int16:
                    var res1 = System.Convert.ToInt16(input);
                    var localLimit = Math.Min(2, limit);
                    for (int i = 0; i < localLimit; i++)
                    {
                        if (byteOrder == ByteOrder.LittleEndian)
                            result[i] = (byte)((res1 >> (i * 8)) & 0xFF);
                        else
                            result[i] = (byte)((res1 >> ((localLimit - i - 1) * 8)) & 0xFF);
                    }
                    break;
                case TypeCode.UInt16:
                    var res2 = System.Convert.ToUInt16(input);
                    localLimit = Math.Min(2, limit);
                    for (int i = 0; i < localLimit; i++)
                    {
                        if (byteOrder == ByteOrder.LittleEndian)
                            result[i] = (byte)((res2 >> (i * 8)) & 0xFF);
                        else
                            result[i] = (byte)((res2 >> ((localLimit - i - 1) * 8)) & 0xFF);
                    }
                    break;
                case TypeCode.Int32:
                    var res3 = System.Convert.ToInt32(input);
                    localLimit = Math.Min(4, limit);
                    for (int i = 0; i < localLimit; i++)
                    {
                        if (byteOrder == ByteOrder.LittleEndian)
                            result[i] = (byte)((res3 >> (i * 8)) & 0xFF);
                        else
                            result[i] = (byte)((res3 >> ((localLimit - i - 1) * 8)) & 0xFF);
                    }
                    break;
                case TypeCode.UInt32:
                    var res4 = System.Convert.ToUInt32(input);
                    localLimit = Math.Min(4, limit);
                    for (int i = 0; i < localLimit; i++)
                    {
                        if (byteOrder == ByteOrder.LittleEndian)
                            result[i] = (byte)((res4 >> (i * 8)) & 0xFF);
                        else
                            result[i] = (byte)((res4 >> ((localLimit - i - 1) * 8)) & 0xFF);
                    }
                    break;
                case TypeCode.Int64:
                    var res5 = System.Convert.ToInt64(input);
                    localLimit = Math.Min(8, limit);
                    for (int i = 0; i < localLimit; i++)
                    {
                        if (byteOrder == ByteOrder.LittleEndian)
                            result[i] = (byte)((res5 >> (i * 8)) & 0xFF);
                        else
                            result[i] = (byte)((res5 >> ((localLimit - i - 1) * 8)) & 0xFF);
                    }
                    break;
                case TypeCode.UInt64:
                    var res6 = System.Convert.ToUInt64(input);
                    localLimit = Math.Min(8, limit);
                    for (int i = 0; i < localLimit; i++)
                    {
                        if (byteOrder == ByteOrder.LittleEndian)
                            result[i] = (byte)((res6 >> (i * 8)) & 0xFF);
                        else
                            result[i] = (byte)((res6 >> ((localLimit - i - 1) * 8)) & 0xFF);
                    }
                    break;
            }

            return result;
        }
        #endregion

        public static int ChangeBitDepth(int value, int bitDepthFrom, int bitDepthTo)
        {
            if (bitDepthTo < 0)
                throw new ArgumentOutOfRangeException(nameof(bitDepthTo));
            if (bitDepthFrom < 0)
                throw new ArgumentOutOfRangeException(nameof(bitDepthFrom));
            if (bitDepthFrom == 0 || bitDepthTo == 0)
                return 0;
            if (bitDepthFrom == bitDepthTo)
                return value;

            if (bitDepthFrom < bitDepthTo)
            {
                var fromMaxRange = (1 << bitDepthFrom) - 1;
                var toMaxRange = (1 << bitDepthTo) - 1;

                var div = 1;
                while (toMaxRange % fromMaxRange != 0)
                {
                    div <<= 1;
                    toMaxRange = ((toMaxRange + 1) << 1) - 1;
                }

                return value * (toMaxRange / fromMaxRange) / div;
            }
            else
            {
                var fromMax = 1 << bitDepthFrom;
                var toMax = 1 << bitDepthTo;

                var limit = fromMax / toMax;

                return value / limit;
            }
        }
    }
}
