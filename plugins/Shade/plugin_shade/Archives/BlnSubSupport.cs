using Konnect.Contract.DataClasses.Plugin.File.Archive;

namespace plugin_shade.Archives
{
	// Archive index maps to the following bins for Inazuma Eleven Strikers 2013
	// 0x00 => grp.bin
        // 0x01 => scn.bin
        // 0x02 => scn_sh.bin
        // 0x03 => ui.bin
        // 0x04 => dat.bin
	
	// For Suzumiya Haruhi no Heiretsu (https://github.com/jonko0493/HaruhiHeiretsuEditor/blob/main/KuriimuBlnPort/BlnSupport/Archives/BlnSubSupport.cs)
	// 0x00 => grp.bin
	// 0x01 => dat.bin
	// 0x02 => scr.bin
    // 0x03 => evt.bin
	
    class BlnSubEntry
    {
        public int archiveIndex;    // index to an external bin
        public int archiveOffset;   // offset into that external bin
        public int size;
    }


    class BlnSubArchiveFile : ShadeArchiveFile
    {
        public BlnSubEntry Entry { get; }

        public BlnSubArchiveFile(ArchiveFileInfo fileInfo, BlnSubEntry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }
}
