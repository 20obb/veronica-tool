using System;
using System.Collections.Generic;
using Claunia.PropertyList;
using BypassTool.Models;
using BypassTool.Utils;

namespace BypassTool.Core
{
    /// <summary>
    /// Generates plist files to bypass iOS Setup Assistant screens
    /// </summary>
    public class SetupAssistantModifier
    {
        #region Fields

        private readonly Logger _logger = Logger.Instance;
        private readonly PathResolver _pathResolver;

        /// <summary>
        /// Complete list of setup items to skip
        /// </summary>
        private static readonly string[] SkipSetupItems = new[]
        {
            "WiFi",
            "Location",
            "Restore",
            "RestoreBackup",
            "Android",
            "AppleID",
            "TOS",
            "Siri",
            "SiriEducationalScreen",
            "Diagnostics",
            "DiagnosticsUpload",
            "ScreenTime",
            "UpdateCompleted",
            "SoftwareUpdate",
            "AutomaticSoftwareUpdate",
            "AppStore",
            "Passcode",
            "Biometric",
            "TouchID",
            "FaceID",
            "Payment",
            "Zoom",
            "DisplayTone",
            "TrueTone",
            "MessagingActivationUsingPhoneNumber",
            "HomeButtonSensitivity",
            "CloudStorage",
            "ScreenSaver",
            "TapToSetup",
            "Keyboard",
            "PreferredLanguage",
            "SpokenLanguage",
            "VoiceOver",
            "WatchMigration",
            "OnBoarding",
            "TVProviderSignIn",
            "TVHomeScreenSync",
            "Privacy",
            "PrivacyBlurb",
            "TVRoom",
            "iMessageAndFaceTime",
            "ExpressLanguage",
            "Welcome",
            "Appearance",
            "SIMSetup",
            "DeviceToDeviceMigration",
            "UnlockWithWatch",
            "IntendedUser",
            "TermsOfAddress",
            "EmergencySOS",
            "Safety",
            "ActionButton",
            "MusicHaptics",
            "AccessibilityAppearance",
            "IntelligenceFlow"
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new SetupAssistantModifier instance
        /// </summary>
        public SetupAssistantModifier()
        {
            _logger.Debug("Initializing SetupAssistantModifier...");
            _pathResolver = new PathResolver();
        }

        /// <summary>
        /// Creates a new SetupAssistantModifier for specific iOS version
        /// </summary>
        public SetupAssistantModifier(IOSVersion version) : this()
        {
            if (version != null)
            {
                _pathResolver.IOSVersion = version;
            }
        }

        #endregion

        #region Setup Bypass Generation

        /// <summary>
        /// Generates com.apple.purplebuddy.plist to skip Setup Assistant
        /// </summary>
        public byte[] GeneratePurpleBuddyPlist()
        {
            _logger.Info("Generating com.apple.purplebuddy.plist...");

            try
            {
                var dict = new NSDictionary();

                // Main completion flags
                dict.Add("SetupDone", new NSNumber(true));
                dict.Add("SetupFinished", new NSNumber(true));
                dict.Add("SetupRan", new NSNumber(true));
                dict.Add("SetupFinishedAllSteps", new NSNumber(true));

                // Skip items array
                var skipArray = new NSArray();
                foreach (var item in SkipSetupItems)
                {
                    skipArray.Add(new NSString(item));
                }
                dict.Add("SkipSetupItems", skipArray);

                // Additional flags
                dict.Add("UserHasAttemptedRestore", new NSNumber(true));
                dict.Add("RestoreCompleted", new NSNumber(true));
                dict.Add("AppleIDDone", new NSNumber(true));
                dict.Add("WiFiDone", new NSNumber(true));
                dict.Add("DiagnosticsDone", new NSNumber(true));
                dict.Add("LocationServicesDone", new NSNumber(true));
                dict.Add("SiriDone", new NSNumber(true));
                dict.Add("TouchIDDone", new NSNumber(true));
                dict.Add("BiometricDone", new NSNumber(true));

                // Cloud and backup
                dict.Add("iCloudBackupsAvailable", new NSNumber(false));
                dict.Add("iTunesBackupsAvailable", new NSNumber(false));
                dict.Add("CloudDocumentsEnabled", new NSNumber(false));

                // Version tracking
                dict.Add("SetupVersion", new NSNumber(3));
                dict.Add("SetupLastPhase", new NSString("Complete"));

                byte[] plistData = PlistHelper.ToBinary(dict);

                _logger.Debug($"Generated purplebuddy.plist: {plistData.Length} bytes");
                return plistData;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate purplebuddy.plist", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates com.apple.SetupAssistant.plist
        /// </summary>
        public byte[] GenerateSetupAssistantPlist()
        {
            _logger.Info("Generating com.apple.SetupAssistant.plist...");

            try
            {
                var dict = new NSDictionary();

                // Completion flags
                dict.Add("DidSeeAccessibility", new NSNumber(true));
                dict.Add("DidSeeAppearanceSetup", new NSNumber(true));
                dict.Add("DidSeeApplePaySetup", new NSNumber(true));
                dict.Add("DidSeeBuddyRestore", new NSNumber(true));
                dict.Add("DidSeeCloudSetup", new NSNumber(true));
                dict.Add("DidSeeGetStarted", new NSNumber(true));
                dict.Add("DidSeeHomeButtonHint", new NSNumber(true));
                dict.Add("DidSeeLocationServicesAlert", new NSNumber(true));
                dict.Add("DidSeeHomeButtonSensitivity", new NSNumber(true));
                dict.Add("DidSeeiCloudDriveSetup", new NSNumber(true));
                dict.Add("DidSeeiCloudLoginForStoreClearedAccount", new NSNumber(true));
                dict.Add("DidSeeiCloudSecuritySetup", new NSNumber(true));
                dict.Add("DidSeePasscodePane", new NSNumber(true));
                dict.Add("DidSeePrivacy", new NSNumber(true));
                dict.Add("DidSeeScreenTimePrompt", new NSNumber(true));
                dict.Add("DidSeeSiriSetup", new NSNumber(true));
                dict.Add("DidSeeSoftwareUpdatePage", new NSNumber(true));
                dict.Add("DidSeeSplash", new NSNumber(true));
                dict.Add("DidSeeTelephonyAlert", new NSNumber(true));
                dict.Add("DidSeeTouchIDSetup", new NSNumber(true));
                dict.Add("DidSeeTrueTonePane", new NSNumber(true));
                dict.Add("DidSeeZoomTutorial", new NSNumber(true));
                dict.Add("DidSeeActivationLock", new NSNumber(true));
                dict.Add("DidSeeOnBoarding", new NSNumber(true));

                // State
                dict.Add("LastBuddyID", new NSString("com.apple.Setup"));
                dict.Add("LastPreBuddyBuildVersion", new NSString(""));
                dict.Add("SetupShortVersion", new NSString("1.0"));

                byte[] plistData = PlistHelper.ToBinary(dict);

                _logger.Debug($"Generated SetupAssistant.plist: {plistData.Length} bytes");
                return plistData;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate SetupAssistant.plist", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates com.apple.springboard.plist with setup complete flags
        /// </summary>
        public byte[] GenerateSpringBoardPlist()
        {
            _logger.Info("Generating com.apple.springboard.plist...");

            try
            {
                var dict = new NSDictionary();

                // Setup complete indicators
                dict.Add("SBSetupDone", new NSNumber(true));
                dict.Add("SBDidShowSetup", new NSNumber(true));
                dict.Add("SBFirstBootWithActivation", new NSNumber(true));

                byte[] plistData = PlistHelper.ToBinary(dict);

                _logger.Debug($"Generated springboard.plist: {plistData.Length} bytes");
                return plistData;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate springboard.plist", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates all setup bypass files
        /// </summary>
        /// <returns>Dictionary of filename -> data</returns>
        public Dictionary<string, byte[]> GenerateAllSetupBypassFiles()
        {
            _logger.Info("Generating all setup bypass files...");

            var files = new Dictionary<string, byte[]>();

            try
            {
                // Main purplebuddy plist
                files["com.apple.purplebuddy.plist"] = GeneratePurpleBuddyPlist();

                // SetupAssistant plist
                files["com.apple.SetupAssistant.plist"] = GenerateSetupAssistantPlist();

                // SpringBoard plist
                files["com.apple.springboard.plist"] = GenerateSpringBoardPlist();

                _logger.Info($"Generated {files.Count} setup bypass files");
                return files;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate setup bypass files", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the installation paths for setup bypass files
        /// </summary>
        /// <returns>Dictionary of filename -> remote path</returns>
        public Dictionary<string, string> GetSetupBypassPaths()
        {
            var paths = new Dictionary<string, string>();

            string purpleBuddyPath = _pathResolver.GetPurpleBuddyPath();
            string prefsPath = _pathResolver.GetMobilePreferencesPath();

            paths["com.apple.purplebuddy.plist"] = purpleBuddyPath;
            paths["com.apple.SetupAssistant.plist"] = $"{prefsPath}/com.apple.SetupAssistant.plist";
            paths["com.apple.springboard.plist"] = $"{prefsPath}/com.apple.springboard.plist";

            return paths;
        }

        /// <summary>
        /// Generates setup bypass files with resolved paths
        /// </summary>
        /// <returns>Dictionary of full path -> data</returns>
        public Dictionary<string, byte[]> GenerateSetupBypassFilesWithPaths()
        {
            var files = GenerateAllSetupBypassFiles();
            var paths = GetSetupBypassPaths();
            var result = new Dictionary<string, byte[]>();

            foreach (var kvp in files)
            {
                if (paths.TryGetValue(kvp.Key, out string path))
                {
                    result[path] = kvp.Value;
                }
            }

            return result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of setup items that will be skipped
        /// </summary>
        public static IReadOnlyList<string> SkipItems => SkipSetupItems;

        #endregion
    }
}
