using Komponent.IO;
using Kontract.Interfaces.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace plugin_krypto_aes
{
    internal static class RequestMethods
    {
        public static byte[] RequestKey(Action<RequestDataEventArgs> requestEvent, string message, int keyLength, out string error)
        {
            error = string.Empty;

            var eventArgs = new RequestDataEventArgs(message, keyLength * 2, false);
            requestEvent(eventArgs);

            if (eventArgs.Data == null)
            {
                error = "Data not given.";
                return null;
            }
            if (eventArgs.Data.Length != keyLength * 2)
            {
                error = "Data has unexpected length.";
                return null;
            }

            return eventArgs.Data.Hexlify();
        }

        public static long RequestNumber(Action<RequestDataEventArgs> requestEvent, string message, long defaultValue, out string error)
        {
            error = string.Empty;

            var eventArgs = new RequestDataEventArgs(message, -1, false);
            requestEvent(eventArgs);

            if (eventArgs.Data == null)
            {
                error = "Data not given.";
                return defaultValue;
            }
            if (!Regex.IsMatch(eventArgs.Data, "^\\d+$"))
            {
                error = "Data is no number.";
                return defaultValue;
            }

            return Convert.ToInt64(eventArgs.Data);
        }

        public static string RequestFile(Action<RequestDataEventArgs> requestEvent, string message, out string error)
        {
            error = string.Empty;

            var eventArgs = new RequestDataEventArgs(message, -1, true);
            requestEvent(eventArgs);

            if (eventArgs.Data == null)
            {
                error = "Data not given.";
                return null;
            }

            return eventArgs.Data;
        }
    }
}
