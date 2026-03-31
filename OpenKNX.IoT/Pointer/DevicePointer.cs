using Microsoft.Extensions.Logging;
using OpenKNX.CoAP.Enums;
using OpenKNX.IoT.Database;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Resources;
using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Text;

namespace OpenKNX.IoT.Pointer
{
    [ResourceBase("/dev")]
    internal class DevicePointer : ResourceTable
    {
        private InitialDeviceConfig _config;

        public DevicePointer(ResourceContext db, InitialDeviceConfig config) : base(db)
        {
            _config = config;
        }

        public DevicePointer(ResourceContext db, InitialDeviceConfig config, ILoggerFactory? loggerFactory) : base(db, loggerFactory)
        {
            _config = config;
            SaveEntryDefault("/sn", _config.Serialnumber, ResourceTypes.String, true);
        }

        internal override void InitTable()
        {
            SaveEntryDefault("/sn", "", ResourceTypes.String);
            SaveEntryDefault("/iid", (long)0, ResourceTypes.BigInteger);
            SaveEntryDefault("/sna", 0xFF, ResourceTypes.Integer);
            SaveEntryDefault("/da", 0xFF, ResourceTypes.Integer);
        }

        [Resource(Method.GET, "/sn")]
        public byte[]? SerialNumberGet()
        {
            string value = GetResourceEntry<string>("/sn");
            return ReturnTextString(value);
        }

        [Resource(Method.GET, "/iid", [2, 3])]
        public byte[]? InstallationIdGet()
        {
            long value = GetResourceEntry<long>("/iid");
            return ReturnInteger((int)value); // TODO fix?
        }

        [Resource(Method.GET, "/mid")]
        public byte[]? ManufacturerIdGet()
        {
            //ResourceData serialNumber = GetResourceEntry("/mid", (0x00FA).ToString(), ResourceTypes.Integer);

            return ReturnInteger(_config.ManufacturerId);
        }

        [Resource(Method.GET, "/sna", [2, 3])]
        public byte[]? SubnetAddressGet()
        {
            int value = GetResourceEntry<int>("/sna");
            return ReturnInteger(value);
        }

        [Resource(Method.GET, "/da", [2, 3])]
        public byte[]? DeviceAddressGet()
        {
            int value = GetResourceEntry<int>("/da");
            return ReturnInteger(value);
        }

        // Save this as base64 encoded in knxprod HardwareType
        [Resource(Method.GET, "/hwt")]
        public byte[]? HardwareTypeGet()
        {
            return ReturnTextString(_config.HardwareType);
        }

        [Resource(Method.GET, "/hwv")]
        public byte[]? HardwareVersionGet()
        {
            return ReturnByteArray(_config.HardwareVersion);
        }

        [Resource(Method.GET, "/fwv")]
        public byte[]? FirmwareVersionGet()
        {
            return ReturnByteArray(_config.FirmwareVersion);
        }

        // Maximum Length 10
        [Resource(Method.GET, "/model")]
        public byte[]? ModelGet()
        {
            if(_config.Model.Length > 10)
                return ReturnTextString(_config.Model.Substring(0, 10));
            return ReturnTextString(_config.Model);
        }

        [Resource(Method.GET, "/pm")]
        public byte[]? ProgrammingModeGet()
        {
            return ReturnBool(false);
        }

        [Resource(Method.PUT, "/pm")]
        public ResourceResponse? ProgrammingModePut()
        {
            ResourceResponse response = new ResourceResponse();
            response.Method = Method.Changed;
            response.Payload = [];
            return response;
        }

        // Minimum Length 
        [Resource(Method.GET, "/hname")]
        public byte[]? HostnameGet()
        {
            return ReturnTextString($"knx-{_config.Serialnumber.ToUpper()}.local");
        }

        // Maximum Length 253
        [Resource(Method.GET, "/ipv6")]
        public byte[]? IpAddressGet()
        {
            return ReturnByteString("2a02807122819dc0c4cbf5a49bd08466");
        }

        // Maximum Length 253
        [Resource(Method.GET, "/port")]
        public byte[]? PortGet()
        {
            return ReturnInteger(5683);
        }

        // Maximum Length 253
        [Resource(Method.GET, "/mport")]
        public byte[]? MulticastPortGet()
        {
            return ReturnInteger(5683);
        }
    }
}
