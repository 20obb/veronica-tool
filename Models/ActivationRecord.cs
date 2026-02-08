using System;
using System.Collections.Generic;

namespace BypassTool.Models
{
    /// <summary>
    /// Represents an iOS activation record structure
    /// </summary>
    public class ActivationRecord
    {
        #region Core Fields

        /// <summary>
        /// Activation state value
        /// </summary>
        public string ActivationState { get; set; } = "Activated";

        /// <summary>
        /// Whether activation info is complete
        /// </summary>
        public bool ActivationInfoComplete { get; set; } = true;

        /// <summary>
        /// Activation state acknowledged by iTunes
        /// </summary>
        public string ActivationStateAcknowledged { get; set; } = "true";

        #endregion

        #region Device Identification

        /// <summary>
        /// Device UDID this record is for
        /// </summary>
        public string UniqueDeviceID { get; set; }

        /// <summary>
        /// Device serial number
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Device ECID (hex string)
        /// </summary>
        public string UniqueChipID { get; set; }

        /// <summary>
        /// Product type (e.g., iPhone10,3)
        /// </summary>
        public string ProductType { get; set; }

        /// <summary>
        /// Device class (iPhone, iPad)
        /// </summary>
        public string DeviceClass { get; set; }

        /// <summary>
        /// iOS build version
        /// </summary>
        public string BuildVersion { get; set; }

        /// <summary>
        /// iOS product version
        /// </summary>
        public string ProductVersion { get; set; }

        #endregion

        #region Cryptographic Data

        /// <summary>
        /// Device certificate request data
        /// </summary>
        public byte[] DeviceCertRequest { get; set; }

        /// <summary>
        /// FairPlay certificate data
        /// </summary>
        public byte[] FairPlayCertChain { get; set; }

        /// <summary>
        /// FairPlay key data for DRM bypass
        /// </summary>
        public byte[] FairPlayKeyData { get; set; }

        /// <summary>
        /// Account token data
        /// </summary>
        public byte[] AccountToken { get; set; }

        /// <summary>
        /// Account token certificate chain
        /// </summary>
        public byte[] AccountTokenCertChain { get; set; }

        /// <summary>
        /// Account token signature
        /// </summary>
        public byte[] AccountTokenSignature { get; set; }

        /// <summary>
        /// Activation info XML/plist data
        /// </summary>
        public byte[] ActivationInfoComplete_Data { get; set; }

        /// <summary>
        /// Activation signature
        /// </summary>
        public byte[] ActivationSignature { get; set; }

        #endregion

        #region Additional Fields

        /// <summary>
        /// iCloud account token (if applicable)
        /// </summary>
        public string iCloudAccountToken { get; set; }

        /// <summary>
        /// Brick state (should be false for bypass)
        /// </summary>
        public bool BrickState { get; set; } = false;

        /// <summary>
        /// Timestamp when record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Record version/format
        /// </summary>
        public int RecordVersion { get; set; } = 2;

        #endregion

        #region Methods

        /// <summary>
        /// Creates an activation record from device info
        /// </summary>
        public static ActivationRecord FromDevice(DeviceInfo device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            return new ActivationRecord
            {
                UniqueDeviceID = device.UDID,
                SerialNumber = device.SerialNumber,
                UniqueChipID = device.ECID,
                ProductType = device.ProductType,
                DeviceClass = device.DeviceClass,
                BuildVersion = device.BuildVersion,
                ProductVersion = device.ProductVersion,
                ActivationState = "Activated",
                ActivationInfoComplete = true,
                BrickState = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Validates the record has all required fields
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(UniqueDeviceID)
                && !string.IsNullOrEmpty(ProductType)
                && !string.IsNullOrEmpty(ActivationState);
        }

        /// <summary>
        /// Converts record to dictionary for plist generation
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                { "ActivationState", ActivationState },
                { "ActivationStateAcknowledged", ActivationStateAcknowledged },
                { "ActivationInfoComplete", ActivationInfoComplete },
                { "UniqueDeviceID", UniqueDeviceID ?? "" },
                { "SerialNumber", SerialNumber ?? "" },
                { "ProductType", ProductType ?? "" },
                { "DeviceClass", DeviceClass ?? "" },
                { "BuildVersion", BuildVersion ?? "" },
                { "BrickState", BrickState }
            };

            // Add binary data if present
            if (DeviceCertRequest != null && DeviceCertRequest.Length > 0)
                dict["DeviceCertRequest"] = DeviceCertRequest;

            if (FairPlayKeyData != null && FairPlayKeyData.Length > 0)
                dict["FairPlayKeyData"] = FairPlayKeyData;

            if (AccountTokenSignature != null && AccountTokenSignature.Length > 0)
                dict["AccountTokenSignature"] = AccountTokenSignature;

            return dict;
        }

        public override string ToString()
        {
            return $"ActivationRecord[{UniqueDeviceID?.Substring(0, 8) ?? "null"}...] - {ActivationState}";
        }

        #endregion
    }
}
