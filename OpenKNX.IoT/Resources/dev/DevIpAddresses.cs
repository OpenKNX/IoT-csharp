using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.dev
{
    internal class DevIpAddresses : Resource
    {
        private DeviceData _deviceData;

        public DevIpAddresses(DeviceData deviceData) : base("ipv6")
        {
            _deviceData = deviceData;
        }

        protected override void DoGet(CoapExchange exchange)
        {
            // TODO get real IPs
            byte[] payload = CborHelper.ReturnByteString("2a02807122819dc0c4cbf5a49bd08466");
            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }
    }
}
