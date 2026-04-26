using System;
using System.Threading.Tasks;
using ShieldX.Models;

namespace ShieldX.Services
{
    public class SecurityScoreEngine
    {
        private static readonly Lazy<SecurityScoreEngine> _instance = new Lazy<SecurityScoreEngine>(() => new SecurityScoreEngine());
        public static SecurityScoreEngine Instance => _instance.Value;

        private SecurityScoreEngine()
        {
            // Start periodic score recalculation
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    RecalculateScore();
                }
            });
        }

        public void RecalculateScore()
        {
            int score = 100;

            // Module-based deductions
            if (!ModuleManager.Instance.IsActive("RealTimeProtection")) score -= 30;
            if (!ModuleManager.Instance.IsActive("RansomwareShield")) score -= 15;
            if (!ModuleManager.Instance.IsActive("WebShield")) score -= 10;
            if (!ModuleManager.Instance.IsActive("FirewallMonitor")) score -= 10;
            if (!ModuleManager.Instance.IsActive("ExploitGuard")) score -= 10;
            if (!ModuleManager.Instance.IsActive("EmailProtection")) score -= 5;
            if (!ModuleManager.Instance.IsActive("DNSFilter")) score -= 5;
            if (!ModuleManager.Instance.IsActive("BehaviorMonitor")) score -= 5;

            // Time-based deductions
            var lastScanAge = DateTime.Now - AppState.Instance.LastScanTime;
            if (lastScanAge > TimeSpan.FromDays(3)) score -= 10;

            // Ensure score is between 0 and 100
            score = Math.Max(0, Math.Min(100, score));

            AppState.Instance.SecurityScore = score;
        }
    }
}