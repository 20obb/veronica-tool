using System;

namespace BypassTool.Models
{
    /// <summary>
    /// Represents comprehensive information about an iOS device
    /// </summary>
    public class DeviceInfo
    {
        #region Device Identifiers

        /// <summary>
        /// Unique Device Identifier (40-character hex string)
        /// </summary>
        public string UDID { get; set; }

        /// <summary>
        /// Unique Chip ID (ECID) - used for personalization
        /// </summary>
        public string ECID { get; set; }

        /// <summary>
        /// Device serial number
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// WiFi MAC address
        /// </summary>
        public string WiFiAddress { get; set; }

        /// <summary>
        /// Bluetooth MAC address
        /// </summary>
        public string BluetoothAddress { get; set; }

        #endregion

        #region Device Properties

        /// <summary>
        /// Product type (e.g., "iPhone10,3", "iPad8,1")
        /// </summary>
        public string ProductType { get; set; }

        /// <summary>
        /// iOS version (e.g., "16.3.1")
        /// </summary>
        public string ProductVersion { get; set; }

        /// <summary>
        /// Build version (e.g., "20D67")
        /// </summary>
        public string BuildVersion { get; set; }

        /// <summary>
        /// Device class (iPhone, iPad, iPod)
        /// </summary>
        public string DeviceClass { get; set; }

        /// <summary>
        /// User-assigned device name
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Hardware model identifier
        /// </summary>
        public string HardwareModel { get; set; }

        /// <summary>
        /// Model number
        /// </summary>
        public string ModelNumber { get; set; }

        /// <summary>
        /// Region info
        /// </summary>
        public string RegionInfo { get; set; }

        /// <summary>
        /// Timezone setting
        /// </summary>
        public string TimeZone { get; set; }

        /// <summary>
        /// SIM carrier name (if cellular)
        /// </summary>
        public string CarrierName { get; set; }

        /// <summary>
        /// IMEI (cellular devices only)
        /// </summary>
        public string IMEI { get; set; }

        /// <summary>
        /// Phone number (if available)
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Total storage capacity in bytes
        /// </summary>
        public long TotalCapacity { get; set; }

        /// <summary>
        /// Available storage in bytes
        /// </summary>
        public long AvailableCapacity { get; set; }

        #endregion

        #region Activation Status

        /// <summary>
        /// Current activation state
        /// Values: "Activated", "Unactivated", "FactoryActivated", "ActivationError"
        /// </summary>
        public string ActivationState { get; set; }

        /// <summary>
        /// Whether the device is activation locked
        /// </summary>
        public bool IsActivationLocked { get; set; }

        /// <summary>
        /// Whether Find My iPhone is enabled
        /// </summary>
        public bool IsFindMyEnabled { get; set; }

        /// <summary>
        /// Whether the device has a passcode set
        /// </summary>
        public bool HasPasscode { get; set; }

        #endregion

        #region Connection Status

        /// <summary>
        /// Whether device is jailbroken (SSH accessible)
        /// </summary>
        public bool IsJailbroken { get; set; }

        /// <summary>
        /// Device IP address (for SSH connection)
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// SSH port (default: 22)
        /// </summary>
        public int SSHPort { get; set; } = 22;

        /// <summary>
        /// Whether device is currently connected
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Connection method (USB, WiFi)
        /// </summary>
        public string ConnectionType { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the iOS major version number
        /// </summary>
        public int IOSMajorVersion
        {
            get
            {
                if (string.IsNullOrEmpty(ProductVersion))
                    return 0;

                var parts = ProductVersion.Split('.');
                if (parts.Length > 0 && int.TryParse(parts[0], out int major))
                    return major;

                return 0;
            }
        }

        /// <summary>
        /// Gets a friendly display name for the device
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(DeviceName))
                    return DeviceName;

