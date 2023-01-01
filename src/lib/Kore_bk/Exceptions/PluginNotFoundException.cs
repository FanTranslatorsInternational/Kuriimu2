﻿using System;
using System.Runtime.Serialization;
using Kontract.Interfaces.Plugins.Loaders;

namespace Kore.Exceptions
{
    /// <summary>
    /// Exception thrown by <see cref="IPluginLoader"/> trying to load non-existing plugin.
    /// </summary>
    [Serializable]
    public class PluginNotFoundException : Exception
    {
        /// <summary>
        /// Name of the plugin that was not found.
        /// </summary>
        public string Adapter { get; }

        /// <summary>
        /// Creates a new instance of <see cref="PluginNotFoundException"/>.
        /// </summary>
        /// <param name="adapter">Name of the plugin not found.</param>
        public PluginNotFoundException(string adapter) : base($"Adapter {adapter} could not be found.")
        {
            Adapter = adapter;
        }

        /// <inheritdoc cref="ISerializable.GetObjectData(SerializationInfo,StreamingContext)"/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Adapter), Adapter);
            base.GetObjectData(info, context);
        }
    }
}
