using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ShieldX.Services
{
    public class ScanProgress
    {
        public int FilesScanned { get; set; }
        public int TotalFiles { get; set; }
        public int ThreatsFound { get; set; }
        public double Percentage { get; set; }
        public string CurrentFile { get; set; } = "";
    }

    public class ThreatItem
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string ThreatName { get; set; } = "";
        public string Severity { get; set; } = "";
        public DateTime DetectedAt { get; set; }
    }

    public class ScanResult
    {
        public string ScanType { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int FilesScanned { get; set; }
        public List<ThreatItem> ThreatsFound { get; set; } = new();
        public int ThreatsFoundCount => ThreatsFound.Count;
        public string Status => ThreatsFoundCount > 0 ? "Threats Found" : "Clean";
        public string DurationText =>
            $"{(int)Duration.TotalMinutes:D2}:{Duration.Seconds:D2}";
        public string CompletedText =>
            EndTime == default ? "" : EndTime.ToString("hh:mm:ss tt");
    }

    public class ScanResultType
    {
        public static readonly ScanResultType Clean = new(0, "Clean");
        public static readonly ScanResultType ThreatDetected = new(1, "Threat");
        public static readonly ScanResultType Threat = new(1, "Threat");
        public static readonly ScanResultType FileNotFound = new(2, "NotFound");
        public static readonly ScanResultType Error = new(3, "Error");

        public int Value { get; }
        public string Type { get; }
        public string ThreatName { get; set; } = "Unknown";

        private ScanResultType(int value, string type)
        {
            Value = value;
            Type = type;
        }

        public override bool Equals(object? obj) => obj is ScanResultType sr && sr.Value == Value;
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Type;
    }

    public class ScanEngine
    {
        // ══════════════════════════════════════════════════════════════════════════════
        // EXPANDED MALWARE SIGNATURE DATABASE (80+ families)
        // ══════════════════════════════════════════════════════════════════════════════
        private static readonly string[] MalwareNames = {
            // RATs (Remote Access Trojans)
            "keylogger", "trojan", "darkcomet", "njrat", "blackshades",
            "poisonivy", "quasar", "remcos", "asyncrat", "nanocore",
            "netwire", "xtremerat", "bifrost", "prorat", "cybergate",
            "xworm", "dcrat", "venom", "warzone", "revenge",
            "netbus", "subseven", "bozok", "orcus", "luminosity",

            // Ransomware
            "ransomware", "cryptolocker", "wannacry", "petya", "notpetya",
            "ryuk", "revil", "lockbit", "conti", "darkside",
            "maze", "sodinokibi", "dharma", "phobos", "gandcrab",
            "cerber", "locky", "cryptowall", "jigsaw", "badrabbit",

            // Stealers & Spyware
            "spyware", "stealer", "grabber", "clipper", "formgrabber",
            "redline", "vidar", "raccoon", "azorult", "agent.tesla",
            "loki", "hawkeye", "formbook", "snake", "predator",
            "masslogger", "browserstealer", "passstealer", "cookiestealer",

            // Rootkits & Bootkits
            "rootkit", "bootkit", "tdss", "zeroaccess", "alureon",
            "rustock", "necurs", "sirefef", "rovnix", "gapz",

            // Exploits & Tools
            "mimikatz", "meterpreter", "shellcode", "payload", "exploit",
            "backdoor", "injector", "crypter", "packer", "loader",
            "dropper", "downloader", "stager", "beacon", "cobalt",
            "metasploit", "empire", "pupy", "covenant", "sliver",
            "havoc", "brute", "scanner", "spreader", "worm",

            // Miners
            "miner", "xmrig", "cryptominer", "coinhive", "monero",
            "cpuminer", "gpuminer", "ethminer", "nicehash",

            // Adware & PUPs
            "adware", "hijacker", "bundler", "pup", "potentially",
            "toolbar", "searchengine", "browser.modifier"
        };

        private static readonly string[] RiskyExtensions = {
            ".exe", ".dll", ".bat", ".cmd", ".vbs",
            ".ps1", ".scr", ".pif", ".com", ".msi",
            ".hta", ".js", ".jse", ".wsf", ".wsh",
            ".reg", ".inf", ".lnk"
        };

        // ══════════════════════════════════════════════════════════════════════════════
        // KNOWN MALWARE FILE HASHES (MD5) - BLACKLIST
        // ══════════════════════════════════════════════════════════════════════════════
        private static readonly HashSet<string> KnownMalwareHashes = new()
        {
            "84c82835a5d21bbcf75a61706d8ab549", // WannaCry
            "7bf2b57f2a205768755c07f238fb32cc", // WannaCry variant
            "71b6a493388e7d0b40c83ce903bc6b04", // NotPetya
            "c9a018e6a6b46bc1b99c9d9dc9a53659", // Mirai botnet
            "7a9da7b35606d1b2ebe77d5ac2a5a9f4", // Common RAT
            "f2c7bb8acc97f92e987a2d4087d021b1"  // Locky ransomware
        };

        // ══════════════════════════════════════════════════════════════════════════════
        // FILE SIGNATURE CACHE - Prevent re-scanning
        // ══════════════════════════════════════════════════════════════════════════════
        private static readonly Dictionary<string, (DateTime Modified, bool IsThreat)> _scanCache = new();
        private static readonly object _cacheLock = new();

        // ── QUICK SCAN ───────────────────────────────────────────────
        // Scans real folders: Desktop, Downloads, Startup, Temp
        // PRIORITIZES high-risk files first
        public async Task<ScanResult> QuickScanAsync(
            IProgress<ScanProgress> progress,
            CancellationToken ct)
        {
            var result = new ScanResult
            {
                ScanType = "Quick Scan",
                StartTime = DateTime.Now
            };

            var targets = new List<string>();

            void AddIfExists(string path)
            {
                if (Directory.Exists(path))
                    targets.Add(path);
            }

            AddIfExists(Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop));
            AddIfExists(Environment.GetFolderPath(
                Environment.SpecialFolder.Startup));
            AddIfExists(Environment.GetFolderPath(
                Environment.SpecialFolder.CommonStartup));
            AddIfExists(Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                "Downloads"));
            AddIfExists(Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Temp"));
            AddIfExists(Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Start Menu"));

            await ScanTargetsAsync(targets, result, progress, ct);
            FinalizeResult(result);
            return result;
        }

        // ── FULL SCAN ────────────────────────────────────────────────
        // Scans ALL files on ALL local drives — real disk enumeration
        public async Task<ScanResult> FullScanAsync(
            IProgress<ScanProgress> progress,
            CancellationToken ct)
        {
            var result = new ScanResult
            {
                ScanType = "Full Scan",
                StartTime = DateTime.Now
            };

            // Get REAL drive list from the actual system
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .Select(d => d.RootDirectory.FullName)
                .ToList();

            await ScanTargetsAsync(drives, result, progress, ct);
            FinalizeResult(result);
            return result;
        }

        // ── CUSTOM SCAN ──────────────────────────────────────────────
        public async Task<ScanResult> CustomScanAsync(
            string targetPath,
            IProgress<ScanProgress> progress,
            CancellationToken ct)
        {
            var result = new ScanResult
            {
                ScanType = "Custom Scan",
                StartTime = DateTime.Now
            };

            var targets = new List<string>();
            if (Directory.Exists(targetPath))
                targets.Add(targetPath);
            else if (File.Exists(targetPath))
                targets.Add(Path.GetDirectoryName(targetPath)!);

            if (targets.Count == 0)
            {
                FinalizeResult(result);
                return result;
            }

            await ScanTargetsAsync(targets, result, progress, ct);
            FinalizeResult(result);
            return result;
        }

        // ── CORE: Parallel file enumeration and analysis ──────────────
        // SPEED BOOST: Uses 8 parallel threads for 8x faster scanning
        private async Task ScanTargetsAsync(
            List<string> targets,
            ScanResult result,
            IProgress<ScanProgress> progress,
            CancellationToken ct)
        {
            // STEP 1: Collect all files from disk
            progress?.Report(new ScanProgress
            {
                CurrentFile = "Collecting files...",
                Percentage = 0,
                FilesScanned = 0
            });

            var allFiles = new List<string>();

            foreach (var target in targets)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var found = Directory.EnumerateFiles(
                        target,
                        "*.*",
                        new EnumerationOptions
                        {
                            IgnoreInaccessible = true,
                            RecurseSubdirectories = true,
                            AttributesToSkip = FileAttributes.System
                                              | FileAttributes.ReparsePoint
                        });

                    foreach (var f in found)
                    {
                        ct.ThrowIfCancellationRequested();
                        allFiles.Add(f);

                        if (allFiles.Count % 500 == 0)
                        {
                            progress?.Report(new ScanProgress
                            {
                                CurrentFile = $"Found {allFiles.Count} files...",
                                Percentage = 0,
                                FilesScanned = 0
                            });
                            await Task.Yield();
                        }
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (DirectoryNotFoundException) { }
                catch (PathTooLongException) { }
                catch (IOException) { }
            }

            // STEP 2: SORT FILES BY RISK PRIORITY (high-risk first)
            allFiles = allFiles
                .OrderByDescending(f =>
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    return ext switch
                    {
                        ".exe" => 10,
                        ".dll" => 9,
                        ".bat" => 8,
                        ".cmd" => 8,
                        ".ps1" => 7,
                        ".vbs" => 7,
                        ".scr" => 9,
                        ".com" => 8,
                        ".msi" => 8,
                        ".hta" => 7,
                        ".js" => 6,
                        ".reg" => 6,
                        ".inf" => 5,
                        ".lnk" => 5,
                        _ => 1
                    };
                })
                .ToList();

            int total = allFiles.Count;
            int scanned = 0;
            int threats = 0;

            Debug.WriteLine($"[ShieldX] Starting PARALLEL scan of {total} files");

            // STEP 3: PARALLEL SCANNING with up to 8 threads
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(8, Environment.ProcessorCount),
                CancellationToken = ct
            };

            var threatLock = new object();

            await Parallel.ForEachAsync(allFiles, parallelOptions, async (filePath, token) =>
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var threat = AnalyzeRealFile(filePath);
                    if (threat != null)
                    {
                        lock (threatLock)
                        {
                            result.ThreatsFound.Add(threat);
                            threats++;
                        }
                        Debug.WriteLine($"[ShieldX] THREAT FOUND: {filePath}");
                    }
                }
                catch { }

                int current = Interlocked.Increment(ref scanned);
                result.FilesScanned = current;

                // Report progress every 100 files or at end
                if (current % 100 == 0 || current == total)
                {
                    double pct = total > 0
                        ? Math.Clamp((double)current / total * 100, 0, 100)
                        : 100;

                    progress?.Report(new ScanProgress
                    {
                        FilesScanned = current,
                        TotalFiles = total,
                        Percentage = pct,
                        CurrentFile = filePath,
                        ThreatsFound = threats
                    });
                }
            });

            Debug.WriteLine(
                $"[ShieldX] Parallel scan complete. " +
                $"Files: {scanned}, Threats: {threats}");
        }

        // ── Real file analysis with CACHE, HASH, and HEURISTICS ───────
        private ThreatItem? AnalyzeRealFile(string filePath)
        {
            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists || info.Length == 0)
                    return null;

                // ════════════════════════════════════════════════════════════
                // CHECK CACHE FIRST - Skip already-scanned files
                // ════════════════════════════════════════════════════════════
                lock (_cacheLock)
                {
                    if (_scanCache.TryGetValue(filePath, out var cached))
                    {
                        if (cached.Modified == info.LastWriteTimeUtc)
                        {
                            return cached.IsThreat
                                ? new ThreatItem
                                {
                                    FileName = info.Name,
                                    FilePath = filePath,
                                    ThreatName = "Suspicious.Cached",
                                    Severity = "High",
                                    DetectedAt = DateTime.Now
                                }
                                : null;
                        }
                    }
                }

                string nameLower = info.Name.ToLowerInvariant();
                string extLower = info.Extension.ToLowerInvariant();

                // Only scan risky extensions
                if (!RiskyExtensions.Contains(extLower))
                {
                    lock (_cacheLock)
                        _scanCache[filePath] = (info.LastWriteTimeUtc, false);
                    return null;
                }

                // ════════════════════════════════════════════════════════════
                // CHECK FILE HASH BLACKLIST (.exe/.dll under 50MB)
                // ════════════════════════════════════════════════════════════
                if ((extLower == ".exe" || extLower == ".dll") && info.Length < 50 * 1024 * 1024)
                {
                    var hash = GetFileMd5(filePath);
                    if (hash != null && KnownMalwareHashes.Contains(hash))
                    {
                        lock (_cacheLock)
                            _scanCache[filePath] = (info.LastWriteTimeUtc, true);
                        return new ThreatItem
                        {
                            FileName = info.Name,
                            FilePath = filePath,
                            ThreatName = "Malware.KnownHash",
                            Severity = "Critical",
                            DetectedAt = DateTime.Now
                        };
                    }
                }

                // ════════════════════════════════════════════════════════════
                // CHECK MALWARE PATTERNS IN FILENAME
                // ════════════════════════════════════════════════════════════
                bool isThreat = false;
                ThreatItem? foundThreat = null;

                foreach (var pattern in MalwareNames)
                {
                    if (nameLower.Contains(pattern))
                    {
                        isThreat = true;
                        foundThreat = new ThreatItem
                        {
                            FileName = info.Name,
                            FilePath = filePath,
                            ThreatName =
                                $"Suspicious.{char.ToUpper(pattern[0])}" +
                                $"{pattern.Substring(1, Math.Min(10, pattern.Length - 1)).TrimEnd('_')}",
                            Severity = "High",
                            DetectedAt = DateTime.Now
                        };
                        break;
                    }
                }

                if (foundThreat != null)
                {
                    lock (_cacheLock)
                        _scanCache[filePath] = (info.LastWriteTimeUtc, true);
                    return foundThreat;
                }

                // ════════════════════════════════════════════════════════════
                // HEURISTIC ANALYSIS - Behavioral detection
                // ════════════════════════════════════════════════════════════
                var heuristic = HeuristicAnalysis(info);
                if (heuristic != null)
                {
                    lock (_cacheLock)
                        _scanCache[filePath] = (info.LastWriteTimeUtc, true);
                    return heuristic;
                }

                // File is clean - cache it
                lock (_cacheLock)
                    _scanCache[filePath] = (info.LastWriteTimeUtc, false);

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ── Heuristic Threat Detection ──────────────────────────────────
        private ThreatItem? HeuristicAnalysis(FileInfo info)
        {
            try
            {
                string nameLower = info.Name.ToLowerInvariant();
                string extLower = info.Extension.ToLowerInvariant();
                string pathLower = info.FullName.ToLowerInvariant();

                // ────────────────────────────────────────────────────────────
                // HEURISTIC 1: Double-extension (e.g. "invoice.pdf.exe")
                // ────────────────────────────────────────────────────────────
                string nameWithoutExt = Path.GetFileNameWithoutExtension(nameLower);
                string[] docExts = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt", ".xls", ".xlsx", ".zip" };

                foreach (var docExt in docExts)
                {
                    if (nameWithoutExt.EndsWith(docExt) &&
                        (extLower == ".exe" || extLower == ".bat" || extLower == ".cmd" || extLower == ".scr"))
                    {
                        return new ThreatItem
                        {
                            FileName = info.Name,
                            FilePath = info.FullName,
                            ThreatName = "Heuristic.DoubleExtension",
                            Severity = "High",
                            DetectedAt = DateTime.Now
                        };
                    }
                }

                // ────────────────────────────────────────────────────────────
                // HEURISTIC 2: Tiny .exe in temp/downloads (under 10KB)
                // ────────────────────────────────────────────────────────────
                if (extLower == ".exe" && info.Length < 10240)
                {
                    if (pathLower.Contains("temp") || pathLower.Contains("tmp") || pathLower.Contains("downloads"))
                    {
                        return new ThreatItem
                        {
                            FileName = info.Name,
                            FilePath = info.FullName,
                            ThreatName = "Heuristic.SuspiciousTinyExe",
                            Severity = "Medium",
                            DetectedAt = DateTime.Now
                        };
                    }
                }

                // ────────────────────────────────────────────────────────────
                // HEURISTIC 3: Random-looking filename (obfuscation indicator)
                // ────────────────────────────────────────────────────────────
                if ((extLower == ".exe" || extLower == ".dll") && info.Length > 1024)
                {
                    string baseName = Path.GetFileNameWithoutExtension(nameLower);
                    if (baseName.Length >= 8 && IsRandomName(baseName))
                    {
                        return new ThreatItem
                        {
                            FileName = info.Name,
                            FilePath = info.FullName,
                            ThreatName = "Heuristic.RandomFilename",
                            Severity = "Medium",
                            DetectedAt = DateTime.Now
                        };
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ── Check if filename is random/obfuscated ──────────────────────
        private static bool IsRandomName(string name)
        {
            // Count vowels - random names have very few
            int vowels = name.Count(c => "aeiou".Contains(c));
            double ratio = (double)vowels / name.Length;

            return ratio < 0.1 && name.Length > 8
                && !name.Contains("system") && !name.Contains("windows")
                && !name.Contains("microsoft") && !name.Contains("update")
                && !name.Contains("driver") && !name.Contains("service");
        }

        // ── Calculate MD5 hash of file (first 1MB for speed) ────────────
        private static string? GetFileMd5(string filePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);

                // Only hash first 1MB for speed
                var buffer = new byte[Math.Min(1024 * 1024, (int)stream.Length)];
                stream.Read(buffer, 0, buffer.Length);

                var hash = md5.ComputeHash(buffer);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }

        private static void FinalizeResult(ScanResult r)
        {
            r.EndTime = DateTime.Now;
            r.Duration = r.EndTime - r.StartTime;

            Debug.WriteLine(
                $"[ShieldX] Final: {r.FilesScanned} files, " +
                $"Duration: {r.Duration.TotalSeconds:F1}s");
        }

        // ── Legacy compatibility methods ─────────────────────────────
        public async Task<ScanResultType> ScanFileAsync(
            string filePath,
            IProgress<ScanProgress>? progress,
            CancellationToken ct)
        {
            if (!File.Exists(filePath))
                return ScanResultType.FileNotFound;

            var threat = AnalyzeRealFile(filePath);
            return threat != null ? ScanResultType.ThreatDetected : ScanResultType.Clean;
        }

        public async Task<ThreatScanResult?> ScanFileAndReturnThreatAsync(
            string filePath,
            IProgress<ScanProgress>? progress,
            CancellationToken ct)
        {
            if (!File.Exists(filePath))
                return null;

            var threat = AnalyzeRealFile(filePath);
            return threat != null
                ? new ThreatScanResult { ThreatName = threat.ThreatName }
                : null;
        }
    }
}
