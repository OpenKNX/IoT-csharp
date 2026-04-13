using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.dev
{
    internal class DevPort : Resource
    {
        private DeviceData _deviceData;

        public DevPort(DeviceData deviceData) : base("port")
        {
            _deviceData = deviceData;
        }

        protected override void DoGet(CoapExchange exchange)
        {
            // TODO get real Port?
            byte[] payload = CborHelper.ReturnInteger(5683);
            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }
    }
}
