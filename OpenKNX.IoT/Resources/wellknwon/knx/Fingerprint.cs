using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.Server.Resources;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace OpenKNX.IoT.Resources.wellknwon.knx
{
    internal class Fingerprint : Resource
    {
        private DeviceData _deviceData;

        public Fingerprint(DeviceData deviceData) : base("f")
        {
            _deviceData = deviceData;
        }

        protected override void DoGet(CoapExchange exchange)
        {
            string fpg = _deviceData._resourceHelper.GetResourceEntryObject<string>("/fp/g") ?? "";
            string fpr = _deviceData._resourceHelper.GetResourceEntryObject<string>("/fp/r") ?? "";
            string fpp = _deviceData._resourceHelper.GetResourceEntryObject<string>("/fp/p") ?? "";

            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes($"{fpg}-{fpr}-{fpp}");
            byte[] hash = sha.ComputeHash(bytes);
            ulong fingerprint = BitConverter.ToUInt64(hash, 0);

            exchange.Respond(StatusCode.Content, CborHelper.ReturnUnsignedBigInteger(fingerprint), MediaType.ApplicationCbor);
        }
    }
}
