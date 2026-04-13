using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Resources.ap;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources
{
    internal class ApplicationProgramResource : Resource
    {
        public ApplicationProgramResource(DeviceData deviceData) : base("ap")
        {
            Add(new ProgramVersion(deviceData));
        }
    }
}
