using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranslationAPI
{
    /// <summary>
    /// Type of string tables.
    /// </summary>
    public enum StringTableType
    {
        /// <summary>
        /// Core strings.
        /// </summary>
        Core,
        /// <summary>
        /// Item strings.
        /// </summary>
        Items,
        /// <summary>
        /// Enemy strings.
        /// </summary>
        Enemies,
        /// <summary>
        /// UI strings.
        /// </summary>
        UI,
        /// <summary>
        /// Intro strings.
        /// </summary>
        Intro,
        /// <summary>
        /// Synergy strings.
        /// </summary>
        Synergy
    }
}
