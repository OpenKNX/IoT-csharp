using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Enums;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Received;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources.a
{
    internal class LoadStateMachine : Resource
    {
        private ILogger? _logger;
        private DeviceData _deviceData;

        public LoadStateMachine(DeviceData deviceData, ILoggerFactory? loggerFactory = null) : base("lsm")
        {
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger("Resource[/a/lsm]");

            _deviceData = deviceData;
        }

        protected override void DoGet(CoapExchange exchange)
        {
            byte[] payload = CborHelper.ReturnInteger((int)_deviceData.LoadStateMachine, 3);
            exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationCbor);
        }

        protected override void DoPost(CoapExchange exchange)
        {
            LoadStateMachineMessage message = CborHelper.Deserialize<LoadStateMachineMessage>(exchange.Request.Payload);

            LoadStateMachineStates oldState = _deviceData.LoadStateMachine;
            switch (message.Event)
            {
                case LoadStateMachineEvents.LoadComplete:
                    {
                        _logger?.LogInformation("Set to: Loaded");
                        _deviceData.SetLoadStateMachine(LoadStateMachineStates.Loaded);
                        break;
                    }

                case LoadStateMachineEvents.StartLoading:
                    {
                        _logger?.LogInformation("Set to: Loading");
                        _deviceData.SetLoadStateMachine(LoadStateMachineStates.Loading);
                        break;
                    }

                case LoadStateMachineEvents.Unload:
                    {
                        //ResetForeignResourceEntry("/fp/g");
                        //ResetForeignResourceEntry("/fp/r");
                        //ResetForeignResourceEntry("/fp/p");
                        //ResetForeignResourceEntry("/p");
                        _logger?.LogInformation("Set to: Unloaded");
                        _deviceData.SetLoadStateMachine(LoadStateMachineStates.Unloaded);
                        break;
                    }

                default:
                    _logger?.LogError("Unsupported lsm event: " + message.Event);
                    return;
            }

            byte[] payload = CborHelper.ReturnInteger((int)_deviceData.LoadStateMachine, 3);
            exchange.Respond(StatusCode.Changed, payload, MediaType.ApplicationCbor);
        }
    }
}
