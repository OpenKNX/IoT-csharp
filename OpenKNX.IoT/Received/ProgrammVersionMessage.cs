using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Received
{
    internal class ProgrammVersionMessage
    {
        [CborKey(1)]
        public List<uint>? ProgramVersion { get; set; }
    }
}
