using OpenKNX.IoT.Received;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Models
{
    internal class GroupMessage
    {
        [CborKey(4)]
        public int SourceAddress { get; set; }
        [CborKey(5)]
        public GroupMessageValue Value { get; set; } = new GroupMessageValue();
    }
}
