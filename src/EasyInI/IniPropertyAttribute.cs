using System;

namespace EasyInI
{
    /// <summary>
    /// Maps a class property to an INI key.
    /// Optionally specifies the target section name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IniPropertyAttribute : Attribute
    {
        /// <summary>
        /// Gets the INI key name that this property maps to.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the INI section name for this property.
        /// If not set, the section from <see cref="IniSectionAttribute"/>
        /// on the class is used, or the class name itself as a fallback.
        /// </summary>
        public string Section { get; set; }

        /// <summary>
        /// Maps a property to an INI key with the given name.
        /// </summary>
        /// <param name="name">The INI key name.</param>
        public IniPropertyAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Specifies the default INI section name for a class when none is
    /// specified on individual properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IniSectionAttribute : Attribute
    {
        /// <summary>
        /// Gets the section name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Sets the default section for all properties in this class.
        /// </summary>
        public IniSectionAttribute(string name)
        {
            Name = name;
        }
    }
}
