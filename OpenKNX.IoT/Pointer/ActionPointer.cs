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

namespace OpenKNX.IoT.Pointer
{
    [ResourceBase("/a")]
    internal class ActionPointer : ResourceTable
    {
        public event EventHandler<LoadStateMachineStates>? ApplicationStateChanged;

        public ActionPointer(ResourceContext db) : base(db) { }
        public ActionPointer(ResourceContext db, ILoggerFactory? loggerFactory) : base(db, loggerFactory) { }

        internal override void InitTable()
        {
            SaveEntryDefault("/lsm", LoadStateMachineStates.Unloaded, ResourceTypes.Integer);
        }

        [Resource(Method.GET, "/lsm", [2, 7])]
        public byte[]? LoadStateMachineGet()
        {
            LoadStateMachineStates state = GetResourceEntry<LoadStateMachineStates>("/lsm");
            return ReturnInteger((int)state, 3);
        }

        [Resource(Method.POST, "/lsm")]
        public byte[]? LoadStateMachinePost(CoapMessage request)
        {
            LoadStateMachineMessage message = CborHelper.Deserialize<LoadStateMachineMessage>(request.Payload);

            LoadStateMachineStates state = GetResourceEntry<LoadStateMachineStates>("/lsm");
            LoadStateMachineStates oldState = state;

            switch (message.Event)
            {
                case LoadStateMachineEvents.LoadComplete:
                    {
                        state = LoadStateMachineStates.Loaded;
                        SaveResourceEntry("/lsm", state);
                        break;
                    }

                case LoadStateMachineEvents.StartLoading:
                    {
                        state = LoadStateMachineStates.Loading;
                        SaveResourceEntry("/lsm", state);
                        break;
                    }

                case LoadStateMachineEvents.Unload:
                    {
                        ResetForeignResourceEntry("/fp/g");
                        ResetForeignResourceEntry("/fp/r");
                        ResetForeignResourceEntry("/fp/p");
                        ResetForeignResourceEntry("/p");
                        state = LoadStateMachineStates.Unloaded;
                        SaveResourceEntry("/lsm", state);
                        break;
                    }

                default:
                    _logger?.LogError("Unsupported lsm event: " + message.Event);
                    return null;
            }

            if(oldState != state)
            {
                ApplicationStateChanged?.Invoke(this, state);
            }

            return ReturnInteger((int)state, 3);
        }
    }
}
