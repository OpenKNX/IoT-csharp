using OpenKNX.CoAP.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ResourceBaseAttribute : Attribute
    {
        public string Path { get; }
        public bool RequireSecure { get; }

        public ResourceBaseAttribute(string path, bool requireSecure = true)
        {
            Path = path;
            RequireSecure = requireSecure;
        }
    }
}