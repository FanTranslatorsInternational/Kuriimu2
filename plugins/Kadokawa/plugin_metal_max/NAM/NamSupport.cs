using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_metal_max.NAM
{
    public enum NamFile : int
    {
        ItemList = 12,
        EnemyList = 12
    }

    public sealed class ItemListArrEntry
    {
        public short Offset;
        public short Unk1;
        public int Unk2;
        public int Unk3;
    }

    public sealed class Entry
    {
        public short Offset;
        public int Index;
        public string Text;
    }
}
