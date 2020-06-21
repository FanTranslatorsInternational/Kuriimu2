using System;
using Komponent.IO.Attributes;

namespace plugin_tri_ace.Archives
{
    class PackHeader
    {
        [FixedLength(4)]
        public string magic = "P@CK";
        public short version = 3;
        public short fileCount;
    }

    class PackFileEntry
    {
        public int offset;
        public int fileType; // 2 = P@CK; 0x400 = mpak8
        public int unk0; // Maybe ID?
        public int zero0;
    }

    static class PackSupport
    {
        public static Guid[] RetrievePluginMapping(int fileType)
        {
            switch (fileType)
            {
                case 0x2:
                    return new[] { Guid.Parse("8c81d937-e1a8-42e6-910a-d9911a6a93af") };

                default:
                    return null;
            }
        }
    }
}
