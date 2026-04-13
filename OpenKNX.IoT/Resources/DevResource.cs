using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Resources.dev;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources
{
    internal class DevResource : Resource
    {
        public DevResource(DeviceData _deviceData, ILoggerFactory? loggerFactory = null) : base("dev")
        {
            Add(new DevDeviceAddress(_deviceData));
            Add(new DevFirmwareVersion(_deviceData));
            Add(new DevHardwareType(_deviceData));
            Add(new DevHardwareVersion(_deviceData));
            Add(new DevHostname(_deviceData));
            Add(new DevIpAddresses(_deviceData));
            Add(new DevManufacturerId(_deviceData));
            Add(new DevModel(_deviceData));
            Add(new DevMulticastPort(_deviceData));
            Add(new DevPort(_deviceData));
            Add(new DevProgMode(_deviceData, loggerFactory));
            Add(new DevSerialnumber(_deviceData));
            Add(new DevSubnetAddress(_deviceData));

            _deviceData._resourceHelper.SaveEntryDefault("/dev/iia", _deviceData.InstallationId, ResourceTypes.BigInteger);
            _deviceData._resourceHelper.SaveEntryDefault("/dev/ia", _deviceData.IndividualAddress, ResourceTypes.Integer);
        }
    }
}
