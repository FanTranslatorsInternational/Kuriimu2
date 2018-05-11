using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Komponent.IO
{
    public static class Tools
    {
        // Endian Swapping Support
        public static void AdjustByteOrder(Type type, byte[] buffer, ByteOrder byteOrder, int startOffset = 0)
        {
            if (BitConverter.IsLittleEndian == (byteOrder == ByteOrder.LittleEndian)) return;

            if (type.IsPrimitive)
            {
                if (type == typeof(short) || type == typeof(ushort) ||
                    type == typeof(int) || type == typeof(uint) ||
                    type == typeof(long) || type == typeof(ulong))
                {
                    Array.Reverse(buffer);
                    return;
                }
            }

            foreach (var field in type.GetFields())
            {
                var fieldType = field.FieldType;

                // Ignore static fields
                if (field.IsStatic) continue;

                // Ignore the ByteOrder enum type or else we break byte order detection
                if (fieldType.BaseType == typeof(Enum) && fieldType != typeof(ByteOrder))
                    fieldType = fieldType.GetFields()[0].FieldType;

                // Swap bytes only for the following types
                // TODO: Add missing types to this list
                if (fieldType == typeof(short) || fieldType == typeof(ushort) ||
                    fieldType == typeof(int) || fieldType == typeof(uint) ||
                    fieldType == typeof(long) || fieldType == typeof(ulong))
                {
                    var offset = Marshal.OffsetOf(type, field.Name).ToInt32();

                    // Enums
                    if (fieldType.IsEnum)
                        fieldType = Enum.GetUnderlyingType(fieldType);

                    // Check for sub-fields to recurse if necessary
                    var subFields = fieldType.GetFields().Where(subField => subField.IsStatic == false).ToArray();
                    var effectiveOffset = startOffset + offset;

                    if (subFields.Length == 0)
                        Array.Reverse(buffer, effectiveOffset, Marshal.SizeOf(fieldType));
                    else
                        AdjustByteOrder(fieldType, buffer, byteOrder, effectiveOffset);
                }
            }
        }
    }
}
