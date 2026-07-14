using System;

namespace EasyIni
{
    /// <summary>
    /// Specifies how to handle duplicate keys within a section during parsing.
    /// </summary>
    public enum DuplicateKeyBehavior
    {
        /// <summary>
        /// The last occurrence of a key overwrites earlier ones.
        /// </summary>
        Overwrite,

        /// <summary>
        /// A duplicate key causes a parse exception.
        /// </summary>
        Throw,

        /// <summary>
        /// The first occurrence is kept; later duplicates are ignored.
        /// </summary>
        Ignore
    }

    /// <summary>
    /// Configuration options for INI parsing and serialization behavior.
    /// </summary>
    public class IniParseOptions
    {
        /// <summary>
        /// Gets or sets whether section and key names are case-sensitive.
        /// Default: false (case-insensitive).
        /// </summary>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// Gets or sets how duplicate keys within a section are handled.
        /// Default: Overwrite.
        /// </summary>
        public DuplicateKeyBehavior DuplicateKeyBehavior { get; set; }

        /// <summary>
        /// Gets or sets whether values wrapped in double quotes have their quotes
        /// stripped during parsing. Default: true.
        /// </summary>
        public bool AllowQuotedValues { get; set; }

        /// <summary>
        /// Gets or sets whether leading and trailing whitespace is trimmed
        /// from keys and unquoted values. Default: true.
        /// </summary>
        public bool TrimWhitespace { get; set; }

        /// <summary>
        /// Gets the default parse options (case-insensitive, overwrite duplicates,
        /// allow quotes, trim whitespace).
        /// </summary>
        public static IniParseOptions Default
        {
            get
            {
                return new IniParseOptions
                {
                    CaseSensitive = false,
                    DuplicateKeyBehavior = DuplicateKeyBehavior.Overwrite,
                    AllowQuotedValues = true,
                    TrimWhitespace = true
                };
            }
        }

        /// <summary>
        /// Creates a new instance with default values.
        /// </summary>
        public IniParseOptions()
        {
            CaseSensitive = false;
            DuplicateKeyBehavior = DuplicateKeyBehavior.Overwrite;
            AllowQuotedValues = true;
            TrimWhitespace = true;
        }

        /// <summary>
        /// Returns the string comparer implied by CaseSensitive setting.
        /// </summary>
        internal StringComparer GetKeyComparer()
        {
            return CaseSensitive
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;
        }
    }
}
