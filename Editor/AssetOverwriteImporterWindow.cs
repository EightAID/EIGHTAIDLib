using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Editor
{
    public sealed class AssetOverwriteImporterWindow : EditorWindow
    {
        private const string BackupRoot = "Assets/_AssetOverwriteBackups";

        private readonly List<DroppedFile> _droppedFiles = new();
        private readonly List<OverwriteCandidate> _candidates = new();
        private Vector2 _scrollPos;
        private bool _createBackup = true;
        private bool _searchOnlySelectionFolder;

        private sealed class DroppedFile
        {
            public string sourcePath;
            public string fileName;
        }

        private sealed class OverwriteCandidate
        {
            public DroppedFile droppedFile;
            public List<string> assetPaths = new();
            public int selectedIndex;
        }

        [MenuItem("Tools/EIGHTAID/Asset Overwrite Importer")]
        private static void Open()
        {
            GetWindow<AssetOverwriteImporterWindow>("Asset Overwrite").minSize = new Vector2(520f, 420f);
        }

        private void OnGUI()
        {
            GUILayout.Label("Asset Overwrite Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Drop external files here. Existing Assets with the same file name and extension can be overwritten without changing their .meta files.",
                MessageType.Info);

            _createBackup = EditorGUILayout.Toggle("Create Backup", _createBackup);
            _searchOnlySelectionFolder = EditorGUILayout.Toggle("Search Selection Folder Only", _searchOnlySelectionFolder);

            DrawDropArea();

            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _droppedFiles.Count > 0;
                if (GUILayout.Button("Clear", GUILayout.Width(100f)))
                {
                    ClearFiles();
                }

                GUILayout.FlexibleSpace();

                GUI.enabled = HasOverwritableCandidates();
                if (GUILayout.Button("Overwrite Selected", GUILayout.Width(160f), GUILayout.Height(28f)))
                {
                    OverwriteSelected();
                }

                GUI.enabled = true;
            }

            EditorGUILayout.Space(8f);
            DrawCandidateList();
        }

        private void DrawDropArea()
        {
            Rect dropRect = GUILayoutUtility.GetRect(0f, 92f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drop files here", EditorStyles.helpBox);

            var labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            };
            GUI.Label(dropRect, "Drop files here", labelStyle);

            HandleDragAndDrop(Event.current, dropRect);

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select Files...", GUILayout.Width(140f)))
                {
                    SelectFilesWithDialog();
                }
            }
        }

        private void DrawCandidateList()
        {
            if (_candidates.Count == 0)
            {
                EditorGUILayout.HelpBox("No files are queued.", MessageType.None);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < _candidates.Count; i++)
            {
                OverwriteCandidate candidate = _candidates[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(candidate.droppedFile.fileName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Source", candidate.droppedFile.sourcePath);

                    if (candidate.assetPaths.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No matching asset was found.", MessageType.Warning);
                        continue;
                    }

                    string[] options = candidate.assetPaths.ToArray();
                    candidate.selectedIndex = Mathf.Clamp(candidate.selectedIndex, 0, options.Length - 1);
                    candidate.selectedIndex = EditorGUILayout.Popup("Overwrite Target", candidate.selectedIndex, options);

                    if (candidate.assetPaths.Count > 1)
                    {
                        EditorGUILayout.HelpBox("Multiple matching assets were found. Select the target to overwrite.", MessageType.Info);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void HandleDragAndDrop(Event currentEvent, Rect dropRect)
        {
            if (currentEvent == null)
            {
                return;
            }

            if (currentEvent.type != EventType.DragUpdated && currentEvent.type != EventType.DragPerform)
            {
                return;
            }

            if (!dropRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            currentEvent.Use();

            if (currentEvent.type != EventType.DragPerform)
            {
                return;
            }

            DragAndDrop.AcceptDrag();
            AddDroppedFiles(DragAndDrop.paths);
        }

        private void SelectFilesWithDialog()
        {
            string path = EditorUtility.OpenFilePanel("Select asset file", string.Empty, string.Empty);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            AddDroppedFiles(new[] { path });
        }

        private void AddDroppedFiles(string[] paths)
        {
            bool changed = false;

            for (int i = 0; i < paths.Length; i++)
            {
                string sourcePath = paths[i];
                if (string.IsNullOrEmpty(sourcePath) || Directory.Exists(sourcePath) || !File.Exists(sourcePath))
                {
                    continue;
                }

                string fullPath = Path.GetFullPath(sourcePath);
                if (_droppedFiles.Any(file => string.Equals(file.sourcePath, fullPath, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                _droppedFiles.Add(new DroppedFile
                {
                    sourcePath = fullPath,
                    fileName = Path.GetFileName(fullPath)
                });
                changed = true;
            }

            if (changed)
            {
                RefreshCandidates();
            }
        }

        private void RefreshCandidates()
        {
            _candidates.Clear();
            string[] searchRoots = GetSearchRoots();

            for (int i = 0; i < _droppedFiles.Count; i++)
            {
                DroppedFile droppedFile = _droppedFiles[i];
                var candidate = new OverwriteCandidate
                {
                    droppedFile = droppedFile,
                    assetPaths = FindMatchingAssetPaths(droppedFile.fileName, searchRoots)
                };
                _candidates.Add(candidate);
            }

            Repaint();
        }

        private string[] GetSearchRoots()
        {
            if (!_searchOnlySelectionFolder)
            {
                return new[] { "Assets" };
            }

            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(selectedPath))
            {
                return new[] { "Assets" };
            }

            if (File.Exists(selectedPath))
            {
                selectedPath = Path.GetDirectoryName(selectedPath)?.Replace('\\', '/') ?? "Assets";
            }

            if (!AssetDatabase.IsValidFolder(selectedPath))
            {
                return new[] { "Assets" };
            }

            return new[] { selectedPath };
        }

        private static List<string> FindMatchingAssetPaths(string fileName, string[] searchRoots)
        {
            string assetName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string[] guids = AssetDatabase.FindAssets(assetName, searchRoots);
            var matches = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                if (!string.Equals(Path.GetFileName(assetPath), fileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(Path.GetExtension(assetPath), extension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matches.Add(assetPath);
            }

            matches.Sort(StringComparer.OrdinalIgnoreCase);
            return matches;
        }

        private bool HasOverwritableCandidates()
        {
            for (int i = 0; i < _candidates.Count; i++)
            {
                if (_candidates[i].assetPaths.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void OverwriteSelected()
        {
            var targets = _candidates.Where(candidate => candidate.assetPaths.Count > 0).ToList();
            if (targets.Count == 0)
            {
                return;
            }

            bool overwrite = EditorUtility.DisplayDialog(
                "Overwrite Assets",
                $"Overwrite {targets.Count} asset(s)? Existing .meta files will be kept.",
                "Overwrite",
                "Cancel");
            if (!overwrite)
            {
                return;
            }

            string backupFolder = _createBackup ? CreateBackupFolder() : string.Empty;
            int overwriteCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < targets.Count; i++)
                {
                    OverwriteCandidate candidate = targets[i];
                    string targetAssetPath = candidate.assetPaths[candidate.selectedIndex];
                    string targetFullPath = GetFullPathFromAssetPath(targetAssetPath);

                    if (_createBackup)
                    {
                        BackupAsset(targetAssetPath, targetFullPath, backupFolder);
                    }

                    File.Copy(candidate.droppedFile.sourcePath, targetFullPath, true);
                    AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
                    overwriteCount++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EIGHTAIDLib] Asset overwrite failed: {ex}");
                EditorUtility.DisplayDialog("Error", $"Asset overwrite failed.\n\n{ex.Message}", "OK");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[EIGHTAIDLib] Overwrote {overwriteCount} asset(s).");
            EditorUtility.DisplayDialog("Complete", $"Overwrote {overwriteCount} asset(s).", "OK");
            ClearFiles();
        }

        private static string CreateBackupFolder()
        {
            string folder = $"{BackupRoot}/{DateTime.Now:yyyyMMdd_HHmmss}";
            EnsureAssetFolder(BackupRoot);
            EnsureAssetFolder(folder);
            return folder;
        }

        private static void BackupAsset(string targetAssetPath, string targetFullPath, string backupFolder)
        {
            string relativePath = targetAssetPath.Substring("Assets/".Length);
            string backupAssetPath = $"{backupFolder}/{relativePath}";
            string backupFullPath = GetFullPathFromAssetPath(backupAssetPath);
            string backupDirectory = Path.GetDirectoryName(backupFullPath);

            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            File.Copy(targetFullPath, backupFullPath, true);
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/') ?? "Assets";
            string folderName = Path.GetFileName(assetFolder);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static string GetFullPathFromAssetPath(string assetPath)
        {
            string relativePath = assetPath.Substring("Assets".Length).TrimStart('/', '\\');
            return Path.Combine(Application.dataPath, relativePath);
        }

        private void ClearFiles()
        {
            _droppedFiles.Clear();
            _candidates.Clear();
            Repaint();
        }
    }
}
