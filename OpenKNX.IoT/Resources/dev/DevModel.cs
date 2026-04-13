using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.dev
{
    internal class DevModel : Resource
    {
        private DeviceData _deviceData;

        public DevModel(DeviceData deviceData) : base("model")
        {
            _deviceData = deviceData;
        }

        protected override void DoGet(CoapExchange exchange)
        {
            byte[] payload = CborHelper.ReturnTextString(_deviceData.Model);
            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }
    }
}
