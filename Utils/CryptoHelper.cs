using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BypassTool.Utils
{
    /// <summary>
    /// Cryptographic helper for generating fake activation data
    /// </summary>
    public class CryptoHelper : IDisposable
    {
        #region Fields

        private readonly Logger _logger = Logger.Instance;
        private readonly RandomNumberGenerator _rng;
        private RSA _rsa;
        private bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new CryptoHelper instance
        /// </summary>
        public CryptoHelper()
        {
            _rng = RandomNumberGenerator.Create();
            _rsa = RSA.Create(2048);
        }

        #endregion

        #region Random Data Generation

        /// <summary>
        /// Generates random bytes
        /// </summary>
        public byte[] GenerateRandomBytes(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be positive", nameof(length));

            var buffer = new byte[length];
            _rng.GetBytes(buffer);
            return buffer;
        }

        /// <summary>
        /// Generates a random hex string
        /// </summary>
        public string GenerateRandomHex(int byteLength)
        {
            var bytes = GenerateRandomBytes(byteLength);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Generates a fake UDID (40 hex characters)
        /// </summary>
        public string GenerateFakeUDID()
        {
            return GenerateRandomHex(20);
        }

        /// <summary>
        /// Generates a fake ECID
        /// </summary>
        public string GenerateFakeECID()
        {
            return GenerateRandomHex(8);
        }

        /// <summary>
        /// Generates a fake serial number
        /// </summary>
        public string GenerateFakeSerialNumber()
        {
            // Apple serial format: XXXXXYYYYY (12 characters)
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";
            var buffer = GenerateRandomBytes(12);
            var sb = new StringBuilder(12);
            
            for (int i = 0; i < 12; i++)
            {
                sb.Append(chars[buffer[i] % chars.Length]);
            }
            
            return sb.ToString();
        }

        #endregion

        #region Hashing

        /// <summary>
        /// Computes SHA-1 hash
        /// </summary>
        public byte[] ComputeSHA1(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(data);
            }
        }

        /// <summary>
        /// Computes SHA-1 hash and returns hex string
        /// </summary>
        public string ComputeSHA1Hex(byte[] data)
        {
            var hash = ComputeSHA1(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Computes SHA-256 hash
        /// </summary>
        public byte[] ComputeSHA256(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        /// <summary>
        /// Computes SHA-256 hash and returns hex string
        /// </summary>
        public string ComputeSHA256Hex(byte[] data)
        {
            var hash = ComputeSHA256(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Computes SHA-256 hash of a string
        /// </summary>
        public byte[] ComputeSHA256(string data)
        {
            return ComputeSHA256(Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Computes MD5 hash
        /// </summary>
        public byte[] ComputeMD5(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }

        #endregion

        #region Signature Generation

        /// <summary>
        /// Generates a fake RSA signature
        /// </summary>
        public byte[] GenerateFakeSignature(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            try
            {
                // Sign with RSA-SHA256
                return _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate signature", ex);
                // Return random bytes as fallback
                return GenerateRandomBytes(256);
            }
        }

        /// <summary>
        /// Generates a fake signature for device activation
        /// </summary>
        public byte[] GenerateActivationSignature(string udid, string ecid, string serialNumber)
        {
            if (string.IsNullOrEmpty(udid))
                throw new ArgumentNullException(nameof(udid));

            // Concatenate device identifiers
            string dataToSign = $"{udid}:{ecid ?? ""}:{serialNumber ?? ""}";
            var data = Encoding.UTF8.GetBytes(dataToSign);
            
            return GenerateFakeSignature(data);
        }

        /// <summary>
        /// Generates a fake device certificate
        /// </summary>
        public byte[] GenerateFakeDeviceCertificate(string udid, string productType)
        {
            _logger.Debug("Generating fake device certificate...");

            // Create a simple certificate-like structure
            // This is a placeholder structure, not a real X.509 cert
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                // Header
                writer.Write((byte)0x30); // SEQUENCE
                writer.Write((byte)0x82); // Length prefix
                
                // Certificate body
                var certBody = new StringBuilder();
                certBody.AppendLine($"-----BEGIN CERTIFICATE-----");
                
                // Generate random base64 content
                var randomData = GenerateRandomBytes(512);
                var base64 = Convert.ToBase64String(randomData);
                
                // Split into 64-char lines
                for (int i = 0; i < base64.Length; i += 64)
                {
                    int len = Math.Min(64, base64.Length - i);
                    certBody.AppendLine(base64.Substring(i, len));
                }
                
                certBody.AppendLine($"-----END CERTIFICATE-----");
                
                return Encoding.ASCII.GetBytes(certBody.ToString());
            }
        }

        #endregion

        #region Key Generation

        /// <summary>
        /// Generates fake FairPlay key data
        /// </summary>
        public byte[] GenerateFakeFairPlayData()
        {
            _logger.Debug("Generating fake FairPlay data...");

            // FairPlay data structure (simplified)
            var fpData = new byte[128];
            _rng.GetBytes(fpData);

            // Add magic header
            fpData[0] = 0x46; // 'F'
            fpData[1] = 0x50; // 'P'
            fpData[2] = 0x01; // Version
            fpData[3] = 0x00;

            return fpData;
        }

        /// <summary>
        /// Generates fake account token data
        /// </summary>
        public byte[] GenerateFakeAccountToken(string udid)
        {
            _logger.Debug("Generating fake account token...");

            // Create token structure
            var tokenData = new StringBuilder();
            tokenData.Append("ActToken:");
            tokenData.Append(udid ?? "unknown");
            tokenData.Append(":");
            tokenData.Append(DateTime.UtcNow.Ticks);
            
            var data = Encoding.UTF8.GetBytes(tokenData.ToString());
            
            // Hash it
            var hash = ComputeSHA256(data);
            
            // Combine
            var result = new byte[data.Length + hash.Length];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);
            Buffer.BlockCopy(hash, 0, result, data.Length, hash.Length);
            
            return result;
        }

        /// <summary>
        /// Gets the RSA public key in DER format
        /// </summary>
        public byte[] GetPublicKeyDER()
        {
            return _rsa.ExportSubjectPublicKeyInfo();
        }

        /// <summary>
        /// Gets the RSA public key in PEM format
        /// </summary>
        public string GetPublicKeyPEM()
        {
            var der = _rsa.ExportSubjectPublicKeyInfo();
            var base64 = Convert.ToBase64String(der);
            
            var sb = new StringBuilder();
            sb.AppendLine("-----BEGIN PUBLIC KEY-----");
            
            for (int i = 0; i < base64.Length; i += 64)
            {
                int len = Math.Min(64, base64.Length - i);
                sb.AppendLine(base64.Substring(i, len));
            }
            
            sb.AppendLine("-----END PUBLIC KEY-----");
            return sb.ToString();
        }

        #endregion

        #region Encryption

        /// <summary>
        /// Encrypts data using AES
        /// </summary>
        public byte[] EncryptAES(byte[] data, byte[] key, byte[] iv)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));
            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes (256 bits)", nameof(key));
            if (iv == null || iv.Length != 16)
                throw new ArgumentException("IV must be 16 bytes", nameof(iv));

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                    }
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Decrypts data using AES
        /// </summary>
        public byte[] DecryptAES(byte[] encryptedData, byte[] key, byte[] iv)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));
            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes (256 bits)", nameof(key));
            if (iv == null || iv.Length != 16)
                throw new ArgumentException("IV must be 16 bytes", nameof(iv));

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedData, 0, encryptedData.Length);
                    }
                    return ms.ToArray();
                }
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Converts byte array to hex string
        /// </summary>
        public static string BytesToHex(byte[] bytes)
        {
            if (bytes == null)
                return null;
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Converts hex string to byte array
        /// </summary>
        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return null;

            hex = hex.Replace("-", "").Replace(" ", "");
            
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _rng?.Dispose();
            _rsa?.Dispose();
        }

        #endregion
    }
}
