using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.Plist;
using iMobileDevice.Afc;
using Renci.SshNet;
using BypassTool.Models;
using BypassTool.Utils;

namespace BypassTool.Core
{
    /// <summary>
    /// Manages iOS device detection and communication via libimobiledevice
    /// </summary>
    public class DeviceManager : IDisposable
    {
        #region Fields

        private readonly Logger _logger = Logger.Instance;
        private readonly IiDeviceApi _iDeviceApi;
        private readonly ILockdownApi _lockdownApi;
        private readonly IPlistApi _plistApi;
        private readonly IAfcApi _afcApi;
        
        private iDeviceHandle _deviceHandle;
        private LockdownClientHandle _lockdownHandle;
        private bool _isConnected;
        private DeviceInfo _currentDevice;
        private bool _disposed;

        /// <summary>
        /// Event raised when a device is connected
        /// </summary>
        public event EventHandler<DeviceInfo> DeviceConnected;

        /// <summary>
        /// Event raised when a device is disconnected
        /// </summary>
        public event EventHandler DeviceDisconnected;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new DeviceManager instance
        /// </summary>
        public DeviceManager()
        {
            _logger.Info("Initializing DeviceManager...");

            try
            {
                // Initialize libimobiledevice
                NativeLibraries.Load();
                _logger.Debug("Native libraries loaded successfully");

                // Get API instances
                _iDeviceApi = LibiMobileDevice.Instance.iDevice;
                _lockdownApi = LibiMobileDevice.Instance.Lockdown;
                _plistApi = LibiMobileDevice.Instance.Plist;
                _afcApi = LibiMobileDevice.Instance.Afc;

                _logger.Info("DeviceManager initialized successfully");
            }
            catch (DllNotFoundException ex)
            {
                _logger.Error("Failed to load libimobiledevice. Please ensure the library is installed.", ex);
                throw new InvalidOperationException(
                    "libimobiledevice not found. Please install iTunes or libimobiledevice.", ex);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize DeviceManager", ex);
                throw;
            }
        }

        #endregion

        #region Device Detection

