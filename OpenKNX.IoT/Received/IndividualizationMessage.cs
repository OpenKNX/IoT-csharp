using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Received
{
    internal class IndividualizationMessage
    {
        [CborKey(12)]
        public int? IndividualAddress { get; set; }
        [CborKey(26)]
        public long? InstallationId { get; set; }
        [CborKey(25)]
        public int? FID { get; set; }
    }
}
