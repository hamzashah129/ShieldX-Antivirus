using System;
using System.Collections.Generic;

namespace ShieldX.Models
{
    public class EngineResult
    {
        public string EngineName  { get; set; } = "";
        public string Result      { get; set; } = "";  
        // "Clean", "Malicious", "Suspicious", "Unknown"
        public string Category    { get; set; } = "";
        public bool   IsClean     => Result == "Clean" || 
                                     Result == "Undetected";
        public bool   IsMalicious => Result == "Malicious";
        public bool   IsSuspicious=> Result == "Suspicious";
    }

    public class ThreatScanReport
    {
        public string   Target        { get; set; } = "";
        public string   TargetType    { get; set; } = ""; 
        // "URL", "File", "Hash", "IP", "Domain"
        public DateTime ScannedAt     { get; set; }
        public int      TotalEngines  { get; set; }
        public int      MaliciousCount{ get; set; }
        public int      SuspiciousCount{get; set; }
        public int      CleanCount    { get; set; }
        public string   OverallRating { get; set; } = "";
        // "Safe", "Suspicious", "Dangerous", "Unknown"
        public string   ThreatName    { get; set; } = "";
        public List<EngineResult> EngineResults { get; set; } = new();
        public Dictionary<string, string> Details { get; set; } = new();
    }
}
