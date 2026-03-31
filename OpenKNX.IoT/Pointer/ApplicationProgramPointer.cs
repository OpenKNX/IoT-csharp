using OpenKNX.IoT.Database;
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
    [ResourceBase("/ap")]
    internal class ApplicationProgramPointer : ResourceTable
    {
        public ApplicationProgramPointer(ResourceContext db) : base(db) { }
        public ApplicationProgramPointer(ResourceContext db, ILoggerFactory? loggerFactory) : base(db, loggerFactory) { }

        internal override void InitTable()
        {
            SaveEntryDefault("/pv", new uint[] { 0x00, 0x00, 0x00 }, ResourceTypes.UnsignedIntegerArray);
        }

        [Resource(Method.GET, "/pv")]
        public byte[]? ProgramVersionGet()
        {
            uint[] value = GetResourceEntry<uint[]>("/pv");
            return ReturnByteArray(value);
        }

        [Resource(Method.PUT, "/pv")]
        public byte[]? ProgramVersionPut(CoapMessage request)
        {
            ProgrammVersionMessage message = CborHelper.Deserialize<ProgrammVersionMessage>(request.Payload);

            if (message.ProgramVersion == null)
                return null;

            SaveResourceEntry("/pv", message.ProgramVersion.ToArray());

            return [];
        }

    }
}
