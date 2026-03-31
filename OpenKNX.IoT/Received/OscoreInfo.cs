using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Received
{
    internal class OscoreInfo
    {
        [CborKey(0)]
        public string? KeyId { get; set; }
        [CborKey(2)]
        public byte[]? MasterSecret { get; set; }
        [CborKey(4)]
        public uint? Algo { get; set; }
        [CborKey(6)]
        public string? KeyIdContext { get; set; }
    }
}
