using System;
using System.Collections;
using System.Collections.Generic;

namespace EasyIni
{
    /// <summary>
    /// Represents an INI section: [SectionName] followed by key=value entries.
    /// Entries are ordered as they appear in the file.
    /// </summary>
    public class IniSection : IEnumerable<IniEntry>
    {
        private readonly List<IniEntry> _entries;
        private readonly IEqualityComparer<string> _keyComparer;

        /// <summary>
        /// Gets the section name (without brackets).
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Comment lines appearing before the [section] header.
        /// Each element is one comment line without the leading ';' or '#'.
        /// </summary>
        public List<string> LeadingComments { get; private set; }

        /// <summary>
        /// Gets or sets the entry value for the specified key.
        /// Setting a key that does not exist will add a new entry at the end.
        /// </summary>
        public IniValue this[string key]
        {
            get
            {
                var entry = FindEntry(key);
                if (entry != null)
                    return entry.Value;
                throw new KeyNotFoundException(
                    string.Format("Key '{0}' not found in section '{1}'.", key, Name));
            }
            set
            {
                var entry = FindEntry(key);
                if (entry != null)
                {
                    entry.Value = value;
                }
                else
                {
                    _entries.Add(new IniEntry(key, value));
                }
            }
        }

        /// <summary>
        /// Creates a section with the given name.
        /// </summary>
        /// <param name="name">Section name (without brackets).</param>
        /// <param name="keyComparer">Key equality comparer for case sensitivity.</param>
        public IniSection(string name, IEqualityComparer<string> keyComparer)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Name = name;
            _keyComparer = keyComparer ?? StringComparer.OrdinalIgnoreCase;
            _entries = new List<IniEntry>();
            LeadingComments = new List<string>();
        }

        /// <summary>
        /// Creates a section with case-insensitive key lookup (default).
        /// </summary>
        public IniSection(string name)
            : this(name, StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Gets the number of entries in this section.
        /// </summary>
        public int Count
        {
            get { return _entries.Count; }
        }

        /// <summary>
        /// Returns all entries in file order.
        /// </summary>
        public IEnumerable<IniEntry> Entries
        {
            get { return _entries; }
        }

        /// <summary>
        /// Returns all keys in file order.
        /// </summary>
        public IEnumerable<string> Keys
        {
            get
            {
                for (int i = 0; i < _entries.Count; i++)
                    yield return _entries[i].Key;
            }
        }

        /// <summary>
        /// Returns the IniEntry for the specified key, or null if not found.
        /// </summary>
        public IniEntry GetEntry(string key)
        {
            return FindEntry(key);
        }

        /// <summary>
        /// Checks whether a key exists in this section.
        /// </summary>
        public bool ContainsKey(string key)
        {
            return FindEntry(key) != null;
        }

        /// <summary>
        /// Gets a value by key, returning the specified default if the key is not found.
        /// </summary>
        public IniValue GetValueOrDefault(string key, IniValue defaultValue)
        {
            var entry = FindEntry(key);
            if (entry != null)
                return entry.Value;
            return defaultValue;
        }

        /// <summary>
        /// Gets a typed value by key.
        /// Throws KeyNotFoundException if the key does not exist.
        /// </summary>
        public T Get<T>(string key)
        {
            var entry = FindEntry(key);
            if (entry == null)
                throw new KeyNotFoundException(
                    string.Format("Key '{0}' not found in section '{1}'.", key, Name));
            return entry.Value.Get<T>();
        }

        /// <summary>
        /// Gets a typed value by key, returning the specified default if not found or conversion fails.
        /// </summary>
        public T Get<T>(string key, T defaultValue)
        {
            var entry = FindEntry(key);
            if (entry == null)
                return defaultValue;
            try
            {
                return entry.Value.Get<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Sets a key to a string value. Creates the key if it does not exist.
        /// Returns the IniEntry that was created or updated.
        /// </summary>
        public IniEntry Set(string key, string value)
        {
            var entry = FindEntry(key);
            if (entry != null)
            {
                entry.Value = new IniValue(value);
                return entry;
            }
            var newEntry = new IniEntry(key, value);
            _entries.Add(newEntry);
            return newEntry;
        }

        /// <summary>
        /// Sets a key to an IniValue. Creates the key if it does not exist.
        /// Returns the IniEntry that was created or updated.
        /// </summary>
        public IniEntry Set(string key, IniValue value)
        {
            var entry = FindEntry(key);
            if (entry != null)
            {
                entry.Value = value;
                return entry;
            }
            var newEntry = new IniEntry(key, value);
            _entries.Add(newEntry);
            return newEntry;
        }

        /// <summary>
        /// Removes the entry with the specified key.
        /// Returns true if the entry was found and removed.
        /// </summary>
        public bool Remove(string key)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_keyComparer.Equals(_entries[i].Key, key))
                {
                    _entries.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private IniEntry FindEntry(string key)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_keyComparer.Equals(_entries[i].Key, key))
                    return _entries[i];
            }
            return null;
        }

        /// <summary>
        /// Returns an enumerator over entries in file order.
        /// </summary>
        public IEnumerator<IniEntry> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
