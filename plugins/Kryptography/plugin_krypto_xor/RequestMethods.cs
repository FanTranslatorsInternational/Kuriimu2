using Komponent.IO;
using Kontract.Interfaces.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace plugin_krypto_xor
{
    internal static class RequestMethods
    {
        public static byte[] RequestKey(Action<RequestDataEventArgs> requestEvent, string message, int keyLength, out string error)
        {
            error = string.Empty;

            var eventArgs = new RequestDataEventArgs(message, keyLength < 0 ? -1 : keyLength * 2, false);
            requestEvent(eventArgs);

            if (eventArgs.Data == null)
            {
                error = "Data not given.";
                return null;
            }
            if (keyLength >= 0 && eventArgs.Data.Length != keyLength * 2)
            {
                error = "Data has unexpected length.";
                return null;
            }

            return eventArgs.Data.Hexlify();
        }
    }
}
