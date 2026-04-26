namespace ShieldX.Models
{
    public enum ThreatSeverity
    {
        Low,
        Medium,
        High,
        Unknown
    }

    public class ThreatResult
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public ThreatSeverity Severity { get; set; }

        public string SeverityLabel => Severity switch
        {
            ThreatSeverity.High => "HIGH",
            ThreatSeverity.Medium => "MEDIUM",
            ThreatSeverity.Low => "LOW",
            _ => "UNKNOWN"
        };

        public string DisplayIcon => Severity switch
        {
            ThreatSeverity.High => "🔴",
            ThreatSeverity.Medium => "🟡",
            ThreatSeverity.Low => "🟢",
            _ => "⚪"
        };
    }
}
