using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EasyInI
{
    /// <summary>
    /// Static parser for INI text and files.
    /// Produces IniData with comment and format preservation.
    /// </summary>
    public static class IniParser
    {
        /// <summary>
        /// Parses an INI string into an IniData object.
        /// </summary>
        /// <param name="iniText">The INI content.</param>
        /// <param name="options">Parse options, or null for defaults.</param>
        /// <returns>Parsed IniData instance.</returns>
        /// <exception cref="IniParseException">Thrown when the INI text is malformed
        /// or when DuplicateKeyBehavior is Throw and a duplicate is encountered.</exception>
        public static IniData Parse(string iniText, IniParseOptions options)
        {
            if (iniText == null)
                throw new ArgumentNullException("iniText");
            if (options == null)
                options = IniParseOptions.Default;

            using (var reader = new StringReader(iniText))
            {
                return ParseReader(reader, options);
            }
        }

        /// <summary>
        /// Parses an INI string using default options.
        /// </summary>
        public static IniData Parse(string iniText)
        {
            return Parse(iniText, null);
        }

        /// <summary>
        /// Parses an INI file into an IniData object.
        /// </summary>
        /// <param name="filePath">Path to the INI file.</param>
        /// <param name="options">Parse options, or null for defaults.</param>
        /// <returns>Parsed IniData instance.</returns>
        public static IniData ParseFile(string filePath, IniParseOptions options)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");
            if (options == null)
                options = IniParseOptions.Default;

            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                return ParseReader(reader, options);
            }
        }

        /// <summary>
        /// Parses an INI file using default options.
        /// </summary>
        public static IniData ParseFile(string filePath)
        {
            return ParseFile(filePath, null);
        }

        /// <summary>
        /// Tries to parse an INI string. Returns false on failure and sets
        /// the error message in <paramref name="error"/>.
        /// </summary>
        public static bool TryParse(string iniText, out IniData result, out string error)
        {
            return TryParse(iniText, null, out result, out error);
        }

        /// <summary>
        /// Tries to parse an INI string with the given options.
        /// </summary>
        public static bool TryParse(string iniText, IniParseOptions options,
            out IniData result, out string error)
        {
            result = null;
            error = null;
            try
            {
                result = Parse(iniText, options);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static IniData ParseReader(TextReader reader, IniParseOptions options)
        {
            var keyComparer = options.GetKeyComparer();
            var sectionComparer = keyComparer;
            var data = new IniData(sectionComparer, keyComparer);

            var commentLines = new List<string>();
            IniSection currentSection = data.Global;
            int lineNumber = 0;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                string trimmed = options.TrimWhitespace ? line.Trim() : line;

                // Empty line
                if (trimmed.Length == 0)
                {
                    // If inside a comment block, preserve as blank line in comments
                    if (commentLines.Count > 0)
                        commentLines.Add(string.Empty);
                    continue;
                }

                // Comment line
                if (trimmed[0] == ';' || trimmed[0] == '#')
                {
                    commentLines.Add(trimmed.Substring(1));
                    continue;
                }

                // Section header [SectionName]
                if (trimmed[0] == '[')
                {
                    int closeBracket = trimmed.IndexOf(']');
                    if (closeBracket < 0)
                        throw new IniParseException(
                            string.Format("Line {0}: Unclosed section header.", lineNumber));

                    string sectionName = trimmed.Substring(1, closeBracket - 1);
                    if (options.TrimWhitespace)
                        sectionName = sectionName.Trim();

                    // Get or create the section; duplicate sections merge
                    currentSection = data.AddOrGetSection(sectionName);

                    // Attach leading comments to section (append on duplicate)
                    if (commentLines.Count > 0)
                    {
                        currentSection.LeadingComments.AddRange(commentLines);
                        commentLines.Clear();
                    }
                    continue;
                }

                // key=value line (or key with empty value when no '=')
                int eqPos = trimmed.IndexOf('=');
                string key;
                string valueStr;

                if (eqPos >= 0)
                {
                    key = trimmed.Substring(0, eqPos);
                    valueStr = trimmed.Substring(eqPos + 1);
                }
                else
                {
                    // No '=' — treat entire line as key with empty value
                    key = trimmed;
                    valueStr = string.Empty;
                }

                if (options.TrimWhitespace)
                    key = key.Trim();

                // Extract inline comment from value part
                string inlineComment = null;
                int commentPos = FindCommentStart(valueStr, 0, options.AllowQuotedValues);
                if (commentPos >= 0)
                {
                    inlineComment = valueStr.Substring(commentPos + 1).Trim();
                    valueStr = valueStr.Substring(0, commentPos);
                }

                if (options.TrimWhitespace)
                    valueStr = valueStr.Trim();

                // Unquote if needed
                if (options.AllowQuotedValues && IsQuoted(valueStr))
                {
                    valueStr = Unquote(valueStr);
                }

                AddEntryToSection(currentSection, key, valueStr,
                    commentLines, inlineComment, options, lineNumber);
            }

            return data;
        }

        private static void AddEntryToSection(IniSection section, string key, string value,
            List<string> commentLines, string inlineComment,
            IniParseOptions options, int lineNumber)
        {
            // Check for duplicate key
            var existing = section.GetEntry(key);
            if (existing != null)
            {
                switch (options.DuplicateKeyBehavior)
                {
                    case DuplicateKeyBehavior.Throw:
                        throw new IniParseException(
                            string.Format("Line {0}: Duplicate key '{1}' in section '{2}'.",
                                lineNumber, key, section.Name));
                    case DuplicateKeyBehavior.Overwrite:
                        existing.Value = new IniValue(value);
                        if (commentLines.Count > 0)
                        {
                            existing.LeadingComments.Clear();
                            existing.LeadingComments.AddRange(commentLines);
                        }
                        if (inlineComment != null)
                            existing.InlineComment = inlineComment;
                        commentLines.Clear();
                        return;
                    case DuplicateKeyBehavior.Ignore:
                        commentLines.Clear();
                        return;
                }
            }

            // New entry
            var entry = section.Set(key, value);
            if (commentLines.Count > 0)
            {
                entry.LeadingComments.Clear();
                entry.LeadingComments.AddRange(commentLines);
            }
            entry.InlineComment = inlineComment;
            commentLines.Clear();
        }

        /// <summary>
        /// Finds the start of a comment (; or #) outside of quotes within text.
        /// Returns the index of the comment character, or -1 if none found.
        /// </summary>
        internal static int FindCommentStart(string text, int start, bool respectQuotes)
        {
            bool inQuotes = false;
            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];
                if (respectQuotes && c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (!inQuotes && (c == ';' || c == '#'))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns true if the value is wrapped in double quotes.
        /// </summary>
        internal static bool IsQuoted(string value)
        {
            if (value == null || value.Length < 2)
                return false;
            return value[0] == '"' && value[value.Length - 1] == '"';
        }

        /// <summary>
        /// Removes surrounding quotes and unescapes doubled quotes.
        /// </summary>
        internal static string Unquote(string value)
        {
            if (!IsQuoted(value))
                return value;
            return value.Substring(1, value.Length - 2).Replace("\"\"", "\"");
        }
    }

    /// <summary>
    /// Exception thrown when INI parsing fails.
    /// </summary>
    public class IniParseException : Exception
    {
        /// <summary>
        /// Creates an IniParseException with a message.
        /// </summary>
        public IniParseException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates an IniParseException with a message and inner exception.
        /// </summary>
        public IniParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
