using System;
using System.Collections.Generic;
using System.Text;

namespace Komponent.Font
{
    class BinPackerNode
    {
        public BinPackerNode rightNode;
        public BinPackerNode bottomNode;
        public double posX;
        public double posZ;
        public double width;
        public double height;
        public bool isOccupied;
    }
}
