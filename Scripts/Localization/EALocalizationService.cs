using System;
using System.Collections.Generic;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Localization
{
    /// <summary>
    /// Resources 上の CSV を使って、UI テキストの現在言語と解決結果を管理します。
    /// プロジェクト側は起動時や初回参照前に ConfigureUiTable を呼ぶと、CSV パスや列番号を差し替えられます。
    /// </summary>
    public static class EALocalizationService
    {
        private const string DefaultUiCsvResourcePath = "Localize/TextDatas/UITextLocalization";
        private const string DefaultLanguagePrefKey = "localization.language";

        private static readonly Dictionary<string, string> UiSourceToEnglish = new Dictionary<string, string>();

        private static string _uiCsvResourcePath = DefaultUiCsvResourcePath;
        private static string _languagePrefKey = DefaultLanguagePrefKey;
        private static int _uiSourceColumnIndex;
        private static int _uiEnglishColumnIndex = 1;
        private static bool _loaded;
        private static EALocalizationLanguage _currentLanguage;

        public static event Action<EALocalizationLanguage> LanguageChanged;

        public static EALocalizationLanguage CurrentLanguage
        {
            get
            {
                EnsureLoaded();
                return _currentLanguage;
            }
        }

        public static void ConfigureUiTable(
            string uiCsvResourcePath,
            int sourceColumnIndex = 0,
            int englishColumnIndex = 1,
            string languagePrefKey = DefaultLanguagePrefKey)
        {
            _uiCsvResourcePath = string.IsNullOrEmpty(uiCsvResourcePath) ? DefaultUiCsvResourcePath : uiCsvResourcePath;
            _uiSourceColumnIndex = Mathf.Max(0, sourceColumnIndex);
            _uiEnglishColumnIndex = Mathf.Max(0, englishColumnIndex);
            _languagePrefKey = string.IsNullOrEmpty(languagePrefKey) ? DefaultLanguagePrefKey : languagePrefKey;
            Reload();
        }

        public static void Reload()
        {
            _loaded = false;
            EnsureLoaded();
        }

        public static void SetLanguage(EALocalizationLanguage language)
        {
            EnsureLoaded();
            if (_currentLanguage == language)
            {
                return;
            }

            _currentLanguage = language;
            PlayerPrefs.SetInt(_languagePrefKey, (int)_currentLanguage);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke(_currentLanguage);
        }

        public static string ResolveUi(string sourceText)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(sourceText) || _currentLanguage == EALocalizationLanguage.Japanese)
            {
                return sourceText;
            }

            return UiSourceToEnglish.TryGetValue(sourceText, out string localizedText)
                ? localizedText
                : sourceText;
        }

        public static bool HasUiKey(string sourceText)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(sourceText) && UiSourceToEnglish.ContainsKey(sourceText);
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            _currentLanguage = (EALocalizationLanguage)PlayerPrefs.GetInt(_languagePrefKey, (int)EALocalizationLanguage.Japanese);
            LoadUiTable();
        }

        private static void LoadUiTable()
        {
            UiSourceToEnglish.Clear();
            TextAsset csv = Resources.Load<TextAsset>(_uiCsvResourcePath);
            if (csv == null)
            {
                Debug.LogWarning($"[EIGHTAID Localization] UI csv was not found. path={_uiCsvResourcePath}");
                return;
            }

            List<string[]> rows = EALocalizationCsvUtility.Parse(csv.text);
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                string key = GetCell(row, _uiSourceColumnIndex);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                UiSourceToEnglish[key] = GetCell(row, _uiEnglishColumnIndex);
            }
        }

        private static string GetCell(string[] row, int index)
        {
            return index >= 0 && index < row.Length ? row[index] : string.Empty;
        }
    }
}
