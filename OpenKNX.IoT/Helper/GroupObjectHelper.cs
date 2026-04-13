using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.OSCOAP;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.COSE;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Models;
using Org.BouncyCastle.Cms;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Com.AugustCellars.CoAP.OSCOAP.SecurityContext;

namespace OpenKNX.IoT.Helper
{
    internal class GroupObjectHelper
    {
        private ILogger? _logger;
        private ResourceHelper _resourceHelper;
        private List<long> _lastSentMessageIds = new();

        public event EventHandler<GroupMessageEvent>? GroupMessageReceived;

        private Dictionary<string, SecurityContext> _receivingContexts = new();

        public GroupObjectHelper(ResourceHelper resourceHelper, ILoggerFactory? loggerFactory = null)
        {
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger<GroupObjectHelper>();

            _resourceHelper = resourceHelper;
        }

        public void HandleMessage(GroupMessage groupMessage, SecurityContext context)
        {
            string keyId = Convert.ToHexString(context.Recipient.Id);
            string keyIdContext = Convert.ToHexString(context.GroupId);
            _logger?.LogInformation($"GroupObject received: src={groupMessage.SourceAddress:X4} dst={groupMessage.Value.GroupAddress:X4} val={groupMessage.Value.Value} t={groupMessage.Value.ServiceTypeCode} keyId={keyId}");
        
            if(!CheckAuthenticationToken(keyId, keyIdContext, groupMessage.Value.GroupAddress))
            {
                _logger?.LogError($"KeyId is not assigned with the groupaddress");
                return;
            }

            // TODO: check partialIV as Sequencenumber. Question: where do i get it from?
            _logger?.LogWarning("Checking Sequencenumber is not implemented yet");

            IEnumerable<GroupObjectTableEntry> entries = _resourceHelper.GetResourceEntryObject<List<GroupObjectTableEntry>>("/fp/g")?.Where(r => r.GroupAddresses?.Contains(groupMessage.Value.GroupAddress) == true) ?? [];

            // 3/10/5 Table 19
            foreach (GroupObjectTableEntry entry in entries)
            {
                switch (groupMessage.Value.ServiceTypeCode)
                {
                    case "w":
                        {
                            if (!entry.GetFlag(Enums.CFlags.Write))
                            {
                                _logger?.LogWarning($"Entry '{entry.Href}' is not writable");
                                return;
                            }
                            UpdateGroupObject(groupMessage, entry);
                            break;
                        }

                    case "r":
                        {
                            if (!entry.GetFlag(Enums.CFlags.Read))
                            {
                                _logger?.LogWarning($"Entry '{entry.Href}' is not readable");
                                return;
                            }
                            throw new NotImplementedException("Reading group objects is not implemented yet");
                        }

                    case "a":
                        {
                            if (!entry.GetFlag(Enums.CFlags.Update))
                            {
                                _logger?.LogWarning($"Entry '{entry.Href}' is not updateable");
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

        public void SendGroupMessage(string href, object value)
        {
            GroupObjectTableEntry? entry = _resourceHelper.GetResourceEntryObject<List<GroupObjectTableEntry>>("/fp/g")?.SingleOrDefault(r => r.Href == href);
            if(entry == null)
            {
                _logger?.LogError($"No entry with href '{href}'");
                return;
            }

            uint groupAddress = entry.GroupAddresses?.First() ?? 0;
            if(groupAddress == 0)
            {
                _logger?.LogError($"Could not get sending groupaddress for href '{href}'");
                return;
            }

            _logger?.LogInformation($"Sending href '{href}': dst={groupAddress:X4} val={value}");

            RecipientPublisherEntry? publisher = _resourceHelper.GetResourceEntryObject<List<RecipientPublisherEntry>>("/fp/r")?.SingleOrDefault(p => p.GroupAddresses?.Contains(groupAddress) == true);
            if (publisher == null)
            {
                _logger?.LogError($"GroupAddress {groupAddress:X4} has no puslisher entry");
                return;
            }


            long installationId = _resourceHelper.GetResourceEntry<long>("/dev/iia") ?? 0;
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

            _logger?.LogInformation($"Publishing to {remoteEndPoint}");

            List<TokenEntry> tokens = _resourceHelper.GetResourceEntryObject<List<TokenEntry>>("/auth/at") ?? new();
            string groupAddressStr = groupAddress.ToString("X4");
            TokenEntry? token = tokens.SingleOrDefault(t => t.Profile == Enums.Profiles.CoapOscore && t.SendId == groupAddressStr);

            if(token == null)
            {
                _logger?.LogError($"Could not found token for groupaddress {groupAddressStr}");
                return;
            }

            long sequenceNumber = token.SequenceNumber;
            IncreaseSequenceNumber(token);


            GroupMessage message = new GroupMessage();
            message.Value = new GroupMessageValue(groupAddress, "w", value);
            message.SourceAddress = 0x2003;
            byte[] payload = CborHelper.Serialize(message);

            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);


            var wsli = interfaces.FirstOrDefault(i => i.Name.Contains("WSL"));
            if (wsli != null)
            {
                var ipv6s = wsli.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6);

                _ = SendRequest(payload, token, ipv6s.Address, remoteEndPoint, sequenceNumber);

                //endpoint.Start();
                //endpoint.SendRequest(request);
                //endpoint.Stop();
                //endpoint.Dispose();

                //request.Send(endpoint);
                //Response? response = null;
                //response = request.WaitForResponse();
            }

            //foreach (var nic in interfaces)
            //{
            //    var ipv6s = nic.GetIPProperties().UnicastAddresses
            //        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6);

            //    foreach (var ipv6 in ipv6s)
            //    {
            //        _logger?.LogInformation($"Sending on interface {nic.Name} from {ipv6.Address}");

            //        _ = SendRequest(payload, token, ipv6.Address, remoteEndPoint, sequencenumbersending++);
            //    }
            //}
        }

        private void IncreaseSequenceNumber(TokenEntry token)
        {
            List<TokenEntry> tokens = _resourceHelper.GetResourceEntryObject<List<TokenEntry>>("/auth/at") ?? new();
            TokenEntry? tokenToUpdate = tokens.SingleOrDefault(t => t.Profile == Enums.Profiles.CoapOscore && t.SendId == token.SendId && t.Id == token.Id);
            if (tokenToUpdate == null)
            {
                _logger?.LogError($"Could not find token to increase sequencenumber: id={token.Id} idContext={token.KeyIdContext}");
                return;
            }
            tokenToUpdate.SequenceNumber++;
            _resourceHelper.SaveResourceEntry("/auth/at", tokens);
            long sequencenumbersending = token.SequenceNumber++;
            _resourceHelper.SaveResourceEntry("/auth/at", tokens);
        }

        private async Task SendRequest(byte[] payload, TokenEntry token, IPAddress localIPAddress, IPEndPoint remoteEndPoint, long sequenceNumber, byte[]? echo = null)
        {

            var ctx = SecurityContext.DeriveContext(token.MasterSecret, Convert.FromHexString(token.KeyIdContext), Convert.FromHexString(token.SendId), []);
            ctx.GroupId = Convert.FromHexString(token.KeyIdContext);
            //ctx.SetIsGroupContext(true);
            ctx.Sender.SequenceNumber = sequenceNumber;
            ctx.OscoreEvents += HandleOscoreEvents;

            Request request = new(Method.POST, false);
            request.ContentFormat = MediaType.ApplicationCbor;
            request.Accept = MediaType.ApplicationCbor;
            request.Type = MessageType.NON;
            request.URI = new Uri($"coap://{remoteEndPoint}/k");
            request.Payload = payload;
            request.OscoreContext = ctx;

            if(echo != null)
            {
                Option opt = Option.Create(OptionType.Echo, echo);
                request.AddOption(opt);
            }

            CoapConfig config = new();
            config.TokenLength = 8;
            var endpoint = new CoAPEndPoint(new IPEndPoint(localIPAddress, 0), config);

            endpoint.Start();
            request.EndPoint = endpoint;
            endpoint.ReceivingResponse += Endpoint_ReceivingResponse;

            request.Send(endpoint);
            Response? response = null;
            DateTime timestamp = DateTime.UtcNow;

            while( response != null || (DateTime.UtcNow - timestamp).TotalSeconds < 5)
            {
                response = request.WaitForResponse(5000);
                if(response != null)
                {
                    if(response.StatusCode == StatusCode.Unauthorized && response.Echo != null)
                    {
                        _logger?.LogInformation($"Got Echo Request from {response.Source}");
                        _ = SendRequest(payload, token, localIPAddress, (IPEndPoint)response.Source, sequenceNumber + 1, response.Echo);
                        IncreaseSequenceNumber(token);
                        continue;
                    }
                    _logger?.LogInformation($"Received Response: {response}");
                }
            }
            endpoint.Stop();
            endpoint.Dispose();
        }

        private void Endpoint_ReceivingResponse(object? sender, MessageEventArgs<Response> e)
        {
            
        }

        private void HandleOscoreEvents(object? sender, OscoreEvent e)
        {
            if (e.Code == OscoreEvent.EventCode.UnknownGroupIdentifier)
            {
                string keyId = Convert.ToHexString(e.KeyIdentifier);
                string groupId = Convert.ToHexString(e.GroupIdentifier);
                List<TokenEntry> entries = _resourceHelper.GetResourceEntryObject<List<TokenEntry>>("/auth/at") ?? new();

                TokenEntry? token = entries.SingleOrDefault(t => t.ReceiveId == keyId && t.KeyIdContext == groupId);
                if(token == null)
                {
                    if(groupId.Length > 14)
                    {
                        // this is a echo response with a random groupId
                        token = entries.SingleOrDefault(t => t.SendId == keyId);

                        if(token == null)
                        {
                            _logger?.LogError($"OSCORE event UnknownGroupIdentifier: could not answer echo response for keyId={keyId} groupId={groupId}");
                            return;
                        }

                        var ctx = SecurityContext.DeriveContext(token.MasterSecret, e.GroupIdentifier, e.KeyIdentifier, e.KeyIdentifier);
                        ctx.GroupId = e.GroupIdentifier;
                        ctx.OscoreEvents += HandleOscoreEvents;
                        e.SecurityContext = ctx;
                        return;
                    }
                    _logger?.LogWarning($"OSCORE event UnknownGroupIdentifier: could not find token for keyId={keyId} keyIdCtx={groupId}");
                    return;
                }

                if (_receivingContexts.ContainsKey(groupId))
                {
                    e.SecurityContext = _receivingContexts[groupId];
                    return;
                }

                //var ctx = SecurityContext.DeriveContext(token.MasterSecret, e.GroupIdentifier, e.KeyIdentifier, e.KeyIdentifier);
                //ctx.GroupId = e.GroupIdentifier;
                //ctx.OscoreEvents += Handle2OscoreEvents;
                //e.SecurityContext = ctx;
                ////TODO set sequencenumber?
                //_receivingContexts[groupId] = ctx;
                return;
            }
            else if(e.Code == OscoreEvent.EventCode.UnknownKeyIdentifier)
            {
                string keyId = Convert.ToHexString(e.KeyIdentifier);
                string groupId = Convert.ToHexString(e.GroupIdentifier);
                List<TokenEntry> entries = _resourceHelper.GetResourceEntryObject<List<TokenEntry>>("/auth/at") ?? new();
                TokenEntry? token = entries.SingleOrDefault(t => t.ReceiveId == keyId && t.KeyIdContext == groupId);
                if (token == null)
                {
                    if (groupId.Length > 14)
                    {
                        // this is a echo response with a random groupId
                        token = entries.SingleOrDefault(t => t.SendId == keyId);

                        if (token == null)
                        {
                            _logger?.LogError($"OSCORE event UnknownGroupIdentifier: could not answer echo response for keyId={keyId} groupId={groupId}");
                            return;
                        }

                        var x = SecurityContext.DeriveEntityContext(token.MasterSecret, e.GroupIdentifier, e.KeyIdentifier, []);
                        e.RecipientContext = x;
                        return;
                    }
                }

                _logger?.LogError($"OSCORE event UnknownKeyIdentifier: received message with unknown keyId={keyId} groupId={groupId}");
            }
            else
            {
                _logger?.LogWarning($"Received unknown OSCORE event: {e.Code}");
            }
        }

        private void UpdateGroupObject(GroupMessage groupMessage, GroupObjectTableEntry entry)
        {
            _logger?.LogInformation($"Updating entry '{entry.Href}' with value '{groupMessage.Value.Value}'");
            GroupMessageReceived?.Invoke(this, new GroupMessageEvent(groupMessage.SourceAddress, entry.Href!, groupMessage.Value.Value));
        }

        private bool CheckAuthenticationToken(string keyId, string groupId, uint groupAddress)
        {
            List<TokenEntry> tokens = _resourceHelper.GetResourceEntryObject<List<TokenEntry>>("/auth/at") ?? new();
            TokenEntry? token = tokens.SingleOrDefault(t => t.Profile == Enums.Profiles.CoapOscore && t.KeyIdContext == groupId && (t.SendId == keyId || t.ReceiveId == keyId));

            if (token == null)
                return false;

            return token.Scope.Contains(groupAddress);
        }
    }
}
