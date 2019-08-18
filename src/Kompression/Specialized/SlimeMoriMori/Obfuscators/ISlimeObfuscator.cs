using System.IO;

namespace Kompression.Specialized.SlimeMoriMori.Obfuscators
{
    interface ISlimeObfuscator
    {
        void Obfuscate(Stream input);
    }
}
