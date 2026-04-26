# ShieldX Error Handling & Feature Integration Guide

## Quick Start Overview

This guide explains how to integrate the new error handling framework and feature improvements into existing ShieldX services.

## 1. Complete File List of New Additions

### Exception Framework
- `src/Exceptions/ShieldXException.cs` - Base exception class
- `src/Exceptions/DatabaseException.cs` - Database operation exceptions
- `src/Exceptions/ScanException.cs` - Scan operation exceptions
- `src/Exceptions/QuarantineException.cs` - Quarantine operation exceptions
- `src/Exceptions/ServiceException.cs` - Service initialization exceptions
- `src/Exceptions/EncryptionException.cs` - Security operation exceptions

### Utility Frameworks
- `src/Utils/ResilienceUtility.cs` - Retry logic with exponential backoff
- `src/Utils/CircuitBreaker.cs` - Circuit breaker pattern
- `src/Utils/PerformanceMonitor.cs` - Performance tracking
- `src/Utils/ValidationUtility.cs` - Input validation
- `src/Utils/ServiceHealthMonitor.cs` - Service health tracking
- `src/Utils/ErrorHandler.cs` - User error display
- `src/Utils/ServiceInitializationManager.cs` - Async initialization management
- `src/Utils/ConfigurationValidator.cs` - Configuration validation
- `src/Utils/CacheManager.cs` - Generic caching with expiration
- `src/Utils/BackgroundTaskCoordinator.cs` - Background task management
- `src/Utils/DiagnosticLogger.cs` - Enhanced diagnostic logging

## 2. Integration Patterns by Component

### Pattern 1: Async Operations with Retry

**Old Pattern:**
```csharp
public async Task<string> LookupThreatAsync(string sha256)
{
    using var connection = OpenConnection();
    using var cmd = new SqliteCommand("SELECT ...", connection);
    var result = await cmd.ExecuteScalarAsync();
    return result?.ToString();
}
```

**New Pattern:**
```csharp
public async Task<string> LookupThreatAsync(string sha256)
{
    try
    {
        sha256 = ValidationUtility.ValidateNotNullOrEmpty(sha256, nameof(sha256));
        
        return await ResilienceUtility.ExecuteWithRetryAsync(
            async () =>
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand("SELECT ...", connection);
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString();
            },
            operationName: "Lookup threat",
            maxRetries: 2);
    }
    catch (Exception ex)
    {
        throw new DatabaseException("lookup", "Failed to lookup threat", ex);
    }
}
```

**Key Changes:**
1. Input validation with `ValidationUtility`
2. Wrapped in `ResilienceUtility.ExecuteWithRetryAsync()`
3. Proper exception hierarchy with `DatabaseException`

### Pattern 2: Scan Operations with Error Handling

**Example: Enhance ScanEngine.QuickScanAsync()**

```csharp
public async Task<ScanResult> QuickScanAsync(
    IProgress<ScanProgress> progress,
    CancellationToken ct)
{
    using (DiagnosticLogger.Enter("QuickScan", "Starting quick scan"))
    {
        try
        {
            var result = new ScanResult
            {
                ScanType = "Quick Scan",
                StartTime = DateTime.Now
            };

            // Validate scan target
            ValidationUtility.ValidateDiskSpace(Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop), minFreeMb: 50);

            var targets = BuildScanTargets();
            
            // Use performance monitoring
            await PerformanceMonitor.MeasureAsync(
                async () => await PerformScanAsync(targets, result, progress, ct),
                operationName: "Quick scan execution",
                slowThresholdMs: 30000);

            return result;
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError(ex, "Quick scan failed");
            throw new ScanException("Quick", "Quick scan failed", ex);
        }
    }
}
```

### Pattern 3: Service Initialization

**Example: Replace service initialization in App.xaml.cs**

