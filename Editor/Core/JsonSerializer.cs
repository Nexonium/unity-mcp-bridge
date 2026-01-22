using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace UnityMCPBridge.Core
{
    /// <summary>
    /// Simple JSON serializer without external dependencies.
    /// Supports basic types, arrays, lists, and anonymous objects.
    /// </summary>
    public static class JsonSerializer
    {
        public static string Serialize(object obj)
        {
            if (obj == null) return "null";

            var sb = new StringBuilder();
            SerializeValue(obj, sb);
            return sb.ToString();
        }

        private static void SerializeValue(object value, StringBuilder sb)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            var type = value.GetType();

            if (value is string str)
            {
                SerializeString(str, sb);
            }
            else if (value is bool b)
            {
                sb.Append(b ? "true" : "false");
            }
            else if (value is DateTime dt)
            {
                sb.Append('"');
                sb.Append(dt.ToString("o"));
                sb.Append('"');
            }
            else if (IsNumericType(type))
            {
                sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
            else if (value is IDictionary dict)
            {
                SerializeDictionary(dict, sb);
            }
            else if (value is IEnumerable enumerable && !(value is string))
            {
                SerializeArray(enumerable, sb);
            }
            else if (type.IsClass || type.IsValueType && !type.IsPrimitive)
            {
                SerializeObject(value, sb);
            }
            else
            {
                SerializeString(value.ToString(), sb);
            }
        }

        private static void SerializeString(string str, StringBuilder sb)
        {
            sb.Append('"');
            foreach (var c in str)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private static void SerializeArray(IEnumerable enumerable, StringBuilder sb)
        {
            sb.Append('[');
            var first = true;
            foreach (var item in enumerable)
            {
                if (!first) sb.Append(',');
                first = false;
                SerializeValue(item, sb);
            }
            sb.Append(']');
        }

        private static void SerializeDictionary(IDictionary dict, StringBuilder sb)
        {
            sb.Append('{');
            var first = true;
            foreach (DictionaryEntry entry in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                SerializeString(entry.Key.ToString(), sb);
                sb.Append(':');
                SerializeValue(entry.Value, sb);
            }
            sb.Append('}');
        }

        private static void SerializeObject(object obj, StringBuilder sb)
        {
            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            sb.Append('{');
            var first = true;

            foreach (var prop in properties)
            {
                if (!prop.CanRead) continue;
                
                try
                {
                    var value = prop.GetValue(obj);
                    if (!first) sb.Append(',');
                    first = false;
                    
                    // Convert property name to camelCase
                    var name = ToCamelCase(prop.Name);
                    SerializeString(name, sb);
                    sb.Append(':');
                    SerializeValue(value, sb);
                }
                catch
                {
                    // Skip properties that throw exceptions
                }
            }

            sb.Append('}');
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(float) ||
                   type == typeof(double) || type == typeof(decimal) || type == typeof(short) ||
                   type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
                   type == typeof(ushort) || type == typeof(sbyte);
        }

        private static string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
                return str;
            
            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }
    }
}
