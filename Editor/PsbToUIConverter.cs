using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EightAID.EIGHTAIDLib.Editor
{
    /// <summary>
    /// Reads PSD/PSB layer data and generates a UI prefab hierarchy from imported sprites.
    /// </summary>
    public sealed class PsbToUIConverter : EditorWindow
    {
        private Object _psbAsset;
        private string _savePath = "Assets/Prefab/UI";
        private bool _createCanvas;
        private Vector2 _scrollPos;

        private readonly List<LayerInfo> _layers = new();
        private readonly Dictionary<string, Sprite> _spritesByName = new();
        private Vector2 _docSize;
        private bool _isLoaded;

        private sealed class LayerInfo
        {
            public string layerName;
            public string spriteName;
            public bool isGroup;
            public int parentIndex;
            public bool isImported;
            public Rect spriteRect;
            public Vector2 spritePosition;
        }

        [MenuItem("Tools/EIGHTAID/PSB To UI Converter")]
        private static void Open()
        {
            GetWindow<PsbToUIConverter>("PSB To UI").minSize = new Vector2(460f, 540f);
        }

        private void OnGUI()
        {
            GUILayout.Label("PSD/PSB to UI Canvas Converter", EditorStyles.boldLabel);
            DrawDivider();
            EditorGUILayout.Space(6f);

            EditorGUI.BeginChangeCheck();
            _psbAsset = EditorGUILayout.ObjectField("PSD/PSB File", _psbAsset, typeof(Object), false);
            if (EditorGUI.EndChangeCheck())
            {
                ResetLoadedData();
            }

            GUI.enabled = _psbAsset != null;
            if (GUILayout.Button("Load Layer Data", GUILayout.Height(28f)))
            {
                LoadPsbData();
            }

            GUI.enabled = true;

            if (!_isLoaded)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            DrawLayerPreview();

            EditorGUILayout.Space(8f);
            DrawDivider();
            EditorGUILayout.Space(6f);

            GUILayout.Label("Prefab Settings", EditorStyles.boldLabel);
            _savePath = EditorGUILayout.TextField("Save Folder", _savePath);
            _createCanvas = EditorGUILayout.Toggle("Create Canvas Root", _createCanvas);

            EditorGUILayout.HelpBox(
                _createCanvas
                    ? "Creates a prefab with Canvas, CanvasScaler, and GraphicRaycaster."
                    : "Creates only a RectTransform root. Place it under an existing Canvas.",
                MessageType.Info);

            EditorGUILayout.Space(8f);

            Color originalBackground = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
            if (GUILayout.Button("Generate Prefab", GUILayout.Height(36f)))
            {
                GeneratePrefab();
            }

            GUI.backgroundColor = originalBackground;
        }

        private void ResetLoadedData()
        {
            _isLoaded = false;
            _layers.Clear();
            _spritesByName.Clear();
            _docSize = Vector2.zero;
        }

        private void LoadPsbData()
        {
            ResetLoadedData();

            string path = AssetDatabase.GetAssetPath(_psbAsset);
            if (!IsSupportedPsdAssetPath(path))
            {
                EditorUtility.DisplayDialog("Error", "Select a PSD or PSB file.", "OK");
                return;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && !_spritesByName.ContainsKey(sprite.name))
                {
                    _spritesByName[sprite.name] = sprite;
                }
            }

            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find the importer.", "OK");
                return;
            }

            var serializedImporter = new SerializedObject(importer);
            var dataById = new Dictionary<string, (Rect rect, Vector2 pos)>();
            var dataByName = new Dictionary<string, (Rect rect, Vector2 pos)>();

            SerializedProperty layeredProp = serializedImporter.FindProperty("m_LayeredSpriteImportData");
            if (layeredProp != null)
            {
                for (int i = 0; i < layeredProp.arraySize; i++)
                {
                    SerializedProperty element = layeredProp.GetArrayElementAtIndex(i);
                    string spriteId = ReadStringProp(element, "m_SpriteID");
                    string spriteName = ReadStringProp(element, "m_Name");

                    SerializedProperty rectProp = element.FindPropertyRelative("m_Rect");
                    SerializedProperty positionProp = element.FindPropertyRelative("spritePosition");
                    if (rectProp == null || positionProp == null)
                    {
                        continue;
                    }

                    var rect = new Rect(
                        rectProp.FindPropertyRelative("x")?.floatValue ?? 0f,
                        rectProp.FindPropertyRelative("y")?.floatValue ?? 0f,
                        rectProp.FindPropertyRelative("width")?.floatValue ?? 0f,
                        rectProp.FindPropertyRelative("height")?.floatValue ?? 0f);
                    var position = new Vector2(
                        positionProp.FindPropertyRelative("x")?.floatValue ?? 0f,
                        positionProp.FindPropertyRelative("y")?.floatValue ?? 0f);

                    if (!string.IsNullOrEmpty(spriteId) && !dataById.ContainsKey(spriteId))
                    {
                        dataById[spriteId] = (rect, position);
                    }

                    if (!string.IsNullOrEmpty(spriteName) && !dataByName.ContainsKey(spriteName))
                    {
                        dataByName[spriteName] = (rect, position);
                    }
                }
            }

            SerializedProperty psdLayersProp = serializedImporter.FindProperty("m_PsdLayers");
            if (psdLayersProp == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Could not read m_PsdLayers. The 2D PSD Importer package is required.",
                    "OK");
                return;
            }

            for (int i = 0; i < psdLayersProp.arraySize; i++)
            {
                SerializedProperty element = psdLayersProp.GetArrayElementAtIndex(i);
                string layerName = ReadStringProp(element, "m_Name");
                string spriteName = ReadStringProp(element, "m_SpriteName");
                bool isGroup = element.FindPropertyRelative("m_IsGroup")?.boolValue ?? false;
                int parentIndex = element.FindPropertyRelative("m_ParentIndex")?.intValue ?? -1;
                bool isImported = element.FindPropertyRelative("m_IsImported")?.boolValue ?? false;
                string spriteId = ReadStringProp(element, "m_SpriteID");

                if (string.IsNullOrEmpty(spriteName))
                {
                    spriteName = layerName;
                }

                var info = new LayerInfo
                {
                    layerName = layerName,
                    spriteName = spriteName,
                    isGroup = isGroup,
                    parentIndex = parentIndex,
                    isImported = isImported
                };

                if (!isGroup)
                {
                    if (!string.IsNullOrEmpty(spriteId) && dataById.TryGetValue(spriteId, out (Rect rect, Vector2 pos) byId))
                    {
                        info.spriteRect = byId.rect;
                        info.spritePosition = byId.pos;
                    }
                    else if (dataByName.TryGetValue(spriteName, out (Rect rect, Vector2 pos) byName))
                    {
                        info.spriteRect = byName.rect;
                        info.spritePosition = byName.pos;
                    }
                }

                _layers.Add(info);
            }

            float maxX = 0f;
            float maxY = 0f;
            for (int i = 0; i < _layers.Count; i++)
            {
                LayerInfo layer = _layers[i];
                if (layer.isGroup || layer.spriteRect.width <= 0f)
                {
                    continue;
                }

                maxX = Mathf.Max(maxX, layer.spritePosition.x + layer.spriteRect.width);
                maxY = Mathf.Max(maxY, layer.spritePosition.y + layer.spriteRect.height);
            }

            _docSize = new Vector2(maxX, maxY);
            _isLoaded = true;
            Repaint();
        }

        private void DrawLayerPreview()
        {
            GUILayout.Label(
                $"Layers - Document Size: {_docSize.x} x {_docSize.y} px  (Sprites: {_spritesByName.Count})",
                EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(220f));

            for (int i = 0; i < _layers.Count; i++)
            {
                LayerInfo layer = _layers[i];
                int depth = CalcDepth(i);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(depth * 16f);

                if (layer.isGroup)
                {
                    GUI.color = new Color(0.55f, 0.85f, 1f);
                    GUILayout.Label($"> {layer.layerName}");
                }
                else if (layer.isImported && _spritesByName.ContainsKey(layer.spriteName))
                {
                    GUI.color = Color.white;
                    GUILayout.Label($"* {layer.spriteName}");
                    GUILayout.FlexibleSpace();
                    GUI.color = new Color(0.6f, 0.6f, 0.6f);
                    GUILayout.Label(
                        $"{layer.spriteRect.width}x{layer.spriteRect.height}  pos({layer.spritePosition.x},{layer.spritePosition.y})",
                        GUILayout.Width(220f));
                }
                else
                {
                    GUI.color = new Color(0.4f, 0.4f, 0.4f);
                    GUILayout.Label($"- {layer.layerName} (Skipped)");
                }

                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private int CalcDepth(int index)
        {
            int depth = 0;
            int parent = _layers[index].parentIndex;
            while (parent >= 0 && parent < _layers.Count && depth < 32)
            {
                depth++;
                parent = _layers[parent].parentIndex;
            }

            return depth;
        }

        private void GeneratePrefab()
        {
            if (_docSize.x <= 0f || _docSize.y <= 0f)
            {
                EditorUtility.DisplayDialog("Error", "Could not determine the document size.", "OK");
                return;
            }

            EnsureDirectory(_savePath);

            string assetPath = AssetDatabase.GetAssetPath(_psbAsset);
            string psbName = Path.GetFileNameWithoutExtension(assetPath);
            string prefabPath = $"{_savePath}/{psbName}_UI.prefab";

            if (File.Exists(prefabPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Prefab",
                    $"{prefabPath} already exists. Overwrite it?",
                    "Overwrite",
                    "Cancel");
                if (!overwrite)
                {
                    return;
                }
            }

            var root = new GameObject(psbName, typeof(RectTransform));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = _docSize;

            GameObject saveTarget = root;
            if (_createCanvas)
            {
                var canvasObject = new GameObject($"{psbName}_Canvas");
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = _docSize;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

                canvasObject.AddComponent<GraphicRaycaster>();
                root.transform.SetParent(canvasObject.transform, false);
                saveTarget = canvasObject;
            }

            var objectsByIndex = new Dictionary<int, GameObject>();
            Vector2 documentCenter = _docSize * 0.5f;

            for (int i = 0; i < _layers.Count; i++)
            {
                LayerInfo layer = _layers[i];
                Transform parent = layer.parentIndex >= 0 && objectsByIndex.TryGetValue(layer.parentIndex, out GameObject parentObject)
                    ? parentObject.transform
                    : root.transform;

                if (layer.isGroup)
                {
                    var groupObject = new GameObject(layer.layerName, typeof(RectTransform));
                    RectTransform rect = groupObject.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = _docSize;
                    groupObject.transform.SetParent(parent, false);
                    groupObject.transform.SetAsFirstSibling();
                    objectsByIndex[i] = groupObject;
                    continue;
                }

                if (!layer.isImported || !_spritesByName.TryGetValue(layer.spriteName, out Sprite sprite))
                {
                    continue;
                }

                var imageObject = new GameObject(layer.spriteName, typeof(RectTransform));
                RectTransform imageRect = imageObject.GetComponent<RectTransform>();
                Image image = imageObject.AddComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;

                Vector2 center = layer.spritePosition + new Vector2(layer.spriteRect.width * 0.5f, layer.spriteRect.height * 0.5f);
                imageRect.anchorMin = new Vector2(0.5f, 0.5f);
                imageRect.anchorMax = new Vector2(0.5f, 0.5f);
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageRect.sizeDelta = new Vector2(layer.spriteRect.width, layer.spriteRect.height);
                imageRect.anchoredPosition = center - documentCenter;

                imageObject.transform.SetParent(parent, false);
                imageObject.transform.SetAsFirstSibling();
                objectsByIndex[i] = imageObject;
            }

            PrefabUtility.SaveAsPrefabAsset(saveTarget, prefabPath);
            DestroyImmediate(saveTarget);
            AssetDatabase.Refresh();

            GameObject createdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = createdPrefab;

            Debug.Log($"[EIGHTAIDLib] PSD/PSB to UI prefab generated: {prefabPath}");
            EditorUtility.DisplayDialog("Complete", $"Generated prefab:\n{prefabPath}", "OK");
        }

        private static string ReadStringProp(SerializedProperty element, string propertyName)
        {
            SerializedProperty property = element.FindPropertyRelative(propertyName);
            if (property == null)
            {
                return string.Empty;
            }

            try
            {
                return property.stringValue ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void DrawDivider()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.35f, 0.35f, 0.35f));
        }

        private static void EnsureDirectory(string assetPath)
        {
            if (!assetPath.StartsWith("Assets"))
            {
                return;
            }

            string relativePath = assetPath.Substring("Assets".Length).TrimStart('/', '\\');
            string fullPath = Path.Combine(Application.dataPath, relativePath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
            }
        }

        private static bool IsSupportedPsdAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string extension = Path.GetExtension(assetPath).ToLowerInvariant();
            return extension == ".psd" || extension == ".psb";
        }
    }
}
