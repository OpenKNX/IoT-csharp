using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Models
{
    public class InitialDeviceConfig
    {
        public string Serialnumber { get; set; }
        public string Password { get; set; }
        public int ManufacturerId { get; set; }
        public string HardwareType { get; set; }
        public string HardwareVersion { get; set; }
        public string FirmwareVersion { get; set; }
        public string Model { get; set; }

        public string KeyId { get; set; }
        public string KeyIdContext { get; set; }
        public string MasterSecret { get; set; }
    }
}
