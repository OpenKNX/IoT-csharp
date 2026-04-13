using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.dev
{
    internal class DevSubnetAddress : Resource
    {
        private DeviceData _deviceData;

        public DevSubnetAddress(DeviceData deviceData) : base("sna")
        {
            _deviceData = deviceData;

            int subnet = (_deviceData.IndividualAddress >> 8) & 0xFF;
            int device = _deviceData.IndividualAddress & 0xFF;
            _deviceData._resourceHelper.SaveEntryDefault("/dev/sna", subnet, ResourceTypes.Integer);
        }

        protected override void DoGet(CoapExchange exchange)
        {
            int subnet = _deviceData.IndividualAddress >> 8;
            byte[] payload = CborHelper.ReturnInteger(subnet);
            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }
    }
}
