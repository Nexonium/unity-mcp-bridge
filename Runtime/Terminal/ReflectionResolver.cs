using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityMCPBridge.Terminal
{
    /// <summary>
    /// Resolves component property/field paths via reflection.
    /// Supports nested value types (e.g. Transform.position.x).
    /// </summary>
    public static class ReflectionResolver
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Get a property/field value from a component by dot-separated path.
        /// Path format: ComponentType.field.subfield (e.g. "Transform.position.x")
        /// </summary>
        public static ResolveResult GetValue(GameObject obj, string path)
        {
            if (obj == null)
                return ResolveResult.Fail("GameObject is null.");
            if (string.IsNullOrEmpty(path))
                return ResolveResult.Fail("Empty path.");

            var parts = path.Split('.');
            if (parts.Length < 2)
                return ResolveResult.Fail("Path must be ComponentType.property[.subfield...]. Example: Transform.position.x");

            var component = FindComponent(obj, parts[0]);
            if (component == null)
                return ResolveResult.Fail($"Component '{parts[0]}' not found on '{obj.name}'.");

            object current = component;
            for (var i = 1; i < parts.Length; i++)
            {
                var member = FindMember(current.GetType(), parts[i]);
                if (member == null)
                    return ResolveResult.Fail($"Member '{parts[i]}' not found on {current.GetType().Name}.");

                current = GetMemberValue(member, current);
                if (current == null && i < parts.Length - 1)
                    return ResolveResult.Fail($"Null reference at '{parts[i]}'.");
            }

            return ResolveResult.Ok(current, FormatValue(current));
        }

        /// <summary>
        /// Set a property/field value on a component by dot-separated path.
        /// Handles value-type reconstruction for nested structs (e.g. position.x).
        /// </summary>
        public static ResolveResult SetValue(GameObject obj, string path, string rawValue)
        {
            if (obj == null)
                return ResolveResult.Fail("GameObject is null.");
            if (string.IsNullOrEmpty(path))
                return ResolveResult.Fail("Empty path.");

            var parts = path.Split('.');
            if (parts.Length < 2)
                return ResolveResult.Fail("Path must be ComponentType.property[.subfield...] value");

            var component = FindComponent(obj, parts[0]);
            if (component == null)
                return ResolveResult.Fail($"Component '{parts[0]}' not found on '{obj.name}'.");

            // Build chain: [component, member1Value, member2Value, ...]
            // For Transform.position.x: chain = [Transform, Vector3, float]
            var chain = new object[parts.Length];
            var members = new MemberInfo[parts.Length - 1];
            chain[0] = component;

            for (var i = 1; i < parts.Length; i++)
            {
                var member = FindMember(chain[i - 1].GetType(), parts[i]);
                if (member == null)
                    return ResolveResult.Fail($"Member '{parts[i]}' not found on {chain[i - 1].GetType().Name}.");
                members[i - 1] = member;
                chain[i] = GetMemberValue(member, chain[i - 1]);
            }

            // Parse the new value to match the leaf type
            var leafType = GetMemberType(members[^1]);
            if (!TryParseValue(rawValue, leafType, out var parsed))
                return ResolveResult.Fail($"Cannot parse '{rawValue}' as {leafType.Name}.");

            // Set the leaf value
            chain[^1] = parsed;

            // Propagate backwards through value types
            // For Transform.position.x: set x on position struct, then set position on Transform
            for (var i = parts.Length - 1; i >= 1; i--)
            {
                SetMemberValue(members[i - 1], chain[i - 1], chain[i]);

                // If the owner is a value type and not the component itself, we need to propagate up
                if (i > 1 && chain[i - 1].GetType().IsValueType)
                    chain[i - 1] = chain[i - 1]; // already boxed, mutation happened in place
            }

            // Read back to confirm
            var confirm = GetMemberValue(members[0], component);
            for (var i = 1; i < members.Length; i++)
                confirm = GetMemberValue(members[i], confirm);

            return ResolveResult.Ok(confirm, $"{path} = {FormatValue(confirm)}");
        }

        /// <summary>
        /// List all accessible members of a component.
        /// </summary>
        public static string ListMembers(GameObject obj, string componentName)
        {
            if (obj == null) return "GameObject is null.";

            var component = FindComponent(obj, componentName);
            if (component == null)
                return $"Component '{componentName}' not found on '{obj.name}'.";

            var type = component.GetType();
            var sb = new StringBuilder();
            sb.AppendLine($"Members of [{type.Name}]:");

            // Properties
            var props = type.GetProperties(MemberFlags)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name);

            foreach (var prop in props)
            {
                try
                {
                    var val = prop.GetValue(component);
                    var readOnly = prop.CanWrite ? "" : " (read-only)";
                    sb.AppendLine($"  .{prop.Name}: {prop.PropertyType.Name} = {FormatValue(val)}{readOnly}");
                }
                catch
                {
                    sb.AppendLine($"  .{prop.Name}: {prop.PropertyType.Name} = <error reading>");
                }
            }

            // Fields (public only, to reduce noise)
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(f => f.Name);

            foreach (var field in fields)
            {
                try
                {
                    var val = field.GetValue(component);
                    sb.AppendLine($"  .{field.Name}: {field.FieldType.Name} = {FormatValue(val)}");
                }
                catch
                {
                    sb.AppendLine($"  .{field.Name}: {field.FieldType.Name} = <error reading>");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static Component FindComponent(GameObject obj, string name)
        {
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp == null) continue;
                if (string.Equals(comp.GetType().Name, name, StringComparison.OrdinalIgnoreCase))
                    return comp;
            }
            return null;
        }

        private static MemberInfo FindMember(Type type, string name)
        {
            // Try property first
            var prop = type.GetProperty(name, MemberFlags);
            if (prop != null) return prop;

            // Case-insensitive fallback
            prop = type.GetProperties(MemberFlags)
                .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (prop != null) return prop;

            // Try field
            var field = type.GetField(name, MemberFlags);
            if (field != null) return field;

            // Case-insensitive fallback
            field = type.GetFields(MemberFlags)
                .FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            return field;
        }

        private static object GetMemberValue(MemberInfo member, object owner)
        {
            return member switch
            {
                PropertyInfo prop => prop.GetValue(owner),
                FieldInfo field => field.GetValue(owner),
                _ => null
            };
        }

        private static void SetMemberValue(MemberInfo member, object owner, object value)
        {
            switch (member)
            {
                case PropertyInfo prop:
                    if (prop.CanWrite)
                        prop.SetValue(owner, value);
                    break;
                case FieldInfo field:
                    field.SetValue(owner, value);
                    break;
            }
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo prop => prop.PropertyType,
                FieldInfo field => field.FieldType,
                _ => typeof(object)
            };
        }

        private static bool TryParseValue(string raw, Type target, out object result)
        {
            result = null;

            try
            {
                if (target == typeof(float))
                {
                    if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    { result = v; return true; }
                }
                else if (target == typeof(int))
                {
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                    { result = v; return true; }
                }
                else if (target == typeof(double))
                {
                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    { result = v; return true; }
                }
                else if (target == typeof(bool))
                {
                    if (bool.TryParse(raw, out var v))
                    { result = v; return true; }
                    if (raw == "1") { result = true; return true; }
                    if (raw == "0") { result = false; return true; }
                }
                else if (target == typeof(string))
                {
                    result = raw;
                    return true;
                }
                else if (target == typeof(long))
                {
                    if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                    { result = v; return true; }
                }
                else if (target.IsEnum)
                {
                    if (Enum.TryParse(target, raw, true, out var v))
                    { result = v; return true; }
                }
                else if (target == typeof(Vector2))
                {
                    result = ParseVector2(raw);
                    return result != null;
                }
                else if (target == typeof(Vector3))
                {
                    result = ParseVector3(raw);
                    return result != null;
                }
                else if (target == typeof(Color))
                {
                    if (ColorUtility.TryParseHtmlString(raw, out var c))
                    { result = c; return true; }
                }
            }
            catch
            {
                // Parsing failed
            }

            return false;
        }

        private static object ParseVector2(string raw)
        {
            var trimmed = raw.Trim('(', ')');
            var parts = trimmed.Split(',');
            if (parts.Length != 2) return null;

            if (float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                return new Vector2(x, y);
            return null;
        }

        private static object ParseVector3(string raw)
        {
            var trimmed = raw.Trim('(', ')');
            var parts = trimmed.Split(',');
            if (parts.Length != 3) return null;

            if (float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                return new Vector3(x, y, z);
            return null;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "null";

            return value switch
            {
                float f => f.ToString("G", CultureInfo.InvariantCulture),
                double d => d.ToString("G", CultureInfo.InvariantCulture),
                Vector2 v2 => $"({v2.x:G}, {v2.y:G})",
                Vector3 v3 => $"({v3.x:G}, {v3.y:G}, {v3.z:G})",
                Vector4 v4 => $"({v4.x:G}, {v4.y:G}, {v4.z:G}, {v4.w:G})",
                Quaternion q => $"({q.x:G}, {q.y:G}, {q.z:G}, {q.w:G})",
                Color c => $"({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})",
                bool b => b ? "true" : "false",
                Enum e => e.ToString(),
                _ => value.ToString()
            };
        }

        public class ResolveResult
        {
            public bool Success { get; private set; }
            public object Value { get; private set; }
            public string Display { get; private set; }
            public string Error { get; private set; }

            public static ResolveResult Ok(object value, string display)
                => new() { Success = true, Value = value, Display = display };

            public static ResolveResult Fail(string error)
                => new() { Success = false, Error = error };
        }
    }
}
