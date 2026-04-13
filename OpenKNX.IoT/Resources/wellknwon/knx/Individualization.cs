using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Received;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.wellknwon.knx
{
    internal class Individualization : Resource
    {
        private ILogger? _logger;
        private DeviceData _deviceData;

        public Individualization(DeviceData deviceData, ILoggerFactory? loggerFactory) : base("ia")
        {
            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger("Resource[/.well-known/knx/ia]");

            _deviceData = deviceData;
        }

        protected override void DoPost(CoapExchange exchange)
        {
            IndividualizationMessage? message = CborHelper.Deserialize<IndividualizationMessage>(exchange.Request.Payload);
            _deviceData.SetIndividualAddress(message.IndividualAddress);
            _deviceData.SetInstallationId(message.InstallationId);

            _logger?.LogInformation($"Changed Individualization: IA={_deviceData.IndividualAddress:x4}, InstallationId={_deviceData.InstallationId:x}");

            exchange.Respond(StatusCode.Changed, []);
        }
    }
}
