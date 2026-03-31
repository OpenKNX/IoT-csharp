using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Models
{
    public class GroupMessageEvent
    {
        public int SourceAddress { get; set; }
        public string Href { get; set; }
        public object Data { get; set; }

        public GroupMessageEvent(int sourceAddress, string href, object data)
        {
            Href = href;
            Data = data;
        }
    }
}
