using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_yuusha_shisu.PAC
{
	public class PacPlugin : IFilePlugin, IIdentifyFiles
	{
		public Guid PluginId => Guid.Parse("0066a5a4-1303-4673-bc7f-1742879c3562");
		public string[] FileExtensions => new[] { "*.pac" };
		public PluginMetadata Metadata { get; }

		public BtxPlugin()
		{
			Metadata = new PluginMetadata("Death of a Hero", "StorMyu");
		}

		public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
		{
			var fileStream = await fileSystem.OpenFileAsync(filePath);
			using (var br = new BinaryReaderX(fileStream))
				return br.ReadString(4) == "ARC\0";
		}

		public IPluginState CreatePluginState(IPluginManager pluginManager)
		{
			return new PacState();
		}
	}
}
