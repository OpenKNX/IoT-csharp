using OpenKNX.IoT.Database;
using OpenKNX.IoT.Enums;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Received;
using OpenKNX.IoT.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.CoAP;
using OpenKNX.CoAP.Enums;
using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Text;
using OpenKNX.IoT.Models;

namespace OpenKNX.IoT.Pointer
{
    [ResourceBase("/p", true)]
    internal class ParameterPointer : ResourceTable
    {
        public event EventHandler<LoadStateMachineStates>? ApplicationStateChanged;

        private ResourceContext _context;

        public ParameterPointer(ResourceContext db) : base(db)
        {
            _context = db;
        }

        public ParameterPointer(ResourceContext db, ILoggerFactory? loggerFactory) : base(db, loggerFactory)
        {
            _context = db;
        }

        internal override void InitTable()
        {
            SaveEntryDefault("/globalTestParameter", 0, ResourceTypes.UnsignedInteger);
        }

        //[Resource(Method.GET, "/lsm", [2, 7])]
        //public byte[]? LoadStateMachineGet()
        //{
        //    LoadStateMachineStates state = GetResourceEntry<LoadStateMachineStates>("/lsm");
        //    return ReturnInteger((int)state, 3);
        //}


        public List<GenericInternalInfo> GetAllParameters()
        {
            List<GenericInternalInfo> parameters = new();
            foreach(var entry in _context.Resources.Where(p => p.Id.StartsWith("/p/")).ToList())
            {
                parameters.Add(new(entry.Id, entry.ResourceType.ToString(), BitConverter.ToString(entry.Data)));
            }
            return parameters;
        }

        [Resource(Method.GET, "*")]
        public ResourceResponse? HandleNotFoundGet(CoapMessage request, string path)
        {
            return null;
        }

        [Resource(Method.PUT, "*")]
        public ResourceResponse? HandleNotFoundPut(CoapMessage request, string path)
        {
            Option? content = request.Options.SingleOrDefault(o => o.Description == OptionDescription.ContentFormat);
            if (content == null)
            {
                return null;
            }

            Formats format = (Formats)content.Payload[0];

            switch (format)
            {
                case Formats.ApplicationCbor:
                    {
                        ReadParameterCbor(path, request.Payload);
                        break;
                    }

                default:
                    _logger?.LogError("Unsupported Format in Parameter Request");
                    return null;
            }

            ResourceResponse response = new ResourceResponse();
            response.Method = Method.Changed;
            response.Payload = [];
            return response;
        }

        private void ReadParameterCbor(string path, byte[] data)
        {
            CborReader reader = new CborReader(data);
            reader.ReadStartMap();
            int key = reader.ReadInt32();
            object propValue;

            CborReaderState state = reader.PeekState();
            switch(state)
            {
                case CborReaderState.UnsignedInteger:
                    {
                        uint value = reader.ReadUInt32();
                        propValue = value;
                        SaveResourceEntry(path, value);
                        break;
                    }

                default:
                    _logger?.LogError($"Unsupported type in Cbor '{state}'");
                    return;
            }

            _logger?.LogInformation($"Received new Parameter '{path}' with value: {propValue}");
        }
    }
}
