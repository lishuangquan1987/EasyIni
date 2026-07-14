using System;
using System.Collections.Generic;
using System.Reflection;

namespace EasyInI
{
    /// <summary>
    /// Provides generic serialization and deserialization between
    /// POCO objects and IniData.
    /// </summary>
    public static class IniSerializer
    {
        /// <summary>
        /// Deserializes an IniData instance into a new object of type T.
        /// Only properties marked with <see cref="IniPropertyAttribute"/> are mapped.
        /// Note: deserialization only searches named sections, not the Global section.
        /// </summary>
        /// <typeparam name="T">The target type. Must have a parameterless constructor.</typeparam>
        /// <param name="ini">The parsed INI data.</param>
        /// <returns>A new instance of T populated from the INI data.</returns>
        public static T Deserialize<T>(IniData ini) where T : new()
        {
            if (ini == null)
                throw new ArgumentNullException("ini");

            var obj = new T();
            var type = typeof(T);
            var defaultSection = GetDefaultSection(type);

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (!prop.CanWrite)
                    continue;

                var attr = GetIniPropertyAttribute(prop);
                if (attr == null)
                    continue;

                string sectionName = attr.Section ?? defaultSection;
                string keyName = attr.Name;

                IniSection section = FindSection(ini, sectionName);
                if (section == null)
                    continue;

                var entry = section.GetEntry(keyName);
                if (entry == null)
                    continue;

                SetPropertyValue(obj, prop, entry.Value);
            }

            return obj;
        }

        /// <summary>
        /// Deserializes an INI string directly into an object of type T.
        /// </summary>
        public static T Deserialize<T>(string iniText) where T : new()
        {
            return Deserialize<T>(IniParser.Parse(iniText));
        }

        /// <summary>
        /// Serializes an object into an IniData instance.
        /// Properties are grouped into sections based on their
        /// <see cref="IniPropertyAttribute.Section"/> or the class-level
        /// <see cref="IniSectionAttribute"/>.
        /// </summary>
        public static IniData Serialize<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            var type = typeof(T);
            var defaultSection = GetDefaultSection(type);
            var data = new IniData();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (!prop.CanRead)
                    continue;

                var attr = GetIniPropertyAttribute(prop);
                if (attr == null)
                    continue;

                string sectionName = attr.Section ?? defaultSection;
                string keyName = attr.Name;
                object value = prop.GetValue(obj, null);
                string valueStr = value != null ? value.ToString() : string.Empty;

                data.Set(sectionName, keyName, valueStr);
            }

            return data;
        }

        /// <summary>
        /// Serializes an object directly to an INI string.
        /// </summary>
        public static string SerializeToString<T>(T obj)
        {
            return Serialize(obj).ToString();
        }

        private static string GetDefaultSection(Type type)
        {
            var sectionAttr = (IniSectionAttribute)Attribute.GetCustomAttribute(
                type, typeof(IniSectionAttribute));
            if (sectionAttr != null)
                return sectionAttr.Name;
            return type.Name;
        }

        private static IniPropertyAttribute GetIniPropertyAttribute(PropertyInfo prop)
        {
            return (IniPropertyAttribute)Attribute.GetCustomAttribute(
                prop, typeof(IniPropertyAttribute));
        }

        private static IniSection FindSection(IniData ini, string sectionName)
        {
            if (ini.ContainsSection(sectionName))
                return ini[sectionName];
            return null;
        }

        private static void SetPropertyValue<T>(T obj, PropertyInfo prop, IniValue value)
        {
            try
            {
                var convertedValue = ConvertIniValue(value, prop.PropertyType);
                prop.SetValue(obj, convertedValue, null);
            }
            catch
            {
                // Silently skip on conversion failure
            }
        }

        private static object ConvertIniValue(IniValue value, Type targetType)
        {
            if (!value.HasValue)
                return GetDefault(targetType);

            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                if (string.IsNullOrEmpty(value.Value))
                    return null;
                targetType = underlyingType;
            }

            if (targetType == typeof(string))
                return value.Value;

            if (targetType == typeof(IniValue))
                return value;

            if (targetType.IsEnum)
                return Enum.Parse(targetType, value.Value, true);

            var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(typeof(string)))
                return converter.ConvertFromInvariantString(value.Value);

            return Convert.ChangeType(value.Value, targetType,
                System.Globalization.CultureInfo.InvariantCulture);
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }
    }
}
