using Komponent.IO;
using System;
using System.Text.RegularExpressions;
using Kontract.Models.Intermediate;

namespace plugin_krypto_aes
{
    internal static class RequestMethods
    {
        public static byte[] RequestKey(Action<RequestDataEventArgs> requestEvent, string message, int keyLength, string requestId, out string error)
        {
            error = string.Empty;

            var eventArgs = new RequestDataEventArgs(message, keyLength * 2, false, requestId);
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

        public static long RequestNumber(Action<RequestDataEventArgs> requestEvent, string message, long defaultValue, string requestId, out string error)
        {
            error = string.Empty;

            var eventArgs = new RequestDataEventArgs(message, -1, false, requestId);
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

        public static string RequestFile(Action<RequestDataEventArgs> requestEvent, string message, string requestId, out string error)
        {
            error = string.Empty;

            var eventArgs = new RequestDataEventArgs(message, -1, true, requestId);
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
