using OpenKNX.CoAP.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Resources
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ResourceAttribute : Attribute
    {
        public Method Method { get; }
        public string Path { get; }
        public bool RequireSecure { get; }
        public int[]? EraseCodes { get; }

        public ResourceAttribute(Method method, string path, int[]? eraseCodes = null, bool requireSecure = true)
        {
            Method = method;
            Path = path;
            RequireSecure = requireSecure;
            EraseCodes = eraseCodes;
        }
    }
}
