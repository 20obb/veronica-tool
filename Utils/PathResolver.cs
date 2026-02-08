using System;
using System.Collections.Generic;
using BypassTool.Models;

namespace BypassTool.Utils
{
    /// <summary>
    /// Resolves iOS file system paths based on iOS version
    /// </summary>
    public class PathResolver
    {
        #region Fields

        private readonly Logger _logger = Logger.Instance;
        private IOSVersion _iosVersion;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new PathResolver with default iOS version
        /// </summary>
        public PathResolver()
        {
            _iosVersion = IOSVersion.IOS_15;
        }

        /// <summary>
        /// Creates a new PathResolver for specific iOS version
        /// </summary>
        public PathResolver(string iosVersionString)
        {
            if (IOSVersion.TryParse(iosVersionString, out var version))
            {
                _iosVersion = version;
            }
            else
            {
                _logger.Warning($"Could not parse iOS version: {iosVersionString}, defaulting to iOS 15");
                _iosVersion = IOSVersion.IOS_15;
            }
        }

        /// <summary>
        /// Creates a new PathResolver for specific iOS version
        /// </summary>
        public PathResolver(IOSVersion version)
        {
            _iosVersion = version ?? IOSVersion.IOS_15;
        }

        #endregion

        #region Path Constants

        // Root paths
        private const string VAR_ROOT = "/var/root";
        private const string VAR_MOBILE = "/var/mobile";
        private const string VAR_CONTAINERS = "/var/containers";
        private const string PRIVATE_VAR = "/private/var";

        // Lockdown paths
        private const string LOCKDOWN_DIR = "/var/root/Library/Lockdown";
        private const string LOCKDOWN_ACTIVATION = "/var/root/Library/Lockdown/activation_record.plist";
        private const string LOCKDOWN_DATA_ARK = "/var/root/Library/Lockdown/data_ark.plist";

        // New activation path (iOS 15+)
        private const string NEW_ACTIVATION_DIR = "/var/containers/Data/System/com.apple.mobileactivationd/Library";
        private const string NEW_ACTIVATION_RECORD = "/var/containers/Data/System/com.apple.mobileactivationd/Library/activation_record.plist";

        // Setup Assistant paths
        private const string PURPLEBUDDY_OLD = "/var/mobile/Library/Preferences/com.apple.purplebuddy.plist";
        private const string PURPLEBUDDY_NEW = "/var/containers/Shared/SystemGroup/systemgroup.com.apple.configurationprofiles/Library/ConfigurationProfiles/PublicInfo/com.apple.purplebuddy.plist";

        // FairPlay paths
        private const string FAIRPLAY_DIR = "/var/mobile/Library/FairPlay";
        private const string ITUNES_CONTROL = "/var/mobile/Library/FairPlay/iTunes_Control";
        
        // Preferences
        private const string MOBILE_PREFS = "/var/mobile/Library/Preferences";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the iOS version for path resolution
        /// </summary>
        public IOSVersion IOSVersion
        {
            get => _iosVersion;
            set => _iosVersion = value ?? IOSVersion.IOS_15;
        }

        #endregion

        #region Path Resolution Methods

        /// <summary>
        /// Gets the path to the activation record
        /// </summary>
        public string GetActivationRecordPath()
        {
            if (_iosVersion >= IOSVersion.IOS_15)
            {
                return NEW_ACTIVATION_RECORD;
            }
            return LOCKDOWN_ACTIVATION;
        }

        /// <summary>
        /// Gets the path to the activation directory
        /// </summary>
        public string GetActivationDirectory()
        {
            if (_iosVersion >= IOSVersion.IOS_15)
            {
                return NEW_ACTIVATION_DIR;
            }
            return LOCKDOWN_DIR;
        }

        /// <summary>
        /// Gets the path to data_ark.plist
        /// </summary>
        public string GetDataArkPath()
        {
            return LOCKDOWN_DATA_ARK;
        }

        /// <summary>
        /// Gets the path to com.apple.purplebuddy.plist
        /// </summary>
        public string GetPurpleBuddyPath()
        {
            if (_iosVersion >= IOSVersion.IOS_14)
            {
                return PURPLEBUDDY_NEW;
            }
            return PURPLEBUDDY_OLD;
        }

        /// <summary>
        /// Gets the FairPlay directory
        /// </summary>
        public string GetFairPlayDirectory()
        {
            return FAIRPLAY_DIR;
        }

