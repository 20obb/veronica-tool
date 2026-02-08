using System;
using System.Collections.Generic;
using System.Text;
using Claunia.PropertyList;
using BypassTool.Models;
using BypassTool.Utils;

namespace BypassTool.Core
{
    /// <summary>
    /// Generates fake activation records and related plist files for iOS activation bypass
    /// </summary>
    public class ActivationEngine : IDisposable
    {
        #region Fields

        private readonly Logger _logger = Logger.Instance;
        private readonly CryptoHelper _cryptoHelper;
        private readonly PathResolver _pathResolver;
        private bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new ActivationEngine instance
        /// </summary>
        public ActivationEngine()
        {
            _logger.Info("Initializing ActivationEngine...");
            _cryptoHelper = new CryptoHelper();
            _pathResolver = new PathResolver();
            _logger.Debug("ActivationEngine initialized");
        }

        #endregion

        #region Activation Record Generation

        /// <summary>
        /// Generates a complete activation record for the device
        /// </summary>
        /// <param name="device">Device information</param>
        /// <returns>Binary plist data for activation_record.plist</returns>
        public byte[] GenerateActivationRecord(DeviceInfo device)
        {
            ErrorHandler.ValidateNotNull(device, nameof(device));
            
            _logger.Info($"Generating activation record for {device.ProductType}...");

            try
            {
                // Update path resolver for iOS version
                if (IOSVersion.TryParse(device.ProductVersion, out var version))
                {
                    _pathResolver.IOSVersion = version;
                }

                // Create main dictionary
                var record = new NSDictionary();

                // Core activation fields
                record.Add("ActivationState", new NSString("Activated"));
                record.Add("ActivationStateAcknowledged", new NSNumber(true));
                record.Add("ActivationInfoComplete", new NSNumber(true));
                record.Add("BrickState", new NSNumber(false));

                // Device identification
                record.Add("UniqueDeviceID", new NSString(device.UDID ?? ""));
                record.Add("SerialNumber", new NSString(device.SerialNumber ?? ""));
                record.Add("ProductType", new NSString(device.ProductType ?? ""));
                record.Add("DeviceClass", new NSString(device.DeviceClass ?? "iPhone"));
                record.Add("BuildVersion", new NSString(device.BuildVersion ?? ""));
                record.Add("ProductVersion", new NSString(device.ProductVersion ?? ""));

                // ECID (if available)
                if (!string.IsNullOrEmpty(device.ECID))
                {
                    record.Add("UniqueChipID", new NSString(device.ECID));
                }

                // Generate cryptographic data
                byte[] deviceCert = _cryptoHelper.GenerateFakeDeviceCertificate(device.UDID, device.ProductType);
                byte[] signature = _cryptoHelper.GenerateActivationSignature(device.UDID, device.ECID, device.SerialNumber);
                byte[] fairPlayData = _cryptoHelper.GenerateFakeFairPlayData();
                byte[] accountToken = _cryptoHelper.GenerateFakeAccountToken(device.UDID);

                // Add cryptographic data
                record.Add("DeviceCertRequest", new NSData(deviceCert));
                record.Add("AccountTokenSignature", new NSData(signature));
                record.Add("FairPlayKeyData", new NSData(fairPlayData));
                record.Add("AccountToken", new NSData(accountToken));

                // Generate activation info blob
                var activationInfo = GenerateActivationInfoBlob(device);
                record.Add("ActivationInfoXML", new NSData(activationInfo));

                // Add timestamp
                record.Add("ActivationTime", new NSDate(DateTime.UtcNow));

                // Convert to binary plist
                byte[] plistData = PlistHelper.ToBinary(record);

                _logger.Info($"Generated activation record: {plistData.Length} bytes");
                return plistData;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate activation record", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a wildcard activation record (simplified)
        /// </summary>
        public byte[] GenerateWildcardRecord(DeviceInfo device)
        {
            ErrorHandler.ValidateNotNull(device, nameof(device));

            _logger.Info("Generating wildcard record...");

            try
            {
                var record = new NSDictionary();

                // Minimal activation state
                record.Add("ActivationState", new NSString("Activated"));
                record.Add("ActivationStateAcknowledged", new NSNumber(true));
                record.Add("BrickState", new NSNumber(false));
                
                // Device identifier
                record.Add("UniqueDeviceID", new NSString(device.UDID ?? "*"));

                // Wildcard indicator
                record.Add("WildcardActivation", new NSNumber(true));

                byte[] plistData = PlistHelper.ToBinary(record);

                _logger.Debug($"Generated wildcard record: {plistData.Length} bytes");
                return plistData;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate wildcard record", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates data_ark.plist for iOS 15+ devices
        /// </summary>
        public byte[] GenerateDataArk(DeviceInfo device)
        {
            ErrorHandler.ValidateNotNull(device, nameof(device));

            _logger.Info("Generating data_ark.plist...");

            try
            {
                var dataArk = new NSDictionary();

                // Root keys
                var dashRoot = new NSDictionary();
                
                // Activation-related keys
                dashRoot.Add("-ActivationState", new NSString("Activated"));
                dashRoot.Add("-ActivationStateAcknowledged", new NSNumber(true));
                dashRoot.Add("-ProductType", new NSString(device.ProductType ?? ""));
                dashRoot.Add("-ProductVersion", new NSString(device.ProductVersion ?? ""));
                dashRoot.Add("-BuildVersion", new NSString(device.BuildVersion ?? ""));
                dashRoot.Add("-UniqueDeviceID", new NSString(device.UDID ?? ""));
                dashRoot.Add("-SerialNumber", new NSString(device.SerialNumber ?? ""));
                dashRoot.Add("-BrickState", new NSNumber(false));
                
                // Device info keys
                dashRoot.Add("-DeviceName", new NSString(device.DeviceName ?? "iPhone"));
                dashRoot.Add("-DeviceClass", new NSString(device.DeviceClass ?? "iPhone"));
                
                // Network addresses
                dashRoot.Add("-WiFiAddress", new NSString(device.WiFiAddress ?? ""));
                dashRoot.Add("-BluetoothAddress", new NSString(device.BluetoothAddress ?? ""));

                dataArk.Add("-", dashRoot);

                // Add timestamp
                dataArk.Add("com.apple.mobileactivationd.time", new NSDate(DateTime.UtcNow));

                byte[] plistData = PlistHelper.ToBinary(dataArk);

                _logger.Debug($"Generated data_ark.plist: {plistData.Length} bytes");
                return plistData;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate data_ark.plist", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates activation lock bypass record
        /// </summary>
        public byte[] GenerateLockBypassRecord(DeviceInfo device)
        {
            ErrorHandler.ValidateNotNull(device, nameof(device));

            _logger.Info("Generating lock bypass record...");

            try
            {
                var record = new NSDictionary();

                // Bypass flags
                record.Add("BypassStatus", new NSNumber(true));
                record.Add("BypassActivationLock", new NSNumber(true));
                record.Add("BypassMDMLock", new NSNumber(true));
                record.Add("ActivationState", new NSString("Activated"));
                
                // Device info
                record.Add("UniqueDeviceID", new NSString(device.UDID ?? ""));
                record.Add("ProductType", new NSString(device.ProductType ?? ""));
                
                // Timestamp
                record.Add("BypassTime", new NSDate(DateTime.UtcNow));

                byte[] plistData = PlistHelper.ToBinary(record);

                _logger.Debug($"Generated lock bypass record: {plistData.Length} bytes");
                return plistData;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate lock bypass record", ex);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generates the activation info XML blob
        /// </summary>
        private byte[] GenerateActivationInfoBlob(DeviceInfo device)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
            sb.AppendLine("<plist version=\"1.0\">");
            sb.AppendLine("<dict>");
            
            sb.AppendLine($"    <key>UniqueDeviceID</key>");
            sb.AppendLine($"    <string>{device.UDID ?? ""}</string>");
            
            sb.AppendLine($"    <key>ProductType</key>");
            sb.AppendLine($"    <string>{device.ProductType ?? ""}</string>");
            
            sb.AppendLine($"    <key>ProductVersion</key>");
            sb.AppendLine($"    <string>{device.ProductVersion ?? ""}</string>");
            
            sb.AppendLine($"    <key>BuildVersion</key>");
            sb.AppendLine($"    <string>{device.BuildVersion ?? ""}</string>");
            
            sb.AppendLine($"    <key>SerialNumber</key>");
            sb.AppendLine($"    <string>{device.SerialNumber ?? ""}</string>");
            
            sb.AppendLine($"    <key>ActivationRandomness</key>");
            sb.AppendLine($"    <string>{_cryptoHelper.GenerateRandomHex(16)}</string>");
            
            sb.AppendLine($"    <key>ActivationState</key>");
            sb.AppendLine($"    <string>Activated</string>");
            
            sb.AppendLine("</dict>");
            sb.AppendLine("</plist>");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// Generates all required activation files for the device
        /// </summary>
        /// <returns>Dictionary of filename -> data</returns>
        public Dictionary<string, byte[]> GenerateAllActivationFiles(DeviceInfo device)
        {
            ErrorHandler.ValidateNotNull(device, nameof(device));

            _logger.Info("Generating all activation files...");

            var files = new Dictionary<string, byte[]>();

            try
            {
                // Main activation record
                files["activation_record.plist"] = GenerateActivationRecord(device);

                // Wildcard record (backup)
                files["wildcard_record.plist"] = GenerateWildcardRecord(device);

                // Data ark (iOS 15+)
                var iosVersion = IOSVersion.TryParse(device.ProductVersion, out var ver) ? ver : IOSVersion.IOS_15;
                if (iosVersion >= IOSVersion.IOS_15)
                {
                    files["data_ark.plist"] = GenerateDataArk(device);
                }

                // Lock bypass record
                files["bypass_record.plist"] = GenerateLockBypassRecord(device);

                _logger.Info($"Generated {files.Count} activation files");
                return files;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate activation files", ex);
                throw;
            }
        }

        /// <summary>
        /// Validates an existing activation record
        /// </summary>
        public bool ValidateActivationRecord(byte[] plistData)
        {
            if (plistData == null || plistData.Length == 0)
                return false;

            try
            {
                var plist = PlistHelper.Parse(plistData);
                if (plist is NSDictionary dict)
                {
                    // Check required keys
                    string activationState = PlistHelper.GetString(dict, "ActivationState");
                    return activationState == "Activated";
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Activation record validation failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cryptoHelper?.Dispose();
            _logger.Debug("ActivationEngine disposed");
        }

        #endregion
    }
}
