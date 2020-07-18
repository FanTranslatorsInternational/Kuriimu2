using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_nintendo.Archives
{
    public class WiiDiscPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("e5a2f369-2daa-4575-ae4f-f980aac8f2c3");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.cgrp" };
        public PluginMetadata Metadata { get; }

        public WiiDiscPlugin()
        {
            Metadata = new PluginMetadata("WiiDisc", "onepiecefreak", "The disc format for the Nintendo Wii");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new WiiDiscState();
        }
    }
}
