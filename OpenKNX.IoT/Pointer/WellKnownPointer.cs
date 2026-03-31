using Microsoft.Extensions.Logging;
using OpenKNX.CoAP;
using OpenKNX.CoAP.Enums;
using OpenKNX.IoT.Database;
using OpenKNX.IoT.Migrations;
using OpenKNX.IoT.Received;
using OpenKNX.IoT.Resources;
using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;

namespace OpenKNX.IoT.Pointer
{
    [ResourceBase("/.well-known", false)]
    internal class WellKnownPointer : ResourceTable
    {
        const string basePath = "";

        public event EventHandler<int>? ResetReceived;
        public event EventHandler? DoReset;

        public WellKnownPointer(ResourceContext db) : base(db) { }
        public WellKnownPointer(ResourceContext db, ILoggerFactory? loggerFactory) : base(db, loggerFactory) { }

        internal override void InitTable()
        {

        }

        [Resource(Method.GET, "/knx/f")]
        public byte[] FingerprintGet()
        {
            string fpg = GetForeignResourceEntry<string>("/fp/g");
            string fpr = GetForeignResourceEntry<string>("/fp/r");
            string fpp = GetForeignResourceEntry<string>("/fp/p");

            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes($"{fpg}-{fpr}-{fpp}");
            byte[] hash = sha.ComputeHash(bytes);
            ulong fingerprint = BitConverter.ToUInt64(hash, 0);
            return ReturnUnsignedBigInteger(fingerprint);
        }

        [Resource(Method.GET, "/core", null, false)]
        public ResourceResponse? HardwareVersionGet(CoapMessage request)
        {
            if (request.Queries.ContainsKey("ep"))
            {
                string ep = request.Queries["ep"];

                if (ep.StartsWith("knx://sn."))
                {
                    string serial = GetForeignResourceEntry<string>("/dev/sn");
                    if (ep != $"knx://sn.{serial.ToLower()}")
                        return null;

                    long installationId = GetForeignResourceEntry<long>("/dev/iid");
                    int deviceSubnet = GetForeignResourceEntry<int>("/dev/sna");
                    int deviceAddress = GetForeignResourceEntry<int>("/dev/da");
                    string responseText = $"<{basePath}>;ep=\"knx://sn.{serial.ToLower()} knx://ia.{installationId:x}.{deviceSubnet:x2}{deviceAddress:x2}\"";

                    ResourceResponse response = new ResourceResponse();
                    response.Method = Method.Content;
                    response.Type = MessageType.NonConfirmable;
                    response.Payload = Encoding.UTF8.GetBytes(responseText);
                    response.Format = Formats.ApplicationLinkFormat;
                    return response;
                }

                if(ep.StartsWith("knx://ia."))
                {
                    long installationId = GetForeignResourceEntry<long>("/dev/iid");
                    int deviceSubnet = GetForeignResourceEntry<int>("/dev/sna");
                    int deviceAddress = GetForeignResourceEntry<int>("/dev/da");

                    if (ep != $"knx://ia.{installationId:x}.{deviceSubnet:x2}{deviceAddress:x2}")
                        return null;

                    string serial = GetForeignResourceEntry<string>("/dev/sn");
                    string responseText = $"<{basePath}>;ep=\"knx://sn.{serial.ToLower()} knx://ia.{installationId:x}.{deviceSubnet:x2}{deviceAddress:x2}\"";

                    ResourceResponse response = new ResourceResponse();
                    response.Method = Method.Content;
                    response.Type = MessageType.NonConfirmable;
                    response.Payload = Encoding.UTF8.GetBytes(responseText);
                    response.Format = Formats.ApplicationLinkFormat;
                    return response;
                }
            }

            return null;
        }

        [Resource(Method.POST, "/knx/ia")]
        public ResourceResponse? DeviceIndividualizationPost(CoapMessage request)
        {
            bool changed = false;
            IndividualizationMessage message = Helper.CborHelper.Deserialize<IndividualizationMessage>(request.Payload);
            if(message.InstallationId != null)
            {
                SaveForeignResourceEntry("/dev/iid", message.InstallationId);
                changed = true;
            }
            if(message.IndividualAddress != null)
            {
                int deviceSubnet = message.IndividualAddress.Value >> 8;
                int deviceAddress = message.IndividualAddress.Value & 0xFF;
                _logger?.LogInformation($"Changed individual address to {deviceSubnet >> 4}.{deviceSubnet & 0xF}.{deviceAddress}");
                SaveForeignResourceEntry("/dev/sna", deviceSubnet);
                SaveForeignResourceEntry("/dev/da", deviceAddress);
                changed = true;
            }

            if(changed)
            {
                ResourceResponse response = new ResourceResponse();
                response.Method = Method.Changed;
                response.Payload = [];
                return response;
            }
            return null;
        }

        [Resource(Method.GET, "/knx", null, true)]
        public byte[]? KnxInfoGet()
        {
            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);

            // api
            writer.WriteTextString("api");
            writer.WriteStartMap(2);
            // version
            writer.WriteTextString("version");
            writer.WriteTextString("1.0.0");
            // base
            writer.WriteTextString("base");
            writer.WriteTextString("/");

            writer.WriteEndMap();

            writer.WriteEndMap();

            return writer.Encode();
        }

        [Resource(Method.POST, "/knx", null)]
        public byte[]? KnxReset(CoapMessage request)
        {
            RestartMessage message = Helper.CborHelper.Deserialize<RestartMessage>(request.Payload);

            if (message.EraseCode == null)
                return null;

            CborWriter writer = new CborWriter();
            if (message.EraseCode != 1 &&
                message.EraseCode != 2 &&
                message.EraseCode != 3 && 
                message.EraseCode != 7)
            {
                writer.WriteStartMap(1);
                writer.WriteTextString("code");
                writer.WriteInt32(2); // Unsupported erase code
                writer.WriteEndMap();
                return writer.Encode();
            }

            ResetReceived?.Invoke(this, message.EraseCode ?? 0);

            writer.WriteStartMap(2);
            writer.WriteTextString("code");
            writer.WriteInt32(0); // No error
            writer.WriteTextString("time");
            writer.WriteInt32(1); // 5s to reboot
            writer.WriteEndMap();

            DoReset?.Invoke(this, EventArgs.Empty);

            return writer.Encode();
        }
    }
}