                return GetFriendlyProductName();
            }
        }

        /// <summary>
        /// Gets formatted storage info
        /// </summary>
        public string StorageInfo
        {
            get
            {
                if (TotalCapacity <= 0)
                    return "Unknown";

                double usedGB = (TotalCapacity - AvailableCapacity) / (1024.0 * 1024.0 * 1024.0);
                double totalGB = TotalCapacity / (1024.0 * 1024.0 * 1024.0);

                return $"{usedGB:F1} GB / {totalGB:F1} GB";
            }
        }

        /// <summary>
        /// Check if device supports checkm8 exploit (A5-A11 chips)
        /// </summary>
        public bool SupportsCheckm8
        {
            get
            {
                if (string.IsNullOrEmpty(ProductType))
                    return false;

                // A11 and earlier are vulnerable
                // iPhone X (iPhone10,3/6) is A11 (last vulnerable)
                // iPhone XS (iPhone11,2) is A12 (not vulnerable)
                
                string pt = ProductType.ToLower();
                
                // iPhone models
                if (pt.StartsWith("iphone"))
                {
                    string numPart = pt.Substring(6).Split(',')[0];
                    if (int.TryParse(numPart, out int model))
                    {
                        return model <= 10; // iPhone X and earlier
                    }
                }
                
                // iPad models
                if (pt.StartsWith("ipad"))
                {
                    string numPart = pt.Substring(4).Split(',')[0];
                    if (int.TryParse(numPart, out int model))
                    {
                        return model <= 7; // iPad Pro 2nd gen and earlier
                    }
                }

                return false;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a friendly name for the product type
        /// </summary>
        public string GetFriendlyProductName()
        {
            if (string.IsNullOrEmpty(ProductType))
                return "Unknown Device";

            // Common iPhone models
            switch (ProductType)
            {
                // iPhone
                case "iPhone7,1": return "iPhone 6 Plus";
                case "iPhone7,2": return "iPhone 6";
                case "iPhone8,1": return "iPhone 6s";
                case "iPhone8,2": return "iPhone 6s Plus";
                case "iPhone8,4": return "iPhone SE (1st gen)";
                case "iPhone9,1":
                case "iPhone9,3": return "iPhone 7";
                case "iPhone9,2":
                case "iPhone9,4": return "iPhone 7 Plus";
                case "iPhone10,1":
                case "iPhone10,4": return "iPhone 8";
                case "iPhone10,2":
                case "iPhone10,5": return "iPhone 8 Plus";
                case "iPhone10,3":
                case "iPhone10,6": return "iPhone X";
                case "iPhone11,2": return "iPhone XS";
                case "iPhone11,4":
                case "iPhone11,6": return "iPhone XS Max";
                case "iPhone11,8": return "iPhone XR";
                case "iPhone12,1": return "iPhone 11";
                case "iPhone12,3": return "iPhone 11 Pro";
                case "iPhone12,5": return "iPhone 11 Pro Max";
                case "iPhone12,8": return "iPhone SE (2nd gen)";
                case "iPhone13,1": return "iPhone 12 mini";
                case "iPhone13,2": return "iPhone 12";
                case "iPhone13,3": return "iPhone 12 Pro";
                case "iPhone13,4": return "iPhone 12 Pro Max";
                case "iPhone14,2": return "iPhone 13 Pro";
                case "iPhone14,3": return "iPhone 13 Pro Max";
                case "iPhone14,4": return "iPhone 13 mini";
                case "iPhone14,5": return "iPhone 13";
                case "iPhone14,6": return "iPhone SE (3rd gen)";
                case "iPhone14,7": return "iPhone 14";
                case "iPhone14,8": return "iPhone 14 Plus";
                case "iPhone15,2": return "iPhone 14 Pro";
                case "iPhone15,3": return "iPhone 14 Pro Max";
                
                // iPad
                case "iPad6,11":
                case "iPad6,12": return "iPad (5th gen)";
                case "iPad7,5":
                case "iPad7,6": return "iPad (6th gen)";
                case "iPad7,11":
                case "iPad7,12": return "iPad (7th gen)";
                
                default: return ProductType;
            }
        }

        /// <summary>
        /// Validates the device info has required fields
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(UDID) 
                && !string.IsNullOrEmpty(ProductType) 
                && !string.IsNullOrEmpty(ProductVersion);
        }

        /// <summary>
        /// Returns a summary string of device info
        /// </summary>
        public override string ToString()
        {
            return $"{GetFriendlyProductName()} ({ProductType}) - iOS {ProductVersion} - {ActivationState}";
        }

        #endregion
    }
}
