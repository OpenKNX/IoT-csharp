using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.dev
{
    internal class DevHostname : Resource
    {
        private DeviceData _deviceData;

        public DevHostname(DeviceData deviceData) : base("hname")
        {
            _deviceData = deviceData;
        }

        protected override void DoGet(CoapExchange exchange)
        {
            byte[] payload = CborHelper.ReturnTextString($"knx-{_deviceData.Serialnumber.ToLower()}.local");
            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }
    }
}