        /// <summary>
        /// Gets the iTunes Control directory
        /// </summary>
        public string GetITunesControlPath()
        {
            return ITUNES_CONTROL;
        }

        /// <summary>
        /// Gets the mobile preferences directory
        /// </summary>
        public string GetMobilePreferencesPath()
        {
            return MOBILE_PREFS;
        }

        /// <summary>
        /// Gets the lockdown directory
        /// </summary>
        public string GetLockdownDirectory()
        {
            return LOCKDOWN_DIR;
        }

        #endregion

        #region File Mapping

        /// <summary>
        /// Gets all files that need to be injected for activation bypass
        /// </summary>
        public Dictionary<string, string> GetActivationBypassFiles()
        {
            var files = new Dictionary<string, string>();

            // Activation record
            files["activation_record.plist"] = GetActivationRecordPath();

            // Data ark (iOS 15+)
            if (_iosVersion >= IOSVersion.IOS_15)
            {
                files["data_ark.plist"] = GetDataArkPath();
            }

            // Wildcard record (fallback)
            files["wildcard_record.plist"] = LOCKDOWN_DIR + "/wildcard_record.plist";

            return files;
        }

        /// <summary>
        /// Gets all files that need to be injected for setup bypass
        /// </summary>
        public Dictionary<string, string> GetSetupBypassFiles()
        {
            var files = new Dictionary<string, string>();

            // PurpleBuddy
            files["com.apple.purplebuddy.plist"] = GetPurpleBuddyPath();

            // Additional setup files
            files["com.apple.SetupAssistant.plist"] = MOBILE_PREFS + "/com.apple.SetupAssistant.plist";

            return files;
        }

        /// <summary>
        /// Gets all files that need to be injected for complete bypass
        /// </summary>
        public Dictionary<string, string> GetCompleteBypassFiles()
        {
            var files = new Dictionary<string, string>();

            // Add activation files
            foreach (var kvp in GetActivationBypassFiles())
            {
                files[kvp.Key] = kvp.Value;
            }

            // Add setup files
            foreach (var kvp in GetSetupBypassFiles())
            {
                if (!files.ContainsKey(kvp.Key))
                {
                    files[kvp.Key] = kvp.Value;
                }
            }

            return files;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Checks if a path is valid for the current iOS version
        /// </summary>
        public bool IsValidPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Must start with / (absolute path)
            if (!path.StartsWith("/"))
                return false;

            // Check for disallowed paths
            string[] disallowed = { "/bin/", "/sbin/", "/usr/bin/", "/usr/sbin/" };
            foreach (var prefix in disallowed)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Warning($"Path is in system binary directory: {path}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the parent directory of a path
        /// </summary>
        public string GetParentDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            int lastSlash = path.LastIndexOf('/');
            if (lastSlash <= 0)
                return "/";

            return path.Substring(0, lastSlash);
        }

        /// <summary>
        /// Gets the filename from a path
        /// </summary>
        public string GetFileName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            int lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0)
                return path;

            return path.Substring(lastSlash + 1);
        }

        /// <summary>
        /// Combines iOS paths (using forward slashes)
        /// </summary>
        public string CombinePaths(string basePath, string relativePath)
        {
            if (string.IsNullOrEmpty(basePath))
                return relativePath;
            if (string.IsNullOrEmpty(relativePath))
                return basePath;

            basePath = basePath.TrimEnd('/');
            relativePath = relativePath.TrimStart('/');

            return $"{basePath}/{relativePath}";
        }

        #endregion

        #region Debug

        /// <summary>
        /// Logs all resolved paths for debugging
        /// </summary>
        public void LogAllPaths()
        {
            _logger.Debug($"=== iOS {_iosVersion} Path Resolution ===");
            _logger.Debug($"Activation Record: {GetActivationRecordPath()}");
            _logger.Debug($"Activation Dir: {GetActivationDirectory()}");
            _logger.Debug($"Data Ark: {GetDataArkPath()}");
            _logger.Debug($"PurpleBuddy: {GetPurpleBuddyPath()}");
            _logger.Debug($"FairPlay Dir: {GetFairPlayDirectory()}");
            _logger.Debug($"Lockdown Dir: {GetLockdownDirectory()}");
        }

        #endregion
    }
}
