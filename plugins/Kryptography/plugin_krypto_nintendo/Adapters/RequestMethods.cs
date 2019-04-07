using Komponent.IO;
using Kontract.Interfaces.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Adapters
{
    internal static class RequestMethods
    {
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
