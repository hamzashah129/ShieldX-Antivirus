using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Serilog;
using ShieldX.Models;
using ShieldX.Utils;

namespace ShieldX.Services
{
    /// <summary>
    /// Database service with advanced error handling and recovery mechanisms.
    /// Implements resilience patterns for database operations.
    /// </summary>
    public class DatabaseService
    {
        private static readonly Lazy<DatabaseService> _instance = new Lazy<DatabaseService>(() => new DatabaseService());
        public static DatabaseService Instance => _instance.Value;

        private readonly string _dbPath;
        private readonly string _connectionString;

        private DatabaseService()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShieldX", "data");
            _dbPath = Path.Combine(appDataPath, "shieldx.db");
            _connectionString = $"Data Source={_dbPath}";
        }

        public void InitializeDatabase()
        {
            try
            {
                ValidationUtility.ValidateDiskSpace(Path.GetDirectoryName(_dbPath), minFreeMb: 50);
                Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));

                ResilienceUtility.ExecuteWithRetry(
                    () =>
                    {
                        using var connection = OpenConnection();
                        CreateTables(connection);
                        MigrateSchema(connection);
                        InitializeThreatDatabase(connection);
                        return true;
                    },
                    operationName: "Database initialization",
                    maxRetries: 3,
                    initialDelayMs: 200);

