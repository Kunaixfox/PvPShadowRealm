using System;
using System.Text.Json.Serialization;

namespace Teh.BHUD.PvPShadowRealmModule.Models
{
    public class BlacklistedPlayer
    {
        public enum BlacklistReason
        {
            Scam,
            RMT,
            GW2E,
            Other,
            Unknown
        }

        private string _ign;

        public string Ign
        {
            get => _ign;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("IGN cannot be empty or null.");
                _ign = value.Trim().ToLowerInvariant();
            }
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BlacklistReason Reason { get; set; } = BlacklistReason.Unknown;

        public string Notes { get; set; }

        public BlacklistedPlayer() { }

        public BlacklistedPlayer(string ign, BlacklistReason reason = BlacklistReason.Unknown, string notes = "")
        {
            Ign = ign;
            Reason = reason;
            Notes = notes;
        }

        public override string ToString()
        {
            return $"IGN: {Ign}, Reason: {Reason}, Notes: {Notes}";
        }
    }
}
