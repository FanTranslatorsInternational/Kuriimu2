using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Attributes;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.FileSystem.Nodes.Physical;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Layout;
using Kontract.Models.Layout;

namespace plugin_test_adapters.Layout
{
    [Export(typeof(IPlugin))]
    [PluginExtensionInfo("*.layout")]
    [PluginInfo("Test-Layout-Id")]
    public class TestLayoutPlugin : ILayoutAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        public RootLayoutElement Layout { get; private set; }

        public bool Identify(StreamInfo file, BaseReadOnlyDirectoryNode fileSystem)
        {
            using (var br = new BinaryReader(file.FileData, Encoding.ASCII, LeaveOpen))
            {
                return br.ReadUInt32() == 0x17171717;
            }
        }

        public void Dispose()
        {

        }

        public void Load(StreamInfo input, BaseReadOnlyDirectoryNode fileSystem)
        {
            using (var br = new BinaryReaderX(input.FileData, Encoding.ASCII, LeaveOpen))
            {
                var header = br.ReadType<Header>();
                Layout = (RootLayoutElement)ReadLayout(br, null);
            }
        }

        private LayoutElement ReadLayout(BinaryReaderX br, LayoutElement parent)
        {
            var type = br.ReadInt32();
            switch (type)
            {
                // Root pane
                case 1:
                    var element = br.ReadType<RootElement>();
                    var root = new RootLayoutElement(new Size(element.width, element.height), new Point(element.x, element.y));

                    for (int i = 0; i < element.childrenCount; i++)
                    {
                        br.BaseStream.Position = element.childrenOffsets[i];
                        root.Children.Add(ReadLayout(br, root));
                    }

                    return root;
                // window
                case 2:
                    var element2 = br.ReadType<WindowElement>();

                    if (element2.childrenCount > 0)
                    {
                        var window = new LeafLayoutElement(new Size(element2.width, element2.height), new Point(element2.x, element2.y), parent);
                        switch (element2.anchor)
                        {
                            case 1:
                                window.ParentAnchor = LocationAnchor.Center;
                                break;
                            case 2:
                                window.ParentAnchor = LocationAnchor.Bottom;
                                break;
                        }

                        return window;
                    }
                    else
                    {
                        var window = new ParentLayoutElement(new Size(element2.width, element2.height), new Point(element2.x, element2.y), parent);
                        switch (element2.anchor)
                        {
                            case 1:
                                window.ParentAnchor = LocationAnchor.Center;
                                break;
                            case 2:
                                window.ParentAnchor = LocationAnchor.Bottom;
                                break;
                        }

                        for (int i = 0; i < element2.childrenCount; i++)
                        {
                            br.BaseStream.Position = element2.childrenOffsets[i];
                            window.Children.Add(ReadLayout(br, window));
                        }

                        return window;
                    }
                default:
                    throw new InvalidOperationException($"Element type {type} not supported.");
            }
        }

        public bool LeaveOpen { get; set; }

        public void Save(StreamInfo output, PhysicalDirectoryNode fileSystem, int versionIndex = 0)
        {
            throw new NotImplementedException();
        }
    }

    [Alignment(0x10)]
    class Header
    {
        public int magic;
        public int fileSize;
        public int rootOffset;
    }

    class RootElement
    {
        public int width;
        public int height;
        public int x;
        public int y;
        public int childrenCount;
        [VariableLength("childrenCount")]
        public int[] childrenOffsets;
    }

    class WindowElement
    {
        public int width;
        public int height;
        public int x;
        public int y;
        public int anchor;
        public int childrenCount;
        [VariableLength("childrenCount")]
        public int[] childrenOffsets;
    }
}
