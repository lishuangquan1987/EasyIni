using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EasyIni
{
    /// <summary>
    /// Represents a complete INI file: a collection of named sections
    /// plus a Global section for entries that appear before any [section] header.
    /// </summary>
    public class IniData
    {
        private readonly List<IniSection> _sections;
        private readonly IEqualityComparer<string> _sectionComparer;
        private readonly IEqualityComparer<string> _keyComparer;

        /// <summary>
        /// Gets the default section for keys declared before any [section] header.
        /// </summary>
        public IniSection Global { get; private set; }

        /// <summary>
        /// Gets or creates the section with the given name.
        /// Section name lookup respects the case-sensitivity setting from parse options.
        /// </summary>
        public IniSection this[string sectionName]
        {
            get
            {
                var section = FindSection(sectionName);
                if (section != null)
                    return section;
                throw new KeyNotFoundException(
                    string.Format("Section '[{0}]' not found.", sectionName));
            }
        }

        /// <summary>
        /// Creates an IniData instance with the specified comparers.
        /// </summary>
        public IniData(IEqualityComparer<string> sectionComparer, IEqualityComparer<string> keyComparer)
        {
            _sectionComparer = sectionComparer ?? StringComparer.OrdinalIgnoreCase;
            _keyComparer = keyComparer ?? StringComparer.OrdinalIgnoreCase;
            _sections = new List<IniSection>();
            Global = new IniSection(string.Empty, _keyComparer);
        }

        /// <summary>
        /// Creates an IniData with case-insensitive section and key names (default).
        /// </summary>
        public IniData()
            : this(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Gets the number of sections (excluding Global).
        /// </summary>
        public int SectionCount
        {
            get { return _sections.Count; }
        }

        /// <summary>
        /// Returns all sections in file order (excluding Global).
        /// </summary>
        public IEnumerable<IniSection> Sections
        {
            get { return _sections; }
        }

        /// <summary>
        /// Checks whether a named section exists.
        /// </summary>
        public bool ContainsSection(string sectionName)
        {
            return FindSection(sectionName) != null;
        }

        /// <summary>
        /// Adds a new section. Throws if a section with the same name already exists.
        /// </summary>
        public IniSection AddSection(string sectionName)
        {
            if (FindSection(sectionName) != null)
                throw new InvalidOperationException(
                    string.Format("Section '[{0}]' already exists.", sectionName));
            var section = new IniSection(sectionName, _keyComparer);
            _sections.Add(section);
            return section;
        }

        /// <summary>
        /// Gets an existing section or creates it if it does not exist.
        /// </summary>
        public IniSection AddOrGetSection(string sectionName)
        {
            var section = FindSection(sectionName);
            if (section != null)
                return section;
            section = new IniSection(sectionName, _keyComparer);
            _sections.Add(section);
            return section;
        }

        /// <summary>
        /// Removes the specified section. Returns true if it was found and removed.
        /// </summary>
        public bool RemoveSection(string sectionName)
        {
            for (int i = 0; i < _sections.Count; i++)
            {
                if (_sectionComparer.Equals(_sections[i].Name, sectionName))
                {
                    _sections.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Shortcut: gets a typed value from a section.
        /// </summary>
        public T Get<T>(string section, string key)
        {
            return this[section].Get<T>(key);
        }

        /// <summary>
        /// Shortcut: gets a typed value with a default.
        /// </summary>
        public T Get<T>(string section, string key, T defaultValue)
        {
            var sec = FindSection(section);
            if (sec != null)
                return sec.Get(key, defaultValue);
            return defaultValue;
        }

        /// <summary>
        /// Shortcut: sets a value in a section. Creates the section if it does not exist.
        /// </summary>
        public IniEntry Set(string section, string key, string value)
        {
            var sec = FindSection(section);
            if (sec == null)
                sec = AddSection(section);
            return sec.Set(key, value);
        }

        /// <summary>
        /// Shortcut: sets an IniValue in a section. Creates the section if it does not exist.
        /// </summary>
        public IniEntry Set(string section, string key, IniValue value)
        {
            var sec = FindSection(section);
            if (sec == null)
                sec = AddSection(section);
            return sec.Set(key, value);
        }

        private IniSection FindSection(string name)
        {
            for (int i = 0; i < _sections.Count; i++)
            {
                if (_sectionComparer.Equals(_sections[i].Name, name))
                    return _sections[i];
            }
            return null;
        }

        /// <summary>
        /// Serializes the entire IniData to INI text, preserving comments and entry order.
        /// </summary>
        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Writes the INI content to the given TextWriter.
        /// </summary>
        public void WriteTo(TextWriter writer)
        {
            // Global section entries (no section header)
            WriteSectionEntries(writer, Global, writeHeader: false);

            for (int i = 0; i < _sections.Count; i++)
            {
                var section = _sections[i];

                // Blank line between sections (unless first)
                if (i > 0 || Global.Count > 0)
                    writer.WriteLine();

                WriteSectionEntries(writer, section, writeHeader: true);
            }
        }

        /// <summary>
        /// Saves the INI content to a file.
        /// </summary>
        public void Save(string filePath)
        {
            Save(filePath, Encoding.UTF8);
        }

        /// <summary>
        /// Saves the INI content to a file with the specified encoding.
        /// </summary>
        public void Save(string filePath, Encoding encoding)
        {
            using (var writer = new StreamWriter(filePath, false, encoding))
            {
                WriteTo(writer);
            }
        }

        private static void WriteSectionEntries(TextWriter writer, IniSection section, bool writeHeader)
        {
            // Section leading comments
            if (section.LeadingComments.Count > 0)
            {
                foreach (var comment in section.LeadingComments)
                {
                    writer.Write(";");
                    writer.WriteLine(comment);
                }
            }

            // Section header
            if (writeHeader)
            {
                writer.Write("[");
                writer.Write(section.Name);
                writer.WriteLine("]");
            }

            // Entries
            foreach (var entry in section.Entries)
            {
                // Entry leading comments
                if (entry.LeadingComments.Count > 0)
                {
                    foreach (var comment in entry.LeadingComments)
                    {
                        writer.Write(";");
                        writer.WriteLine(comment);
                    }
                }

                // key=value
                writer.Write(entry.Key);
                writer.Write("=");
                writer.Write(NeedQuoting(entry.Value)
                    ? QuoteValue(entry.Value.ToString())
                    : entry.Value.ToString());

                // inline comment
                if (entry.InlineComment != null)
                {
                    writer.Write(" ; ");
                    writer.Write(entry.InlineComment);
                }

                writer.WriteLine();
            }
        }

        private static bool NeedQuoting(IniValue value)
        {
            if (!value.HasValue)
                return false;
            var s = value.ToString();
            if (string.IsNullOrEmpty(s))
                return false;
            // Quote if value contains ';', '#', '=', leading/trailing whitespace, or newlines
            if (s.IndexOf(';') >= 0)
                return true;
            if (s.IndexOf('#') >= 0)
                return true;
            if (s.IndexOf('=') >= 0)
                return true;
            if (s.Length > 0 && (char.IsWhiteSpace(s[0]) || char.IsWhiteSpace(s[s.Length - 1])))
                return true;
            if (s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0)
                return true;
            return false;
        }

        private static string QuoteValue(string value)
        {
            // Escape embedded double-quotes
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
