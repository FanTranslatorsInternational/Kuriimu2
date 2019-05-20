using System;
using Kontract.Models.Intermediate;

namespace plugin_krypto_nintendo
{
    internal static class RequestMethods
    {
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
