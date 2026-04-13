using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Enums;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Text;

namespace OpenKNX.IoT.Resources.p
{
    internal class Parameter : Resource
    {
        private ILogger? _logger;
        private DeviceData _deviceData;

        public Parameter(DeviceData deviceData, string name, ILoggerFactory? loggerFactory = null) : base(name)
        {
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger($"Resource[/p/{name}]");
            _deviceData = deviceData;

            _deviceData._resourceHelper.SaveEntryDefault($"/p/{name}", null, ResourceTypes.Object);
        }

        protected override void DoPut(CoapExchange exchange)
        {
            if(exchange.Request.ContentType != MediaType.ApplicationCbor)
            {
            }

            switch(exchange.Request.ContentType)
            {
                case MediaType.ApplicationCbor:
                    ReadParameterCbor(exchange);
                    break;


                default:
                    _logger?.LogError($"Not supported content type {exchange.Request.ContentType}");
                    exchange.Respond(StatusCode.BadRequest, "Only application/cbor supported");
                    return;
            }

            exchange.Respond(StatusCode.Changed, []);
        }

        private void ReadParameterCbor(CoapExchange exchange)
        {
            string path = $"/p/{Name}";
            CborReader reader = new CborReader(exchange.Request.Payload);
            reader.ReadStartMap();
            int key = reader.ReadInt32();
            object propValue;

            CborReaderState state = reader.PeekState();
            switch (state)
            {
                case CborReaderState.UnsignedInteger:
                    {
                        uint value = reader.ReadUInt32();
                        propValue = value;
                        _deviceData._resourceHelper.SaveResourceEntry(path, value);
                        break;
                    }

                default:
                    _logger?.LogError($"Unsupported type in Cbor '{state}'");
                    return;
            }

            _logger?.LogInformation($"Received new Parameter '{path}' with value: {propValue}");
        }
    }
}
