using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;

namespace OpenKNX.IoT.Resources.wellknwon
{
    internal class WellknownCore : Resource
    {
        private DeviceData _deviceData;

        public WellknownCore(DeviceData deviceData) : base("core")
        {
            _deviceData = deviceData;
            Attributes.Title = "Wellknown Core Resource";
        }

        protected override void DoGet(CoapExchange exchange)
        {
            string? query = exchange.Request.UriQueries.FirstOrDefault();
            if (query == null)
                return;

            string[] parts = query.Split('=');

            if (parts[0] == "ep")
            {
                string epValue = parts[1];
                if (epValue.StartsWith("knx://sn."))
                {
                    if (epValue != $"knx://sn.{_deviceData.Serialnumber.ToLower()}")
                        return;


                    string payload = $"<>;ep=\"knx://sn.{_deviceData.Serialnumber} knx://ia.{_deviceData.InstallationId:x}.{_deviceData.IndividualAddress:x4}\"";
                    exchange.Respond(StatusCode.Content, payload, MediaType.ApplicationLinkFormat);
                }

                if (epValue.StartsWith("knx://ia."))
                {
                    if (epValue != $"knx://ia.{_deviceData.InstallationId:x}.{_deviceData.IndividualAddress:x4}")
                        return;

                    string responseText = $"<>;ep=\"knx://sn.{_deviceData.Serialnumber.ToLower()} knx://ia.{_deviceData.InstallationId:x}.{_deviceData.IndividualAddress:x4}\"";
                    
                    exchange.Respond(StatusCode.Content, Encoding.UTF8.GetBytes(responseText), MediaType.ApplicationLinkFormat);
                }
            }

        }
    }
}
