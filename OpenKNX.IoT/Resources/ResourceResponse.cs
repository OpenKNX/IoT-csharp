using OpenKNX.CoAP.Enums;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources
{
    internal class ResourceResponse
    {
        public Method? Method { get; set; }
        public MessageType? Type { get; set; }
        public Formats? Format { get; set; }
        public byte[]? Payload { get; set; }
        public bool RequireSecure { get; set; }
    }
}