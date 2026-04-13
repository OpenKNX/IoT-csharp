using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Received;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.ap
{
    internal class ProgramVersion : Resource
    {
        private DeviceData _deviceData;

        public ProgramVersion(DeviceData deviceData) : base("pv")
        {
            _deviceData = deviceData;

            _deviceData._resourceHelper.SaveEntryDefault("/ap/pv", Array.Empty<int>(), ResourceTypes.UnsignedIntegerArray);
        }

        protected override void DoGet(CoapExchange exchange)
        {
            uint[] value = _deviceData._resourceHelper.GetResourceEntryObject<uint[]>("/ap/pv") ?? [0x00, 0x00, 0x00];
            byte[] payload = CborHelper.ReturnUintArray(value);
            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }

        protected override void DoPut(CoapExchange exchange)
        {
            ProgrammVersionMessage message = CborHelper.Deserialize<ProgrammVersionMessage>(exchange.Request.Payload);
            if(message.ProgramVersion == null)
            {
                exchange.Respond(StatusCode.BadRequest, "ProgramVersion is required");
                return;
            }
            _deviceData._resourceHelper.SaveResourceEntry("/ap/pv", message.ProgramVersion);
            exchange.Respond(StatusCode.Changed);
        }
    }
}
