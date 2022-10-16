using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranslationAPI
{
    /// <summary>
    /// Handles translations for various string tables of various languages.
    /// </summary>
    public class Translation
    {
        /// <summary>
        /// The current language's string table.
        /// </summary>
        public Dictionary<string, StringTableManager.StringCollection> Table
        {
            get
            {
                if(currentTable != null)
                {
                    return currentTable;
                }
                currentTable = getTable();
                return currentTable;
            }
        }
        
        /// <summary>
        /// If the current language or <paramref name="overrideLang"/> are equal to <see cref="language"/> of this translation, apply the translation changes.
        /// </summary>
        /// <param name="overrideLang">If not <see langword="null"/>, will be used instead of the current language to check if the translations should be applied.</param>
        /// <returns></returns>
        public bool UpdateLanguage(StringTableManager.GungeonSupportedLanguages? overrideLang = null)
        {
            if((overrideLang ?? StringTableManager.CurrentLanguage) == language)
            {
                int i = 0;
                foreach(KeyValuePair<string, StringTableManager.StringCollection> pair in strings)
                {
                    i++;
                    Table[pair.Key] = pair.Value;
                }
                if(i > 0)
                {
                    JournalEntry.ReloadDataSemaphore++;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a new string to this translation and if the current language is equal to <see cref="language"/> of this translation, applies the change.
        /// </summary>
        /// <param name="key">The key to string.</param>
        /// <param name="value">The translated string.</param>
        public void AddStringTranslation(string key, string value)
        {
            StringTableManager.SimpleStringCollection collection = new StringTableManager.SimpleStringCollection();
            collection.AddString(value, 1f);
            strings[key] = collection;
            if(StringTableManager.CurrentLanguage == language)
            {
                Table[key] = collection;
                JournalEntry.ReloadDataSemaphore++;
            }
        }

        /// <summary>
        /// Type of table this translation applies to.
        /// </summary>
        public StringTableType type;
        /// <summary>
        /// The language this translation applies to.
        /// </summary>
        public StringTableManager.GungeonSupportedLanguages language;
        /// <summary>
        /// Translated strings.
        /// </summary>
        public Dictionary<string, StringTableManager.StringCollection> strings;
        /// <summary>
        /// Method that gets the current language's string table.
        /// </summary>
        public Func<Dictionary<string, StringTableManager.StringCollection>> getTable;
        private Dictionary<string, StringTableManager.StringCollection> currentTable;
    }
}
