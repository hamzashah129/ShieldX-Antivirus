# Dashboard Tab Analysis

## Overview
The Dashboard is the main landing page of ShieldX Antivirus, providing a real-time summary of system security status, active protection modules, and recent threat detections. It implements an MVVM architecture with data binding to live system state.

---

## Architecture

### Files Structure
- **View**: [src/Views/DashboardPage.xaml](src/Views/DashboardPage.xaml) - UI definition (WPF Page)
- **Code-behind**: [src/Views/DashboardPage.xaml.cs](src/Views/DashboardPage.xaml.cs) - Minimal logic (just initialization)
- **ViewModel**: [src/ViewModels/DashboardViewModel.cs](src/ViewModels/DashboardViewModel.cs) - Business logic & data binding
- **Models**: 
  - [src/Models/AlertItem.cs](src/Models/AlertItem.cs) - Represents individual threat alerts
  - [src/Models/AppState.cs](src/Models/AppState.cs) - Global application state (singleton)

### MVVM Pattern
- **Code-behind** creates `DashboardViewModel` and sets it as `DataContext`
- **ViewModel** exposes `ObservableCollection` properties for UI binding
- **Models** use `INotifyPropertyChanged` for reactive updates
- **View** uses WPF binding expressions (`{Binding ...}`) to update automatically

---

## UI Components

### 1. **Welcome Header Section**
```
┌─────────────────────────────────────────┐
│ Welcome to ShieldX Professional    [🛡️] │
│ Your system is protected...             │
└─────────────────────────────────────────┘
```
- Display text is static (hardcoded)
- Shield emoji as visual accent
- Uses `GlassCard` style for modern appearance

### 2. **Stats Cards Row (4 columns)**

