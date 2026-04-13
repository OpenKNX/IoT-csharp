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
    internal class GroupObjectTable : Resource
    {
        private ILogger? _logger;
        private DeviceData _deviceData;

        public List<GroupObjectTableEntry> GroupObjects = new List<GroupObjectTableEntry>();

        public GroupObjectTable(DeviceData deviceData, ILoggerFactory? loggerFactory = null) : base("g")
        {
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger("Resource[/fp/g]");

            _deviceData = deviceData;

            _deviceData._resourceHelper.SaveEntryDefault("/fp/g", GroupObjects, ResourceTypes.Object);
        }

        protected override void DoPost(CoapExchange exchange)
        {
            List<GroupObjectTableEntry> messages = CborHelper.Deserialize<List<GroupObjectTableEntry>>(exchange.Request.Payload);

            bool isCreating = false;
            bool isDeleting = false;
            bool isChanging = false;
            bool isNotFound = false;

            foreach (GroupObjectTableEntry message in messages)
            {
                if (GroupObjects.Any(go => go.Id == message.Id))
                {
                    if (message.IsEmpty())
                    {
                        _logger?.LogInformation($"GroupObject entry delete:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        GroupObjects.RemoveAll(go => go.Id == message.Id);
                        isDeleting = true;
                    }
                    else
                    {
                        _logger?.LogInformation($"GroupObject entry update:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        GroupObjectTableEntry token = GroupObjects.First(go => go.Id == message.Id);
                        token.Update(message);
                        isChanging = true;
                    }
                }
                else
                {
                    if(message.IsEmpty())
                    {
                        _logger?.LogError($"GroupObject entry delete: not found {message.Id}");
                        isNotFound = true;
                        break;
                    }
                    _logger?.LogInformation($"GroupObject entry create:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                    GroupObjects.Add(message);
                    isCreating = true;
                }
            }

            _deviceData._resourceHelper.SaveResourceEntry("/fp/g", GroupObjects);

            if (isNotFound)
            {
                exchange.Respond(StatusCode.NotFound, []);
                return;
            }

            if(isChanging)
                exchange.Respond(StatusCode.Changed, []);
            else if (isCreating)
                exchange.Respond(StatusCode.Created, []);
            else
                exchange.Respond(StatusCode.Deleted, []);
        }
    }
}
