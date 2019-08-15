﻿using System;
using System.IO;

namespace Kompression.LempelZiv.Parser
{
    public interface ILzParser : IDisposable
    {
        LzMatch[] Parse(byte[] input,int startPosition);
    }
}
