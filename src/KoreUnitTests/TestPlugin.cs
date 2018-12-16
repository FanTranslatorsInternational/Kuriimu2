using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using System.IO;

namespace KoreUnitTests
{
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [Export(typeof(ITest))]
    [PluginExtensionInfo("*.test")]
    [PluginInfo("TestId", "TestPlugin", "Test", "onepiecefreak", "github.com", "A test plugin for UnitTests")]
    public class TestPlugin : IIdentifyFiles, ILoadFiles, ISaveFiles, ITest
    {
        public List<string> Communication { get; set; }

        public void Dispose()
        {
            ;
        }

        public bool Identify(string filename)
        {
            using (var br = new BinaryReader(File.OpenRead(filename)))
                return br.ReadUInt32() == 0x16161616;
        }

        public void Load(string filename)
        {
            Communication = new List<string>() { "string1", "string2" };
        }

        public void Save(string filename, int versionIndex = 0)
        {
            Communication.Add("string3");
        }
    }
}
