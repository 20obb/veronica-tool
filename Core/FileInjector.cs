using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using BypassTool.Models;
using BypassTool.Utils;

namespace BypassTool.Core
{
    /// <summary>
    /// Handles file injection to iOS devices via SSH or AFC
    /// </summary>
    public class FileInjector : IDisposable
    {
        #region Fields

        private readonly Logger _logger = Logger.Instance;
        private readonly PathResolver _pathResolver;
        private SshClient _sshClient;
        private SftpClient _sftpClient;
        private bool _isConnected;
        private bool _disposed;

        /// <summary>
        /// Default SSH username
        /// </summary>
        public string DefaultUsername { get; set; } = "root";

        /// <summary>
        /// Default SSH password
        /// </summary>
        public string DefaultPassword { get; set; } = "alpine";

        /// <summary>
        /// Default SSH port
        /// </summary>
        public int DefaultPort { get; set; } = 22;

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// Event raised on injection progress
        /// </summary>
        public event EventHandler<InjectionProgressEventArgs> ProgressChanged;

        /// <summary>
        /// Returns true if SSH connection is active
        /// </summary>
        public bool IsConnected => _isConnected && _sshClient?.IsConnected == true;

        /// <summary>
        /// Gets the SSH client for external use (e.g., jailbreak detection)
        /// </summary>
        /// <returns>The connected SSH client or null if not connected</returns>
        public SshClient GetSshClient() => _sshClient;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new FileInjector instance
        /// </summary>
        public FileInjector()
        {
            _logger.Info("Initializing FileInjector...");
            _pathResolver = new PathResolver();
            LoadConfiguration();
            _logger.Debug("FileInjector initialized");
        }

        /// <summary>
        /// Creates a new FileInjector with specific iOS version
        /// </summary>
        public FileInjector(IOSVersion version) : this()
        {
            if (version != null)
            {
                _pathResolver.IOSVersion = version;
            }
        }

        #endregion

        #region Configuration

        private void LoadConfiguration()
        {
            try
            {
                var config = System.Configuration.ConfigurationManager.AppSettings;
                
                if (!string.IsNullOrEmpty(config["SSHDefaultUser"]))
                    DefaultUsername = config["SSHDefaultUser"];
                
                if (!string.IsNullOrEmpty(config["SSHDefaultPassword"]))
                    DefaultPassword = config["SSHDefaultPassword"];
                
                if (int.TryParse(config["SSHDefaultPort"], out int port))
                    DefaultPort = port;
                
                if (int.TryParse(config["ConnectionTimeout"], out int timeout))
                    ConnectionTimeout = timeout;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to load SSH configuration: {ex.Message}");
            }
        }

        #endregion

        #region SSH Connection

