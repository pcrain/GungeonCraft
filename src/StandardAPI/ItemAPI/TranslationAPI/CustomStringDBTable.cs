using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranslationAPI
{
	/// <summary>
	/// Handles custom strings for UI and synergy tables.
	/// </summary>
    public class CustomStringDBTable
    {
		/// <summary>
		/// The string table.
		/// </summary>
		public Dictionary<string, StringTableManager.StringCollection> Table
		{
			get
			{
				Dictionary<string, StringTableManager.StringCollection> result;
				if ((result = _cachedTable) == null)
				{
					result = (_cachedTable = _getTable());
				}
				return result;
			}
		}

		/// <summary>
		/// Gets a string with the key of <paramref name="key"/> from the table.
		/// </summary>
		/// <param name="key">The string's key</param>
		/// <returns>The string with the key of <paramref name="key"/>.</returns>
		public StringTableManager.StringCollection this[string key]
		{
			get
			{
				return Table[key];
			}
		}

		/// <summary>
		/// Sets a string with the key of <paramref name="key"/> in the table.
		/// </summary>
		/// <param name="key">The string's key.</param>
		/// <param name="value">The string value.</param>
		public void SetValue(string key, StringTableManager.StringCollection value)
		{
			Table[key] = value;
			_changes[key] = value;
			JournalEntry.ReloadDataSemaphore++;
			TranslationManager.ForceUpdateTranslation();
		}

		/// <summary>
		/// Builds a new <see cref="CustomStringDBTable"/> with the table get function of <paramref name="getTable"/>.
		/// </summary>
		/// <param name="getTable">The function that gets the table for this <see cref="CustomStringDBTable"/></param>
		public CustomStringDBTable(Func<Dictionary<string, StringTableManager.StringCollection>> getTable)
		{
			_getTable = getTable;
			_changes = new Dictionary<string, StringTableManager.StringCollection>();
		}

		/// <summary>
		/// Returns <see langword="true"/> if the table contains <paramref name="key"/>.
		/// </summary>
		/// <param name="key">The key to check</param>
		/// <returns><see langword="true"/> if the table contains <paramref name="key"/>.</returns>
		public bool ContainsKey(string key)
		{
			return Table.ContainsKey(key);
		}

		/// <summary>
		/// Sets a string with the key of <paramref name="key"/> in the table.
		/// </summary>
		/// <param name="key">The string's key.</param>
		/// <param name="value">The string value.</param>
		public void Set(string key, string value)
		{
			StringTableManager.SimpleStringCollection simpleStringCollection = new StringTableManager.SimpleStringCollection();
			simpleStringCollection.AddString(value, 1f);
			SetValue(key, simpleStringCollection);
		}

		/// <summary>
		/// Gets a string with the key of <paramref name="key"/>.
		/// </summary>
		/// <param name="key">The string's key.</param>
		/// <returns>The string with the key of <paramref name="key"/>.</returns>
		public string Get(string key)
		{
			return StringTableManager.GetString(key);
		}

		/// <summary>
		/// Applies all the string changes to the tables of the new language.
		/// </summary>
		public void LanguageChanged()
		{
			_cachedTable = null;
			Dictionary<string, StringTableManager.StringCollection> table = Table;
			foreach (KeyValuePair<string, StringTableManager.StringCollection> keyValuePair in _changes)
			{
				table[keyValuePair.Key] = keyValuePair.Value;
			}
		}

		private readonly Func<Dictionary<string, StringTableManager.StringCollection>> _getTable;
		private readonly Dictionary<string, StringTableManager.StringCollection> _changes;
		private Dictionary<string, StringTableManager.StringCollection> _cachedTable;
	}
}
