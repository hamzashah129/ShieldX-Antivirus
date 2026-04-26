using System;

namespace ShieldX.Models
{
    public class ScanHistoryItem
    {
        public string Id { get; set; }
        public string ScanType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int FilesScanned { get; set; }
        public int ThreatsFound { get; set; }
        public string Duration { get; set; }
        public string ScanPath { get; set; }
        public string Status { get; set; }

        public string ResultLabel => ThreatsFound == 0 ? "✅ Clean" : $"{ThreatsFound} threats";
        public string DateLabel => StartTime.ToString("MMM d HH:mm");
    }
}
