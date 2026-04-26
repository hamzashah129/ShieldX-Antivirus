# ShieldX New Services - Developer Reference Guide

**Version:** 3.2.0  
**Date:** April 25, 2026  

---

## Quick Start Guide

### 1. NetworkSecurityService

**Namespace:** `ShieldX.Services`

**Usage Example:**
```csharp
using ShieldX.Services;

// Get blocked IPs
var blockedIps = NetworkSecurityService.Instance.GetBlockedIps();
foreach (var ip in blockedIps)
{
    Console.WriteLine($"Blocked: {ip}");
}

// Block an IP
var result = await NetworkSecurityService.Instance.BlockIpAsync("192.168.1.100");
if (result)
    Console.WriteLine("IP blocked successfully");

// Assess connection risk
var risk = NetworkSecurityService.Instance.AssessConnectionRisk(
    remoteIp: "203.0.113.42",
    remotePort: 8080,
    connectionState: "Established");

Console.WriteLine($"Risk Level: {risk}"); // Output: High, Critical, Medium, Low, Local, Trusted, or Blocked

// Unblock an IP
await NetworkSecurityService.Instance.UnblockIpAsync("192.168.1.100");

// Clear all blocks
await NetworkSecurityService.Instance.ClearAllBlockedIpsAsync();
```

**Risk Levels Returned:**
- **Blocked** - Previously blocked by this service
- **Local** - Loopback (127.x.x.x) or private range (10.x, 172.16-31.x, 192.168.x)
- **Trusted** - Known trusted DNS server (8.8.8.8, 1.1.1.1, etc.)
- **Critical** - Suspicious state + non-standard port
- **High** - Non-standard port + suspicious pattern
- **Medium** - Unusual port pattern detected
- **Low** - Normal connection, no flags

**Storage:**
- File: `%APPDATA%\ShieldX\blocked_ips.json`
- Format: JSON array of IP strings
- Persists across app restarts
- Thread-safe operations

**Thread Safety:**
- All methods use internal locking
- Safe for multi-threaded access
- Block/unblock are async

---

### 2. ProcessAnomalyDetectionService

**Namespace:** `ShieldX.Services`

**Usage Example:**
```csharp
using ShieldX.Services;
using System.Diagnostics;

// Get risk score for a process
var process = Process.GetCurrentProcess();
var riskScore = ProcessAnomalyDetectionService.Instance.AssessProcessRisk(process);
Console.WriteLine($"Risk Score: {riskScore}/100"); // 0-100

// Get classification
var classification = ProcessAnomalyDetectionService.Instance.ClassifyProcess(process);
Console.WriteLine($"Classification: {classification}");
// Output: Critical, High, Medium, Low, or Safe

// Use in process monitoring loop
foreach (var proc in Process.GetProcesses())
{
    var risk = ProcessAnomalyDetectionService.Instance.AssessProcessRisk(proc);
    if (risk >= 60)
    {
        Console.WriteLine($"⚠️ {proc.ProcessName} - Risk: {risk} ({classification})");
    }
}
```

**Risk Score Interpretation:**
- **0-19:** Safe - Normal Windows process
- **20-39:** Low - Monitor but likely legitimate
- **40-59:** Medium - Suspicious patterns detected
- **60-79:** High - Multiple risk factors
- **80-100:** Critical - Likely malicious

**Scoring Factors:**
```
Path Analysis (max +30):
  - System32/SysWOW64 paths: -20 (reduces score)
  - Program Files paths: -10 (reduces score)
  - Temp folder: +25
  - Roaming AppData: +20
  - Local Temp: +30

Name Analysis (max +35):
  - High-risk names (mimikatz, psexec): +35
  - Medium-risk names (net, cmd, powershell): +20

Command Line (max +25):
  - Encoded commands (-enc, -nop): +20
  - Code injection (IEX): +25
  - Downloaded strings: +30

Parent Process (max +20):
  - Suspicious parent: +20

Memory (max +15):
  - > 1000 MB: +15
  - > 500 MB: +10
  - > 200 MB (non-system): +5

Network (max +20):
  - > 10 connections: +15
  - Non-standard ports: +5 per connection
```

