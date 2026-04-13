using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.dev
{
    internal class DevSerialnumber : Resource
    {
        private DeviceData _deviceData;

        public DevSerialnumber(DeviceData deviceData) : base("sn")
        {
            _deviceData = deviceData;
        }

        protected override void DoGet(CoapExchange exchange)
        {
            byte[] payload = CborHelper.ReturnTextString(_deviceData.Serialnumber);
            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }
    }
}
