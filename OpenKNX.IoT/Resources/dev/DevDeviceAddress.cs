using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.dev
{
    internal class DevDeviceAddress : Resource
    {
        private DeviceData _deviceData;

        public DevDeviceAddress(DeviceData deviceData) : base("da")
        {
            _deviceData = deviceData;

            int subnet = (_deviceData.IndividualAddress >> 8) & 0xFF;
            int device = _deviceData.IndividualAddress & 0xFF;
            _deviceData._resourceHelper.SaveEntryDefault("/dev/da", device, ResourceTypes.Integer);
        }

        protected override void DoGet(CoapExchange exchange)
        {
            int device = _deviceData.IndividualAddress & 0xFF;
            byte[] payload = CborHelper.ReturnInteger(device);
            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }
    }
}
