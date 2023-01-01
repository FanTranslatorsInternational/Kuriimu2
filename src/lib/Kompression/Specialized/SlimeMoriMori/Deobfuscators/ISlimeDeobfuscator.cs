using System.IO;

namespace Kompression.Specialized.SlimeMoriMori.Deobfuscators
{
    interface ISlimeDeobfuscator
    {
        void Deobfuscate(Stream input);
    }
}
