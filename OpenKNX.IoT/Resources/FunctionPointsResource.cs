using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Resources.fp;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources
{
    internal class FunctionPointsResource : Resource
    {
        public FunctionPointsResource(DeviceData deviceData, ILoggerFactory? loggerFactory) : base("fp")
        {
            Add(new GroupObjectTable(deviceData, loggerFactory));
            Add(new RecipientTable(deviceData, loggerFactory));
            Add(new PublisherTable(deviceData, loggerFactory));
        }
    }
}
