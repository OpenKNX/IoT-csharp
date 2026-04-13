using Microsoft.Extensions.Logging;
using OpenKNX.IoT.Database;
using OpenKNX.IoT.Resources;
using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Text;

namespace OpenKNX.IoT.Helper
{
    internal class ResourceHelper
    {
        private ResourceContext _context;
        private ILogger? _logger;

        public ResourceHelper(ResourceContext context, ILoggerFactory? loggerFactory = null)
        {
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger<ResourceHelper>();

            _context = context;
        }

        public T? GetResourceEntry<T>(string path) where T : struct
        {
            lock (_context)
            {
                ResourceData? data = _context.Resources.FirstOrDefault(r => r.Id == path);
                if (data == null)
                {
                    _logger?.LogWarning("Resource entry: path={path} not found", path);
                    return null;
                }
                return ResourceHelper.Deserialize<T>(data.Data, data.ResourceType);
            }
        }

        public T? GetResourceEntryObject<T>(string path) where T : class
        {
            lock (_context)
            {
                ResourceData? data = _context.Resources.FirstOrDefault(r => r.Id == path);
                if (data == null)
                {
                    _logger?.LogWarning("Resource entry: path={path} not found", path);
                    return null;
                }
                return ResourceHelper.Deserialize<T>(data.Data, data.ResourceType);
            }
        }

        public void SaveResourceEntry(string path, object value)
        {
            _logger?.LogDebug($"Save entry: path={path} value={value}");
            lock (_context)
            {
                ResourceData? data = _context.Resources.FirstOrDefault(r => r.Id == path);
                if (data == null)
                {
                    _logger?.LogWarning("Resource entry with path {path} not found, but wanted to save", path);
                    return;
                }
                data.Data = ResourceHelper.Serialize(value, data.ResourceType);
                _context.Update(data);
                _context.SaveChanges();
            }
        }

        public void SaveEntryDefault(string path, object defaultValue, ResourceTypes type, int[]? eraseCodes = null, bool overwrite = false)
        {
            _logger?.LogDebug($"Save default entry: path={path} value={defaultValue} type={type} eraseCodes={string.Join(",", eraseCodes ?? [])} overwrite={overwrite}");
            lock (_context)
            {
                if (_context.Resources.Any(r => r.Id == path))
                {
                    if (overwrite)
                    {
                        _logger?.LogDebug("Resource entry with path {id} already exists, but overwrite is true, overwriting default value", path);
                        ResourceData data = _context.Resources.First(r => r.Id == path);
                        data.Data = ResourceHelper.Serialize(defaultValue, type);
                        data.Default = data.Data;
                        data.ResourceType = type;
                        data.EraseCodes = eraseCodes;
                        _context.Update(data);
                        _context.SaveChanges();
                        return;
                    }
                    _logger?.LogDebug("Resource entry with path {id} already exists, skipping default value", path);
                    return;
                }
                else
                {
                    ResourceData data = new ResourceData();
                    data.Id = path;
                    data.Data = ResourceHelper.Serialize(defaultValue, type);
                    data.Default = data.Data;
                    data.ResourceType = type;
                    data.EraseCodes = eraseCodes;
                    _context.Resources.Add(data);
                    _context.SaveChanges();
                }
            }
        }

        private static byte[] Serialize(object value, ResourceTypes type)
        {
            if(value == null)
            {
                return [];
            }
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.String:
                    {
                        if (type == ResourceTypes.String || type == ResourceTypes.ByteString)
                        {
                            return Encoding.UTF8.GetBytes((string)value);
                        }
                        else
                        {
                            throw new Exception("Ungültiger ResourceType für String.");
                        }
                    }

                case TypeCode.Int32:
                    {
                        return BitConverter.GetBytes((int)value);
                    }
                case TypeCode.Int64:
                    {
                        return BitConverter.GetBytes((long)value);
                    }

                case TypeCode.UInt32:
                    {
                        return BitConverter.GetBytes((uint)value);
                    }

                case TypeCode.UInt64:
                    {
                        return BitConverter.GetBytes((ulong)value);
                    }

                case TypeCode.Object:
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(value);
                        return Encoding.UTF8.GetBytes(json);
                    }

                default:
                    throw new NotImplementedException($"Serializing for type '{value.GetType()}' is not implemented");
            }
        }

        private static T Deserialize<T>(byte[] data, ResourceTypes type)
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.String:
                    {
                        if (type == ResourceTypes.String)
                        {
                            return (T)(object)Encoding.UTF8.GetString(data);
                        }
                        else
                        {
                            return (T)(object)Convert.ToHexString(data);
                        }
                        throw new Exception("Invalid ResourceType for String.");
                    }

                case TypeCode.Int32:
                    {
                        if (data.Length != 4)
                            throw new Exception("Invalid data length for Int32.");
                        return (T)(object)BitConverter.ToInt32(data, 0);
                    }

                case TypeCode.Int64:
                    {
                        if (data.Length != 8)
                            throw new Exception("Invalid data length for Int32.");
                        return (T)(object)BitConverter.ToInt64(data, 0);
                    }

                case TypeCode.UInt32:
                    {
                        if (data.Length != 4)
                            throw new Exception("Invalid data length for UInt32.");
                        return (T)(object)BitConverter.ToUInt32(data, 0);
                    }

                case TypeCode.UInt64:
                    {
                        if (data.Length != 8)
                            throw new Exception("Invalid data length for UInt32.");
                        return (T)(object)BitConverter.ToUInt64(data, 0);
                    }

                case TypeCode.Object:
                    {
                        if(typeof(T).Name.Contains("Nullable"))
                        {
                            Type subtype = typeof(T).GenericTypeArguments[0];
                            return (T?)Deserialize<T?>(data, type);
                        }
                        string json = Encoding.UTF8.GetString(data);
                        return System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
                    }

                default:
                    throw new NotImplementedException($"Deserializing for type '{type}' is not implemented");
            }
        }
    }
}
