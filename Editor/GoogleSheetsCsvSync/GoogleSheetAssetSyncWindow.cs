#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EIGHTAID.EditorTools.GoogleSheets
{
    public sealed class GoogleSheetAssetSyncWindow : EditorWindow
    {
        private const string DefaultConfigPath = "Assets/Editor/GoogleSheetCsvSync/google_sheet_sync_config.txt";

        private string _configPath = DefaultConfigPath;
        private Vector2 _scroll;
        private List<SyncEntry> _entries = new List<SyncEntry>();
        private readonly List<string> _warnings = new List<string>();
        private readonly List<SyncResult> _results = new List<SyncResult>();

        [MenuItem("Window/Tools/Google Sheet Asset Sync")]
        public static void Open()
        {
            var window = GetWindow<GoogleSheetAssetSyncWindow>("Google Sheet Sync");
            window.minSize = new Vector2(760f, 500f);
            window.Show();
        }

        private void OnEnable()
        {
            ReloadConfig();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Google Sheet Asset Sync", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("設定 txt に書いた Google Sheets の edit URL から、保存先拡張子に合わせて CSV または XLSX を取得します。保存先は Assets 配下だけ許可します。", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            _configPath = EditorGUILayout.TextField("Config", _configPath);
            if (GUILayout.Button("Reload", GUILayout.Width(80f)))
            {
                ReloadConfig();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sync All", GUILayout.Height(30f)))
            {
                SyncAll();
            }

            if (GUILayout.Button("Ping Config", GUILayout.Height(30f)))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(_configPath));
            }
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawWarnings();
            DrawEntries();
            DrawResults();
            EditorGUILayout.EndScrollView();
        }

        private void DrawWarnings()
        {
            if (_warnings.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Config Warnings", EditorStyles.boldLabel);
            foreach (string warning in _warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        private void DrawEntries()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField($"Entries ({_entries.Count})", EditorStyles.boldLabel);
            if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox("同期対象がありません。設定 txt を確認してください。", MessageType.None);
                return;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                SyncEntry entry = _entries[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(entry.Name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Sync", GUILayout.Width(80f)))
                {
                    SyncOne(entry);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Format", entry.Format.ToUpperInvariant());
                EditorGUILayout.SelectableLabel(entry.OutputAssetPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight + 2f));
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawResults()
        {
            if (_results.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Last Results", EditorStyles.boldLabel);
            foreach (SyncResult result in _results)
            {
                MessageType type = result.Success ? MessageType.Info : MessageType.Error;
                string state = result.Success ? (result.Changed ? "updated" : "unchanged") : "failed";
                EditorGUILayout.HelpBox($"{result.Name}: {state}\n{result.Message}", type);
            }
        }

        private void ReloadConfig()
        {
            _warnings.Clear();
            _results.Clear();
            _entries = GoogleSheetAssetSyncConfigParser.Load(_configPath, _warnings);
            Repaint();
        }

        private void SyncAll()
        {
            _results.Clear();
            foreach (SyncEntry entry in _entries)
            {
                _results.Add(GoogleSheetAssetSynchronizer.Sync(entry));
            }

            AssetDatabase.Refresh();
        }

        private void SyncOne(SyncEntry entry)
        {
            _results.Clear();
            _results.Add(GoogleSheetAssetSynchronizer.Sync(entry));
            AssetDatabase.Refresh();
        }
    }

    internal sealed class SyncEntry
    {
        public string Name;
        public string SourceUrl;
        public string OutputAssetPath;
        public string Format;
    }

    internal sealed class SyncResult
    {
        public string Name;
        public bool Success;
        public bool Changed;
        public string Message;
    }

    internal static class GoogleSheetAssetSyncConfigParser
    {
        public static List<SyncEntry> Load(string configAssetPath, List<string> warnings)
        {
            var entries = new List<SyncEntry>();
            if (string.IsNullOrWhiteSpace(configAssetPath))
            {
                warnings.Add("Config path is empty.");
                return entries;
            }

            string fullPath = ToFullProjectPath(configAssetPath);
            if (!File.Exists(fullPath))
            {
                warnings.Add($"Config file not found: {configAssetPath}");
                return entries;
            }

            string[] lines = File.ReadAllLines(fullPath, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                List<string> cols = ParseCsvLine(line);
                if (cols.Count < 3)
                {
                    warnings.Add($"Line {i + 1}: expected name,source,output.");
                    continue;
                }

                if (IsHeader(cols))
                {
                    continue;
                }

                string output = NormalizeAssetPath(cols[2]);
                string format = Path.GetExtension(output).TrimStart('.').ToLowerInvariant();
                if (format != "csv" && format != "xlsx")
                {
                    warnings.Add($"Line {i + 1}: unsupported output extension: {output}");
                    continue;
                }

                entries.Add(new SyncEntry
                {
                    Name = cols[0].Trim(),
                    SourceUrl = cols[1].Trim(),
                    OutputAssetPath = output,
                    Format = format
                });
            }

            return entries;
        }

        private static bool IsHeader(List<string> cols)
        {
            return cols.Count >= 3 &&
                   string.Equals(cols[0].Trim(), "name", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(cols[1].Trim(), "source", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(cols[2].Trim(), "output", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Trim().Replace('\\', '/');
        }

        private static string ToFullProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var builder = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        builder.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    result.Add(builder.ToString());
                    builder.Clear();
                }
                else
                {
                    builder.Append(c);
                }
            }

            result.Add(builder.ToString());
            return result;
        }
    }

    internal static class GoogleSheetAssetSynchronizer
    {
        private static readonly Regex SpreadsheetIdRegex = new Regex(@"/spreadsheets/d/([^/]+)", RegexOptions.Compiled);
        private static readonly Regex GidRegex = new Regex(@"[?#&]gid=([0-9]+)", RegexOptions.Compiled);

        public static SyncResult Sync(SyncEntry entry)
        {
            var result = new SyncResult { Name = entry.Name };
            try
            {
                if (!TryBuildExportUrl(entry, out string exportUrl, out string error))
                {
                    result.Message = error;
                    return result;
                }

                if (!IsAllowedOutputPath(entry.OutputAssetPath))
                {
                    result.Message = $"Output must be under Assets/: {entry.OutputAssetPath}";
                    return result;
                }

                byte[] downloaded = DownloadBytes(exportUrl);
                if (downloaded == null || downloaded.Length == 0)
                {
                    result.Message = "Downloaded data is empty.";
                    return result;
                }

                string fullOutputPath = ToFullProjectPath(entry.OutputAssetPath);
                string directory = Path.GetDirectoryName(fullOutputPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                bool changed = !File.Exists(fullOutputPath) || !BytesEqual(File.ReadAllBytes(fullOutputPath), downloaded);
                if (changed)
                {
                    File.WriteAllBytes(fullOutputPath, downloaded);
                    AssetDatabase.ImportAsset(entry.OutputAssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }

                result.Success = true;
                result.Changed = changed;
                result.Message = changed ? $"Saved: {entry.OutputAssetPath}" : $"No changes: {entry.OutputAssetPath}";
                Debug.Log($"[GoogleSheetAssetSync] {entry.Name}: {(changed ? "updated and reimported" : "unchanged")} - {entry.OutputAssetPath}");
                return result;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                Debug.LogError($"[GoogleSheetAssetSync] {entry.Name}: failed - {ex.Message}");
                return result;
            }
        }

        private static bool TryBuildExportUrl(SyncEntry entry, out string exportUrl, out string error)
        {
            exportUrl = string.Empty;
            error = string.Empty;

            Match idMatch = SpreadsheetIdRegex.Match(entry.SourceUrl ?? string.Empty);
            if (!idMatch.Success)
            {
                error = $"Spreadsheet id not found: {entry.SourceUrl}";
                return false;
            }

            string spreadsheetId = idMatch.Groups[1].Value;
            if (entry.Format == "xlsx")
            {
                exportUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=xlsx";
                return true;
            }

            Match gidMatch = GidRegex.Match(entry.SourceUrl ?? string.Empty);
            if (!gidMatch.Success)
            {
                error = $"gid not found for CSV source: {entry.SourceUrl}";
                return false;
            }

            exportUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gidMatch.Groups[1].Value}";
            return true;
        }

        private static byte[] DownloadBytes(string url)
        {
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                return client.DownloadData(url);
            }
        }

        private static bool IsAllowedOutputPath(string assetPath)
        {
            string normalized = (assetPath ?? string.Empty).Replace('\\', '/');
            return normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                   !normalized.Contains("/../") &&
                   !normalized.EndsWith("/..", StringComparison.Ordinal);
        }

        private static string ToFullProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif
