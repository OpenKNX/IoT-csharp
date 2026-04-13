using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Enums;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Resources.a;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources
{
    internal class ActionResource : Resource
    {
        public ActionResource(DeviceData _deviceData, ILoggerFactory? loggerFactory = null) : base("a")
        {
            Add(new LoadStateMachine(_deviceData, loggerFactory));

            _deviceData._resourceHelper.SaveEntryDefault("/a/lsm", LoadStateMachineStates.Unloaded, ResourceTypes.Integer);
        }
    }
}
