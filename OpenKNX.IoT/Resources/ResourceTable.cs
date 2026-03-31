using OpenKNX.IoT.Database;
using OpenKNX.IoT.Helper;
using OpenKNX.IoT.Models;
using Microsoft.Extensions.Logging;
using OpenKNX.CoAP;
using OpenKNX.CoAP.Enums;
using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace OpenKNX.IoT.Resources
{
    internal abstract class ResourceTable
    {
        public string Path { get; private set; }

        internal ILogger? _logger;
        private ResourceContext _db;

        public ResourceTable(ResourceContext db)
        {
            var attr = this.GetType().GetCustomAttribute<ResourceBaseAttribute>();
            if (attr == null)
                throw new Exception("ResourceTable needs ResourceBaseAttribute");
            Path = attr.Path;
            _db = db;
            InitTable();
        }

        public ResourceTable(ResourceContext db, ILoggerFactory? loggerFactory)
        {
            var attr = this.GetType().GetCustomAttribute<ResourceBaseAttribute>();
            if (attr == null)
                throw new Exception("ResourceTable needs ResourceBaseAttribute");
            Path = attr.Path;
            _db = db;
            _logger = loggerFactory?.CreateLogger($"ResourceTable[{Path}]");
            InitTable();
        }

        internal void SaveEntryDefault(string path, object defaultValue, ResourceTypes type, bool overwrite = false)
        {
            string id = Path + path;
            if (_db.Resources.Any(r => r.Id == id))
            {
                if(overwrite)
                {
                    _logger?.LogDebug("Resource entry with path {id} already exists, but overwrite is true, overwriting default value", id);
                    ResourceData data = _db.Resources.First(r => r.Id == id);
                    data.Data = ResourceHelper.Serialize(defaultValue, type);
                    data.Default = data.Data;
                    _db.Update(data);
                    _db.SaveChanges();
                    return;
                }
                _logger?.LogDebug("Resource entry with path {id} already exists, skipping default value", id);
                return;
            } else
            {
                ResourceData data = new ResourceData();
                data.Id = id;
                data.Data = ResourceHelper.Serialize(defaultValue, type);
                data.Default = data.Data;
                data.ResourceType = type;
                _db.Resources.Add(data);
                _db.SaveChanges();
            }
        }

        internal void SaveForeignResourceEntry(string path, object value)
        {
            ResourceData? data = _db.Resources.FirstOrDefault(r => r.Id == path);
            if (data == null)
            {
                _logger?.LogWarning("Resource entry with path {path} not found, but wanted to save", path);
                return;
            }
            data.Data = ResourceHelper.Serialize(value, data.ResourceType);
            _db.Update(data);
            _db.SaveChanges();
        }

        internal void SaveResourceEntry(string path, object value)
        {
            string id = Path + path;
            SaveForeignResourceEntry(id, value);
        }

        public void ResetResourceEntry(string path)
        {
            string id = Path + path;
            ResetForeignResourceEntry(id);
        }

        public void ResetForeignResourceEntry(string path)
        {
            ResourceData? data = _db.Resources.FirstOrDefault(r => r.Id == Path + path);
            if (data == null)
            {
                _logger?.LogWarning("Resource entry with path {path} not found, but wanted to reset", path);
                return;
            }
            data.Data = data.Default;
            _db.Update(data);
            _db.SaveChanges();
        }

        internal T GetForeignResourceEntry<T>(string path)
        {
            ResourceData? data = _db.Resources.FirstOrDefault(r => r.Id == path);
            if (data == null)
                throw new Exception($"Resource entry not found for path: {path}");
            return ResourceHelper.Deserialize<T>(data.Data, data.ResourceType);
        }

        internal T GetResourceEntry<T>(string path)
        {
            string id = Path + path;
            return GetForeignResourceEntry<T>(id);
        }

        internal ResourceData GetForeignResourceEntry(string path)
        {
            ResourceData? data = _db.Resources.FirstOrDefault(r => r.Id == path);
            if (data == null)
                throw new Exception($"Resource entry not found for path: {path}");

            return data;
        }

        internal ResourceData GetResourceEntry(string path)
        {
            string id = Path + path;
            return GetForeignResourceEntry(id);
        }

        internal byte[] ReturnTextString(string value)
        {
            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(1);
            writer.WriteTextString(value);
            writer.WriteEndMap();
            return writer.Encode();
        }

        internal byte[] ReturnByteString(string value)
        {
            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(1);
            writer.WriteByteString(Convert.FromHexString(value));
            writer.WriteEndMap();
            return writer.Encode();
        }

        internal byte[] ReturnUnsignedInteger(uint value, int key = 1)
        {
            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(key);
            writer.WriteUInt32(value);
            writer.WriteEndMap();
            return writer.Encode();
        }

        internal byte[] ReturnUnsignedBigInteger(ulong value, int key = 1)
        {
            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(key);
            writer.WriteUInt64(value);
            writer.WriteEndMap();
            return writer.Encode();
        }

        internal byte[] ReturnInteger(int value, int key = 1)
        {
            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(key);
            writer.WriteInt32(value);
            writer.WriteEndMap();
            return writer.Encode();
        }

        internal byte[] ReturnBigInteger(long value, int key = 1)
        {
            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(key);
            writer.WriteInt64(value);
            writer.WriteEndMap();
            return writer.Encode();
        }

        internal byte[] ReturnByteArray(string value)
        {
            byte[] data = Convert.FromHexString(value);

            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(1);
            writer.WriteStartArray(data.Length);
            for (int i = 0; i < data.Length; i++)
                writer.WriteInt32(data[i]);
            writer.WriteEndArray();
            writer.WriteEndMap();
            return writer.Encode();
        }

        internal byte[] ReturnByteArray(uint[] value)
        {
            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(1);
            writer.WriteStartArray(value.Length);
            for (int i = 0; i < value.Length; i++)
                writer.WriteUInt32(value[i]);
            writer.WriteEndArray();
            writer.WriteEndMap();
            return writer.Encode();
        }

        internal byte[] ReturnBool(bool value)
        {
            CborWriter writer = new CborWriter();
            writer.WriteStartMap(1);
            writer.WriteInt32(1);
            writer.WriteBoolean(value);
            writer.WriteEndMap();
            return writer.Encode();
        }

        public ResourceResponse? GetResourceData(Method method, string path, CoapMessage request)
        {
            _logger?.LogDebug("Getting resource data for path: {path}", path);

            var attrClass = this.GetType().GetCustomAttribute<ResourceBaseAttribute>();
            if (attrClass?.Path != null && path.StartsWith(attrClass.Path))
            {
                path = path.Substring(attrClass.Path.Length);
            }

            var dataMethod = this.GetType().GetMethods().Where(m =>
                m.GetCustomAttribute<ResourceAttribute>()?.Path == path &&
                m.GetCustomAttribute<ResourceAttribute>()?.Method == method
            ).FirstOrDefault();

            ResourceResponse response = new ResourceResponse();
            if (attrClass?.RequireSecure == true)
                response.RequireSecure = true;
            if (dataMethod == null)
            {
                dataMethod = this.GetType().GetMethods().Where(m =>
                    m.GetCustomAttribute<ResourceAttribute>()?.Path == "*" &&
                    m.GetCustomAttribute<ResourceAttribute>()?.Method == method
                ).FirstOrDefault();

                if(dataMethod == null)
                {
                    _logger?.LogError("No method found for: {method} {path}", method, path);
                    return null;
                }
            }

            var attrMethod = dataMethod.GetCustomAttribute<ResourceAttribute>();
            if (attrMethod?.RequireSecure == true)
                response.RequireSecure = true;

            object? returnedValue = null;
            if (dataMethod.GetParameters().Length == 0)
            {
                returnedValue = dataMethod.Invoke(this, null);
            }
            else if (dataMethod.GetParameters().Length == 1)
            {
                returnedValue = dataMethod.Invoke(this, new object?[] { request });
            }
            else if (dataMethod.GetParameters().Length == 2)
            {
                returnedValue = dataMethod.Invoke(this, new object?[] { request, path });
            }
            else
            {
                _logger?.LogError($"Method has a not supported amount of parameters: has={dataMethod.GetParameters().Length} allowed=[0,1,2]");
                return null;
            }

            if (returnedValue == null)
                return null;

            if (returnedValue is ResourceResponse r)
            {
                r.RequireSecure = response.RequireSecure;
                return r;
            }
            else if (returnedValue is byte[] b)
            {
                response.Payload = b;
            }

            return response;
        }

        public virtual void EraseTable(int eraseCode)
        {
            _logger?.LogInformation("Erasing resource table with code {code}", eraseCode);

            var dataMethods = this.GetType().GetMethods().Where(m => m.GetCustomAttribute<ResourceAttribute>()?.EraseCodes.Contains(eraseCode) ?? false);

            foreach (var dataMethod in dataMethods)
            {
                var attr = dataMethod.GetCustomAttribute<ResourceAttribute>();
                if (attr == null)
                    continue;

                ResetResourceEntry(attr.Path);
            }
        }

        internal abstract void InitTable();
    }
}
