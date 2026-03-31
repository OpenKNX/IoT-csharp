using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Enums
{
    internal enum LoadStateMachineStates
    {
        Unloaded = 0,
        Loaded = 1,
        Loading = 2,
        Unloading = 4,
        LoadCompleting = 5
    }
}
