using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Received
{
    internal class SecureInfo
    {
        [CborKey(4)]
        public OscoreInfo? OscoreInfo { get; set; }
    }
}
