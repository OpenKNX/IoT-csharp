using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Resources.auth;
using OpenKNX.IoT.Resources.fp;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources
{
    internal class AuthenticationResource : Resource
    {
        public AuthenticationResource(DeviceData deviceData, ILoggerFactory? loggerFactory) : base("auth")
        {
            Add(new AuthTokens(deviceData, loggerFactory));
        }
    }
}
