using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.Extensions
{
    internal static class StreamExtensions
    {
        public static byte[] ToArray(this Stream input)
        {
            if (input is MemoryStream ms)
                return ms.ToArray();

            var bkPos = input.Position;
            input.Position = 0;
            var buffer = new byte[input.Length];
            input.Read(buffer, 0, buffer.Length);
            input.Position = bkPos;

            return buffer;
        }
    }
}
