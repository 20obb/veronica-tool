using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Claunia.PropertyList;

namespace BypassTool.Utils
{
    /// <summary>
    /// Helper class for creating and manipulating Apple Property List (plist) files
    /// </summary>
    public static class PlistHelper
    {
        private static readonly Logger _logger = Logger.Instance;

        #region Creation Methods

        /// <summary>
        /// Creates a new empty plist dictionary
        /// </summary>
        public static NSDictionary CreateDictionary()
        {
            return new NSDictionary();
        }

        /// <summary>
        /// Creates a plist dictionary from a C# dictionary
        /// </summary>
        public static NSDictionary CreateDictionary(Dictionary<string, object> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var dict = new NSDictionary();
            foreach (var kvp in values)
            {
                dict.Add(kvp.Key, ConvertToNSObject(kvp.Value));
            }
            return dict;
        }

        /// <summary>
        /// Creates a plist array
        /// </summary>
        public static NSArray CreateArray(IEnumerable<object> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var array = new NSArray();
            foreach (var item in items)
            {
                array.Add(ConvertToNSObject(item));
            }
            return array;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serializes a plist object to binary format
        /// </summary>
        public static byte[] ToBinary(NSObject plist)
        {
            if (plist == null)
                throw new ArgumentNullException(nameof(plist));

            try
            {
                using (var ms = new MemoryStream())
                {
                    BinaryPropertyListWriter.Write(ms, plist);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to serialize plist to binary", ex);
                throw;
            }
        }

        /// <summary>
        /// Serializes a plist object to XML format
        /// </summary>
        public static string ToXml(NSObject plist)
        {
            if (plist == null)
                throw new ArgumentNullException(nameof(plist));

            try
            {
                using (var ms = new MemoryStream())
                {
                    PropertyListParser.SaveAsXml(plist, ms);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to serialize plist to XML", ex);
                throw;
            }
        }

        /// <summary>
        /// Serializes a plist object to XML bytes
        /// </summary>
        public static byte[] ToXmlBytes(NSObject plist)
        {
            if (plist == null)
                throw new ArgumentNullException(nameof(plist));

            try
            {
                using (var ms = new MemoryStream())
                {
                    PropertyListParser.SaveAsXml(plist, ms);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to serialize plist to XML bytes", ex);
                throw;
            }
        }

        /// <summary>
        /// Saves a plist to file in binary format
        /// </summary>
        public static void SaveAsBinary(NSObject plist, string filePath)
        {
            if (plist == null)
                throw new ArgumentNullException(nameof(plist));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    BinaryPropertyListWriter.Write(fs, plist);
                }
                
                _logger.Debug($"Saved binary plist to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save binary plist to {filePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// Saves a plist to file in XML format
        /// </summary>
        public static void SaveAsXml(NSObject plist, string filePath)
        {
            if (plist == null)
                throw new ArgumentNullException(nameof(plist));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    PropertyListParser.SaveAsXml(plist, fs);
                }
                
                _logger.Debug($"Saved XML plist to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save XML plist to {filePath}", ex);
                throw;
            }
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Parses a plist from bytes
        /// </summary>
        public static NSObject Parse(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            try
            {
                using (var ms = new MemoryStream(data))
                {
                    return PropertyListParser.Parse(ms);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to parse plist from bytes", ex);
                throw;
            }
        }

        /// <summary>
        /// Parses a plist from a file
        /// </summary>
        public static NSObject ParseFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Plist file not found", filePath);

            try
            {
                return PropertyListParser.Parse(filePath);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to parse plist from {filePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// Parses a plist as dictionary
        /// </summary>
        public static NSDictionary ParseAsDictionary(byte[] data)
        {
            var obj = Parse(data);
            if (obj is NSDictionary dict)
                return dict;
            
            throw new InvalidDataException("Plist is not a dictionary");
        }

        #endregion

        #region Dictionary Operations

        /// <summary>
        /// Gets a string value from a plist dictionary
        /// </summary>
        public static string GetString(NSDictionary dict, string key, string defaultValue = null)
        {
            if (dict == null) return defaultValue;
            
            if (dict.TryGetValue(key, out NSObject value))
            {
                if (value is NSString nsStr)
                    return nsStr.Content;
                return value?.ToString() ?? defaultValue;
            }
            
            return defaultValue;
        }

        /// <summary>
        /// Gets a boolean value from a plist dictionary
        /// </summary>
        public static bool GetBool(NSDictionary dict, string key, bool defaultValue = false)
        {
            if (dict == null) return defaultValue;
            
            if (dict.TryGetValue(key, out NSObject value))
            {
                if (value is NSNumber nsNum)
                    return nsNum.ToBool();
            }
            
            return defaultValue;
        }

        /// <summary>
        /// Gets an integer value from a plist dictionary
        /// </summary>
        public static int GetInt(NSDictionary dict, string key, int defaultValue = 0)
        {
            if (dict == null) return defaultValue;
            
            if (dict.TryGetValue(key, out NSObject value))
            {
                if (value is NSNumber nsNum)
                    return nsNum.ToInt();
            }
            
            return defaultValue;
        }

        /// <summary>
        /// Gets a byte array from a plist dictionary
        /// </summary>
        public static byte[] GetData(NSDictionary dict, string key)
        {
            if (dict == null) return null;
            
            if (dict.TryGetValue(key, out NSObject value))
            {
                if (value is NSData nsData)
                    return nsData.Bytes;
            }
            
            return null;
        }

        /// <summary>
        /// Gets a sub-dictionary from a plist dictionary
        /// </summary>
        public static NSDictionary GetDictionary(NSDictionary dict, string key)
        {
            if (dict == null) return null;
            
            if (dict.TryGetValue(key, out NSObject value))
            {
                if (value is NSDictionary subDict)
                    return subDict;
            }
            
            return null;
        }

        /// <summary>
        /// Sets a value in a plist dictionary
        /// </summary>
        public static void SetValue(NSDictionary dict, string key, object value)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (dict.ContainsKey(key))
                dict.Remove(key);
            
            dict.Add(key, ConvertToNSObject(value));
        }

        #endregion

        #region Conversion Helpers

        /// <summary>
        /// Converts a C# object to an NSObject
        /// </summary>
        private static NSObject ConvertToNSObject(object value)
        {
            if (value == null)
                return new NSString("");

            return value switch
            {
                NSObject ns => ns,
                string s => new NSString(s),
                bool b => new NSNumber(b),
                int i => new NSNumber(i),
                long l => new NSNumber(l),
                double d => new NSNumber(d),
                float f => new NSNumber(f),
                byte[] data => new NSData(data),
                DateTime dt => new NSDate(dt),
                Dictionary<string, object> dict => CreateDictionary(dict),
                IEnumerable<object> list => CreateArray(list),
                _ => new NSString(value.ToString())
            };
        }

        /// <summary>
        /// Converts an NSObject to a C# object
        /// </summary>
        public static object ConvertFromNSObject(NSObject value)
        {
            if (value == null)
                return null;

            return value switch
            {
                NSString s => s.Content,
                NSNumber n => n.isBoolean() ? n.ToBool() : (n.isInteger() ? n.ToLong() : n.ToDouble()),
                NSData d => d.Bytes,
                NSDate dt => dt.Date,
                NSArray arr => arr.Select(ConvertFromNSObject).ToList(),
                NSDictionary dict => dict.ToDictionary(kvp => kvp.Key, kvp => ConvertFromNSObject(kvp.Value)),
                _ => value.ToString()
            };
        }

        #endregion
    }
}
