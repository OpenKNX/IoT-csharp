using OpenKNX.IoT.Enums;
using OpenKNX.IoT.Received;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Models
{
    internal class GroupObjectTableEntry
    {
        [CborKey(0)]
        public int Id { get; set; }
        [CborKey(11)]
        public string? Href { get; set; }
        [CborKey(7)]
        public List<uint>? GroupAddresses { get; set; }
        [CborKey(8)]
        public uint? Flags { get; set; }

        public bool IsEmpty()
        {
            return Href == null && GroupAddresses == null && Flags == null;
        }

        public void Update(GroupObjectTableEntry entry)
        {
            if (entry.Href != null)
                Href = entry.Href;
            if (entry.GroupAddresses != null)
                GroupAddresses = entry.GroupAddresses;
            if (entry.Flags != null)
                Flags = entry.Flags;
        }

        public bool GetFlag(CFlags flag)
        {
            return (Flags & (uint)flag) != 0;
        }
    }
}
