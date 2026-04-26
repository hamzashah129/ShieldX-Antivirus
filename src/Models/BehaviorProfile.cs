using System;
using System.Collections.Generic;

namespace ShieldX.Models
{
    public class BehaviorProfile
    {
        public int Pid { get; set; }
        public string Name { get; set; } = "";
        public List<double> CpuSamples { get; set; } = new();
        public List<int> TcpConnectionCounts { get; set; } = new();
        public int FileWriteCount { get; set; }
        public DateTime FirstSeen { get; set; }
        public double BaselineCpu { get; set; }
        public double BaselineRam { get; set; }
    }
}
