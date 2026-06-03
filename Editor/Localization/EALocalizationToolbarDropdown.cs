using EightAID.EIGHTAIDLib.Localization;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using YujiAp.UnityToolbarExtension.Editor;

namespace EightAID.EIGHTAIDLib.Editor.Localization
{
    /// <summary>
    /// Unity エディタ上部に表示するローカライズ言語切り替えです。
    /// 選択した瞬間に PlayerPrefs 保存、言語変更通知、開いているシーンのテキスト再描画を行います。
    /// </summary>
    public class EALocalizationToolbarDropdown : IToolbarElement
    {
        private static readonly string[] LanguageLabels = { "日本語", "English" };
        private static EALocalizationLanguage _selectedLanguage = EALocalizationService.CurrentLanguage;

        public ToolbarElementLayoutType DefaultLayoutType => ToolbarElementLayoutType.RightSideRightAlign;

        public VisualElement CreateElement()
        {
            var dropdown = new EditorToolbarDropdown();
            dropdown.name = "EALocalizationToolbarDropdown";
            dropdown.tooltip = "ローカライズ表示言語";
            _selectedLanguage = EALocalizationService.CurrentLanguage;
            dropdown.text = GetLanguageLabel(_selectedLanguage);
            dropdown.clicked += () => OpenMenu(dropdown);
            return dropdown;
        }

        private static void OpenMenu(EditorToolbarDropdown dropdown)
        {
            var menu = new GenericMenu();
            for (int i = 0; i < LanguageLabels.Length; i++)
            {
                EALocalizationLanguage language = (EALocalizationLanguage)i;
                bool isCurrent = _selectedLanguage == language;
                menu.AddItem(new GUIContent(LanguageLabels[i]), isCurrent, () =>
                {
                    SetSelectedLanguage(language, dropdown);
                });
            }

            menu.ShowAsContext();
        }

        private static void SetSelectedLanguage(EALocalizationLanguage language, EditorToolbarDropdown dropdown)
        {
            _selectedLanguage = language;
            dropdown.text = GetLanguageLabel(language);
            ApplyLanguage(language);
        }

        private static void ApplyLanguage(EALocalizationLanguage language)
        {
            EALocalizationService.SetLanguage(language);

            foreach (EALocalizedText localize in Resources.FindObjectsOfTypeAll<EALocalizedText>())
            {
                if (localize == null || !localize.gameObject.scene.IsValid())
                {
                    continue;
                }

                localize.RefreshLocalizedText();
                EditorUtility.SetDirty(localize);
            }

            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            InternalEditorUtility.RepaintAllViews();
        }

        private static string GetLanguageLabel(EALocalizationLanguage language)
        {
            int index = (int)language;
            if (index < 0 || index >= LanguageLabels.Length)
            {
                return LanguageLabels[0];
            }

            return LanguageLabels[index];
        }
    }
}