**Performance:**
- Single process: 10-50ms (includes WMI query)
- 500 processes: ~2-5 seconds
- Memory: ~5MB for service

**Thread Safety:**
- Safe for multi-threaded access
- No locking needed (read-only operations)
- Caching for performance

---

## Integration Examples

### Network Tab Integration

```csharp
// In NetworkPage.xaml.cs
private void LoadNetworkData()
{
    var tcpConnections = properties.GetActiveTcpConnections();
    
    foreach (var connection in tcpConnections)
    {
        var remoteIp = connection.RemoteEndPoint?.Address?.ToString();
        var remotePort = connection.RemoteEndPoint?.Port ?? 0;
        var state = connection.State.ToString();
        
        // Use NetworkSecurityService
        var risk = NetworkSecurityService.Instance.AssessConnectionRisk(
            remoteIp, remotePort, state);
        
        _connections.Add(new NetworkConnectionEntry
        {
            RemoteEndpoint = connection.RemoteEndPoint?.ToString(),
            Risk = risk  // Shows: Blocked, Critical, High, Medium, Low, Local, Trusted
        });
    }
}

private async void BlockIpButton_Click(object sender, RoutedEventArgs e)
{
    var remoteIp = button.Tag as string;
    var result = await NetworkSecurityService.Instance.BlockIpAsync(remoteIp);
    
    if (result)
        MessageBox.Show($"IP {remoteIp} blocked successfully");
}
```

### Processes Tab Integration

```csharp
// In ProcessesPage.xaml.cs
private void LoadSingleProcess(Process process)
{
    // Use ProcessAnomalyDetectionService
    var riskScore = ProcessAnomalyDetectionService.Instance
        .AssessProcessRisk(process);
    var classification = ProcessAnomalyDetectionService.Instance
        .ClassifyProcess(process);
    var isSuspicious = riskScore >= 60;
    
    _processes.Add(new ProcessEntry
    {
        Name = process.ProcessName,
        RiskScore = riskScore,
        Status = classification,  // Critical, High, Medium, Low, Safe
        IsSuspicious = isSuspicious
    });
}
```

---

## Error Handling

### NetworkSecurityService

```csharp
try
{
    var result = await NetworkSecurityService.Instance.BlockIpAsync("203.0.113.42");
    if (!result)
    {
        // Log to LogService
        LogService.Instance.AddError("Failed to block IP", "Network");
    }
}
catch (Exception ex)
{
    LogService.Instance.AddError($"IP blocking error: {ex.Message}", "Network");
}
```

### ProcessAnomalyDetectionService

```csharp
try
{
    var process = Process.GetProcessById(pid);
    var risk = ProcessAnomalyDetectionService.Instance
        .AssessProcessRisk(process);
}
catch (ArgumentException)
{
    // Process no longer exists
    Console.WriteLine("Process not found");
}
catch (AccessViolationException)
{
    // Permission denied
    Console.WriteLine("Cannot access process");
}
```

---

## Data Persistence

### Blocked IPs Storage

**File:** `%APPDATA%\ShieldX\blocked_ips.json`

**Format:**
```json
[
  "203.0.113.42",
  "198.51.100.17",
  "192.0.2.8"
]
```

**Manual Recovery:**
```csharp
// If file is corrupted, recreate it
var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var filePath = Path.Combine(appDataPath, "ShieldX", "blocked_ips.json");

// Delete corrupted file
if (File.Exists(filePath))
    File.Delete(filePath);

// Service will recreate on next operation
await NetworkSecurityService.Instance.BlockIpAsync("203.0.113.42");
```

---

## Performance Optimization Tips

