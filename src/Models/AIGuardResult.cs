using System;

namespace ShieldX.Models
{
    public class AIGuardResult
    {
        public float ThreatScore { get; set; }
        public string ThreatClass { get; set; } = "Suspicious";
        public string Reason { get; set; } = "";
        public bool ShouldBlock { get; set; }
        public bool ShouldSuspend { get; set; }
        public ProcessSnapshot Process { get; set; }
        public DateTime DetectedAt { get; set; }
        public string ActionTaken { get; set; } = "";
        public float[] Indicators { get; set; } = new float[6];
    }
}
