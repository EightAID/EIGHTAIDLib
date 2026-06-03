using TMPro;
using TMP_Ruby;
using UnityEngine;
using UnityEngine.Serialization;

namespace EightAID.EIGHTAIDLib.Localization
{
    /// <summary>
    /// TextMeshProUGUI の表示を EALocalizationService の現在言語に合わせて更新します。
    /// sourceText は元のキー文字列を保持するため、エディタ上で英語表示へ切り替えても日本語キーへ戻せます。
    /// </summary>
    public class EALocalizedText : MonoBehaviour
    {
        [FormerlySerializedAs("_textMeshProUGUI")] [SerializeField] private TextMeshProUGUI textMeshProUGUI;
        [SerializeField, HideInInspector] private string sourceText;

        protected TextMeshProUGUI Text => textMeshProUGUI;
        protected string SourceText => sourceText;

        protected virtual void OnEnable()
        {
            CacheTextComponent();
            CacheSourceTextIfNeeded();
            EALocalizationService.LanguageChanged += HandleLanguageChanged;
            RefreshLocalizedText();
        }

        protected virtual void Start()
        {
            CacheTextComponent();
            CacheSourceTextIfNeeded();
            RefreshLocalizedText();
        }

        protected virtual void OnDisable()
        {
            EALocalizationService.LanguageChanged -= HandleLanguageChanged;
        }

        public virtual void RefreshLocalizedText()
        {
            CacheTextComponent();
            CacheSourceTextIfNeeded();
            if (textMeshProUGUI == null)
            {
                Debug.LogWarning("[EIGHTAID Localization] TextMeshProUGUI が見つかりません。");
                return;
            }

            string resolvedText = EALocalizationService.ResolveUi(sourceText);
            ApplyText(PostProcessResolvedText(resolvedText));
        }

        public void SetSourceText(string value)
        {
            CacheTextComponent();
            sourceText = value;
            RefreshLocalizedText();
        }

        protected virtual string PostProcessResolvedText(string resolvedText)
        {
            return resolvedText;
        }

        private void HandleLanguageChanged(EALocalizationLanguage language)
        {
            RefreshLocalizedText();
        }

        private void CacheTextComponent()
        {
            if (textMeshProUGUI == null)
            {
                textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            }
        }

        private void CacheSourceTextIfNeeded()
        {
            if (textMeshProUGUI != null && string.IsNullOrEmpty(sourceText))
            {
                sourceText = textMeshProUGUI.text;
            }
        }

        private void ApplyText(string value)
        {
            if (textMeshProUGUI.TryGetComponent(out TextMeshProRuby rubyText))
            {
                rubyText.Text = value ?? string.Empty;
                return;
            }

            textMeshProUGUI.text = value ?? string.Empty;
        }
    }
}
