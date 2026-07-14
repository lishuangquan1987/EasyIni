using System;
using System.ComponentModel;
using System.Globalization;

namespace EasyIni
{
    /// <summary>
    /// Wraps an INI value string and provides type conversion.
    /// </summary>
    public struct IniValue : IEquatable<IniValue>
    {
        /// <summary>
        /// Gets the raw string value, or null if this instance represents no value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Gets whether this instance holds a non-null value.
        /// </summary>
        public bool HasValue
        {
            get { return Value != null; }
        }

        /// <summary>
        /// Creates an IniValue wrapping the given string.
        /// </summary>
        public IniValue(string value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Converts the value to the specified type.
        /// Throws InvalidCastException if conversion fails.
        /// </summary>
        public T Get<T>()
        {
            if (!HasValue)
                return default(T);

            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                if (string.IsNullOrEmpty(Value))
                    return default(T);
                targetType = underlyingType;
            }

            if (targetType == typeof(string))
                return (T)(object)Value;

            if (targetType == typeof(IniValue))
                return (T)(object)this;

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(typeof(string)))
                return (T)converter.ConvertFromString(null, CultureInfo.InvariantCulture, Value);

            return (T)Convert.ChangeType(Value, targetType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the value to the specified type, returning defaultValue on failure.
        /// </summary>
        public T Get<T>(T defaultValue)
        {
            try
            {
                if (!HasValue)
                    return defaultValue;
                return Get<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Returns the raw string value or the default string if null.
        /// </summary>
        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        /// <summary>
        /// Returns the hash code of the underlying value.
        /// </summary>
        public override int GetHashCode()
        {
            return Value != null ? Value.GetHashCode() : 0;
        }

        /// <summary>
        /// Two IniValue instances are equal if their raw values are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is IniValue))
                return false;
            return Equals((IniValue)obj);
        }

        /// <summary>
        /// Two IniValue instances are equal if their raw values are equal.
        /// </summary>
        public bool Equals(IniValue other)
        {
            return string.Equals(Value, other.Value);
        }

        // --- Implicit conversions (C# 5.0 safe) ---

        /// <summary>
        /// Implicitly converts the IniValue to its raw string.
        /// </summary>
        public static implicit operator string(IniValue v)
        {
            return v.Value;
        }

        /// <summary>
        /// Implicitly converts a string to an IniValue.
        /// </summary>
        public static implicit operator IniValue(string s)
        {
            return new IniValue(s);
        }

        /// <summary>
        /// Implicitly converts the IniValue to an int.
        /// </summary>
        public static implicit operator int(IniValue v)
        {
            return v.Get<int>();
        }

        /// <summary>
        /// Implicitly converts the IniValue to a bool.
        /// </summary>
        public static implicit operator bool(IniValue v)
        {
            return v.Get<bool>();
        }

        /// <summary>
        /// Implicitly converts the IniValue to a double.
        /// </summary>
        public static implicit operator double(IniValue v)
        {
            return v.Get<double>();
        }

        /// <summary>
        /// Returns true if two IniValue instances are equal.
        /// </summary>
        public static bool operator ==(IniValue a, IniValue b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Returns true if two IniValue instances are not equal.
        /// </summary>
        public static bool operator !=(IniValue a, IniValue b)
        {
            return !a.Equals(b);
        }
    }
}
