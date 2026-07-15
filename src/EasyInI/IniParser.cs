using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EasyIni
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
            return ParseFile(filePath, null, options);
        }

        /// <summary>
        /// Parses an INI file with the specified encoding.
        /// </summary>
        /// <param name="filePath">Path to the INI file.</param>
        /// <param name="encoding">Text encoding of the file. If null, encoding is auto-detected
        /// from the file's byte order mark (BOM).</param>
        /// <param name="options">Parse options, or null for defaults.</param>
        /// <returns>Parsed IniData instance.</returns>
        public static IniData ParseFile(string filePath, Encoding encoding, IniParseOptions options)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");
            if (options == null)
                options = IniParseOptions.Default;

            IniData data;

            if (encoding != null)
            {
                // User specified encoding — use it directly, BOM detection on for safety
                using (var reader = new StreamReader(filePath, encoding, true))
                {
                    data = ParseReader(reader, options);
                }
                data.FileEncoding = encoding;
            }
            else
            {
                // Auto-detect encoding from file BOM (single open: detect + read)
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bool hasUtf8Bom;
                    Encoding detected = DetectFileEncoding(fs, out hasUtf8Bom);
                    // Stream is now positioned past the BOM (or at 0 if none)

                    using (var reader = new StreamReader(fs, detected))
                    {
                        data = ParseReader(reader, options);
                    }

                    // Preserve encoding for round-trip:
                    // UTF-8 without BOM → use UTF8Encoding(false) to avoid adding BOM on save
                    // All other cases → use the detected encoding as-is
                    if (detected is UTF8Encoding && !hasUtf8Bom)
                        data.FileEncoding = new UTF8Encoding(false);
                    else
                        data.FileEncoding = detected;
                }
            }

            return data;
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

        /// <summary>
        /// Detects file encoding by reading the byte order mark (BOM) from the stream.
        /// After returning, the stream position is advanced past the BOM (or reset to 0 if no BOM).
        /// Supported: UTF-8, UTF-16 LE, UTF-16 BE, UTF-32 LE.
        /// </summary>
        /// <param name="stream">An open, seekable file stream.</param>
        /// <param name="hasUtf8Bom">True if a UTF-8 BOM was detected.</param>
        /// <returns>The detected encoding. Defaults to UTF-8 if no BOM is found.</returns>
        private static Encoding DetectFileEncoding(FileStream stream, out bool hasUtf8Bom)
        {
            hasUtf8Bom = false;

            byte[] bom = new byte[4];
            int total = 0, n;
            while (total < 4 && (n = stream.Read(bom, total, 4 - total)) > 0)
                total += n;

            int bomLen = 0;
            Encoding result;

            // UTF-32 LE: FF FE 00 00 (check before UTF-16 LE)
            if (total >= 4 && bom[0] == 0xFF && bom[1] == 0xFE
                && bom[2] == 0x00 && bom[3] == 0x00)
            {
                bomLen = 4;
                result = Encoding.UTF32;
            }
            // UTF-16 BE: FE FF
            else if (total >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
            {
                bomLen = 2;
                result = Encoding.BigEndianUnicode;
            }
            // UTF-16 LE: FF FE
            else if (total >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
            {
                bomLen = 2;
                result = Encoding.Unicode;
            }
            // UTF-8: EF BB BF
            else if (total >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                hasUtf8Bom = true;
                bomLen = 3;
                result = Encoding.UTF8;
            }
            // No BOM
            else
            {
                bomLen = 0;
                result = Encoding.UTF8;
            }

            // Position stream past the BOM (or back to 0 if none)
            stream.Seek(bomLen, SeekOrigin.Begin);
            return result;
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
