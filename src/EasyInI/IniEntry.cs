using System.Collections.Generic;

namespace EasyIni
{
    /// <summary>
    /// Represents a single key=value entry within an INI section,
    /// along with its associated comments.
    /// </summary>
    public class IniEntry
    {
        /// <summary>
        /// Gets the key name.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets or sets the entry value.
        /// </summary>
        public IniValue Value { get; set; }

        /// <summary>
        /// Comment lines appearing before this entry.
        /// Each element is one comment line without the leading ';' or '#'.
        /// </summary>
        public List<string> LeadingComments { get; private set; }

        /// <summary>
        /// Comment appearing on the same line after the value, or null.
        /// Does not include the leading ';' or '#'.
        /// </summary>
        public string InlineComment { get; set; }

        /// <summary>
        /// Creates an entry with the specified key and string value.
        /// </summary>
        public IniEntry(string key, string value)
        {
            Key = key;
            Value = new IniValue(value);
            LeadingComments = new List<string>();
        }

        /// <summary>
        /// Creates an entry with the specified key and IniValue.
        /// </summary>
        public IniEntry(string key, IniValue value)
        {
            Key = key;
            Value = value;
            LeadingComments = new List<string>();
        }

        /// <summary>
        /// Returns "key=value".
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}={1}", Key, Value.ToString());
        }
    }
}
