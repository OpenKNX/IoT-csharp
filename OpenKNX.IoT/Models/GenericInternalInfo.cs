using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Models
{
    public class GenericInternalInfo
    {
        public string Id { get; set; }
        public string Info1 { get; set; }
        public string Info2 { get; set; }
        public string Info3 { get; set; }
        public string Info4 { get; set; }
        public string Info5 { get; set; }

        public List<string> InfoList { get; set; }


        public GenericInternalInfo(string id, string? info1 = null, string? info2 = null, string? info3 = null, string? info4 = null, string? info5 = null, List<string>? infoList = null)
        {
            Id = id;
            Info1 = info1 ?? string.Empty;
            Info2 = info2 ?? string.Empty;
            Info3 = info3 ?? string.Empty;
            Info4 = info4 ?? string.Empty;
            Info5 = info5 ?? string.Empty;
            InfoList = infoList ?? new();
        }
    }
}
