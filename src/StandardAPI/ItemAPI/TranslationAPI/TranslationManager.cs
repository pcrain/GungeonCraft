using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace TranslationAPI
{
    /// <summary>
    /// The core class of TranslationAPI that can add new translations.
    /// </summary>
    public static class TranslationManager
    {
        /// <summary>
        /// Inits TranslationAPI. You're not required to call this method, it should be called automatically when you try to add a translation.
        /// </summary>
        public static void Init()
        {
            if (m_initialized)
            {
                return;
            }
            translations = new List<Translation>();
            foreach (StringTableManager.GungeonSupportedLanguages language in Enum.GetValues(typeof(StringTableManager.GungeonSupportedLanguages)))
            {
                if (language != StringTableManager.GungeonSupportedLanguages.RUBEL_TEST && language != StringTableManager.GungeonSupportedLanguages.ENGLISH)
                {
                    translations.Add(new Translation
                    {
                        language = language,
                        strings = new Dictionary<string, StringTableManager.StringCollection>(),
                        getTable = () => StringTableManager.CoreTable,
                        type = StringTableType.Core
                    });
                    translations.Add(new Translation
                    {
                        language = language,
                        strings = new Dictionary<string, StringTableManager.StringCollection>(),
                        getTable = () => StringTableManager.ItemTable,
                        type = StringTableType.Items
                    });
                    translations.Add(new Translation
                    {
                        language = language,
                        strings = new Dictionary<string, StringTableManager.StringCollection>(),
                        getTable = () => StringTableManager.EnemyTable,
                        type = StringTableType.Enemies
                    });
                    translations.Add(new Translation
                    {
                        language = language,
                        strings = new Dictionary<string, StringTableManager.StringCollection>(),
                        getTable = () => UITable,
                        type = StringTableType.UI
                    });
                    translations.Add(new Translation
                    {
                        language = language,
                        strings = new Dictionary<string, StringTableManager.StringCollection>(),
                        getTable = () => StringTableManager.IntroTable,
                        type = StringTableType.Intro
                    });
                    translations.Add(new Translation
                    {
                        language = language,
                        strings = new Dictionary<string, StringTableManager.StringCollection>(),
                        getTable = () => SynergyTable,
                        type = StringTableType.Synergy
                    });
                }
            }
            UIStrings = new CustomStringDBTable(() => UITable);
            SynergyStrings = new CustomStringDBTable(() => SynergyTable);
            ETGMod.Databases.Strings.OnLanguageChanged += DoTranslation;
            SetValueHook = new Hook(typeof(StringDBTable).GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetSetMethod(), typeof(TranslationManager).GetMethod("TableSetValueHook", BindingFlags.Static | BindingFlags.NonPublic));
            m_initialized = true;
        }

        /// <summary>
        /// Unloads TranslationAPI and removes all added translations.
        /// </summary>
        public static void Unload()
        {
            if (!m_initialized)
            {
                return;
            }
            translations?.Clear();
            translations = null;
            UIStrings = null;
            SynergyStrings = null;
            ETGMod.Databases.Strings.OnLanguageChanged -= DoTranslation;
            SetValueHook?.Dispose();
            m_initialized = false;
            StringTableManager.SetNewLanguage(StringTableManager.CurrentLanguage, true);
        }

        private static void TableSetValueHook(Action<StringDBTable, string, StringTableManager.StringCollection> orig, StringDBTable self, string key, StringTableManager.StringCollection value)
        {
            orig(self, key, value);
            ForceUpdateTranslation();
        }

        /// <summary>
        /// Adds a new translation for <paramref name="item"/>'s name.
        /// </summary>
        /// <param name="item">The <see cref="PickupObject"/> to add the translation to.</param>
        /// <param name="language">The language to which the translation will be applied.</param>
        /// <param name="translation">The translated text.</param>
        public static void TranslateItemName(this PickupObject item, StringTableManager.GungeonSupportedLanguages language, string translation)
        {
            item.encounterTrackable?.TranslateName(language, translation);
        }

        /// <summary>
        /// Adds a new translation for <paramref name="item"/>'s short description.
        /// </summary>
        /// <param name="item">The <see cref="PickupObject"/> to add the translation to.</param>
        /// <param name="language">The language to which the translation will be applied.</param>
        /// <param name="translation">The translated text.</param>
        public static void TranslateItemShortDescription(this PickupObject item, StringTableManager.GungeonSupportedLanguages language, string translation)
        {
            item.encounterTrackable?.TranslateShortDescription(language, translation);
        }

        /// <summary>
        /// Adds a new translation for <paramref name="item"/>'s long description.
        /// </summary>
        /// <param name="item">The <see cref="PickupObject"/> to add the translation to.</param>
        /// <param name="language">The language to which the translation will be applied.</param>
        /// <param name="translation">The translated text.</param>
        public static void TranslateItemLongDescription(this PickupObject item, StringTableManager.GungeonSupportedLanguages language, string translation)
        {
            item.encounterTrackable?.TranslateLongDescription(language, translation);
        }

        /// <summary>
        /// Adds a new translation for <paramref name="enemy"/>'s name.
        /// </summary>
        /// <param name="enemy">The <see cref="AIActor"/> to add the translation to.</param>
        /// <param name="language">The language to which the translation will be applied.</param>
        /// <param name="translation">The translated text.</param>
        public static void TranslateEnemyName(this AIActor enemy, StringTableManager.GungeonSupportedLanguages language, string translation)
        {
            enemy.encounterTrackable?.TranslateName(language, translation);
        }

        /// <summary>
        /// Adds a new translation for <paramref name="enemy"/>'s short description.
        /// </summary>
        /// <param name="enemy">The <see cref="AIActor"/> to add the translation to.</param>
        /// <param name="language">The language to which the translation will be applied.</param>
        /// <param name="translation">The translated text.</param>
        public static void TranslateEnemyShortDescription(this AIActor enemy, StringTableManager.GungeonSupportedLanguages language, string translation)
        {
            enemy.encounterTrackable?.TranslateShortDescription(language, translation);
        }

        /// <summary>
        /// Adds a new translation for <paramref name="enemy"/>'s long description.
        /// </summary>
        /// <param name="enemy">The <see cref="AIActor"/> to add the translation to.</param>
        /// <param name="language">The language to which the translation will be applied.</param>
        /// <param name="translation">The translated text.</param>
        public static void TranslateEnemyLongDescription(this AIActor enemy, StringTableManager.GungeonSupportedLanguages language, string translation)
        {
            enemy.encounterTrackable?.TranslateLongDescription(language, translation);
        }

        /// <summary>
        /// Adds a new translation for <paramref name="track"/>'s name.
        /// </summary>
        /// <param name="track">The <see cref="EncounterTrackable"/> to add the translation to.</param>
        /// <param name="language">The language to which the translation will be applied.</param>
        /// <param name="translation">The translated text.</param>
        public static void TranslateName(this EncounterTrackable track, StringTableManager.GungeonSupportedLanguages language, string translation)
        {
            AddTranslation(language, track.journalData.PrimaryDisplayName, translation, StringTableType.Items);
        }

        /// <summary>
        /// Adds a new translation for <paramref name="track"/>'s short description.
        /// </summary>
        /// <param name="track">The <see cref="EncounterTrackable"/> to add the translation to.</param>
        /// <param name="language">The language to which the translation will be applied.</param>
        /// <param name="translation">The translated text.</param>
        public static void TranslateShortDescription(this EncounterTrackable track, StringTableManager.GungeonSupportedLanguages language, string translation)
        {
            AddTranslation(language, track.journalData.NotificationPanelDescription, translation, StringTableType.Items);
        }

        /// <summary>
        /// Adds a new translation for <paramref name="track"/>'s long description.
        /// </summary>
        /// <param name="track">The <see cref="EncounterTrackable"/> to add the translation to.</param>
        /// <param name="language">The language to which the translation will be applied.</param>
        /// <param name="translation">The translated text.</param>
        public static void TranslateLongDescription(this EncounterTrackable track, StringTableManager.GungeonSupportedLanguages language, string translation)
        {
            AddTranslation(language, track.journalData.AmmonomiconFullEntry, translation, StringTableType.Items);
        }

        /// <summary>
        /// Adds a new translation for an individual string.
        /// </summary>
        /// <param name="language">The language to which the translation will be applied.</param>
        /// <param name="key">The string's key</param>
        /// <param name="value">The string</param>
        /// <param name="tableType">Type of the table in which <paramref name="value"/> is in.</param>
        public static void AddTranslation(StringTableManager.GungeonSupportedLanguages language, string key, string value, StringTableType tableType)
        {
            if(translations == null)
            {
                Init();
            }
            if(language == StringTableManager.GungeonSupportedLanguages.ENGLISH)
            {
                switch (tableType)
                {
                    case StringTableType.Core:
                        ETGMod.Databases.Strings.Core.Set(key, value);
                        break;
                    case StringTableType.Items:
                        ETGMod.Databases.Strings.Items.Set(key, value);
                        break;
                    case StringTableType.Enemies:
                        ETGMod.Databases.Strings.Enemies.Set(key, value);
                        break;
                    case StringTableType.UI:
                        UIStrings.Set(key, value);
                        break;
                    case StringTableType.Intro:
                        ETGMod.Databases.Strings.Intro.Set(key, value);
                        break;
                    case StringTableType.Synergy:
                        SynergyStrings.Set(key, value);
                        break;
                }
                return;
            }
            foreach(Translation translation in translations)
            {
                if(translation.language == language && translation.type == tableType)
                {
                    translation.AddStringTranslation(key, value);
                }
            }
        }

        private static void DoTranslation(StringTableManager.GungeonSupportedLanguages lang)
        {
            UIStrings.LanguageChanged();
            SynergyStrings.LanguageChanged();
            foreach(Translation translation in translations)
            {
                translation.UpdateLanguage(lang);
            }
        }

        /// <summary>
        /// Force updates the translations.
        /// </summary>
        public static void ForceUpdateTranslation()
        {
            foreach (Translation translation in translations)
            {
                translation.UpdateLanguage();
            }
        }

        /// <summary>
        /// The table with all the synergy strings.
        /// </summary>
        public static Dictionary<string, StringTableManager.StringCollection> SynergyTable
        {
            get
            {
                StringTableManager.GetSynergyString("#PRISMATISM_IS_BAD");
                return typeof(StringTableManager).GetField("m_synergyTable", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as Dictionary<string, StringTableManager.StringCollection>;
            }
        }

        /// <summary>
        /// The table with all the UI strings.
        /// </summary>
        public static Dictionary<string, StringTableManager.StringCollection> UITable
        {
            get
            {
                StringTableManager.GetSynergyString("#DOWNLOAD_ETG_HARDMODE");
                return typeof(StringTableManager).GetField("m_uiTable", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as Dictionary<string, StringTableManager.StringCollection>;
            }
        }

        /// <summary>
        /// The list of all table translations.
        /// </summary>
        public static List<Translation> translations;
        /// <summary>
        /// The <see cref="CustomStringDBTable"/> for UI strings.
        /// </summary>
        public static CustomStringDBTable UIStrings;
        /// <summary>
        /// The <see cref="CustomStringDBTable"/> for synergy strings.
        /// </summary>
        public static CustomStringDBTable SynergyStrings;
        private static Hook SetValueHook;
        private static bool m_initialized;
    }
}
