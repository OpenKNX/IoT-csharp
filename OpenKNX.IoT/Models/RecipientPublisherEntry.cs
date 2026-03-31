using OpenKNX.IoT.Received;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Models
{
    internal class RecipientPublisherEntry
    {
        [CborKey(0)]
        public int Id { get; set; }
        [CborKey(7)]
        public List<uint>? GroupAddresses { get; set; }
        [CborKey(10)]
        public string? Url { get; set; }
        [CborKey(13)]
        public uint? GroupId { get; set; }

        public bool IsEmpty()
        {
            return Url == null && GroupAddresses == null && GroupId == null;
        }

        public void Update(RecipientPublisherEntry message)
        {
            if (message.Url != null)
                Url = message.Url;
            if (message.GroupAddresses != null)
                GroupAddresses = message.GroupAddresses;
            if (message.GroupId != null)
                GroupId = message.GroupId;
        }
    }
}
