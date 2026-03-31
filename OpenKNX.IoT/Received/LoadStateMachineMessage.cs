using OpenKNX.IoT.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Received
{
    internal class LoadStateMachineMessage
    {
        [CborKey(2)]
        public LoadStateMachineEvents? Event { get; set; }
        [CborKey(3)]
        public LoadStateMachineStates? State { get; set; }
    }
}
