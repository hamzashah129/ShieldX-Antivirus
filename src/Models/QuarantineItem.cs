using System;

namespace ShieldX.Models
{
    /// <summary>
    /// Represents an item in quarantine.
    /// </summary>
    public class QuarantineItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ThreatName { get; set; }
        public string ThreatType { get; set; }
        public string OriginalPath { get; set; }
        public string VaultPath { get; set; }
        public DateTime DateIsolated { get; set; } = DateTime.Now;
        public long? FileSize { get; set; }
        public string SHA256Hash { get; set; }
        public string Status { get; set; } = "Quarantined";
        public DateTime? RestoredAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
