using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Enums
{
    internal enum CFlags
    {
        // 0x01 reserved
        // 0x02 reserved
        Read = 0x08,
        Write = 0x10,
        Init = 0x20,
        Transmission = 0x40,
        Update = 0x80,
    }
}
