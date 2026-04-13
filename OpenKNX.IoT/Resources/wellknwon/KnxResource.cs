using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Received;
using OpenKNX.IoT.Resources.wellknwon.knx;
using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Text;

namespace OpenKNX.IoT.Resources.wellknwon
{
    internal class KnxResource : Resource
    {
        ILogger? _logger;

        public KnxResource(DeviceData deviceData, ILoggerFactory? loggerFactory = null) : base("knx")
        {
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger("Resource[/.well-known/knx]");

            Add(new Individualization(deviceData, loggerFactory));
            Add(new Fingerprint(deviceData));
        }

        protected override void DoGet(CoapExchange exchange)
        {
            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);

            // api
            writer.WriteTextString("api");
            writer.WriteStartMap(2);
            // version
            writer.WriteTextString("version");
            writer.WriteTextString("1.0.0");
            // base
            writer.WriteTextString("base");
            writer.WriteTextString("/");

            writer.WriteEndMap();

            writer.WriteEndMap();

            byte[] payload =  writer.Encode();

            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }

        protected override void DoPost(CoapExchange exchange)
        {
            RestartMessage message = CborHelper.Deserialize<RestartMessage>(exchange.Request.Payload);

            CborWriter writer = new CborWriter();
            if (message.EraseCode != 1 &&
                message.EraseCode != 2 &&
                message.EraseCode != 3 &&
                message.EraseCode != 7)
            {
                writer.WriteStartMap(1);
                writer.WriteTextString("code");
                writer.WriteInt32(2); // Unsupported erase code
                writer.WriteEndMap();
                
                exchange.Respond(StatusCode.Content, writer.Encode(), MediaType.ApplicationCbor);
                return;
            }

            writer.WriteStartMap(2);
            writer.WriteTextString("code");
            writer.WriteInt32(0); // No error
            writer.WriteTextString("time");
            writer.WriteInt32(1); // 5s to reboot
            writer.WriteEndMap();

            exchange.Respond(StatusCode.Content, writer.Encode(), MediaType.ApplicationCbor);
        }
    }
}
