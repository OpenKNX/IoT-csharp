using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Received
{
    internal class RestartMessage
    {
        [CborKey(1)]
        public int? EraseCode { get; set; }
        [CborKey(2)]
        public string? Command { get; set; }
    }
}
