using OpenKNX.IoT.Received;
using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;

namespace OpenKNX.IoT.Helper
{
    internal class CborHelper
    {
        public static byte[] Serialize(object data)
        {
            CborWriter writer = new CborWriter();

            SerializeType(writer, data);

            return writer.Encode();
        }

        public static void SerializeType(CborWriter writer, object data)
        {
            switch (Type.GetTypeCode(data.GetType()))
            {
                case TypeCode.Object:
                    {
                        writer.WriteStartMap(null);
                        SerializeObject(writer, data);
                        writer.WriteEndMap();
                        break;
                    }

                case TypeCode.UInt32:
                    {
                        writer.WriteUInt32((uint)data);
                        break;
                    }

                case TypeCode.Int32:
                    {
                        writer.WriteInt32((int)data);
                        break;
                    }

                case TypeCode.UInt64:
                    {
                        writer.WriteUInt64((ulong)data);
                        break;
                    }

                case TypeCode.Int64:
                    {
                        writer.WriteInt64((long)data);
                        break;
                    }

                case TypeCode.Boolean:
                    {
                        writer.WriteBoolean((bool)data);
                        break;
                    }

                case TypeCode.String:
                    {
                        writer.WriteTextString((string)data);
                        break;
                    }

                default:
                    throw new Exception("Serialize: type not implemented " + data.GetType());
            }
        }

        public static void SerializeObject(CborWriter writer, object data)
        {
            foreach(var prop in data.GetType().GetProperties())
            {
                var keyAttribute = prop.GetCustomAttribute<CborKeyAttribute>();
                if (keyAttribute == null)
                    continue;
                writer.WriteInt32(keyAttribute.Key);
                SerializeType(writer, prop.GetValue(data)!);
            }
        }



        public static T Deserialize<T>(byte[] data)
        {
            T instance = Activator.CreateInstance<T>();
            if(instance == null)
                throw new Exception("Could not create instance of type " + typeof(T).FullName);

            CborReader reader = new CborReader(data);

            ParseItem(reader, instance);

            return instance;
        }

        private static object ParseItem(CborReader reader, object instance)
        {
            switch(reader.PeekState())
            {
                case CborReaderState.StartArray:
                    {
                        var add = instance.GetType().GetMethod("Add");
                        if (add == null)
                            throw new Exception("Array is not a list with Add Method! " + instance.GetType().FullName);

                        int? itemCount = reader.ReadStartArray();

                        for(int i = 0; i < itemCount; i++)
                        {
                            object? item = Activator.CreateInstance(instance.GetType().GenericTypeArguments[0]);
                            if(item == null)
                                throw new Exception("Could not create instance for item: " + instance.GetType().GenericTypeArguments[0].FullName);
                            object createdItem = ParseItem(reader, item);
                            add.Invoke(instance, new object[] { createdItem });
                        }

                        reader.ReadEndArray();
                        break;
                    }

                case CborReaderState.StartMap:
                    {
                        int? itemCount = reader.ReadStartMap();
                        if (itemCount == null)
                            itemCount = int.MaxValue;

                        for (int i = 0; i < itemCount; i++)
                        {
                            if (reader.PeekState() == CborReaderState.EndMap)
                                break;

                            int key = reader.ReadInt32();

                            if(Type.GetTypeCode(instance.GetType()) == TypeCode.Object)
                            {
                                var prop = instance.GetType().GetProperties().FirstOrDefault(p => p.GetCustomAttribute<CborKeyAttribute>()?.Key == key);
                                if (prop == null)
                                    continue;

                                ParseObject(reader, prop.PropertyType, prop, instance);
                            } else
                            {

                            }
                        }

                        reader.ReadEndMap();
                        break;
                    }

                default:
                    return ParseInstance(reader, instance);
            }

            return instance;
        }

