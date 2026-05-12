using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Konnect.Contract.Exceptions.Management.Dialog
{
    public class NotEnoughDialogOptionsProvidedException(int expected, int received) 
        : Exception($"Not enough dialog options provided. (Expected {expected}, Received {received})");
}
