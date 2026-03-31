using OpenKNX.IoT.Database;
using OpenKNX.IoT.Models;
using OpenKNX.IoT.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Helper
{
    internal class ResourceHelper
    {
        internal static T GetResourceEntry<T>(ResourceContext db, string path)
        {
            ResourceData? data = db.Resources.FirstOrDefault(r => r.Id == path);
            if (data == null)
                throw new Exception($"Resource entry not found for path: {path}");
            return Deserialize<T>(data.Data, data.ResourceType);
        }

        internal static byte[] Serialize(object value, ResourceTypes type)
        {
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

        internal static T Deserialize<T>(byte[] data, ResourceTypes type)
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
                        string json = Encoding.UTF8.GetString(data);
                        return System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
                    }

                default:
                    throw new NotImplementedException($"Deserializing for type '{type}' is not implemented");
            }
        }

    }
}
