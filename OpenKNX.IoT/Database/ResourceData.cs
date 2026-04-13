using OpenKNX.IoT.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpenKNX.IoT.Database
{
    internal class ResourceData
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public byte[] Default { get; set; } = Array.Empty<byte>();
        public ResourceTypes ResourceType { get; set; }
        public int[]? EraseCodes { get; set; } = null;
    }
}
