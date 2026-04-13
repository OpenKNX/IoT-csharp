using OpenKNX.IoT.Enums;
using OpenKNX.IoT.Received;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpenKNX.IoT.Models
{
    internal class TokenEntry
    {
        public string Id { get; set; } = string.Empty;
        public List<uint> Scope { get; set; } = new();
        public Profiles Profile { get; set; }
        public string ReceiveId { get; set; } = string.Empty;
        public string SendId { get; set; } = string.Empty;
        public byte[] MasterSecret { get; set; } = Array.Empty<byte>();
        public uint Algo { get; set; }
        public string KeyIdContext { get; set; } = string.Empty;
        public long SequenceNumber { get; set; }

        public TokenEntry() { }

        public TokenEntry(AuthenticationToken authenticationToken)
        {
            Id = authenticationToken.Id ?? throw new ArgumentException("Authentication token must have an Id");
            Scope = authenticationToken.Scope ?? throw new ArgumentException("Authentication token must have a Scope");
            Profile = authenticationToken.Profile ?? throw new ArgumentException("Authentication token must have a Profile");
            SendId = authenticationToken.SecureInfo?.OscoreInfo?.KeyId ?? throw new ArgumentException("Authentication token must have a KeyId");
            MasterSecret = authenticationToken.SecureInfo?.OscoreInfo?.MasterSecret ?? throw new ArgumentException("Authentication token must have a MasterSecret");
            Algo = authenticationToken.SecureInfo?.OscoreInfo?.Algo ?? 10;
            KeyIdContext = authenticationToken.SecureInfo?.OscoreInfo?.KeyIdContext ?? throw new ArgumentException("Authentication token must have a KeyIdContext");
        }

        public void Update(AuthenticationToken newToken)
        {
            if (newToken.SecureInfo?.OscoreInfo?.KeyId != null)
            {
                SendId = newToken.SecureInfo.OscoreInfo.KeyId;
                ReceiveId = newToken.SecureInfo.OscoreInfo.KeyId;
            }
            if (newToken.SecureInfo?.OscoreInfo?.MasterSecret != null)
                MasterSecret = newToken.SecureInfo.OscoreInfo.MasterSecret;
            if (newToken.SecureInfo?.OscoreInfo?.Algo != null)
                Algo = (uint)newToken.SecureInfo.OscoreInfo.Algo;
            if (newToken.SecureInfo?.OscoreInfo?.KeyIdContext != null)
                KeyIdContext = newToken.SecureInfo.OscoreInfo.KeyIdContext;
            if (newToken.Scope != null)
                Scope = newToken.Scope;
            if (newToken.Profile != null)
                Profile = (Profiles)newToken.Profile;
        }

    }
}