        private static void ParseObject(CborReader reader, Type type, PropertyInfo info, object instance)
        {
            switch(reader.PeekState())
            {
                case CborReaderState.UnsignedInteger:
                    ParseUnsignedInteger(reader, type, info, instance);
                    break;

                case CborReaderState.TextString:
                    ParseTextString(reader, type, info, instance);
                    break;

                case CborReaderState.ByteString:
                    ParseByteString(reader, type, info, instance);
                    break;

                case CborReaderState.StartArray:
                    {
                        var item = Activator.CreateInstance(info.PropertyType);
                        ParseItem(reader, item!);
                        info.SetValue(instance, item);
                        break;
                    }

                case CborReaderState.StartMap:
                    {
                        var item = Activator.CreateInstance(info.PropertyType);
                        ParseItem(reader, item!);
                        info.SetValue(instance, item);
                        break;
                    }

                case CborReaderState.Boolean:
                    ParseBoolean(reader, type, info, instance);
                    break;

                default:
                    throw new Exception("Unsupported Type");
            }
        }

        private static void ParseTextString(CborReader reader, Type type, PropertyInfo info, object instance)
        {
            string value = reader.ReadTextString();
            info.SetValue(instance, value);
        }

        private static void ParseBoolean(CborReader reader, Type type, PropertyInfo info, object instance)
        {
            bool value = reader.ReadBoolean();
            info.SetValue(instance, value);
        }

        private static void ParseByteString(CborReader reader, Type type, PropertyInfo info, object instance)
        {
            byte[] value = reader.ReadByteString();

            if(Type.GetTypeCode(info.PropertyType) == TypeCode.String)
            {
                string byteString = BitConverter.ToString(value).Replace("-", "");
                info.SetValue(instance, byteString);
                return;
            }

            info.SetValue(instance, value);
        }

        private static void ParseUnsignedInteger(CborReader reader, Type type, PropertyInfo info, object instance)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.UInt32:
                    {
                        uint value = reader.ReadUInt32();
                        info.SetValue(instance, value);
                        break;
                    }

                case TypeCode.Int32:
                    {
                        int value = reader.ReadInt32();
                        if (type.FullName?.Contains(".Enums.") == true)
                        {
                            object enu = Enum.ToObject(type, value);
                            info.SetValue(instance, enu);
                        }
                        else
                        {
                            info.SetValue(instance, value);
                        }
                        break;
                    }

                case TypeCode.Int64:
                    {
                        long value = reader.ReadInt64();
                        info.SetValue(instance, value);
                        break;
                    }

                case TypeCode.Object:
                    {
                        if (info.PropertyType.Name.StartsWith("Nullable"))
                        {
                            ParseObject(reader, Nullable.GetUnderlyingType(info.PropertyType)!, info, instance);
                        }
                        else if (info.PropertyType.Name.StartsWith("UInt32[]"))
                        {
                            int? size = reader.ReadStartArray();
                            if (size == null)
                                return;

                            uint[] value = new uint[size.Value];
                            for (int j = 0; j < size.Value; j++)
                            {
                                value[j] = reader.ReadUInt32();
                            }
                            reader.ReadEndArray();
                            info.SetValue(instance, value);
                        }
                        break;
                    }

                default:
                    throw new Exception("Unsupported type " + info.PropertyType.FullName);
            }
        }
    
        private static object ParseInstance(CborReader reader, object instance)
        {
            switch (reader.PeekState())
            {
                case CborReaderState.UnsignedInteger:
                    return ParseInstanceUnsignedInteger(reader, instance);

                default:
                    throw new Exception("Unsupported Type");
            }
        }

        private static object ParseInstanceUnsignedInteger(CborReader reader, object instance)
        {
            switch (Type.GetTypeCode(instance.GetType()))
            {
                case TypeCode.UInt32:
                    {
                        return reader.ReadUInt32();
                    }

                case TypeCode.Int32:
                    {
                        return reader.ReadInt32();
                    }

                case TypeCode.Int64:
                    {
                        return reader.ReadInt64();
                    }

                default:
                    throw new Exception("Unsupported type " + instance.GetType().FullName);
            }
        }
    }
}
