using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources
{
    internal class MessagingResource : Resource
    {
        private GroupObjectHelper _groupObjectHelper;

        public MessagingResource(GroupObjectHelper groupObjectHelper) : base("k")
        {
            _groupObjectHelper = groupObjectHelper;
        }

        protected override void DoPost(CoapExchange exchange)
        {
            GroupMessage groupMessage = CborHelper.Deserialize<GroupMessage>(exchange.Request.Payload);
            _groupObjectHelper.HandleMessage(groupMessage, exchange.Request.OscoreContext);
        }
    }
}
