using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komponent.Exceptions
{
    public class UnsupportedTypeException : Exception
    {
        public UnsupportedTypeException(Type type)
            : base($"The given type {type.Name} is not supported.")
        {
        }
    }
}
