using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.auth
{
    internal class AuthTokens : Resource
    {
        private ILogger? _logger;
        private DeviceData _deviceData;
        private List<TokenEntry> AuthenticationTokens = new List<TokenEntry>();

        public AuthTokens(DeviceData deviceData, ILoggerFactory? loggerFactory = null) : base("at")
        {
            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger("Resource[/auth/at]");

            _deviceData = deviceData;

            _deviceData._resourceHelper.SaveEntryDefault("/auth/at", AuthenticationTokens, ResourceTypes.Object);
        }

        protected override void DoPost(CoapExchange exchange)
        {
            List<AuthenticationToken> messages = CborHelper.Deserialize<List<AuthenticationToken>>(exchange.Request.Payload);

            bool isCreating = false;
            bool isDeleting = false;
            bool isChanging = false;
            bool isNotFound = false;

            foreach (AuthenticationToken message in messages)
            {
                if (AuthenticationTokens.Any(at => at.Id == message.Id))
                {
                    if (message.IsEmpty())
                    {
                        _logger?.LogInformation($"Authentication token delete:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        AuthenticationTokens.RemoveAll(at => at.Id == message.Id);
                        isDeleting = true;
                    }
                    else
                    {
                        _logger?.LogInformation($"Authentication token update:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        TokenEntry token = AuthenticationTokens.First(at => at.Id == message.Id);
                        token.Update(message);
                        isChanging = true;
                    }
                }
                else
                {
                    if (message.IsEmpty())
                    {
                        _logger?.LogError($"Authentication token delete: not found {message.Id}");
                        isNotFound = true;
                        break;
                    }
                    _logger?.LogInformation($"Authentication token create:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                    TokenEntry entry = new TokenEntry(message);
                    AuthenticationTokens.Add(entry);
                    isCreating = true;
                }
            }

            _deviceData._resourceHelper.SaveResourceEntry("/auth/at", AuthenticationTokens);

            if (isNotFound)
            {
                exchange.Respond(StatusCode.NotFound, []);
                return;
            }

            if (isChanging)
                exchange.Respond(StatusCode.Changed, []);
            else if (isCreating)
                exchange.Respond(StatusCode.Created, []);
            else if (isDeleting)
                exchange.Respond(StatusCode.Deleted, []);

            _logger?.LogDebug($"Authentication token: Created: {isCreating}, Changed: {isChanging}, Deleted: {isDeleting}");
        }
    }
}