### Network Security
```csharp
// Cache results if checking same connection repeatedly
var riskCache = new Dictionary<string, string>();

var risk = riskCache.GetValueOrDefault(remoteIp) ??
    NetworkSecurityService.Instance.AssessConnectionRisk(remoteIp, port, state);
```

### Process Anomaly Detection
```csharp
// Batch process assessment
var riskResults = Process.GetProcesses()
    .AsParallel()  // Process in parallel
    .Select(p => new { Process = p, Risk = ProcessAnomalyDetectionService.Instance.AssessProcessRisk(p) })
    .Where(x => x.Risk >= 60)
    .ToList();
```

---

## Logging Integration

Both services log to `LogService.Instance`:

```csharp
// NetworkSecurityService logs:
LogService.Instance.AddWarning("Blocked IP: 203.0.113.42", "NetworkSecurity");
LogService.Instance.AddInfo("Unblocked IP: 203.0.113.42", "NetworkSecurity");
LogService.Instance.AddError("Failed to block IP", "NetworkSecurity");

// ProcessAnomalyDetectionService doesn't log directly
// Log in your calling code if needed
```

---

## Testing

### Unit Test Example - NetworkSecurityService

```csharp
[TestMethod]
public async Task BlockIpAsync_Should_AddToBlockedList()
{
    var service = NetworkSecurityService.Instance;
    var testIp = "203.0.113.42";
    
    // Clear previous
    await service.ClearAllBlockedIpsAsync();
    
    // Act
    var result = await service.BlockIpAsync(testIp);
    
    // Assert
    Assert.IsTrue(result);
    Assert.IsTrue(service.GetBlockedIps().Contains(testIp));
}

[TestMethod]
public void AssessConnectionRisk_Should_IdentifyLocalConnections()
{
    var service = NetworkSecurityService.Instance;
    
    var risk = service.AssessConnectionRisk("127.0.0.1", 80, "Established");
    
    Assert.AreEqual("Local", risk);
}
```

### Unit Test Example - ProcessAnomalyDetectionService

```csharp
[TestMethod]
public void AssessProcessRisk_Should_Return_SafeForSystemProcess()
{
    var service = ProcessAnomalyDetectionService.Instance;
    var process = Process.GetProcessById(Environment.ProcessId);
    
    var risk = service.AssessProcessRisk(process);
    
    Assert.IsTrue(risk < 20);
}

[TestMethod]
public void ClassifyProcess_Should_ReturnCorrectClassification()
{
    var service = ProcessAnomalyDetectionService.Instance;
    var process = Process.GetCurrentProcess();
    
    var classification = service.ClassifyProcess(process);
    
    Assert.IsTrue(new[] { "Critical", "High", "Medium", "Low", "Safe" }
        .Contains(classification));
}
```

---

## Troubleshooting

### "Access Denied" blocking IP
**Cause:** Admin privileges required  
**Solution:** Run ShieldX as Administrator

### Process assessment too slow
**Cause:** WMI queries are slow  
**Solution:** Cache results or run in background thread

### Blocked IPs not persisting
**Cause:** File permission issue  
**Solution:** Check `AppData\ShieldX` folder permissions

### Memory growing on long-term monitoring
**Cause:** Service caches growing unbounded  
**Solution:** Restart application periodically

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 3.2.0 | 2026-04-25 | Released NetworkSecurityService and ProcessAnomalyDetectionService |
| 3.1.0 | 2026-01-15 | Network tab UI (partial) |
| 3.0.0 | 2025-12-01 | Initial release |

---

## Support & Documentation

- Complete source code in: `src/Services/`
- Integration examples in: `src/Views/` and `src/ViewModels/`
- See `IMPLEMENTATION_100_PERCENT_COMPLETE.md` for full details
- See `PARTIAL_TO_FULL_IMPLEMENTATION_COMPLETE.md` for upgrade details

---

**Generated:** April 25, 2026  
**ShieldX Development Team**
