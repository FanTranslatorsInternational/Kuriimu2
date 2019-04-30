using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO.Attributes;

namespace WinFormsTest.Archive.Models
{
    [Alignment(0x10)]
    class FileEntry
    {
        public int offset;
        public int size;
        public int nameLength;
        [VariableLength("nameLength",StringEncoding=StringEncoding.UTF8)]
        public string name;
    }
}
