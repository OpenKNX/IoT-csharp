using OpenKNX.IoT.Database;
using OpenKNX.IoT.Enums;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.CoAP;
using OpenKNX.CoAP.Enums;
using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Text;

namespace OpenKNX.IoT.Pointer
{
    [ResourceBase("/auth", true)]
    internal class AuthenticationPointer : ResourceTable
    {
        public event EventHandler<LoadStateMachineStates>? ApplicationStateChanged;

        public List<AuthenticationToken> AuthenticationTokens = new List<AuthenticationToken>();

        public AuthenticationPointer(ResourceContext db) : base(db) { }
        public AuthenticationPointer(ResourceContext db, ILoggerFactory? loggerFactory) : base(db, loggerFactory) { }

        internal override void InitTable()
        {
            //SaveEntryDefault("/lsm", LoadStateMachineStates.Unloaded, ResourceTypes.Integer);
            SaveEntryDefault("/at", AuthenticationTokens, ResourceTypes.Object);
            SaveEntryDefault("/o/replwdo", 32, ResourceTypes.UnsignedInteger);
            SaveEntryDefault("/o/osndelay", 1000, ResourceTypes.UnsignedInteger);

            AuthenticationTokens = GetResourceEntry<List<AuthenticationToken>>("/at");
        }

        [Resource(Method.GET, "/o/replwdo")]
        public byte[]? OscoreReplayWindowGet()
        {
            uint window = GetResourceEntry<uint>("/o/replwdo");
            return ReturnUnsignedInteger(window);
        }

        [Resource(Method.GET, "/o/osndelay")]
        public byte[]? OscoreRandomDelayGet()
        {
            uint window = GetResourceEntry<uint>("/o/osndelay");
            return ReturnUnsignedInteger(window);
        }

        [Resource(Method.POST, "/at")]
        public ResourceResponse? LoadStateMachinePost(CoapMessage request)
        {
            List<AuthenticationToken> messages = CborHelper.Deserialize<List<AuthenticationToken>>(request.Payload);

            foreach(AuthenticationToken message in messages)
            {
                if(AuthenticationTokens.Any(at => at.Id == message.Id))
                {
                    if(message.IsEmpty())
                    {
                        _logger?.LogInformation($"Authentication token delete:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        AuthenticationTokens.RemoveAll(at => at.Id == message.Id);
                    }
                    else
                    {
                        _logger?.LogInformation($"Authentication token update:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        AuthenticationToken token = AuthenticationTokens.First(at => at.Id == message.Id);
                        token.Update(message);
                    }
                }
                else
                {
                    _logger?.LogInformation($"Authentication token create:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                    AuthenticationTokens.Add(message);
                }
            }

            SaveResourceEntry("/at", AuthenticationTokens);

            ResourceResponse response = new ResourceResponse();
            response.Method = Method.Created;
            response.Payload = [];
            return response;
        }
    }
}
