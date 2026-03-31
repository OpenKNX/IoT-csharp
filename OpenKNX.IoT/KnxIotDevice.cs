using Makaretu.Dns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenKNX.CoAP;
using OpenKNX.CoAP.Enums;
using OpenKNX.IoT.Database;
using OpenKNX.IoT.Enums;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Pointer;
using OpenKNX.IoT.Received;
using OpenKNX.IoT.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Cbor;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenKNX.IoT
{
    public class KnxIotDevice
    {
        public event EventHandler<GroupMessageEvent>? GroupMessageReceived;

        private ILogger<KnxIotDevice>? _logger;
        private CoapServer _coapServer;
        private string _basePath = string.Empty;
        private MulticastService _mdns = new MulticastService();
        private List<ResourceTable> _resources = new List<ResourceTable>();
        private ILoggerFactory? _loggerFactory;
        private ResourceContext _context;

        private DevicePointer? _deviceTable;
        private WellKnownPointer? _wellKnownTable;
        private ApplicationProgramPointer? _applicationProgramTable;
        private ActionPointer? _actionTable;
        private ParameterPointer? _parameterTable;
        private FunctionsPointer? _functionPointerTable;
        private AuthenticationPointer? _authenticationTable;

        public KnxIotDevice(string basePath = "")
        {
            _coapServer = new CoapServer();
            _basePath = basePath;

            _context = new ResourceContext();
            _context.Database.Migrate();
        }

        public KnxIotDevice(ILoggerFactory loggerFactory, string basePath = "")
        {
            _logger = loggerFactory.CreateLogger<KnxIotDevice>();
            _loggerFactory = loggerFactory;
            _coapServer = new CoapServer(loggerFactory);
            _basePath = basePath;

            _context = new ResourceContext();
            try
            {
                _context.Database.Migrate();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying database migrations");
                return;
            }
        }

        IEnumerable<IPAddress> GetLocalIPv6()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic =>
                    nic.OperationalStatus == OperationalStatus.Up)
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Where(ua =>
                    ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                .Select(ua => ua.Address);
        }

        public void Start(InitialDeviceConfig config)
        {
            _logger?.LogInformation("Starting KNX IoT device...");
            _coapServer.KeyStorage.AddKey(Convert.FromHexString(config.MasterSecret), null, Convert.FromHexString(config.KeyId), Convert.FromHexString(config.KeyIdContext));

            InitDeviceTable(config);


            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("ff02::fd"), 5683);
            _coapServer.Start(endPoint);
            _coapServer.MessageReceived += Server_MessageReceived;

            _logger?.LogInformation("KNX IoT device started and CoAP server is listening on " + endPoint);

            _logger?.LogInformation("Advertising service via mDNS...");
            var sd = new ServiceDiscovery(_mdns);
            //_mdns.QueryReceived += (s, e) =>
            //{
            //    var names = e.Message.Questions
            //        .Select(q => q.Name + " " + q.Type);
            //    _logger?.LogInformation($"got a query for {String.Join(", ", names)}");
            //};

            var z1 = new ServiceProfile(config.Serialnumber, "_knx._udp", 5353);
            sd.Advertise(z1);
            _mdns.UseIpv4 = false;

            
            _mdns.Start();

            var ipv6Addresses = GetLocalIPv6();

            //var aaaa = new AAAARecord
            //{
            //    Name = $"knx-{config.Serialnumber}.local",
            //    Address = IPAddress.Parse("fe80:5215:f8d3:98c4:458b")
            //};
            //_mdns.SendAnswer(new Message { QR = true, AA = true, Answers = { aaaa } });

            foreach (var ip in ipv6Addresses)
            {
                var aaaa = new AAAARecord
                {
                    Name = $"knx-{config.Serialnumber}.local",
                    Address = ip
                };

                _mdns.SendAnswer(new Message
                {
                    QR = true,
                    AA = true,
                    Answers = { aaaa }
                });
            }

            JoinGroupAddresses();
        }

        public void Stop()
        {
            // TODO stop CoAP Server
            _coapServer.MessageReceived -= Server_MessageReceived;

            _mdns.Stop();
        }

        public DeviceInfo GetDeviceInfo()
        {
            int subnet = _deviceTable?.GetResourceEntry<int>("/sna") ?? 0xFF;
            int devadr = _deviceTable?.GetResourceEntry<int>("/da") ?? 0xFF;
            string physicalAddress = $"{subnet >> 4}.{subnet & 0xF}.{devadr}";
            string serial = _deviceTable?.GetResourceEntry<string>("/sn") ?? "Undefined";
            long iid = _deviceTable?.GetResourceEntry<long>("/iid") ?? 0x00;

            string installation_id = (iid >> 32 & 0xFF).ToString("x");
            installation_id += ":";
            installation_id += (iid >> 16 & 0xFFFF).ToString("x");
            installation_id += ":";
            installation_id += (iid & 0xFFFF).ToString("x");

            bool progmode = false; // TODO get real
            string lsm = _actionTable?.GetResourceEntry<LoadStateMachineStates>("/lsm").ToString() ?? "Unloaded";
            string password = _deviceTable?.GetResourceEntry<string>("/pwd") ?? "unset";
            return new DeviceInfo(physicalAddress, serial, installation_id, $"knx-{serial}", lsm, password, progmode);
        }

        public List<GenericInternalInfo> GetGroupObjectTableInfo()
        {
            List<GenericInternalInfo> infos = new();
            List<GroupObjectTableEntry> GroupObjects = _functionPointerTable?.GetResourceEntry<List<GroupObjectTableEntry>>("/g") ?? new();

            foreach (GroupObjectTableEntry entry in GroupObjects)
            {
                GenericInternalInfo info = new(entry.Id.ToString(), entry.Href, entry.Flags?.ToString("X2"), infoList: entry.GroupAddresses?.Select(s => s.ToString("X4")).ToList());
                infos.Add(info);
            }
            return infos;
        }

        public List<GenericInternalInfo> GetAuthenticationTableInfo()
        {
            List<GenericInternalInfo> infos = new();
            List<AuthenticationToken> entries = _authenticationTable?.GetResourceEntry<List<AuthenticationToken>>("/at") ?? new();

            foreach (AuthenticationToken entry in entries)
            {
                GenericInternalInfo info = new(entry.Id.ToString(),
                    entry.SecureInfo?.OscoreInfo?.KeyId,
                    entry.SecureInfo?.OscoreInfo?.KeyIdContext,
                    BitConverter.ToString(entry.SecureInfo?.OscoreInfo?.MasterSecret ?? Array.Empty<byte>()),
                    entry.Profile.ToString(),
                    entry.Scope?.Select(s => s.ToString("X4")).ToList());
                infos.Add(info);
            }
            return infos;
        }

        public List<GenericInternalInfo> GetParameterTableInfo()
        {
            return _parameterTable?.GetAllParameters() ?? new();
        }

        public List<GenericInternalInfo> GetPublisherTableInfo()
        {
            return GetPublisherRecipientTableInfo("/p");
        }

        public List<GenericInternalInfo> GetRecipientTableInfo()
        {
            return GetPublisherRecipientTableInfo("/r");
        }

        private List<GenericInternalInfo> GetPublisherRecipientTableInfo(string type)
        {
            List<GenericInternalInfo> infos = new();
            List<RecipientPublisherEntry> entries = _functionPointerTable?.GetResourceEntry<List<RecipientPublisherEntry>>(type) ?? new();

            foreach (RecipientPublisherEntry entry in entries)
            {
                long _groupId = entry.GroupId ?? 0;
                string groupId = (_groupId >> 16 & 0xFFFF).ToString("x4");
                groupId += ":";
                groupId += (_groupId & 0xFFFF).ToString("x4");
                GenericInternalInfo info = new(entry.Id.ToString(), groupId, infoList: entry.GroupAddresses?.Select(s => s.ToString("X4")).ToList());
                infos.Add(info);
            }
            return infos;
        }

        private Dictionary<uint, long> _groupAddressSequenceNumber = new();

        private async void ReceivedGroupMessage(CoapMessage coapMessage, GroupMessage groupMessage)
        {
            _logger?.LogInformation($"Received GroupAddress: Source={groupMessage.SourceAddress:X4} Destination={groupMessage.Value.GroupAddress:X4} Value={groupMessage.Value.Value}");

            RequestKeyContext? keyContext = _coapServer.GetKeyContext(coapMessage.Token);
            if(keyContext == null)
            {
                _logger?.LogError($"Could not retrieve RequestKeyContext for token '{coapMessage.Token}'. Withdraw GroupMessage!");
                return;
            }
            // RFC8613 8.2 Verify Request
            string keyId = BitConverter.ToString(keyContext.KeyContext.KeyId).Replace("-", "");
            AuthenticationToken? token = _authenticationTable?.AuthenticationTokens.SingleOrDefault(a => a.SecureInfo?.OscoreInfo?.KeyId == keyId);
            if(token == null)
            {
                _logger?.LogError($"Could not find authentication token for keyId '{keyId}'. Withdraw GroupMessage!");
                return;
            }
            if(token.Scope?.Contains(groupMessage.Value.GroupAddress) == false)
            {
                _logger?.LogError($"Authentication token with keyId '{keyId}' does not have access. Withdraw GroupMessage!");
                return;
            }
            byte[] sequenceBytes = new byte[4];
            int offset = 4 - keyContext.PartialIV.Length;
            for (int i = 0; i < keyContext.PartialIV.Length; i++)
                sequenceBytes[i + offset] = keyContext.PartialIV[i];
            int sequenceNumber = BitConverter.ToInt16(sequenceBytes.Reverse().ToArray());

            if(!_groupAddressSequenceNumber.ContainsKey(groupMessage.Value.GroupAddress))
            {
                _logger?.LogWarning($"Received unknown sequence number, but accept it. Got={sequenceNumber}");
                _groupAddressSequenceNumber[groupMessage.Value.GroupAddress] = sequenceNumber;
            }
            else
            {
                if(sequenceNumber <= _groupAddressSequenceNumber[groupMessage.Value.GroupAddress])
                {
                    _logger?.LogError($"Received same or lower sequence number: Got={sequenceNumber} Last={_groupAddressSequenceNumber[groupMessage.Value.GroupAddress]}");
                    return;
                }
            }

            IEnumerable<GroupObjectTableEntry> entries = _functionPointerTable?.GroupObjects.Where(r => r.GroupAddresses?.Contains(groupMessage.Value.GroupAddress) == true) ?? [];

            // 3/10/5 Table 19
            foreach (GroupObjectTableEntry entry in entries)
            {
                switch (groupMessage.Value.ServiceTypeCode)
                {
                    case "w":
                        {
                            if (!entry.GetFlag(Enums.CFlags.Write))
                            {
                                _logger?.LogWarning($"Entry is not writable");
                                return;
                            }
                            UpdateGroupObject(groupMessage, entry);
                            break;
                        }

                    case "r":
                        {
                            if (!entry.GetFlag(Enums.CFlags.Read))
                            {
                                _logger?.LogWarning($"Entry is not readable");
                                return;
                            }
                            throw new NotImplementedException("Reading group objects is not implemented yet");
                        }

                    case "a":
                        {
                            if (!entry.GetFlag(Enums.CFlags.Update))
                            {
                                _logger?.LogWarning($"Entry is not updateable");
                                return;
                            }
                            UpdateGroupObject(groupMessage, entry);
                            break;
                        }

                    default:
                        {
                            _logger?.LogError($"Unknown Service Type Code '{groupMessage.Value.ServiceTypeCode}'");
                            break;
                        }
                }
            }
        }

        private void UpdateGroupObject(GroupMessage groupMessage, GroupObjectTableEntry entry)
        {
            GroupMessageReceived?.Invoke(this, new GroupMessageEvent(groupMessage.SourceAddress, entry.Href!, groupMessage.Value.Value));
        }

        public void SendGroupMessage(string href, object value)
        {
            GroupObjectTableEntry? entry = _functionPointerTable?.GroupObjects.SingleOrDefault(e => e.Href == href);
            if (entry == null)
            {
                _logger?.LogError($"GroupObject '{href}' does not exist.");
                return;
            }

            uint groupAddress = entry.GroupAddresses?.FirstOrDefault() ?? 0;
            if(groupAddress == 0)
            {
                _logger?.LogInformation($"GroupObject '{href}' is not connected with a group address");
                return;
            }

            //groupAddress = 1;

            _logger?.LogInformation($"Send GroupAddress: Destination={groupAddress.ToString("X4")} Value={value}");

            GroupMessage message = new GroupMessage();
            message.SourceAddress = 0x2003; // TODO get real
            message.Value = new GroupMessageValue()
            {
                GroupAddress = groupAddress,
                ServiceTypeCode = "w",
                Value = value
            };
            byte[] payload = CborHelper.Serialize(message);


            RecipientPublisherEntry? publisher = _functionPointerTable?.GetResourceEntry<List<RecipientPublisherEntry>>("/p").SingleOrDefault(p => p.GroupAddresses.Contains(groupAddress));
            if(publisher == null)
            {
                _logger?.LogError($"GroupAddress {groupAddress:X4} has no puslisher entry");
                return;
            }


            long installationId = ResourceHelper.GetResourceEntry<long>(_context, "/dev/iid");
            string ip = "ff32:0030:fd";
            ip += (installationId >> 32 & 0xFF).ToString("x");
            ip += ":";
            ip += (installationId >> 16 & 0xFFFF).ToString("x");
            ip += ":";
            ip += (installationId & 0xFFFF).ToString("x");
            ip += ":";
            ip += "0000";
            ip += ":";
            ip += (publisher.GroupId >> 16 & 0xFFFF)?.ToString("x4");
            ip += ":";
            ip += (publisher.GroupId & 0xFFFF)?.ToString("x4");
            IPAddress ipaddr = IPAddress.Parse(ip);
            IPEndPoint remoteEndPoint = new IPEndPoint(ipaddr, 5683);


            AuthenticationToken? authtoken = _authenticationTable?.AuthenticationTokens.SingleOrDefault(t => t.Profile == Profiles.CoapOscore && t.Scope?.Contains(groupAddress) == true);
            if(authtoken == null)
            {
                _logger?.LogError($"GroupAddress {groupAddress:X4} has no token");
                return;
            }
            //authtoken.SecureInfo.OscoreInfo.KeyIdContext = "2001061aab03";

            string token = RandomNumberGenerator.GetHexString(16);
            CoapMessage coap = new CoapMessage(Method.POST, MessageType.NonConfirmable, payload, token);
            coap.Path = "/k";
            coap.Accept = Formats.ApplicationCbor;
            coap.AddOption(OptionDescription.ContentFormat, [(byte)Formats.ApplicationCbor]);

            byte[] partial = [partialIVcounter++];
            coap.Security = new (token,
                Convert.FromHexString(authtoken.SecureInfo?.OscoreInfo?.KeyId),
                Convert.FromHexString(authtoken.SecureInfo?.OscoreInfo?.KeyIdContext),
                partial);
            _ = _coapServer.SendAsync(coap, remoteEndPoint);
        }

        byte partialIVcounter = 0xa4;

        private async void Server_MessageReceived(object? sender, CoapMessage e)
        {
            if (sender == null)
                return;
            if (e.RemoteEndPoint == null)
                return;

            try
            {
                CoapServer server = (CoapServer)sender;

                if (!e.Path.StartsWith("/.") && e.Path.StartsWith($"/{_basePath}"))
                {
                    e.Path = e.Path.Substring(_basePath.Length);
                }

                if (e.Path == "/k")
                {
                    // This is a broadcast message (telegram with group address)
                    GroupMessage groupMessage = CborHelper.Deserialize<GroupMessage>(e.Payload);
                    ReceivedGroupMessage(e, groupMessage);
                    return;
                }

                // we got a resource request, check if we have it
                string resourcePath = e.Path.Substring(0, e.Path.IndexOf('/', 1));
                string resourceId = e.Path.Substring(resourcePath.Length);
                ResourceTable? resource = _resources.FirstOrDefault(r => r.Path == resourcePath);

                if (resource != null)
                {
                    ResourceResponse? resourceResponse = resource.GetResourceData(e.Method, resourceId, e);
                    if(resourceResponse == null)
                    {
                        // Resource exists but we should not send a response
                        _logger?.LogWarning("Resource returned that we should not send answer");
                        return;
                    }

                    if(resourceResponse.Payload == null)
                    {
                        _logger?.LogWarning($"Could not read resource '{e.Path}'");
                        CoapMessage responseFail = new(Method.NotFound, MessageType.Acknowledgement, [], e.Token)
                        {
                            Security = resourceResponse.RequireSecure ? new(e.Token) : null,
                            MessageId = e.MessageId,
                        };
                        await server.SendAsync(responseFail, e.RemoteEndPoint);
                        return;
                    }


                    CoapMessage response = new(resourceResponse.Method ?? Method.Changed, 
                        resourceResponse.Type ?? MessageType.Acknowledgement, 
                        resourceResponse.Payload, 
                        e.Token)
                    {
                        Security = resourceResponse.RequireSecure ? new(e.Token) : null,
                        MessageId = (resourceResponse.Type == MessageType.NonConfirmable) ? 0 : e.MessageId,
                    };


                    if(resourceResponse.Format != null)
                        response.AddOption(OptionDescription.ContentFormat, [(byte)resourceResponse.Format]);
                    else
                        response.AddOption(OptionDescription.ContentFormat, [(byte)Formats.ApplicationCbor]);

                    await server.SendAsync(response, e.RemoteEndPoint);

                }
                else
                {
                    _logger?.LogWarning($"Received request for unknown resource '{resourcePath}'");
                    CoapMessage response = new(Method.NotFound, MessageType.Acknowledgement, [], e.Token)
                    {
                        Security = new(e.Token),
                        MessageId = e.MessageId,
                    };
                    await server.SendAsync(response, e.RemoteEndPoint);
                }

                var x = "";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing CoAP message");
            }
        }

        private void RestartReceived(object? sender, int eraseCode)
        {
            if (_deviceTable == null)
            {
                _logger?.LogError("Device Table not initialized, cannot process restart request");
                return;
            }
            if (_wellKnownTable == null)
            {
                _logger?.LogError("Wellknown Table not initialized, cannot process restart request");
                return;
            }
            if (_applicationProgramTable == null)
            {
                _logger?.LogError("Application Program Table not initialized, cannot process restart request");
                return;
            }
            if (_actionTable == null)
            {
                _logger?.LogError("Action Table not initialized, cannot process restart request");
                return;
            }
            if (_parameterTable == null)
            {
                _logger?.LogError("Parameter Table not initialized, cannot process restart request");
                return;
            }
            if (_functionPointerTable == null)
            {
                _logger?.LogError("Function Pointer Table not initialized, cannot process restart request");
                return;
            }
            if (_authenticationTable == null)
            {
                _logger?.LogError("Function Pointer Table not initialized, cannot process restart request");
                return;
            }

            _deviceTable.EraseTable(eraseCode);
            _wellKnownTable.EraseTable(eraseCode);
            _applicationProgramTable.EraseTable(eraseCode);
            _actionTable.EraseTable(eraseCode);
            _parameterTable.EraseTable(eraseCode);
            _functionPointerTable.EraseTable(eraseCode);
            _authenticationTable.EraseTable(eraseCode);

            // TODO: Maybe we should reset the CoAP server?
            JoinGroupAddresses();
        }

        private void InitDeviceTable(InitialDeviceConfig config)
        {
            _logger?.LogInformation("Initializing device table...");

            _wellKnownTable = new WellKnownPointer(_context, _loggerFactory);
            _wellKnownTable.ResetReceived += RestartReceived;
            _resources.Add(_wellKnownTable);

            _deviceTable = new DevicePointer(_context, config, _loggerFactory);
            _deviceTable.SaveEntryDefault("/pwd", config.Password, ResourceTypes.String, true);
            _resources.Add(_deviceTable);

            _applicationProgramTable = new ApplicationProgramPointer(_context, _loggerFactory);
            _resources.Add(_applicationProgramTable);

            _actionTable = new ActionPointer(_context, _loggerFactory);
            _resources.Add(_actionTable);

            _parameterTable = new ParameterPointer(_context, _loggerFactory);
            _resources.Add(_parameterTable);

            _functionPointerTable = new FunctionsPointer(_context, _loggerFactory);
            _resources.Add(_functionPointerTable);

            _authenticationTable = new AuthenticationPointer(_context, _loggerFactory);
            _resources.Add(_authenticationTable);

            foreach (var key in _authenticationTable.AuthenticationTokens)
            {
                if (key.Profile != Enums.Profiles.CoapOscore)
                    continue;
                OscoreInfo info = key.SecureInfo?.OscoreInfo!;
                if(info.MasterSecret == null || info.KeyId == null || info.KeyIdContext == null)
                {
                    _logger?.LogError($"Invalid OSCORE key information for token {key.Id}");
                    continue;
                }
                byte[] keyId = Convert.FromHexString(info.KeyId);
                byte[] keyIdContext = Convert.FromHexString(info.KeyIdContext);
                OscoreKeyContext keyContext = new OscoreKeyContext(info.MasterSecret, [], keyId, keyIdContext);
                _coapServer.KeyStorage.AddKey(keyContext);
            }
        }

        private void JoinGroupAddresses()
        {
            if(_functionPointerTable == null)
            {
                _logger?.LogError("Function Pointer Table not initialized, cannot join group addresses");
                return;
            }

            List<uint> groupIds = new();
            List<RecipientPublisherEntry> entries = _functionPointerTable.Recipient;
            entries.AddRange(_functionPointerTable.Publisher);
            foreach (var entry in entries)
            {
                if (entry.GroupId == null)
                    continue;
                if (!groupIds.Contains(entry.GroupId.Value))
                    groupIds.Add(entry.GroupId.Value);
            }

            long installationId = ResourceHelper.GetResourceEntry<long>(_context, "/dev/iid");
            // ff 32 00 30 fd 5e 1e 4f e5 67 00 00 ac c5 57 31
            foreach (uint groupId in groupIds)
            {
                string ip = "ff32:0030:fd";
                ip += (installationId >> 32 & 0xFF).ToString("x");
                ip += ":";
                ip += (installationId >> 16 & 0xFFFF).ToString("x");
                ip += ":";
                ip += (installationId & 0xFFFF).ToString("x");
                ip += ":";
                ip += "0000";
                ip += ":";
                ip += (groupId >> 16 & 0xFFFF).ToString("x4");
                ip += ":";
                ip += (groupId & 0xFFFF).ToString("x4");
                IPAddress ipaddr = IPAddress.Parse(ip);
                _coapServer.AddMulticastAddress(ipaddr);
            }
        }
    }
}
