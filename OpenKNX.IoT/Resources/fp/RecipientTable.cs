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
    internal class RecipientTable : Resource
    {
        private ILogger? _logger;
        private DeviceData _deviceData;

        private List<RecipientPublisherEntry> Recipients = new List<RecipientPublisherEntry>();

        public RecipientTable(DeviceData deviceData, ILoggerFactory? loggerFactory = null) : base("r")
        {
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger("Resource[/fp/r]");

            _deviceData = deviceData;

            _deviceData._resourceHelper.SaveEntryDefault("/fp/r", Recipients, ResourceTypes.Object);
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
                if (Recipients.Any(go => go.Id == message.Id))
                {
                    if (message.IsEmpty())
                    {
                        _logger?.LogInformation($"Recipient entry delete:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        Recipients.RemoveAll(go => go.Id == message.Id);
                        isDeleting = true;
                    }
                    else
                    {
                        _logger?.LogInformation($"Recipient entry update:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        RecipientPublisherEntry token = Recipients.First(go => go.Id == message.Id);
                        token.Update(message);
                        isChanging = true;
                    }
                }
                else
                {
                    if(message.IsEmpty())
                    {
                        _logger?.LogError($"Recipient entry delete: not found {message.Id}");
                        isNotFound = true;
                        break;
                    }
                    _logger?.LogInformation($"Recipient entry create:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                    Recipients.Add(message);
                    isCreating = true;
                }
            }

            _deviceData._resourceHelper.SaveResourceEntry("/fp/r", Recipients);

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

            _logger?.LogDebug($"Recipient entry: Created: {isCreating}, Changed: {isChanging}, Deleted: {isDeleting}");
        }
    }
}