        /// <summary>
        /// Detects all connected iOS devices
        /// </summary>
        /// <returns>List of detected device information</returns>
        public List<DeviceInfo> DetectDevices()
        {
            _logger.Info("Detecting iOS devices...");
            var devices = new List<DeviceInfo>();

            try
            {
                // Get device list
                int count = 0;
                ReadOnlyCollection<string> udids;

                var result = _iDeviceApi.idevice_get_device_list(out udids, ref count);

                if (result != iDeviceError.Success)
                {
                    if (result == iDeviceError.NoDevice)
                    {
                        _logger.Info("No iOS devices detected");
                        return devices;
                    }

                    _logger.Error($"Failed to get device list: {result}");
                    throw new InvalidOperationException($"Failed to get device list: {result}");
                }

                _logger.Info($"Found {count} iOS device(s)");

                // Get info for each device
                foreach (var udid in udids)
                {
                    try
                    {
                        _logger.Debug($"Getting info for device: {udid}");
                        
                        var deviceInfo = GetDeviceInfoByUDID(udid);
                        if (deviceInfo != null)
                        {
                            devices.Add(deviceInfo);
                            _logger.Info($"Device found: {deviceInfo.DisplayName} ({deviceInfo.ProductType})");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Failed to get info for device {udid}: {ex.Message}");
                    }
                }

                return devices;
            }
            catch (Exception ex)
            {
                _logger.Error("Error detecting devices", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets device info by UDID without maintaining connection
        /// </summary>
        private DeviceInfo GetDeviceInfoByUDID(string udid)
        {
            iDeviceHandle deviceHandle = null;
            LockdownClientHandle lockdownHandle = null;

            try
            {
                // Connect to device
                var result = _iDeviceApi.idevice_new(out deviceHandle, udid);
                if (result != iDeviceError.Success)
                {
                    _logger.Warning($"Failed to connect to device {udid}: {result}");
                    return null;
                }

                // Create lockdown client
                var lockResult = _lockdownApi.lockdownd_client_new_with_handshake(
                    deviceHandle, out lockdownHandle, "BypassTool");

                if (lockResult != LockdownError.Success)
                {
                    _logger.Warning($"Failed to create lockdown client for {udid}: {lockResult}");
                    return null;
                }

                // Get device info
                var deviceInfo = new DeviceInfo { UDID = udid };
                PopulateDeviceInfo(deviceInfo, lockdownHandle);

                return deviceInfo;
            }
            finally
            {
                // Cleanup
                if (lockdownHandle != null && !lockdownHandle.IsInvalid)
                {
                    lockdownHandle.Dispose();
                }
                if (deviceHandle != null && !deviceHandle.IsInvalid)
                {
                    deviceHandle.Dispose();
                }
            }
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Connects to a device by UDID
        /// </summary>
        /// <param name="udid">Device UDID</param>
        /// <returns>True if connection successful</returns>
        public bool ConnectToDevice(string udid)
        {
            if (string.IsNullOrEmpty(udid))
            {
                throw new ArgumentNullException(nameof(udid));
            }

            _logger.Info($"Connecting to device: {udid}");

            // Retry logic for more robust connection
            int maxRetries = 3;
            int retryDelay = 500; // ms

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Disconnect if already connected (clean slate)
                    if (_isConnected)
                    {
                        _logger.Debug("Disconnecting existing connection before new connection...");
                        Disconnect();
                    }

                    // Create new device handle
                    _logger.Debug($"Creating device handle (attempt {attempt}/{maxRetries})...");
                    var result = _iDeviceApi.idevice_new(out _deviceHandle, udid);
                    
                    if (result != iDeviceError.Success)
                    {
                        _logger.Warning($"Failed to connect to device (attempt {attempt}): {result}");
                        
                        if (result == iDeviceError.NoDevice)
                        {
                            _logger.Warning("Device not found. Make sure it's connected and trusted.");
                        }
                        
                        if (attempt < maxRetries)
                        {
                            System.Threading.Thread.Sleep(retryDelay);
                            continue;
                        }
                        
                        _logger.Error($"Failed to connect after {maxRetries} attempts: {result}");
                        return false;
                    }

                    if (_deviceHandle == null || _deviceHandle.IsInvalid)
                    {
                        _logger.Error("Device handle is invalid");
                        if (attempt < maxRetries)
                        {
                            System.Threading.Thread.Sleep(retryDelay);
                            continue;
                        }
                        return false;
                    }

                    _logger.Debug("Device handle created successfully");

                    // Create lockdown client with handshake
                    _logger.Debug("Performing lockdown handshake...");
                    var lockResult = _lockdownApi.lockdownd_client_new_with_handshake(
                        _deviceHandle, out _lockdownHandle, "BypassTool");

                    if (lockResult != LockdownError.Success)
                    {
                        _logger.Warning($"Lockdown handshake failed (attempt {attempt}): {lockResult}");
                        
                        // Cleanup device handle
                        if (_deviceHandle != null && !_deviceHandle.IsInvalid)
                        {
                            _deviceHandle.Dispose();
                            _deviceHandle = null;
                        }
                        
                        if (lockResult == LockdownError.PairingFailed)
                        {
                            _logger.Error("Device not trusted. Please tap 'Trust' on your device.");
                        }
                        else if (lockResult == LockdownError.InvalidConf)
                        {
                            _logger.Error("Invalid configuration. Try disconnecting and reconnecting the device.");
                        }
                        
                        if (attempt < maxRetries)
                        {
                            System.Threading.Thread.Sleep(retryDelay);
                            continue;
                        }
                        
                        return false;
                    }

                    if (_lockdownHandle == null || _lockdownHandle.IsInvalid)
                    {
                        _logger.Error("Lockdown handle is invalid");
                        if (_deviceHandle != null && !_deviceHandle.IsInvalid)
                        {
                            _deviceHandle.Dispose();
                            _deviceHandle = null;
                        }
                        if (attempt < maxRetries)
                        {
                            System.Threading.Thread.Sleep(retryDelay);
                            continue;
                        }
                        return false;
                    }

                    _logger.Debug("Lockdown client created successfully");

                    // Get device info
                    _currentDevice = new DeviceInfo { UDID = udid };
                    PopulateDeviceInfo(_currentDevice, _lockdownHandle);
                    _currentDevice.IsConnected = true;

                    _isConnected = true;

                    _logger.Info($"✓ Connected to: {_currentDevice.DisplayName}");
                    _logger.Info($"  iOS Version: {_currentDevice.ProductVersion}");
                    _logger.Info($"  Activation State: {_currentDevice.ActivationState}");

                    // Raise event
                    DeviceConnected?.Invoke(this, _currentDevice);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error connecting to device (attempt {attempt}): {ex.Message}");
                    
                    // Cleanup on error
                    if (_lockdownHandle != null && !_lockdownHandle.IsInvalid)
                    {
                        _lockdownHandle.Dispose();
                        _lockdownHandle = null;
                    }
                    if (_deviceHandle != null && !_deviceHandle.IsInvalid)
                    {
                        _deviceHandle.Dispose();
                        _deviceHandle = null;
                    }
                    
                    if (attempt < maxRetries)
                    {
                        System.Threading.Thread.Sleep(retryDelay);
                        continue;
                    }
                    
                    throw;
                }
            }

            return false;
        }

        /// <summary>
        /// Disconnects from the current device
        /// </summary>
        public void Disconnect()
        {
            _logger.Debug("Disconnecting from device...");

            try
            {
                if (_lockdownHandle != null && !_lockdownHandle.IsInvalid)
                {
                    _lockdownHandle.Dispose();
                    _lockdownHandle = null;
                }

                if (_deviceHandle != null && !_deviceHandle.IsInvalid)
                {
                    _deviceHandle.Dispose();
                    _deviceHandle = null;
                }

                if (_isConnected)
                {
                    _isConnected = false;
                    if (_currentDevice != null)
                    {
                        _currentDevice.IsConnected = false;
                    }
                    _currentDevice = null;

                    DeviceDisconnected?.Invoke(this, EventArgs.Empty);
                }

                _logger.Debug("Disconnected successfully");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error during disconnect: {ex.Message}");
            }
        }

        #endregion

        #region Device Information

        /// <summary>
        /// Gets information about the currently connected device
        /// </summary>
        public DeviceInfo GetDeviceInfo()
        {
            if (!_isConnected || _currentDevice == null)
            {
                throw new InvalidOperationException("No device connected");
            }

            // Refresh device info
            PopulateDeviceInfo(_currentDevice, _lockdownHandle);

            return _currentDevice;
        }

        /// <summary>
        /// Populates device info from lockdown client
        /// </summary>
        private void PopulateDeviceInfo(DeviceInfo deviceInfo, LockdownClientHandle lockdown)
        {
            _logger.Debug("Reading device information...");

            // Basic identifiers
            deviceInfo.ProductType = GetLockdownValue(lockdown, "ProductType") ?? "Unknown";
            deviceInfo.ProductVersion = GetLockdownValue(lockdown, "ProductVersion") ?? "0.0";
            deviceInfo.BuildVersion = GetLockdownValue(lockdown, "BuildVersion");
            deviceInfo.SerialNumber = GetLockdownValue(lockdown, "SerialNumber");
            deviceInfo.ECID = GetLockdownValue(lockdown, "UniqueChipID");
            deviceInfo.DeviceClass = GetLockdownValue(lockdown, "DeviceClass");
            deviceInfo.DeviceName = GetLockdownValue(lockdown, "DeviceName");
            deviceInfo.HardwareModel = GetLockdownValue(lockdown, "HardwareModel");
            deviceInfo.ModelNumber = GetLockdownValue(lockdown, "ModelNumber");
            deviceInfo.RegionInfo = GetLockdownValue(lockdown, "RegionInfo");
            deviceInfo.TimeZone = GetLockdownValue(lockdown, "TimeZone");

            // Network info
            deviceInfo.WiFiAddress = GetLockdownValue(lockdown, "WiFiAddress");
            deviceInfo.BluetoothAddress = GetLockdownValue(lockdown, "BluetoothAddress");

            // Cellular info (if applicable)
            deviceInfo.IMEI = GetLockdownValue(lockdown, "InternationalMobileEquipmentIdentity");
            deviceInfo.CarrierName = GetLockdownValue(lockdown, "CarrierBundleInfoArray", null);
            deviceInfo.PhoneNumber = GetLockdownValue(lockdown, "PhoneNumber");

            // Activation state
            deviceInfo.ActivationState = GetLockdownValue(lockdown, "ActivationState") ?? "Unknown";
            
            string activationLocked = GetLockdownValue(lockdown, "ActivationLocked");
            deviceInfo.IsActivationLocked = activationLocked?.ToLower() == "true";

            // Find My iPhone
            string findMy = GetLockdownValue(lockdown, "FMiPActive");
            deviceInfo.IsFindMyEnabled = findMy?.ToLower() == "true";

            // Passcode
            string hasPasscode = GetLockdownValue(lockdown, "PasswordProtected");
            deviceInfo.HasPasscode = hasPasscode?.ToLower() == "true";

            // Connection type
            deviceInfo.ConnectionType = "USB";

            _logger.Debug($"Device info populated: {deviceInfo.ProductType}, iOS {deviceInfo.ProductVersion}");
        }

        /// <summary>
        /// Gets a value from lockdown service
        /// </summary>
        private string GetLockdownValue(LockdownClientHandle lockdown, string key, string domain = null)
        {
            if (lockdown == null || lockdown.IsInvalid)
                return null;

            try
            {
                PlistHandle plist;
                var result = _lockdownApi.lockdownd_get_value(lockdown, domain, key, out plist);

                if (result != LockdownError.Success || plist == null || plist.IsInvalid)
                    return null;

                try
                {
                    // Get plist type
                    var plistType = _plistApi.plist_get_node_type(plist);

                    // Use plist_to_xml to get value as string
                    string xmlString;
                    uint length = 0;
                    _plistApi.plist_to_xml(plist, out xmlString, ref length);
                    
                    if (string.IsNullOrEmpty(xmlString))
                        return null;
                    
                    // Parse the XML to extract the value
                    // The XML format is: <?xml...><plist...><string>VALUE</string></plist>
                    // or <integer>VALUE</integer>, <true/>, <false/> etc.
                    
                    switch (plistType)
                    {
                        case PlistType.String:
                            var stringMatch = System.Text.RegularExpressions.Regex.Match(xmlString, @"<string>(.*?)</string>");
                            return stringMatch.Success ? stringMatch.Groups[1].Value : null;

                        case PlistType.Boolean:
                            if (xmlString.Contains("<true/>"))
                                return "true";
                            if (xmlString.Contains("<false/>"))
                                return "false";
                            return null;

                        case PlistType.Uint:
                        case PlistType.Real:
                            var numMatch = System.Text.RegularExpressions.Regex.Match(xmlString, @"<(?:integer|real)>(.*?)</(?:integer|real)>");
                            return numMatch.Success ? numMatch.Groups[1].Value : null;

                        default:
                            return null;
                    }
                }
                finally
                {
                    if (plist != null && !plist.IsInvalid)
                    {
                        plist.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to get lockdown value '{key}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the current activation state
        /// </summary>
        public string GetActivationState()
        {
            if (!_isConnected)
                throw new InvalidOperationException("No device connected");

            return GetLockdownValue(_lockdownHandle, "ActivationState") ?? "Unknown";
        }

        #endregion

        #region Jailbreak Detection

        /// <summary>
        /// Checks if the connected device is jailbroken (rootful or rootless)
        /// </summary>
        /// <returns>True if jailbreak is detected</returns>
        public bool IsDeviceJailbroken()
        {
            if (!_isConnected)
                throw new InvalidOperationException("No device connected");

            _logger.Debug("Checking for jailbreak (rootful and rootless)...");

            try
            {
                // Method 1: Try AFC to check for common jailbreak indicators
                AfcClientHandle afcHandle;
                var result = _afcApi.afc_client_start_service(_deviceHandle, out afcHandle, "BypassTool");

                if (result == AfcError.Success && afcHandle != null && !afcHandle.IsInvalid)
                {
                    try
                    {
                        // Rootful jailbreak paths to check via AFC
                        string[] rootfulPaths = new[]
                        {
                            "/Applications/Cydia.app",
                            "/Applications/Sileo.app",
                            "/usr/bin/ssh",
                            "/bin/bash",
                            "/etc/apt",
                            "/private/var/lib/apt"
                        };

                        // Rootless jailbreak paths (iOS 15-16+)
                        string[] rootlessPaths = new[]
                        {
                            "/var/jb",                          // Main rootless directory
                            "/var/jb/usr/bin/ssh",              // SSH in rootless
                            "/var/jb/Applications/Sileo.app",   // Sileo in rootless
                            "/var/jb/usr/bin/apt",              // APT in rootless
                            "/.installed_dopamine",             // Dopamine marker
                            "/.installed_palera1n",             // palera1n marker
                            "/var/jb/prep_bootstrap.sh"         // Bootstrap script
                        };

                        // Check rootful paths
                        foreach (var path in rootfulPaths)
                        {
                            try
                            {
                                ReadOnlyCollection<string> fileInfo;
                                var fileResult = _afcApi.afc_get_file_info(afcHandle, path, out fileInfo);
                                
                                if (fileResult == AfcError.Success && fileInfo != null && fileInfo.Count > 0)
                                {
                                    _logger.Info($"Rootful jailbreak detected: {path}");
                                    afcHandle.Dispose();
                                    return true;
                                }
                            }
                            catch { }
                        }

                        // Check rootless paths
                        foreach (var path in rootlessPaths)
                        {
                            try
                            {
                                ReadOnlyCollection<string> fileInfo;
                                var fileResult = _afcApi.afc_get_file_info(afcHandle, path, out fileInfo);
                                
                                if (fileResult == AfcError.Success && fileInfo != null && fileInfo.Count > 0)
                                {
                                    _logger.Info($"Rootless jailbreak detected: {path}");
                                    afcHandle.Dispose();
                                    return true;
                                }
                            }
                            catch { }
                        }
                    }
                    finally
                    {
                        afcHandle.Dispose();
                    }
                }

                // If we got here without detecting jailbreak, assume not jailbroken
                // Note: AFC has limited access, so jailbreak may still be present
                // Full detection requires SSH connection
                _logger.Debug("No jailbreak indicators found via AFC (may need SSH for full detection)");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Jailbreak detection via AFC failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if device is jailbroken via SSH (more reliable detection)
        /// </summary>
        /// <param name="sshClient">Connected SSH client</param>
        /// <returns>True if jailbreak is detected</returns>
        public bool IsDeviceJailbrokenViaSSH(SshClient sshClient)
        {
            if (sshClient == null || !sshClient.IsConnected)
            {
                _logger.Warning("SSH client not connected for jailbreak detection");
                return false;
            }

            _logger.Debug("Checking jailbreak status via SSH...");

            try
            {
                // Paths to check for rootful jailbreak
                string[] rootfulPaths = new[]
                {
                    "/Applications/Cydia.app",
                    "/Applications/Sileo.app",
                    "/usr/bin/ssh",
                    "/bin/bash",
                    "/etc/apt",
                    "/private/var/lib/apt"
                };

                // Paths to check for rootless jailbreak (iOS 15-16+)
                string[] rootlessPaths = new[]
                {
                    "/var/jb",                          // Main rootless directory
                    "/var/jb/usr/bin/ssh",              // SSH in rootless
                    "/var/jb/Applications/Sileo.app",   // Sileo in rootless
                    "/var/jb/usr/bin/apt",              // APT in rootless
                    "/.installed_dopamine",             // Dopamine marker
                    "/.installed_palera1n",             // palera1n marker
                    "/var/jb/prep_bootstrap.sh"         // Bootstrap script
                };

                // Check rootful paths
                foreach (var path in rootfulPaths)
                {
                    try
                    {
                        var cmd = sshClient.CreateCommand($"test -e {path} && echo 'EXISTS' || echo 'NOT_FOUND'");
                        var result = cmd.Execute().Trim();

                        if (result == "EXISTS")
                        {
                            _logger.Info($"Rootful jailbreak detected via SSH: {path}");
                            return true;
                        }
                    }
                    catch { }
                }

                // Check rootless paths
                foreach (var path in rootlessPaths)
                {
                    try
                    {
                        var cmd = sshClient.CreateCommand($"test -e {path} && echo 'EXISTS' || echo 'NOT_FOUND'");
                        var result = cmd.Execute().Trim();

                        if (result == "EXISTS")
                        {
                            _logger.Info($"Rootless jailbreak detected via SSH: {path}");
                            return true;
                        }
                    }
                    catch { }
                }

                // Additional check: uname -v for jailbreak signature
                try
                {
                    var unameCmd = sshClient.CreateCommand("uname -v");
                    var unameResult = unameCmd.Execute().ToLower();
                    if (unameResult.Contains("jailbreak") || unameResult.Contains("palera1n") ||
                        unameResult.Contains("checkra1n") || unameResult.Contains("dopamine") ||
                        unameResult.Contains("fugu"))
                    {
                        _logger.Info("Jailbreak detected via uname signature");
                        return true;
                    }
                }
                catch { }

                _logger.Warning("No jailbreak indicators found via SSH");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking jailbreak status via SSH: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if device is in DFU mode
        /// </summary>
        public bool IsInDFUMode()
        {
            // DFU mode detection would require different logic
            // For now, return false as this would need irecovery
            return false;
        }

        /// <summary>
        /// Checks if device is in Recovery mode
        /// </summary>
        public bool IsInRecoveryMode()
        {
            // Recovery mode detection
            // Would need additional libimobiledevice calls
            return false;
        }

        #endregion

        #region Device Operations

        /// <summary>
        /// Reboots the connected device
        /// </summary>
        public bool RebootDevice()
        {
            if (!_isConnected)
                throw new InvalidOperationException("No device connected");

            _logger.Info("Rebooting device...");

            try
            {
                // Use diagnostics_relay service to reboot
                // This is a simplified implementation
                // Full implementation would use diagnostics_relay API

                _logger.Warning("Reboot via diagnostics_relay not fully implemented");
                _logger.Info("Please manually reboot the device");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to reboot device", ex);
                return false;
            }
        }

        /// <summary>
        /// Validates that a device is suitable for bypass
        /// </summary>
        public bool ValidateDeviceForBypass(out string errorMessage)
        {
            errorMessage = null;

            if (!_isConnected || _currentDevice == null)
            {
                errorMessage = "No device connected";
                return false;
            }

            // Check iOS version (12-16 supported)
            int iosMajor = _currentDevice.IOSMajorVersion;
            if (iosMajor < 12 || iosMajor > 16)
            {
                errorMessage = $"iOS {_currentDevice.ProductVersion} is not supported. Supported: iOS 12-16";
                return false;
            }

            // Check if already activated
            if (_currentDevice.ActivationState == "Activated")
            {
                errorMessage = "Device is already activated";
                return false;
            }

            // Check for checkm8 support
            if (!_currentDevice.SupportsCheckm8)
            {
                _logger.Warning($"Device {_currentDevice.ProductType} may not support checkm8 (A12+ chip)");
            }

            return true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether a device is currently connected
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Gets the currently connected device info
        /// </summary>
        public DeviceInfo CurrentDevice => _currentDevice;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Disconnect();
            _logger.Debug("DeviceManager disposed");
        }

        #endregion
    }
}
