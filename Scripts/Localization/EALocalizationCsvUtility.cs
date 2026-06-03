using System.Collections.Generic;
using System.Text;

namespace EightAID.EIGHTAIDLib.Localization
{
    /// <summary>
    /// ローカライズ CSV の読み書きで使う最小限の CSV ユーティリティです。
    /// ダブルクォート、カンマ、改行を含むセルを扱えます。
    /// </summary>
    public static class EALocalizationCsvUtility
    {
        public static List<string[]> Parse(string csvText)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(csvText))
            {
                return rows;
            }

            var row = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char c = csvText[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        bool isEscapedQuote = i + 1 < csvText.Length && csvText[i + 1] == '"';
                        if (isEscapedQuote)
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(cell.ToString());
                        cell.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(cell.ToString());
                        cell.Clear();
                        rows.Add(row.ToArray());
                        row = new List<string>();
                        break;
                    default:
                        cell.Append(c);
                        break;
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row.ToArray());
            }

            return rows;
        }

        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            bool requiresQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            if (!requiresQuote)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
