using System.Collections.Generic;
using EightAID.EIGHTAIDLib.UI;
using UnityEditor;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Editor
{
    public sealed class UIHitAreaVisualizerWindow : EditorWindow
    {
        private Color _visualizationColor = new(1f, 0f, 0f, 0.9f);
        private bool _drawOnlyWhenSelected = true;
        private bool _includeInactive = true;
        private bool _drawRaycastTargets = true;
        private bool _drawChildColliders = true;
        private bool _includeRootRectTransform;
        private float _fillAlpha = 0.08f;
        private float _lineThickness = 2f;

        [MenuItem("Tools/EIGHTAID/UI Hit Area Visualizer")]
        private static void Open()
        {
            GetWindow<UIHitAreaVisualizerWindow>("UI Hit Visualizer");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Visualization Settings", EditorStyles.boldLabel);
            _visualizationColor = EditorGUILayout.ColorField("Color", _visualizationColor);
            _drawOnlyWhenSelected = EditorGUILayout.Toggle("Only When Selected", _drawOnlyWhenSelected);
            _includeInactive = EditorGUILayout.Toggle("Include Inactive", _includeInactive);
            _drawRaycastTargets = EditorGUILayout.Toggle("Draw Raycast Targets", _drawRaycastTargets);
            _drawChildColliders = EditorGUILayout.Toggle("Draw Child Colliders", _drawChildColliders);
            _includeRootRectTransform = EditorGUILayout.Toggle("Include Root Rect", _includeRootRectTransform);
            _fillAlpha = EditorGUILayout.Slider("Fill Alpha", _fillAlpha, 0f, 1f);
            _lineThickness = EditorGUILayout.FloatField("Line Thickness", _lineThickness);

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Apply adds or updates UIHitAreaVisualizer on the target objects. " +
                "Remove deletes the component from the same targets.",
                MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Selected roots: {GetSelectionRoots().Count}", EditorStyles.boldLabel);

                if (GUILayout.Button("Apply To Selected"))
                {
                    ApplyToTargets(GetSelectionRoots());
                }

                if (GUILayout.Button("Remove From Selected"))
                {
                    RemoveFromTargets(GetSelectionRoots());
                }
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Scene canvases: {GetSceneCanvasRoots().Count}", EditorStyles.boldLabel);

                if (GUILayout.Button("Apply To All Scene Canvases"))
                {
                    ApplyToTargets(GetSceneCanvasRoots());
                }

                if (GUILayout.Button("Remove From All Scene Canvases"))
                {
                    RemoveFromTargets(GetSceneCanvasRoots());
                }
            }
        }

        private void ApplyToTargets(List<GameObject> targets)
        {
            if (targets.Count == 0)
            {
                ShowNotification(new GUIContent("No targets found."));
                return;
            }

            int changedCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                GameObject target = targets[i];
                if (target == null)
                {
                    continue;
                }

                UIHitAreaVisualizer visualizer = target.GetComponent<UIHitAreaVisualizer>();
                if (visualizer == null)
                {
                    visualizer = Undo.AddComponent<UIHitAreaVisualizer>(target);
                }
                else
                {
                    Undo.RecordObject(visualizer, "Update UI Hit Area Visualizer");
                }

                visualizer.Configure(
                    _visualizationColor,
                    _drawOnlyWhenSelected,
                    _includeInactive,
                    _drawRaycastTargets,
                    _drawChildColliders,
                    _includeRootRectTransform,
                    _fillAlpha,
                    _lineThickness);

                EditorUtility.SetDirty(visualizer);
                changedCount++;
            }

            SceneView.RepaintAll();
            ShowNotification(new GUIContent($"Applied to {changedCount} object(s)."));
        }

        private void RemoveFromTargets(List<GameObject> targets)
        {
            if (targets.Count == 0)
            {
                ShowNotification(new GUIContent("No targets found."));
                return;
            }

            int removedCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                GameObject target = targets[i];
                if (target == null)
                {
                    continue;
                }

                UIHitAreaVisualizer visualizer = target.GetComponent<UIHitAreaVisualizer>();
                if (visualizer == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(visualizer);
                removedCount++;
            }

            SceneView.RepaintAll();
            ShowNotification(new GUIContent($"Removed from {removedCount} object(s)."));
        }

        private static List<GameObject> GetSelectionRoots()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            var roots = new List<GameObject>(selectedObjects.Length);

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                GameObject candidate = selectedObjects[i];
                if (candidate == null)
                {
                    continue;
                }

                bool hasSelectedAncestor = false;
                Transform parent = candidate.transform.parent;
                while (parent != null)
                {
                    if (Selection.Contains(parent.gameObject))
                    {
                        hasSelectedAncestor = true;
                        break;
                    }

                    parent = parent.parent;
                }

                if (!hasSelectedAncestor)
                {
                    roots.Add(candidate);
                }
            }

            return roots;
        }

        private static List<GameObject> GetSceneCanvasRoots()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var roots = new List<GameObject>(canvases.Length);

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                {
                    continue;
                }

                if (EditorUtility.IsPersistent(canvas) || !canvas.gameObject.scene.IsValid())
                {
                    continue;
                }

                roots.Add(canvas.gameObject);
            }

            return roots;
        }
    }
}
