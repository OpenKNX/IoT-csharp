using OpenKNX.IoT.Database;
using OpenKNX.IoT.Enums;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Resources;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using OpenKNX.CoAP;
using OpenKNX.CoAP.Enums;
using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Text;

namespace OpenKNX.IoT.Pointer
{
    [ResourceBase("/fp")]
    internal class FunctionsPointer : ResourceTable
    {
        public event EventHandler<LoadStateMachineStates>? ApplicationStateChanged;

        public List<GroupObjectTableEntry> GroupObjects = new List<GroupObjectTableEntry>();
        public List<RecipientPublisherEntry> Recipient = new List<RecipientPublisherEntry>();
        public List<RecipientPublisherEntry> Publisher = new List<RecipientPublisherEntry>();

        public FunctionsPointer(ResourceContext db) : base(db) { }
        public FunctionsPointer(ResourceContext db, ILoggerFactory? loggerFactory) : base(db, loggerFactory) { }

        internal override void InitTable()
        {
            SaveEntryDefault("/g", Recipient, ResourceTypes.Object);
            SaveEntryDefault("/r", Recipient, ResourceTypes.Object);
            SaveEntryDefault("/p", Publisher, ResourceTypes.Object);

            GroupObjects = GetResourceEntry<List<GroupObjectTableEntry>>("/g");
            Recipient = GetResourceEntry<List<RecipientPublisherEntry>>("/r");
            Publisher = GetResourceEntry<List<RecipientPublisherEntry>>("/p");
        }

        [Resource(Method.POST, "/g")]
        public ResourceResponse? GroupObjectTablePost(CoapMessage request)
        {
            List<GroupObjectTableEntry> messages = CborHelper.Deserialize<List<GroupObjectTableEntry>>(request.Payload);

            foreach (GroupObjectTableEntry message in messages)
            {
                if (GroupObjects.Any(go => go.Id == message.Id))
                {
                    if (message.IsEmpty())
                    {
                        _logger?.LogInformation($"Recipient entry delete:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        GroupObjects.RemoveAll(go => go.Id == message.Id);
                    }
                    else
                    {
                        _logger?.LogInformation($"Recipient entry update:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        GroupObjectTableEntry token = GroupObjects.First(go => go.Id == message.Id);
                        token.Update(message);
                    }
                }
                else
                {
                    _logger?.LogInformation($"Recipient entry create:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                    GroupObjects.Add(message);
                }
            }

            SaveResourceEntry("/g", GroupObjects);

            ResourceResponse response = new ResourceResponse();
            response.Method = Method.Created;
            response.Payload = [];
            return response;
        }

        [Resource(Method.POST, "/r")]
        public ResourceResponse? RecipientTablePost(CoapMessage request)
        {
            List<RecipientPublisherEntry> messages = CborHelper.Deserialize<List<RecipientPublisherEntry>>(request.Payload);

            foreach (RecipientPublisherEntry message in messages)
            {
                if (Recipient.Any(r => r.Id == message.Id))
                {
                    if (message.IsEmpty())
                    {
                        _logger?.LogInformation($"Recipient entry delete:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        Recipient.RemoveAll(r => r.Id == message.Id);
                    } else
                    {
                        _logger?.LogInformation($"Recipient entry update:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        RecipientPublisherEntry token = Recipient.First(r => r.Id == message.Id);
                        token.Update(message);
                    }
                } else
                {
                    _logger?.LogInformation($"Recipient entry create:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                    Recipient.Add(message);
                }
            }

            SaveResourceEntry("/r", Recipient);

            ResourceResponse response = new ResourceResponse();
            response.Method = Method.Created;
            response.Payload = [];
            return response;
        }

        [Resource(Method.POST, "/p")]
        public ResourceResponse? PublisherTablePost(CoapMessage request)
        {
            List<RecipientPublisherEntry> messages = CborHelper.Deserialize<List<RecipientPublisherEntry>>(request.Payload);

            foreach (RecipientPublisherEntry message in messages)
            {
                if (Publisher.Any(p => p.Id == message.Id))
                {
                    if (message.IsEmpty())
                    {
                        _logger?.LogInformation($"Publisher entry delete:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        Publisher.RemoveAll(p => p.Id == message.Id);
                    }
                    else
                    {
                        _logger?.LogInformation($"Publisher entry update:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                        RecipientPublisherEntry token = Publisher.First(p => p.Id == message.Id);
                        token.Update(message);
                    }
                }
                else
                {
                    _logger?.LogInformation($"Publisher entry create:\n\t{System.Text.Json.JsonSerializer.Serialize(message)}");
                    Publisher.Add(message);
                }
            }

            SaveResourceEntry("/p", Publisher);

            ResourceResponse response = new ResourceResponse();
            response.Method = Method.Created;
            response.Payload = [];
            return response;
        }
    }
}
