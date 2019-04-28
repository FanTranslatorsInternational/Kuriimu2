using System;
using Kanvas.Models;

namespace Kanvas.Support
{
    internal static class Convert
    {
        public static TOut FromByteArray<TOut>(byte[] input, ByteOrder byteOrder)
        {
            var outType = typeof(TOut);

            if (!outType.IsPrimitive)
                throw new InvalidOperationException($"Type is not supported in this method.");

            var typeCode = Type.GetTypeCode(outType);
            switch (typeCode)
            {
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return ToNumber<TOut>(input, 2, byteOrder);
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return ToNumber<TOut>(input, 4, byteOrder);
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return ToNumber<TOut>(input, 8, byteOrder);
                default:
                    throw new InvalidOperationException($"{typeCode} is not supported.");
            }
        }

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

        private static T ToNumber<T>(byte[] data, int limit, ByteOrder byteOrder)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit));
            if (data.Length > limit)
                throw new ArgumentOutOfRangeException(nameof(data));

            ulong result = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (byteOrder == ByteOrder.LittleEndian)
                    result |= (ulong)(data[i] << (i * 8));
                else
                    result = (result << 8) | data[i];
            }

            var typeCode = Type.GetTypeCode(typeof(T));
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

            return (T)System.Convert.ChangeType(result, typeof(T));
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

        public static int ChangeBitDepth(int value, int bitDepthFrom, int bitDepthTo)
        {
            if (bitDepthTo < 0 || bitDepthFrom < 0)
                throw new Exception("BitDepths can't be negative!");
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
