using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Enums
{
    internal enum LoadStateMachineEvents
    {
        NoOperation = 0,
        StartLoading = 1,
        LoadComplete = 2,
        Unload = 4
    }
}
