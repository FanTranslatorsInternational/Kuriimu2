using System;
using System.Runtime.Serialization;

namespace Kontract
{
    /// <summary>
    /// Exception thrown by plugins trying to load other plugins.
    /// </summary>
    [Serializable]
    public class PluginNotFoundException : Exception
    {
        public string Adapter { get; private set; }

        public override string Message => $"The {Adapter} could not be found.";

        //public PluginNotFoundException() { }
        public PluginNotFoundException(string adapter) { Adapter = adapter; }
        //public PluginNotFoundException(string message) : base(message) { }
        //public PluginNotFoundException(string message, Exception innerException) : base(message, innerException) { }
        protected PluginNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
