using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Resources.p;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources
{
    internal class ParameterResource : Resource
    {
        private ILogger? _logger;
        private DeviceData _deviceData;

        List<string> Resources = new()
        {
            "globalTestParameter"
        };

        public ParameterResource(DeviceData deviceData, ILoggerFactory? loggerFactory = null) : base("p")
        {
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger("Resource[/p]");

            _deviceData = deviceData;

            foreach(var item in Resources)
            {
                Add(new Parameter(deviceData, item, loggerFactory));
            }
        }
    }
}
