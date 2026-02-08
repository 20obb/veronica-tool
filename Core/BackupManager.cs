using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using BypassTool.Models;
using BypassTool.Utils;

namespace BypassTool.Core
{
    /// <summary>
    /// Manages device state backup and restore for recovery purposes
    /// </summary>
    public class BackupManager : IDisposable
    {
        #region Fields

        private readonly Logger _logger = Logger.Instance;
        private readonly string _backupDirectory;
        private bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new BackupManager instance
        /// </summary>
        public BackupManager()
        {
            _logger.Debug("Initializing BackupManager...");

            // Get backup directory from config or use default
            string configDir = ConfigurationManager.AppSettings["BackupDirectory"];
            _backupDirectory = string.IsNullOrEmpty(configDir)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "backups")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configDir);

            // Create backup directory
            Directory.CreateDirectory(_backupDirectory);

            _logger.Debug($"Backup directory: {_backupDirectory}");
        }

        /// <summary>
        /// Creates a new BackupManager with custom backup directory
        /// </summary>
        public BackupManager(string backupDirectory)
        {
            if (string.IsNullOrEmpty(backupDirectory))
                throw new ArgumentNullException(nameof(backupDirectory));

            _backupDirectory = backupDirectory;
            Directory.CreateDirectory(_backupDirectory);
        }

        #endregion

        #region Backup Creation

        /// <summary>
        /// Creates a backup of the device's current state
        /// </summary>
        /// <param name="device">Device information</param>
        /// <param name="injector">File injector for reading files</param>
        /// <returns>Path to the backup directory</returns>
        public string CreateBackup(DeviceInfo device, FileInjector injector)
        {
            ErrorHandler.ValidateNotNull(device, nameof(device));
            ErrorHandler.ValidateNotNull(injector, nameof(injector));

            if (!injector.IsConnected)
                throw new InvalidOperationException("FileInjector must be connected to device");

            _logger.Info($"Creating backup for device {device.UDID}...");

            try
            {
                // Create backup directory with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupName = $"{device.UDID?.Substring(0, 8) ?? "unknown"}_{timestamp}";
                string backupPath = Path.Combine(_backupDirectory, backupName);
                Directory.CreateDirectory(backupPath);

                _logger.Debug($"Backup path: {backupPath}");

                // Save device info
                SaveDeviceInfo(device, backupPath);

                // Get path resolver for iOS version
                var pathResolver = new PathResolver(device.ProductVersion);

                // Files to backup
                var filesToBackup = new List<string>
                {
                    pathResolver.GetActivationRecordPath(),
                    pathResolver.GetDataArkPath(),
                    pathResolver.GetPurpleBuddyPath(),
                    $"{pathResolver.GetMobilePreferencesPath()}/com.apple.SetupAssistant.plist",
                    $"{pathResolver.GetMobilePreferencesPath()}/com.apple.springboard.plist"
                };

                // Backup each file
                int backedUp = 0;
                foreach (var remotePath in filesToBackup)
                {
                    try
                    {
                        if (injector.FileExists(remotePath))
                        {
                            byte[] data = injector.ReadFile(remotePath);
                            
                            // Save with flattened name
                            string localName = remotePath.Replace("/", "_").TrimStart('_');
                            string localPath = Path.Combine(backupPath, localName);
                            
                            File.WriteAllBytes(localPath, data);
                            backedUp++;
                            
                            _logger.Debug($"Backed up: {remotePath}");
                        }
                        else
                        {
                            _logger.Debug($"File not found (skipping): {remotePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Failed to backup {remotePath}: {ex.Message}");
                    }
                }

                // Create backup manifest
                CreateManifest(device, backupPath, filesToBackup, backedUp);

                _logger.Info($"Backup created: {backupPath} ({backedUp} files)");
                return backupPath;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to create backup", ex);
                throw;
            }
        }

        /// <summary>
        /// Saves device info to backup
        /// </summary>
        private void SaveDeviceInfo(DeviceInfo device, string backupPath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(device, Formatting.Indented);
                File.WriteAllText(Path.Combine(backupPath, "device_info.json"), json);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to save device info: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates backup manifest file
        /// </summary>
        private void CreateManifest(DeviceInfo device, string backupPath, List<string> files, int backedUpCount)
        {
            try
            {
                var manifest = new
                {
                    Version = "1.0",
                    CreatedAt = DateTime.UtcNow,
                    Device = new
                    {
                        UDID = device.UDID,
                        ProductType = device.ProductType,
                        ProductVersion = device.ProductVersion,
                        SerialNumber = device.SerialNumber
                    },
                    Files = files,
                    BackedUpCount = backedUpCount
                };

                var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
                File.WriteAllText(Path.Combine(backupPath, "manifest.json"), json);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to create manifest: {ex.Message}");
            }
        }

        #endregion

        #region Backup Restoration

        /// <summary>
        /// Restores a backup to the device
        /// </summary>
        /// <param name="backupPath">Path to backup directory</param>
        /// <param name="injector">File injector for writing files</param>
        /// <returns>True if restore successful</returns>
        public bool RestoreBackup(string backupPath, FileInjector injector)
        {
            ErrorHandler.ValidateNotEmpty(backupPath, nameof(backupPath));
            ErrorHandler.ValidateNotNull(injector, nameof(injector));

            if (!Directory.Exists(backupPath))
                throw new DirectoryNotFoundException($"Backup directory not found: {backupPath}");

            if (!injector.IsConnected)
                throw new InvalidOperationException("FileInjector must be connected to device");

            _logger.Info($"Restoring backup from: {backupPath}");

            try
            {
                // Read manifest
                string manifestPath = Path.Combine(backupPath, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    _logger.Warning("No manifest found, will restore all plist files");
                }

                // Get all backed up plist files
                var plistFiles = Directory.GetFiles(backupPath, "*.plist");
                if (plistFiles.Length == 0)
                {
                    _logger.Warning("No plist files found in backup");
                    return false;
                }

                // Prepare files for injection
                var files = new Dictionary<string, byte[]>();
                foreach (var localPath in plistFiles)
                {
                    // Convert filename back to path
                    string fileName = Path.GetFileName(localPath);
                    string remotePath = ConvertBackupNameToPath(fileName);
                    
                    if (!string.IsNullOrEmpty(remotePath))
                    {
                        byte[] data = File.ReadAllBytes(localPath);
                        files[remotePath] = data;
                        _logger.Debug($"Prepared for restore: {remotePath}");
                    }
                }

                // Inject files
                bool success = injector.InjectFiles(files);

                if (success)
                {
                    // Restart services
                    injector.RestartActivationDaemon();
                }

                _logger.Info($"Restore {(success ? "completed" : "failed")}");
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to restore backup", ex);
                return false;
            }
        }

        /// <summary>
        /// Converts backup filename back to remote path
        /// </summary>
        private string ConvertBackupNameToPath(string fileName)
        {
            // Common file mappings
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "_var_root_Library_Lockdown_activation_record.plist", "/var/root/Library/Lockdown/activation_record.plist" },
                { "_var_root_Library_Lockdown_data_ark.plist", "/var/root/Library/Lockdown/data_ark.plist" },
                { "_var_mobile_Library_Preferences_com.apple.purplebuddy.plist", "/var/mobile/Library/Preferences/com.apple.purplebuddy.plist" },
                { "_var_mobile_Library_Preferences_com.apple.SetupAssistant.plist", "/var/mobile/Library/Preferences/com.apple.SetupAssistant.plist" },
                { "_var_mobile_Library_Preferences_com.apple.springboard.plist", "/var/mobile/Library/Preferences/com.apple.springboard.plist" }
            };

            if (mappings.TryGetValue(fileName, out string path))
            {
                return path;
            }

            // Try to convert generic name
            if (fileName.StartsWith("_"))
            {
                return "/" + fileName.Substring(1).Replace("_", "/");
            }

            return null;
        }

        #endregion

        #region Backup Management

        /// <summary>
        /// Lists all available backups
        /// </summary>
        /// <returns>List of backup info</returns>
        public List<BackupInfo> ListBackups()
        {
            var backups = new List<BackupInfo>();

            try
            {
                var dirs = Directory.GetDirectories(_backupDirectory);
                
                foreach (var dir in dirs)
                {
                    try
                    {
                        var info = GetBackupInfo(dir);
                        if (info != null)
                        {
                            backups.Add(info);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Failed to read backup info for {dir}: {ex.Message}");
                    }
                }

                // Sort by date (newest first)
                backups.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to list backups", ex);
            }

            return backups;
        }

        /// <summary>
        /// Gets info about a specific backup
        /// </summary>
        private BackupInfo GetBackupInfo(string backupPath)
        {
            if (!Directory.Exists(backupPath))
                return null;

            var info = new BackupInfo
            {
                Path = backupPath,
                Name = Path.GetFileName(backupPath),
                CreatedAt = Directory.GetCreationTime(backupPath)
            };

            // Try to read manifest
            string manifestPath = Path.Combine(backupPath, "manifest.json");
            if (File.Exists(manifestPath))
            {
                try
                {
                    string json = File.ReadAllText(manifestPath);
                    dynamic manifest = JsonConvert.DeserializeObject(json);
                    
                    info.DeviceUDID = manifest?.Device?.UDID;
                    info.ProductType = manifest?.Device?.ProductType;
                    info.ProductVersion = manifest?.Device?.ProductVersion;
                    info.FileCount = manifest?.BackedUpCount ?? 0;
                }
                catch { }
            }

            // Count files if manifest not available
            if (info.FileCount == 0)
            {
                info.FileCount = Directory.GetFiles(backupPath, "*.plist").Length;
            }

            // Calculate size
            info.SizeBytes = GetDirectorySize(backupPath);

            return info;
        }

        /// <summary>
        /// Gets total size of directory in bytes
        /// </summary>
        private long GetDirectorySize(string path)
        {
            long size = 0;
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch { }
            }
            return size;
        }

        /// <summary>
        /// Deletes a backup
        /// </summary>
        public bool DeleteBackup(string backupPath)
        {
            if (string.IsNullOrEmpty(backupPath))
                return false;

            try
            {
                if (Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                    _logger.Info($"Deleted backup: {backupPath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to delete backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cleans old backups, keeping only the most recent ones
        /// </summary>
        /// <param name="keepCount">Number of backups to keep</param>
        public int CleanOldBackups(int keepCount = 5)
        {
            _logger.Info($"Cleaning old backups, keeping {keepCount} most recent...");

            try
            {
                var backups = ListBackups();
                int deleted = 0;

                if (backups.Count > keepCount)
                {
                    for (int i = keepCount; i < backups.Count; i++)
                    {
                        if (DeleteBackup(backups[i].Path))
                        {
                            deleted++;
                        }
                    }
                }

                _logger.Info($"Cleaned {deleted} old backups");
                return deleted;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to clean old backups: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the backup directory path
        /// </summary>
        public string BackupDirectory => _backupDirectory;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// Information about a backup
    /// </summary>
    public class BackupInfo
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DeviceUDID { get; set; }
        public string ProductType { get; set; }
        public string ProductVersion { get; set; }
        public int FileCount { get; set; }
        public long SizeBytes { get; set; }

        public string FormattedSize
        {
            get
            {
                if (SizeBytes < 1024)
                    return $"{SizeBytes} B";
                if (SizeBytes < 1024 * 1024)
                    return $"{SizeBytes / 1024.0:F1} KB";
                return $"{SizeBytes / (1024.0 * 1024.0):F1} MB";
            }
        }

        public override string ToString()
        {
            return $"{Name} - {ProductType ?? "Unknown"} - {FormattedSize}";
        }
    }
}