| Card | Data Source | Color | Purpose |
|------|-------------|-------|---------|
| **Threats Found** | `AppState.TotalThreatsFound` | Red (#FF4757) | Cumulative threats detected |
| **Files Scanned** | `AppState.TotalFilesScanned` | Cyan (#00E5CC) | Total files analyzed |
| **Quarantined** | `AppState.QuarantinedCount` | Orange (#FFA502) | Isolated threats |
| **Security Score** | `AppState.SecurityScore` | Dynamic (binding converter) | Overall protection level (0-100%) |

**Data Binding Pattern:**
```xaml
Text="{Binding SecurityScore, Source={x:Static models:AppState.Instance}, StringFormat={}{0}%}"
```
Uses singleton pattern to access global app state.

### 3. **System Health & Security Status**
- **Progress Bar**: Displays security score visually (0-100%)
- **Dynamic Color**: Changes based on score (via `ScoreToBrushConverter`)
  - Green (80-100): Excellent
  - Yellow (50-79): Warning
  - Red (0-49): Critical
- **Last Scan Time**: Displays with converter formatting
  ```xaml
  StringFormat="Last comprehensive scan: {0}"
  ```

### 4. **Resource Monitor Widget**
```xaml
<local:ResourceMonitorWidget Margin="0,0,0,25"/>
```
- Custom user control (separate component)
- Displays CPU, memory, disk usage in real-time
- Height: Variable, takes up available space

### 5. **Threat Map Widget**
```xaml
<local:ThreatMapWidget Margin="0,0,0,25" Height="300"/>
```
- Custom user control for geographic threat visualization
- Fixed height: 300px
- Shows real-time threat distribution

### 6. **Active Protection Modules**
Displays list of 9 protection modules with status indicators:

**Module List:**
1. RealTimeProtection - Real-time file scanning
2. WebShield - Web browsing protection
3. RansomwareShield - Ransomware detection
4. FirewallMonitor - Network firewall
5. ExploitGuard - Exploit mitigation
6. EmailProtection - Email scanning
7. DNSFilter - Domain blocking
8. BehaviorMonitor - Behavioral detection
9. VulnerabilityScanner - CVE monitoring

**Status Indicator:**
- Green circle (✓ Active) - enabled module
- Gray circle - disabled module
- Each module shows name, description, and toggle status

**ItemsControl Template:**
```xaml
<ItemsControl ItemsSource="{Binding Modules}">
  <ItemsControl.ItemTemplate>
    <DataTemplate>
      <!-- Module item display -->
    </DataTemplate>
  </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 7. **Recent Threats Blocked Today**
- Shows count of threats blocked in current session
- Lists up to 10 most recent alerts
- Each alert displays:
  - ⚠️ Icon
  - Filename (bold)
  - Threat type (red text)
  - Full path (dim gray)
  - Action taken (green, right-aligned)
  - Timestamp (top-right)

**Empty State:** Shows "✓ No threats detected - your system is clean." when `HasNoAlerts` is true

### 8. **Quick Actions**
Three action buttons at bottom:
- **Quick Scan** - Cyan (#00E5CC), black text
- **Full Scan** - Purple (#6C63FF), white text
- **Update Definitions** - Green (#2ED573), black text

*(Note: Buttons appear to not have click handlers wired up in the XAML)*

---

## Data Flow & Binding

### AppState (Singleton)
Global state properties that trigger `PropertyChanged` events:
```csharp
public int TotalThreatsFound { get; set; }
public int TotalFilesScanned { get; set; }
public int QuarantinedCount { get; set; }
public DateTime LastScanTime { get; set; }
public int SecurityScore { get; set; }
public int ScanProgress { get; set; }
public string CurrentScanStatus { get; set; }
```

### DashboardViewModel
**Collections:**
- `RecentAlerts` - `ObservableCollection<AlertItem>` (up to 10 items)
- `ProtectionModules` - `ObservableCollection<ProtectionModule>`

**Scalar Properties:**
- `ThreatBlockedToday` - Counter incremented on threat detection
- `DynamicSecurityScore` - Calculated based on system state
- `HasNoAlerts` - Boolean flag for showing empty state

**Event Handlers:**
```csharp
AppState.Instance.PropertyChanged += OnAppStateChanged  // Recalculate score
App.RealTimeProtection.ThreatDetected += OnThreatDetected  // Add alert
```

### Threat Detection Flow
```
RealTimeProtection Service
    ↓ (ThreatDetected event)
OnThreatDetected() in DashboardViewModel
    ↓
Create AlertItem (filename, threat type, path, action)
    ↓
Insert at beginning of RecentAlerts collection
    ↓
Remove oldest alert if list > 10 items
    ↓
Increment ThreatBlockedToday counter
    ↓
RecalculateSecurityScore()
    ↓
Update UI automatically (via INotifyPropertyChanged)
```

---

## Security Score Calculation

Located in `DashboardViewModel.RecalculateSecurityScore()`:

```csharp
int score = 100;  // Start with perfect score

// Factor 1: Disabled modules (-10 per module)
if (ModuleManager.Instance.Modules != null)
{
    var disabledCount = ModuleManager.Instance.Modules
        .Where(m => m != null && m.IsActive == false)
        .Count();
    score -= disabledCount * 10;
}

// Factor 2: Threats in current session (-5 per threat, max -30)
if (_lastScanThreats > 0)
    score -= Math.Min(30, _lastScanThreats * 5);

// Factor 3: Stale scan data (-15)
if (_lastScanDate == null || 
    DateTime.Now - _lastScanDate.Value > TimeSpan.FromDays(7))
    score -= 15;

// Factor 4: Active threats today (-20)
if (ThreatBlockedToday > 0)
    score -= 20;

// Clamp to valid range
DynamicSecurityScore = Math.Clamp(score, 0, 100);
```

**Score Degradation Table:**
| Condition | Penalty |
|-----------|---------|
| Each disabled module | -10 |
| Each threat detected | -5 (capped at -30) |
| No scan in 7+ days | -15 |
| Active threats today | -20 |
| **Minimum Score** | **0** |
| **Maximum Score** | **100** |

**Recalculation Triggers:**
- Every 30 seconds (timer-based)
- On `AppState` property changed
- When new threat detected
- When `ThreatBlockedToday` changes

---

## Key Features

### ✅ Real-Time Updates
- Dashboard updates automatically when threats are detected
- Security score recalculates every 30 seconds
- Module status changes propagate instantly via binding

### ✅ Alert Management
- Recent alerts stay visible for quick reference
- Maximum 10 alerts displayed (FIFO removal)
- Each alert shows complete threat context (file, type, path, action)

### ✅ Status Indicators
- Color-coded cards for quick visual assessment
- Module status dots (green/gray)
- Dynamic security score color gradient

### ✅ Empty States
- Shows clean system message when no threats detected
- Handles module list gracefully

### ⚠️ Limitations
- Quick action buttons (Scan, Update Definitions) **not wired up**
- No refresh mechanism (relies on event subscriptions)
- No user interaction with individual alerts (no delete/restore buttons)
- Resource Monitor and Threat Map widgets are custom components (not detailed here)

---

## Styling & Theme

### Dynamic Resources Used
- `AppBg` - Page background (dark/light mode)
- `CardBg` - Card backgrounds
- `CardBg2` - Secondary card backgrounds
- `TextPrimary` - Primary text color
- `TextSecondary` - Secondary text color
- `AccentClr` - Accent color (for shield)

### Fixed Colors
- Red alerts: #FF4757
- Cyan success: #00E5CC
- Orange warnings: #FFA502
- Green active: #10B981
- Purple/Blue accents: #6C63FF

### Styling Pattern
```xaml
<Border Style="{StaticResource GlassCard}" ...>
  <!-- Glassmorphism effect, likely semi-transparent with blur -->
</Border>
```

---

## Recommendations for Improvement

1. **Wire up Quick Action Buttons**
   - Add click handlers to navigate to respective pages
   - Show loading indicators during scans

2. **Add Alert Interaction**
   - Right-click context menu (View details, Restore, Delete)
   - Click to navigate to Quarantine page

3. **Module Management**
   - Allow toggling modules directly from dashboard
   - Show reason for disabled modules

4. **Historical Trend**
   - Add mini charts showing security score over time
   - Display threat trends (weekly/monthly)

5. **Performance Monitoring**
   - Ensure 30-second recalculation doesn't cause lag
   - Consider debouncing multiple rapid updates

6. **Accessibility**
   - Add aria-labels for screen readers
   - Keyboard navigation for buttons

---

## Code Quality Notes

✅ **Good Practices:**
- Proper use of MVVM pattern
- Singleton pattern for AppState
- INotifyPropertyChanged for reactive updates
- Clear separation of concerns

⚠️ **Potential Issues:**
- Hardcoded module list (should come from configuration)
- No error handling for null references
- Magic numbers (30-second timer, 10-alert limit)
- `App.RealTimeProtection` global reference (tight coupling)
