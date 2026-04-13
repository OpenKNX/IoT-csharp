using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Received
{
    internal class ProgModeMessage
    {
        [CborKey(1)]
        public bool ProgMode { get; set; }
    }
}
