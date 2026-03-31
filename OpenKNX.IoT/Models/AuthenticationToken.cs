using OpenKNX.IoT.Enums;
using OpenKNX.IoT.Received;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Models
{
    internal class AuthenticationToken
    {
        [CborKey(0)]
        public string? Id { get; set; }
        [CborKey(8)]
        public SecureInfo? SecureInfo { get; set; }
        [CborKey(9)]
        public List<uint>? Scope { get; set; }
        [CborKey(38)]
        public Profiles? Profile { get; set; }

        public bool IsEmpty()
        {
            return SecureInfo == null && Scope == null && Profile == null;
        }

        public void Update(AuthenticationToken newToken)
        {
            if (newToken.SecureInfo != null)
                SecureInfo = newToken.SecureInfo;
            if (newToken.Scope != null)
                Scope = newToken.Scope;
            if (newToken.Profile != null)
                Profile = newToken.Profile;
        }
    }
}
