using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Models
{
    public class DeviceInfo
    {
        public string PhysicalAddress { get; set; }
        public string SerialNumber { get; set; }
        public string InstallationId { get; set; }
        public string Hostname { get; set; }
        public string LoadStateMachine { get; set; }
        public string Password { get; set; }
        public bool ProgMode { get; set; }

        public DeviceInfo(string physicalAddress, string serialNumber, string installationId, string hostname, string loadStateMachine, string password, bool progMode)
        {
            PhysicalAddress = physicalAddress;
            SerialNumber = serialNumber;
            InstallationId = installationId;
            Hostname = hostname;
            LoadStateMachine = loadStateMachine;
            Password = password;
            ProgMode = progMode;
        }
    }
}
