using System;
using System.Collections.Generic;

namespace ShieldX.Models
{
    public class ProcessSnapshot
    {
        public int Pid { get; set; }
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public int ParentPid { get; set; }
        public double CpuPercent { get; set; }
        public long RamMb { get; set; }
        public int ThreadCount { get; set; }
        public List<string> TcpConnections { get; set; } = new();
        public string CommandLine { get; set; } = "";
        public bool IsSigned { get; set; }
        public string SignerName { get; set; } = "";
        public DateTime CapturedAt { get; set; }
        public string UserAccount { get; set; } = "";
    }
}
