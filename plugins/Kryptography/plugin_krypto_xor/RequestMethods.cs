using Komponent.IO;
using System;
using Kontract.Models.Intermediate;

namespace plugin_krypto_xor
{
    internal static class RequestMethods
    {
        public static byte[] RequestKey(Action<RequestDataEventArgs> requestEvent, string message, int keyLength, string requestId, out string error)
        {
            error = string.Empty;

            var eventArgs = new RequestDataEventArgs(message, keyLength < 0 ? -1 : keyLength * 2, false, requestId);
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
