using System;
using System.Text.RegularExpressions;

namespace BypassTool.Models
{
    /// <summary>
    /// Helper class for iOS version parsing and comparison
    /// </summary>
    public class IOSVersion : IComparable<IOSVersion>
    {
        #region Properties

        /// <summary>
        /// Major version (e.g., 16)
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Minor version (e.g., 3)
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Patch version (e.g., 1)
        /// </summary>
        public int Patch { get; }

        /// <summary>
        /// Build version (e.g., "20D67")
        /// </summary>
        public string Build { get; }

        /// <summary>
        /// Original version string
        /// </summary>
        public string VersionString { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new iOS version from components
        /// </summary>
        public IOSVersion(int major, int minor = 0, int patch = 0, string build = null)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build ?? "";
            VersionString = patch > 0 ? $"{major}.{minor}.{patch}" : $"{major}.{minor}";
        }

        /// <summary>
        /// Parses an iOS version string
        /// </summary>
        public static IOSVersion Parse(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                throw new ArgumentException("Version string cannot be null or empty", nameof(versionString));

            // Match patterns like "16.3.1", "16.3", "16"
            var match = Regex.Match(versionString.Trim(), @"^(\d+)(?:\.(\d+))?(?:\.(\d+))?");

            if (!match.Success)
                throw new FormatException($"Invalid iOS version format: {versionString}");

            int major = int.Parse(match.Groups[1].Value);
            int minor = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            int patch = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

            return new IOSVersion(major, minor, patch, null);
        }

        /// <summary>
        /// Tries to parse an iOS version string
        /// </summary>
        public static bool TryParse(string versionString, out IOSVersion version)
        {
            version = null;
            try
            {
                version = Parse(versionString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Comparison Methods

        public int CompareTo(IOSVersion other)
        {
            if (other == null) return 1;

            int majorCompare = Major.CompareTo(other.Major);
            if (majorCompare != 0) return majorCompare;

            int minorCompare = Minor.CompareTo(other.Minor);
            if (minorCompare != 0) return minorCompare;

            return Patch.CompareTo(other.Patch);
        }

        public static bool operator >(IOSVersion left, IOSVersion right)
            => left?.CompareTo(right) > 0;

        public static bool operator <(IOSVersion left, IOSVersion right)
            => left?.CompareTo(right) < 0;

        public static bool operator >=(IOSVersion left, IOSVersion right)
            => left?.CompareTo(right) >= 0;

        public static bool operator <=(IOSVersion left, IOSVersion right)
            => left?.CompareTo(right) <= 0;

        public static bool operator ==(IOSVersion left, IOSVersion right)
            => left?.CompareTo(right) == 0 || (left is null && right is null);

        public static bool operator !=(IOSVersion left, IOSVersion right)
            => !(left == right);

        public override bool Equals(object obj)
        {
            if (obj is IOSVersion other)
                return CompareTo(other) == 0;
            return false;
        }

        public override int GetHashCode()
        {
            return (Major * 10000) + (Minor * 100) + Patch;
        }

        #endregion

        #region Version Checks

        /// <summary>
        /// Checks if this version is in the given range (inclusive)
        /// </summary>
        public bool IsInRange(IOSVersion min, IOSVersion max)
        {
            return this >= min && this <= max;
        }

        /// <summary>
        /// Checks if this version supports checkm8 (iOS 12-16.x on A11 and earlier)
        /// </summary>
        public bool SupportsCheckm8Bypass => Major >= 12 && Major <= 16;

        /// <summary>
        /// Checks if this version requires data_ark.plist (iOS 15+)
        /// </summary>
        public bool RequiresDataArk => Major >= 15;

        /// <summary>
        /// Checks if this version requires new activation format (iOS 15+)
        /// </summary>
        public bool UsesNewActivationFormat => Major >= 15;

        /// <summary>
        /// Checks if this version has enhanced security (iOS 14+)
        /// </summary>
        public bool HasEnhancedSecurity => Major >= 14;

        /// <summary>
        /// Gets the activation record path for this iOS version
        /// </summary>
        public string ActivationRecordPath
        {
            get
            {
                if (Major >= 15)
                    return "/var/containers/Data/System/com.apple.mobileactivationd/Library/activation_record.plist";
                else
                    return "/var/root/Library/Lockdown/activation_record.plist";
            }
        }

        /// <summary>
        /// Gets the data ark path for this iOS version
        /// </summary>
        public string DataArkPath => "/var/root/Library/Lockdown/data_ark.plist";

        /// <summary>
        /// Gets the purplebuddy path for this iOS version
        /// </summary>
        public string PurpleBuddyPath
        {
            get
            {
                if (Major >= 14)
                    return "/var/containers/Shared/SystemGroup/systemgroup.com.apple.configurationprofiles/Library/ConfigurationProfiles/PublicInfo/com.apple.purplebuddy.plist";
                else
                    return "/var/mobile/Library/Preferences/com.apple.purplebuddy.plist";
            }
        }

        #endregion

        #region Static Versions

        /// <summary>iOS 12.0</summary>
        public static readonly IOSVersion IOS_12 = new IOSVersion(12, 0, 0);

        /// <summary>iOS 13.0</summary>
        public static readonly IOSVersion IOS_13 = new IOSVersion(13, 0, 0);

        /// <summary>iOS 14.0</summary>
        public static readonly IOSVersion IOS_14 = new IOSVersion(14, 0, 0);

        /// <summary>iOS 15.0</summary>
        public static readonly IOSVersion IOS_15 = new IOSVersion(15, 0, 0);

        /// <summary>iOS 16.0</summary>
        public static readonly IOSVersion IOS_16 = new IOSVersion(16, 0, 0);

        /// <summary>iOS 17.0</summary>
        public static readonly IOSVersion IOS_17 = new IOSVersion(17, 0, 0);

        #endregion

        public override string ToString() => VersionString;
    }
}
