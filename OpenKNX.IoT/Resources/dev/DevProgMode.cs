using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Received;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.dev
{
    internal class DevProgMode : Resource
    {
        private ILogger? _logger;
        private DeviceData _deviceData;

        public DevProgMode(DeviceData deviceData, ILoggerFactory? loggerFactory = null) : base("pm")
        {
            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger("Resource[/dev/pm]");

            _deviceData = deviceData;
        }

        protected override void DoGet(CoapExchange exchange)
        {
            byte[] payload = CborHelper.ReturnBool(_deviceData.ProgMode);
            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }

        protected override void DoPut(CoapExchange exchange)
        {
            ProgModeMessage message = CborHelper.Deserialize<ProgModeMessage>(exchange.Request.Payload);
            _deviceData.ProgMode = message.ProgMode;

            _logger?.LogInformation("Set ProgMode to {ProgMode}", message.ProgMode ? "ON" : "OFF");

            exchange.Respond(StatusCode.Changed, []);
        }
    }
}