```csharp
private async Task InitializeServicesAsync()
{
    var manager = new ServiceInitializationManager();
    
    manager.AddStep(
        "Database Service",
        async () =>
        {
            DatabaseService.Instance.InitializeDatabase();
            await Task.CompletedTask;
        },
        required: true);

    manager.AddStep(
        "Scan Engine",
        async () =>
        {
            await ScanEngine.Instance.InitializeAsync();
        },
        required: true);

    manager.AddStep(
        "Real-time Protection",
        async () =>
        {
            RealTimeProtectionService.Instance.Start();
            await Task.CompletedTask;
        },
        required: false,
        failureMessage: "Real-time protection initialization failed");

    manager.ProgressChanged += (progress) =>
    {
        MainWindow?.UpdateInitializationProgress(progress);
    };

    bool success = await manager.InitializeAsync();
    
    if (!success)
    {
        var progress = manager.GetProgress();
        foreach (var error in progress.Errors)
        {
            ErrorHandler.HandleException(error.Exception, 
                error.WasRequired ? "Critical Error" : "Warning");
        }
    }
}
```

### Pattern 4: Error Handling & Recovery

**Example: Enhance App.xaml.cs exception handlers**

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // Global exception handler with recovery
    AppDomain.CurrentDomain.UnhandledException += (s, args) =>
    {
        var ex = (Exception)args.ExceptionObject;
        _logger.Fatal(ex, "Unhandled exception");
        
        if (ErrorHandler.TryRecover(ex))
        {
            _logger.Information("Recovery attempt initiated");
        }
        else
        {
            ErrorHandler.HandleException(ex, "Critical Application Error");
            Environment.Exit(1);
        }
    };

    DispatcherUnhandledException += (s, args) =>
    {
        ErrorHandler.HandleException(args.Exception, "UI Error");
        args.Handled = true;
    };

    // Validate configuration before starting
    var configValidation = ConfigurationValidator.ValidateApplicationConfiguration();
    if (!configValidation.IsValid)
    {
        foreach (var error in configValidation.Errors)
        {
            _logger.Fatal(error);
        }
        MessageBox.Show("Application configuration is invalid. Please reinstall.",
            "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }

    // Continue with normal startup...
}
```

### Pattern 5: Background Tasks

**Example: Schedule periodic scans**

```csharp
private void SetupBackgroundTasks()
{
    var coordinator = new BackgroundTaskCoordinator();
    coordinator.Start();

    // Schedule daily full scan
    var dailyScanTask = new BackgroundTaskCoordinator.BackgroundTask
    {
        Name = "Daily Full Scan",
        Action = async (ct) => await ScanEngine.Instance.FullScanAsync(
            new Progress<ScanProgress>(), ct),
        Priority = BackgroundTaskCoordinator.TaskPriority.Normal,
        IsRecurring = true,
        RecurrenceInterval = TimeSpan.FromHours(24),
        MaxRetries = 2
    };

    // Schedule health check every 30 minutes
    var healthCheckTask = new BackgroundTaskCoordinator.BackgroundTask
    {
        Name = "System Health Check",
        Action = async (ct) => await PerformHealthCheckAsync(ct),
        Priority = BackgroundTaskCoordinator.TaskPriority.High,
        IsRecurring = true,
        RecurrenceInterval = TimeSpan.FromMinutes(30),
        MaxRetries = 1
    };

    coordinator.QueueTask(dailyScanTask);
    coordinator.QueueTask(healthCheckTask);
}
```

### Pattern 6: Health Monitoring

**Example: Monitor service health**

```csharp
private void SetupHealthMonitoring()
{
    var healthMonitor = new ServiceHealthMonitor();

    // Database health check
    healthMonitor.RegisterHealthCheck("DatabaseService", () =>
    {
        try
        {
            var count = DatabaseService.Instance.GetQuarantineCountAsync().Result;
            return new ServiceHealthMonitor.HealthReport
            {
                ComponentName = "DatabaseService",
                Status = ServiceHealthMonitor.HealthStatus.Healthy,
                Message = $"Database connected, {count} quarantine items"
            };
        }
        catch (Exception ex)
        {
            return new ServiceHealthMonitor.HealthReport
            {
                ComponentName = "DatabaseService",
                Status = ServiceHealthMonitor.HealthStatus.Critical,
                Message = $"Database error: {ex.Message}"
            };
        }
    });

    // Real-time protection health check
    healthMonitor.RegisterHealthCheck("RealTimeProtection", () =>
    {
        var isActive = RealTimeProtectionService.Instance.IsActive;
        return new ServiceHealthMonitor.HealthReport
        {
            ComponentName = "RealTimeProtection",
            Status = isActive ? ServiceHealthMonitor.HealthStatus.Healthy : ServiceHealthMonitor.HealthStatus.Warning,
            Message = isActive ? "Real-time protection active" : "Real-time protection disabled"
        };
    });

    // Check overall health periodically
    var overallHealth = healthMonitor.GetOverallHealth();
    if (overallHealth != ServiceHealthMonitor.HealthStatus.Healthy)
    {
        // Alert user or take action
    }
}
```

### Pattern 7: Caching with Expiration

**Example: Cache threat database lookups**

```csharp
private readonly CacheManager<string, string> _threatCache = 
    new(defaultExpiration: TimeSpan.FromHours(1));

public async Task<string> LookupThreatAsync(string sha256)
{
    return await _threatCache.GetOrCreateAsync(
        sha256,
        async () => await DatabaseService.Instance.LookupThreatAsync(sha256),
        expiration: TimeSpan.FromHours(24));
}
```

### Pattern 8: Diagnostic Logging

**Example: Enhanced logging in critical operations**

```csharp
public async Task<ScanResult> ScanFileAsync(string filePath)
{
    using (DiagnosticLogger.Enter("ScanFile", filePath))
    {
        try
        {
            DiagnosticLogger.LogInfo("Starting file scan");
            
            var fileInfo = new System.IO.FileInfo(filePath);
            DiagnosticLogger.LogMetric("FileSize", fileInfo.Length / 1024.0, "KB");

            await DiagnosticLogger.LogTimingAsync(
                "File scan analysis",
                async () => await AnalyzeFileAsync(filePath));

            DiagnosticLogger.LogStateChange("ScanFile", "Running", "Completed");
            return new ScanResult { /* ... */ };
        }
        catch (Exception ex)
        {
            ExceptionLogger.LogExceptionWithRecoveryAdvice(ex);
            throw;
        }
    }
}
```

## 3. Service-Specific Integration Checklist

### DatabaseService
- [ ] Use `ValidationUtility` for input validation
- [ ] Wrap all async operations with `ResilienceUtility.ExecuteWithRetryAsync()`
- [ ] Use custom `DatabaseException` for all errors
- [ ] Add disk space validation before operations
- [ ] Add performance monitoring for slow queries

### ScanEngine
- [ ] Validate scan paths with `ConfigurationValidator`
- [ ] Use `ResilienceUtility` for file I/O operations
- [ ] Use `DiagnosticLogger` for detailed logging
- [ ] Handle and wrap exceptions with `ScanException`
- [ ] Add progress reporting with performance metrics

### RealTimeProtectionService
- [ ] Register health checks with `ServiceHealthMonitor`
- [ ] Use circuit breaker for external APIs
- [ ] Add timeout handling with `ResilienceUtility`
- [ ] Log security events with `DomainEventLogger`
- [ ] Enable graceful degradation on failures

### QuarantineManager
- [ ] Validate vault settings with `ConfigurationValidator`
- [ ] Use custom `QuarantineException` for errors
- [ ] Add retry logic for file operations
- [ ] Cache vault entries with `CacheManager`
- [ ] Log quarantine events with `DomainEventLogger`

### App.xaml.cs
- [ ] Use `ServiceInitializationManager` for startup
- [ ] Validate configuration with `ConfigurationValidator`
- [ ] Setup background task coordinator
- [ ] Register health checks for all services
- [ ] Enhanced error handlers with recovery

## 4. Migration Path

1. **Phase 1**: Update exception handling in App.xaml.cs
2. **Phase 2**: Add exception hierarchy to core services
3. **Phase 3**: Integrate retry logic for I/O operations
4. **Phase 4**: Add health monitoring and diagnostics
5. **Phase 5**: Implement background task coordination
6. **Phase 6**: Add UI for health status display

## 5. Testing Recommendations

1. Test exception scenarios with manual fault injection
2. Verify retry logic with network timeouts
3. Test health checks with service failures
4. Validate cache expiration mechanisms
5. Test background task execution and recovery
6. Verify logging output and diagnostics
7. Load test with concurrent operations

## 6. Performance Considerations

- Circuit breaker reduces cascading failures
- Caching improves read performance
- Async operations prevent UI blocking
- Health monitoring enables proactive fixes
- Diagnostic logging adds ~5% overhead

---

**Framework Version**: ShieldX 3.1.0  
**Last Updated**: 2026-04-12
