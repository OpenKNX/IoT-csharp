using OpenKNX.IoT.Received;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Models
{
    internal class GroupMessageValue
    {
        [CborKey(7)]
        public uint GroupAddress { get; set; } = 0;
        [CborKey(6)]
        public string ServiceTypeCode { get; set; } = string.Empty;
        [CborKey(1)]
        public object Value { get; set; } = 0;

        public GroupMessageValue() { }

        public GroupMessageValue(uint groupAddress, string serviceTypeCode, object value)
        {
            GroupAddress = groupAddress;
            ServiceTypeCode = serviceTypeCode;
            Value = value;
        }
    }
}
