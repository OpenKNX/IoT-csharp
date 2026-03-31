using OpenKNX.CoAP.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Received
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class CborKeyAttribute : Attribute
    {
        public int Key { get; set; }

        public CborKeyAttribute(int key)
        {
            Key = key;
        }
    }
}