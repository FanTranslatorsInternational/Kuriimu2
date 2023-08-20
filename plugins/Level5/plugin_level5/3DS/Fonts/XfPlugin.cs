﻿using System;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Plugins.Entry;

namespace plugin_level5._3DS.Fonts
{
    public class XfPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("b1b397c4-9a02-4828-b568-39cad733fa3a");
        public PluginType PluginType => PluginType.Font;
        public string[] FileExtensions => new[] { "*.xf" };
        public PluginMetadata Metadata { get; }

        public XfPlugin()
        {
            Metadata = new PluginMetadata("XF", "onepiecefreak", "Font for 3DS Level-5 games");
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new XfState(pluginManager);
        }
    }
}
