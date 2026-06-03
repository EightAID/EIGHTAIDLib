using TMPro;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Localization
{
    /// <summary>
    /// スクリプトから TextMeshProUGUI に文言を入れるときの補助 API です。
    /// EALocalizedText が付いている場合は元テキストを更新し、ない場合は解決済みテキストを直接入れます。
    /// </summary>
    public static class EALocalizedTextBinder
    {
        public static void ApplyText(TextMeshProUGUI text, string sourceText)
        {
            if (text == null)
            {
                return;
            }

            EALocalizedText localize = text.GetComponent<EALocalizedText>();
            if (localize != null)
            {
                localize.SetSourceText(sourceText);
                return;
            }

            text.text = EALocalizationService.ResolveUi(sourceText);
        }
    }
}