                Log.Information("[Database] Initialization completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Initialization failed: {ex.Message}");
                throw;
            }
        }

        private SqliteConnection OpenConnection()
        {
            try
            {
                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = _dbPath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Cache = SqliteCacheMode.Shared,
                    DefaultTimeout = 30000
                };

                var connection = new SqliteConnection(builder.ToString());
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Failed to open connection: {ex.Message}");
                throw;
            }
        }

        private void CreateTables(SqliteConnection connection)
        {
            try
            {
                ValidationUtility.ValidateNotNull(connection, nameof(connection));

                var commands = new[]
                {
                    @"CREATE TABLE IF NOT EXISTS ScanHistory (
                        Id TEXT PRIMARY KEY,
                        ScanType TEXT NOT NULL,
                        StartTime DATETIME NOT NULL,
                        EndTime DATETIME,
                        FilesScanned INTEGER DEFAULT 0,
                        ThreatsFound INTEGER DEFAULT 0,
                        ThreatsQuarantined INTEGER DEFAULT 0,
                        ScanPath TEXT,
                        Status TEXT DEFAULT 'Running'
                    )",

                    @"CREATE TABLE IF NOT EXISTS QuarantineItems (
                        Id TEXT PRIMARY KEY,
                        ThreatName TEXT NOT NULL,
                        ThreatType TEXT,
                        OriginalPath TEXT NOT NULL,
                        VaultPath TEXT NOT NULL,
                        DateIsolated DATETIME NOT NULL,
                        FileSize INTEGER,
                        SHA256Hash TEXT,
                        Status TEXT DEFAULT 'Quarantined',
                        RestoredAt DATETIME,
                        DeletedAt DATETIME
                    )",

                    @"CREATE TABLE IF NOT EXISTS ActivityLog (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        Level TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        Message TEXT NOT NULL,
                        Details TEXT
                    )",

                    @"CREATE TABLE IF NOT EXISTS ThreatDatabase (
                        SHA256 TEXT PRIMARY KEY,
                        MD5 TEXT,
                        ThreatName TEXT NOT NULL,
                        ThreatFamily TEXT,
                        ThreatType TEXT,
                        Severity TEXT,
                        AddedDate DATETIME,
                        Source TEXT
                    )",

                    @"CREATE TABLE IF NOT EXISTS ModuleStates (
                        ModuleName TEXT PRIMARY KEY,
                        IsActive INTEGER DEFAULT 1,
                        LastToggled DATETIME,
                        StartCount INTEGER DEFAULT 0
                    )",

                    @"CREATE TABLE IF NOT EXISTS ScheduledScans (
                        Id TEXT PRIMARY KEY,
                        Name TEXT NOT NULL,
                        ScanType TEXT NOT NULL,
                        Schedule TEXT NOT NULL,
                        NextRun DATETIME,
                        LastRun DATETIME,
                        IsEnabled INTEGER DEFAULT 1
                    )"
                };

                foreach (var cmd in commands)
                {
                    using var command = new SqliteCommand(cmd, connection);
                    command.ExecuteNonQuery();
                }
                Log.Information("[Database] All database tables created successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Failed to create tables: {ex.Message}");
                throw;
            }
        }

        private void MigrateSchema(SqliteConnection connection)
        {
            try
            {
                EnsureColumnExists(connection, "ActivityLog", "Category", "TEXT", "'General'");
                EnsureColumnExists(connection, "ActivityLog", "Message", "TEXT", "''");
                EnsureColumnExists(connection, "ActivityLog", "Details", "TEXT", "NULL");
                EnsureColumnExists(connection, "ActivityLog", "Timestamp", "DATETIME", "CURRENT_TIMESTAMP");
                EnsureColumnExists(connection, "QuarantineItems", "ThreatType", "TEXT", "NULL");
                EnsureColumnExists(connection, "QuarantineItems", "SHA256Hash", "TEXT", "NULL");
                EnsureColumnExists(connection, "QuarantineItems", "RestoredAt", "DATETIME", "NULL");
                EnsureColumnExists(connection, "QuarantineItems", "DeletedAt", "DATETIME", "NULL");
                EnsureColumnExists(connection, "ScheduledScans", "LastRun", "DATETIME", "NULL");
                EnsureColumnExists(connection, "ScheduledScans", "NextRun", "DATETIME", "NULL");
                Log.Information("[Database] Schema migration completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Schema migration error: {ex.Message}");
            }
        }

        private void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string dataType, string defaultValue)
        {
            try
            {
                ValidationUtility.ValidateNotNullOrEmpty(tableName, nameof(tableName));
                ValidationUtility.ValidateNotNullOrEmpty(columnName, nameof(columnName));

                using var columnCheck = new SqliteCommand($"PRAGMA table_info({tableName})", connection);
                using var reader = columnCheck.ExecuteReader();
                bool exists = false;
                while (reader.Read())
                {
                    if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    using var alter = new SqliteCommand($"ALTER TABLE {tableName} ADD COLUMN {columnName} {dataType} DEFAULT {defaultValue}", connection);
                    alter.ExecuteNonQuery();
                    Log.Debug($"[Database] Added column {columnName} to {tableName}");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[Database] Could not add column {columnName}: {ex.Message}");
            }
        }

        private void InitializeThreatDatabase(SqliteConnection connection)
        {
            try
            {
                ValidationUtility.ValidateNotNull(connection, nameof(connection));

                var threats = new[]
                {
                    ("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", "d41d8cd98f00b204e9800998ecf8427e", "EICAR Test File", "Test", "Test", "Low", DateTime.Now, "Built-in"),
                };

                using var transaction = connection.BeginTransaction();
                try
                {
                    foreach (var (sha256, md5, name, family, type, severity, date, source) in threats)
                    {
                        using var cmd = new SqliteCommand(
                            "INSERT OR IGNORE INTO ThreatDatabase (SHA256, MD5, ThreatName, ThreatFamily, ThreatType, Severity, AddedDate, Source) VALUES (@sha256, @md5, @name, @family, @type, @severity, @date, @source)",
                            connection, transaction);
                        cmd.Parameters.AddWithValue("@sha256", sha256);
                        cmd.Parameters.AddWithValue("@md5", md5);
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@family", family);
                        cmd.Parameters.AddWithValue("@type", type);
                        cmd.Parameters.AddWithValue("@severity", severity);
                        cmd.Parameters.AddWithValue("@date", date);
                        cmd.Parameters.AddWithValue("@source", source);
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    Log.Information("[Database] Threat database initialized with default threats");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error($"[Database] Failed to initialize threats: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Threat initialization error: {ex.Message}");
            }
        }

        public async Task<string> LookupThreatAsync(string sha256)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand("SELECT ThreatName FROM ThreatDatabase WHERE SHA256 = @hash", connection);
                cmd.Parameters.AddWithValue("@hash", sha256);
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch (Exception ex)
            {
                Log.Warning($"[Database] Threat lookup error: {ex.Message}");
                return null;
            }
        }

        public async Task<int> GetQuarantineCountAsync()
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand("SELECT COUNT(*) FROM QuarantineItems WHERE Status = 'Quarantined'", connection);
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Quarantine count error: {ex.Message}");
                return 0;
            }
        }

        public async Task InsertQuarantineItemAsync(QuarantineItem item)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(
                    @"INSERT INTO QuarantineItems (Id, ThreatName, ThreatType, OriginalPath, VaultPath, DateIsolated, FileSize, SHA256Hash, Status)
                      VALUES (@id, @threatName, @threatType, @originalPath, @vaultPath, @dateIsolated, @fileSize, @sha256, @status)",
                    connection);
                cmd.Parameters.AddWithValue("@id", item.Id);
                cmd.Parameters.AddWithValue("@threatName", item.ThreatName);
                cmd.Parameters.AddWithValue("@threatType", item.ThreatType ?? "");
                cmd.Parameters.AddWithValue("@originalPath", item.OriginalPath);
                cmd.Parameters.AddWithValue("@vaultPath", item.VaultPath);
                cmd.Parameters.AddWithValue("@dateIsolated", item.DateIsolated);
                cmd.Parameters.AddWithValue("@fileSize", item.FileSize ?? 0);
                cmd.Parameters.AddWithValue("@sha256", item.SHA256Hash ?? "");
                cmd.Parameters.AddWithValue("@status", "Quarantined");
                await cmd.ExecuteNonQueryAsync();
                Log.Information($"[Database] Quarantined item: {item.ThreatName}");
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Insert quarantine error: {ex.Message}");
                throw;
            }
        }

        public async Task LogActivityAsync(string level, string category, string message, string details = null)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(
                    "INSERT INTO ActivityLog (Level, Category, Message, Details, Timestamp) VALUES (@level, @category, @message, @details, @timestamp)",
                    connection);
                cmd.Parameters.AddWithValue("@level", level);
                cmd.Parameters.AddWithValue("@category", category);
                cmd.Parameters.AddWithValue("@message", message);
                cmd.Parameters.AddWithValue("@details", details ?? "");
                cmd.Parameters.AddWithValue("@timestamp", DateTime.Now);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Log activity error: {ex.Message}");
            }
        }

        public async Task<List<QuarantineItem>> GetQuarantineItemsAsync()
        {
            try
            {
                var items = new List<QuarantineItem>();
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand("SELECT Id, ThreatName, ThreatType, OriginalPath, VaultPath, DateIsolated, FileSize, SHA256Hash, Status FROM QuarantineItems WHERE Status = 'Quarantined' ORDER BY DateIsolated DESC", connection);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new QuarantineItem
                    {
                        Id = reader.GetString(0),
                        ThreatName = reader.GetString(1),
                        ThreatType = reader.IsDBNull(2) ? null : reader.GetString(2),
                        OriginalPath = reader.GetString(3),
                        VaultPath = reader.GetString(4),
                        DateIsolated = reader.GetDateTime(5),
                        FileSize = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                        SHA256Hash = reader.IsDBNull(7) ? null : reader.GetString(7),
                        Status = reader.GetString(8)
                    });
                }
                return items;
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Get quarantine items error: {ex.Message}");
                return new List<QuarantineItem>();
            }
        }

        public async Task SaveModuleStateAsync(string moduleName, bool isActive)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(
                    "INSERT OR REPLACE INTO ModuleStates (ModuleName, IsActive, LastToggled) VALUES (@name, @active, @toggled)",
                    connection);
                cmd.Parameters.AddWithValue("@name", moduleName);
                cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@toggled", DateTime.Now);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Save module state error: {ex.Message}");
            }
        }

        public async Task<bool> GetModuleStateAsync(string moduleName)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand("SELECT IsActive FROM ModuleStates WHERE ModuleName = @name", connection);
                cmd.Parameters.AddWithValue("@name", moduleName);
                var result = await cmd.ExecuteScalarAsync();
                return result != null && Convert.ToInt32(result) == 1;
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Get module state error: {ex.Message}");
                return true;
            }
        }
        public async Task InsertVulnerabilityAsync(string vulnerability)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(
                    "INSERT INTO Vulnerabilities (VulnerabilityName, DateFound) VALUES (@name, @date)",
                    connection);
                cmd.Parameters.AddWithValue("@name", vulnerability);
                cmd.Parameters.AddWithValue("@date", DateTime.Now);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Insert vulnerability error: {ex.Message}");
            }
        }

        public async Task<List<string>> GetVulnerabilitiesAsync()
        {
            try
            {
                var vulns = new List<string>();
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(
                    "SELECT VulnerabilityName FROM Vulnerabilities ORDER BY DateFound DESC LIMIT 100",
                    connection);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    vulns.Add(reader.GetString(0));
                }
                return vulns;
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Get vulnerabilities error: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<LogEntry>> GetRecentLogEntriesAsync(int maxEntries = 1000)
        {
            try
            {
                var entries = new List<LogEntry>();
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(
                    "SELECT Timestamp, Level, Category, Message, Details FROM ActivityLog ORDER BY Timestamp DESC LIMIT @limit",
                    connection);
                cmd.Parameters.AddWithValue("@limit", maxEntries);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    entries.Add(new LogEntry
                    {
                        Timestamp = reader.GetDateTime(0),
                        Level = reader.GetString(1),
                        Category = reader.GetString(2),
                        Message = reader.GetString(3),
                        Details = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }
                return entries;
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Get recent log entries error: {ex.Message}");
                return new List<LogEntry>();
            }
        }

        public async Task ClearLogsAsync()
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand("DELETE FROM ActivityLog", connection);
                await cmd.ExecuteNonQueryAsync();
                Log.Information("[Database] All activity logs cleared successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Clear logs error: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetLogCountAsync()
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand("SELECT COUNT(*) FROM ActivityLog", connection);
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Log.Error($"[Database] Get log count error: {ex.Message}");
                return 0;
            }
        }
    }
}