        /// <summary>
        /// Connects to device via SSH
        /// </summary>
        /// <param name="host">Device IP address or hostname</param>
        /// <param name="username">SSH username (default: root)</param>
        /// <param name="password">SSH password (default: alpine)</param>
        /// <param name="port">SSH port (default: 22)</param>
        /// <returns>True if connection successful</returns>
        public bool ConnectSSH(string host, string username = null, string password = null, int? port = null)
        {
            ErrorHandler.ValidateNotEmpty(host, nameof(host));

            username = username ?? DefaultUsername;
            password = password ?? DefaultPassword;
            int actualPort = port ?? DefaultPort;

            _logger.Info($"Connecting to {host}:{actualPort} via SSH...");

            try
            {
                // Disconnect if already connected
                DisconnectSSH();

                // Create connection info
                var connectionInfo = new ConnectionInfo(
                    host, 
                    actualPort, 
                    username,
                    new PasswordAuthenticationMethod(username, password)
                );
                connectionInfo.Timeout = TimeSpan.FromSeconds(ConnectionTimeout);

                // Create SSH client
                _sshClient = new SshClient(connectionInfo);
                _sshClient.Connect();

                if (!_sshClient.IsConnected)
                {
                    _logger.Error("SSH connection failed: Not connected after Connect()");
                    return false;
                }

                _logger.Debug("SSH connected, creating SFTP client...");

                // Create SFTP client
                _sftpClient = new SftpClient(connectionInfo);
                _sftpClient.Connect();

                if (!_sftpClient.IsConnected)
                {
                    _logger.Error("SFTP connection failed");
                    _sshClient.Disconnect();
                    _sshClient.Dispose();
                    _sshClient = null;
                    return false;
                }

                _isConnected = true;
                _logger.Info("SSH/SFTP connected successfully");

                // Test connection
                var result = ExecuteCommand("echo 'Connection test'");
                _logger.Debug($"Connection test result: {result?.Trim()}");

                return true;
            }
            catch (SocketException ex)
            {
                _logger.Error($"SSH connection failed (socket): {ex.Message}");
                throw;
            }
            catch (Renci.SshNet.Common.SshAuthenticationException ex)
            {
                _logger.Error($"SSH authentication failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error($"SSH connection failed: {ex.Message}");
                DisconnectSSH();
                throw;
            }
        }

        /// <summary>
        /// Disconnects from SSH
        /// </summary>
        public void DisconnectSSH()
        {
            _logger.Debug("Disconnecting SSH...");

            try
            {
                if (_sftpClient != null)
                {
                    if (_sftpClient.IsConnected)
                        _sftpClient.Disconnect();
                    _sftpClient.Dispose();
                    _sftpClient = null;
                }

                if (_sshClient != null)
                {
                    if (_sshClient.IsConnected)
                        _sshClient.Disconnect();
                    _sshClient.Dispose();
                    _sshClient = null;
                }

                _isConnected = false;
                _logger.Debug("SSH disconnected");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error during SSH disconnect: {ex.Message}");
            }
        }

        #endregion

        #region File Injection

        /// <summary>
        /// Injects multiple files via SSH
        /// </summary>
        /// <param name="files">Dictionary of remote path -> file data</param>
        /// <returns>True if all files injected successfully</returns>
        public bool InjectFiles(Dictionary<string, byte[]> files)
        {
            ErrorHandler.ValidateNotNull(files, nameof(files));

            if (!_isConnected || _sftpClient == null)
            {
                throw new InvalidOperationException("SSH not connected. Call ConnectSSH first.");
            }

            _logger.Info($"Injecting {files.Count} files...");

            int successCount = 0;
            int totalFiles = files.Count;
            var failedFiles = new List<string>();

            foreach (var kvp in files)
            {
                string remotePath = kvp.Key;
                byte[] data = kvp.Value;

                try
                {
                    // Report progress
                    int progress = (int)((successCount / (float)totalFiles) * 100);
                    OnProgressChanged(progress, $"Injecting: {Path.GetFileName(remotePath)}");

                    // Inject file
                    if (InjectSingleFile(remotePath, data))
                    {
                        successCount++;
                        _logger.Info($"[OK] {remotePath} ({data.Length} bytes)");
                    }
                    else
                    {
                        failedFiles.Add(remotePath);
                        _logger.Warning($"[FAIL] {remotePath}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to inject {remotePath}: {ex.Message}");
                    failedFiles.Add(remotePath);
                }
            }

            _logger.Info($"Injection complete: {successCount}/{totalFiles} files successful");

            if (failedFiles.Count > 0)
            {
                _logger.Warning($"Failed files: {string.Join(", ", failedFiles)}");
            }

            return failedFiles.Count == 0;
        }

        /// <summary>
        /// Injects a single file via SSH
        /// </summary>
        private bool InjectSingleFile(string remotePath, byte[] data)
        {
            if (!_isConnected || _sftpClient == null)
                return false;

            try
            {
                // Create parent directory
                string remoteDir = GetParentDirectory(remotePath);
                if (!string.IsNullOrEmpty(remoteDir))
                {
                    CreateRemoteDirectory(remoteDir);
                }

                // Upload file
                using (var ms = new MemoryStream(data))
                {
                    _sftpClient.UploadFile(ms, remotePath, true);
                }

                // Set permissions (644 for files)
                SetFilePermissions(remotePath, 644);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to inject file {remotePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates remote directory recursively
        /// </summary>
        private void CreateRemoteDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "/")
                return;

            try
            {
                // Check if directory exists
                if (_sftpClient.Exists(path))
                    return;

                // Create parent first
                string parent = GetParentDirectory(path);
                if (!string.IsNullOrEmpty(parent) && parent != "/")
                {
                    CreateRemoteDirectory(parent);
                }

                // Create directory
                _sftpClient.CreateDirectory(path);
                _logger.Debug($"Created directory: {path}");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to create directory {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets file permissions
        /// </summary>
        private void SetFilePermissions(string path, int mode)
        {
            try
            {
                string command = $"chmod {mode:D3} '{path}'";
                ExecuteCommand(command);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to set permissions on {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets parent directory of a path
        /// </summary>
        private string GetParentDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            int lastSlash = path.LastIndexOf('/');
            if (lastSlash <= 0)
                return "/";

            return path.Substring(0, lastSlash);
        }

        #endregion

        #region SSH Commands

        /// <summary>
        /// Executes a command on the device via SSH
        /// </summary>
        public string ExecuteCommand(string command)
        {
            if (!_isConnected || _sshClient == null)
            {
                throw new InvalidOperationException("SSH not connected");
            }

            _logger.Debug($"Executing: {command}");

            try
            {
                using (var cmd = _sshClient.CreateCommand(command))
                {
                    cmd.CommandTimeout = TimeSpan.FromSeconds(60);
                    string result = cmd.Execute();
                    
                    if (cmd.ExitStatus != 0)
                    {
                        _logger.Warning($"Command exited with code {cmd.ExitStatus}: {cmd.Error}");
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Command execution failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Restarts the mobileactivationd daemon
        /// </summary>
        public bool RestartActivationDaemon()
        {
            _logger.Info("Restarting mobileactivationd...");

            try
            {
                // Kill the daemon (it will auto-restart)
                ExecuteCommand("killall -9 mobileactivationd 2>/dev/null || true");
                Thread.Sleep(1000);

                // Also restart CommCenter for good measure
                ExecuteCommand("killall -9 CommCenter 2>/dev/null || true");
                Thread.Sleep(500);

                _logger.Info("Activation daemon restarted");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to restart activation daemon: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restarts SpringBoard (resprings the device)
        /// </summary>
        public bool RespringDevice()
        {
            _logger.Info("Respringing device...");

            try
            {
                ExecuteCommand("killall -9 SpringBoard");
                _logger.Info("Respring command sent");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to respring: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reboots the device
        /// </summary>
        public bool RebootDevice()
        {
            _logger.Info("Rebooting device...");

            try
            {
                ExecuteCommand("reboot");
                _logger.Info("Reboot command sent");
                DisconnectSSH();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to reboot: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a file exists on the device
        /// </summary>
        public bool FileExists(string remotePath)
        {
            if (!_isConnected || _sftpClient == null)
                return false;

            try
            {
                return _sftpClient.Exists(remotePath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reads a file from the device
        /// </summary>
        public byte[] ReadFile(string remotePath)
        {
            if (!_isConnected || _sftpClient == null)
                throw new InvalidOperationException("SSH not connected");

            try
            {
                using (var ms = new MemoryStream())
                {
                    _sftpClient.DownloadFile(remotePath, ms);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to read file {remotePath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a file from the device
        /// </summary>
        public bool DeleteFile(string remotePath)
        {
            if (!_isConnected || _sftpClient == null)
                return false;

            try
            {
                if (_sftpClient.Exists(remotePath))
                {
                    _sftpClient.DeleteFile(remotePath);
                    _logger.Debug($"Deleted: {remotePath}");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to delete {remotePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes activation files from device (for testing purposes)
        /// </summary>
        /// <returns>True if successful</returns>
        public bool RemoveActivationFiles()
        {
            if (!_isConnected || _sshClient == null)
            {
                _logger.Error("SSH not connected. Cannot remove activation files.");
                return false;
            }

            try
            {
                _logger.Info("Removing activation files from device...");

                // Activation file paths to remove
                string[] activationPaths = new[]
                {
                    "/var/root/Library/Lockdown/activation_records/activation_record.plist",
                    "/var/root/Library/Lockdown/activation_records/wildcard_record.plist",
                    "/var/root/Library/Lockdown/data_ark.plist",
                    "/var/mobile/Library/FairPlay/iTunes_Control/iTunes/ic-info.sisv",
                    "/var/mobile/Library/Preferences/com.apple.purplebuddy.plist"
                };

                int removedCount = 0;

                foreach (var filePath in activationPaths)
                {
                    try
                    {
                        var cmd = _sshClient.CreateCommand($"rm -f {filePath}");
                        cmd.Execute();

                        if (cmd.ExitStatus == 0)
                        {
                            _logger.Info($"Removed: {filePath}");
                            removedCount++;
                        }
                        else
                        {
                            _logger.Debug($"File not found or couldn't remove: {filePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Error removing {filePath}: {ex.Message}");
                    }
                }

                // Clear activation cache
                _logger.Info("Clearing activation cache...");
                try
                {
                    var clearCache = _sshClient.CreateCommand("rm -rf /var/root/Library/Lockdown/activation_records/*");
                    clearCache.Execute();
                }
                catch { }

                // Restart mobileactivationd to apply changes
                _logger.Info("Restarting mobileactivationd...");
                try
                {
                    var restartCmd = _sshClient.CreateCommand("killall -9 mobileactivationd 2>/dev/null || true");
                    restartCmd.Execute();
                }
                catch { }

                _logger.Info($"Successfully processed {removedCount} activation file(s)");
                _logger.Warning("Device will now show 'Unactivated' state. Reboot may be required.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to remove activation files: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Backs up activation files from device before removal (safety)
        /// </summary>
        /// <param name="backupPath">Local path to save backup</param>
        /// <returns>True if successful</returns>
        public bool BackupActivationFiles(string backupPath)
        {
            if (!_isConnected || _sftpClient == null)
            {
                _logger.Error("SFTP not connected. Cannot backup activation files.");
                return false;
            }

            try
            {
                _logger.Info($"Backing up activation files to: {backupPath}");

                // Create backup directory
                Directory.CreateDirectory(backupPath);

                // Files to backup
                string[] filesToBackup = new[]
                {
                    "/var/root/Library/Lockdown/activation_records/activation_record.plist",
                    "/var/root/Library/Lockdown/activation_records/wildcard_record.plist",
                    "/var/root/Library/Lockdown/data_ark.plist",
                    "/var/mobile/Library/Preferences/com.apple.purplebuddy.plist"
                };

                int backedUpCount = 0;

                foreach (var remotePath in filesToBackup)
                {
                    try
                    {
                        string fileName = Path.GetFileName(remotePath);
                        string localPath = Path.Combine(backupPath, fileName);

                        if (_sftpClient.Exists(remotePath))
                        {
                            using (var fileStream = File.Create(localPath))
                            {
                                _sftpClient.DownloadFile(remotePath, fileStream);
                            }
                            _logger.Info($"Backed up: {fileName}");
                            backedUpCount++;
                        }
                        else
                        {
                            _logger.Debug($"File not found (skipping): {remotePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Couldn't backup {remotePath}: {ex.Message}");
                    }
                }

                // Create backup info file
                string infoPath = Path.Combine(backupPath, "backup_info.txt");
                File.WriteAllText(infoPath, 
                    $"Backup Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Files Backed Up: {backedUpCount}\n" +
                    $"Device Connected: Yes\n");

                _logger.Info($"Backup complete: {backedUpCount} file(s) saved to {backupPath}");
                return backedUpCount > 0;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to backup activation files: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores activation files from backup
        /// </summary>
        /// <param name="backupPath">Local path containing backup files</param>
        /// <returns>True if successful</returns>
        public bool RestoreActivationFiles(string backupPath)
        {
            if (!_isConnected || _sftpClient == null)
            {
                _logger.Error("SFTP not connected. Cannot restore activation files.");
                return false;
            }

            if (!Directory.Exists(backupPath))
            {
                _logger.Error($"Backup path does not exist: {backupPath}");
                return false;
            }

            try
            {
                _logger.Info($"Restoring activation files from: {backupPath}");

                // File mappings: local filename -> remote path
                var fileMapping = new Dictionary<string, string>
                {
                    { "activation_record.plist", "/var/root/Library/Lockdown/activation_records/activation_record.plist" },
                    { "wildcard_record.plist", "/var/root/Library/Lockdown/activation_records/wildcard_record.plist" },
                    { "data_ark.plist", "/var/root/Library/Lockdown/data_ark.plist" },
                    { "com.apple.purplebuddy.plist", "/var/mobile/Library/Preferences/com.apple.purplebuddy.plist" }
                };

                int restoredCount = 0;

                foreach (var mapping in fileMapping)
                {
                    string localPath = Path.Combine(backupPath, mapping.Key);
                    string remotePath = mapping.Value;

                    if (File.Exists(localPath))
                    {
                        try
                        {
                            // Ensure remote directory exists
                            string remoteDir = Path.GetDirectoryName(remotePath).Replace("\\", "/");
                            CreateRemoteDirectory(remoteDir);

                            // Upload file
                            using (var fileStream = File.OpenRead(localPath))
                            {
                                _sftpClient.UploadFile(fileStream, remotePath, true);
                            }
                            _logger.Info($"Restored: {mapping.Key}");
                            restoredCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning($"Failed to restore {mapping.Key}: {ex.Message}");
                        }
                    }
                }

                // Restart mobileactivationd
                _logger.Info("Restarting mobileactivationd...");
                try
                {
                    var restartCmd = _sshClient.CreateCommand("killall -9 mobileactivationd 2>/dev/null || true");
                    restartCmd.Execute();
                }
                catch { }

                _logger.Info($"Restore complete: {restoredCount} file(s) restored");
                return restoredCount > 0;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to restore activation files: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Complete Bypass Injection

        /// <summary>
        /// Injects all files required for complete activation bypass
        /// </summary>
        public BypassResult InjectBypassFiles(DeviceInfo device, Dictionary<string, byte[]> activationFiles, Dictionary<string, byte[]> setupFiles)
        {
            ErrorHandler.ValidateNotNull(device, nameof(device));

            _logger.Info("Starting complete bypass injection...");

            var result = new BypassResult { StartTime = DateTime.Now };

            try
            {
                // Update path resolver for iOS version
                if (IOSVersion.TryParse(device.ProductVersion, out var version))
                {
                    _pathResolver.IOSVersion = version;
                }

                // Build complete file list with resolved paths
                var allFiles = new Dictionary<string, byte[]>();

                // Add activation files
                if (activationFiles != null)
                {
                    foreach (var kvp in activationFiles)
                    {
                        string remotePath = ResolveRemotePath(kvp.Key);
                        allFiles[remotePath] = kvp.Value;
                    }
                }

                // Add setup bypass files
                if (setupFiles != null)
                {
                    foreach (var kvp in setupFiles)
                    {
                        string remotePath = ResolveRemotePath(kvp.Key);
                        allFiles[remotePath] = kvp.Value;
                    }
                }

                _logger.Info($"Total files to inject: {allFiles.Count}");

                // Inject all files
                OnProgressChanged(10, "Injecting activation files...");

                if (!InjectFiles(allFiles))
                {
                    result.Success = false;
                    result.ErrorMessage = "Some files failed to inject";
                    result.CurrentStep = BypassStep.InjectingFiles;
                    return result;
                }

                OnProgressChanged(70, "Setting permissions...");

                // Set proper ownership
                ExecuteCommand("chown -R mobile:mobile /var/mobile/Library/");
                ExecuteCommand("chmod -R 755 /var/root/Library/Lockdown/");

                OnProgressChanged(80, "Restarting services...");

                // Restart activation daemon
                RestartActivationDaemon();

                OnProgressChanged(90, "Verifying injection...");

                // Verify key files exist
                string activationPath = _pathResolver.GetActivationRecordPath();
                if (!FileExists(activationPath))
                {
                    result.Success = false;
                    result.ErrorMessage = "Activation record not found after injection";
                    result.CurrentStep = BypassStep.Verifying;
                    return result;
                }

                OnProgressChanged(100, "Bypass complete");

                result.Success = true;
                result.CurrentStep = BypassStep.Complete;
                result.StatusMessage = "All files injected successfully";
                result.EndTime = DateTime.Now;
                result.RequiresReboot = true;

                foreach (var path in allFiles.Keys)
                {
                    result.AddInjectedFile(path);
                }

                _logger.Info($"Bypass injection completed in {result.Duration.TotalSeconds:F1}s");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("Bypass injection failed", ex);
                return ErrorHandler.CreateFailureResult(ex, BypassStep.InjectingFiles, "Bypass injection");
            }
        }

        /// <summary>
        /// Resolves a filename to full remote path
        /// </summary>
        private string ResolveRemotePath(string filename)
        {
            filename = filename.Replace('\\', '/');

            // If already absolute path, return as-is
            if (filename.StartsWith("/"))
                return filename;

            // Resolve based on filename
            return filename switch
            {
                "activation_record.plist" => _pathResolver.GetActivationRecordPath(),
                "data_ark.plist" => _pathResolver.GetDataArkPath(),
                "com.apple.purplebuddy.plist" => _pathResolver.GetPurpleBuddyPath(),
                "wildcard_record.plist" => $"{_pathResolver.GetLockdownDirectory()}/wildcard_record.plist",
                "bypass_record.plist" => $"{_pathResolver.GetLockdownDirectory()}/bypass_record.plist",
                _ => $"{_pathResolver.GetLockdownDirectory()}/{filename}"
            };
        }

        #endregion

        #region Events

        /// <summary>
        /// Raises progress changed event
        /// </summary>
        protected virtual void OnProgressChanged(int progress, string status)
        {
            ProgressChanged?.Invoke(this, new InjectionProgressEventArgs(progress, status));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            DisconnectSSH();
            _logger.Debug("FileInjector disposed");
        }

        #endregion
    }

    /// <summary>
    /// Event args for injection progress
    /// </summary>
    public class InjectionProgressEventArgs : EventArgs
    {
        public int Progress { get; }
        public string Status { get; }

        public InjectionProgressEventArgs(int progress, string status)
        {
            Progress = progress;
            Status = status;
        }
    }
}
