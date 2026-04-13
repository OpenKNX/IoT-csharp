using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.fp
{
    internal class PublisherTable : Resource
    {
        private ILogger? _logger;
        private DeviceData _deviceData;

        private List<RecipientPublisherEntry> Publishers = new List<RecipientPublisherEntry>();

        public PublisherTable(DeviceData deviceData, ILoggerFactory? loggerFactory = null) : base("p")
        {
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger("Resource[/fp/p]");

            _deviceData = deviceData;

            _deviceData._resourceHelper.SaveEntryDefault("/fp/p", Publishers, ResourceTypes.Object);
        }

        protected override void DoPost(CoapExchange exchange)
        {
            List<RecipientPublisherEntry> messages = CborHelper.Deserialize<List<RecipientPublisherEntry>>(exchange.Request.Payload);

            bool isCreating = false;
            bool isDeleting = false;
            bool isChanging = false;
            bool isNotFound = false;

            foreach (RecipientPublisherEntry message in messages)
            {
                if (Publishers.Any(go => go.Id == message.Id))
                {
                    if (message.IsEmpty())
                    {
                        _logger?.LogInformation($"Publisher entry delete:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        Publishers.RemoveAll(go => go.Id == message.Id);
                        isDeleting = true;
                    }
                    else
                    {
                        _logger?.LogInformation($"Publisher entry update:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        RecipientPublisherEntry token = Publishers.First(go => go.Id == message.Id);
                        token.Update(message);
                        isChanging = true;
                    }
                }
                else
                {
                    if(message.IsEmpty())
                    {
                        _logger?.LogError($"Publisher entry delete: not found {message.Id}");
                        isNotFound = true;
                        break;
                    }
                    _logger?.LogInformation($"Publisher entry create:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                    Publishers.Add(message);
                    isCreating = true;
                }
            }

            _deviceData._resourceHelper.SaveResourceEntry("/fp/p", Publishers);

            if (isNotFound)
            {
                exchange.Respond(StatusCode.NotFound, []);
                return;
            }

            if(isChanging)
                exchange.Respond(StatusCode.Changed, []);
            else if (isCreating)
                exchange.Respond(StatusCode.Created, []);
            else if(isDeleting)
                exchange.Respond(StatusCode.Deleted, []);

            _logger?.LogDebug($"Publisher entry: Created: {isCreating}, Changed: {isChanging}, Deleted: {isDeleting}");
        }
    }
}
