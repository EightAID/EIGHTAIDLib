using EightAID.EIGHTAIDLib.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EightAID.EIGHTAIDLib.Editor
{
    public static class SimpleSliderCreator
    {
        private const string MenuPath = "GameObject/UI/EightAID Simple Slider";

        [MenuItem(MenuPath, false, 2035)]
        public static void Create(MenuCommand menuCommand)
        {
            GameObject root = new("SimpleSlider", typeof(RectTransform), typeof(Image), typeof(SimpleSlider));
            Undo.RegisterCreatedObjectUndo(root, "Create EightAID Simple Slider");

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(240f, 32f);

            Image raycastImage = root.GetComponent<Image>();
            raycastImage.color = new Color(1f, 1f, 1f, 0f);
            raycastImage.raycastTarget = true;

            GameObject background = CreateImageChild(root.transform, "Background", new Color(0.22f, 0.22f, 0.22f, 1f));
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.35f);
            backgroundRect.anchorMax = new Vector2(1f, 0.65f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fill = CreateImageChild(root.transform, "Fill", new Color(0.92f, 0.76f, 0.28f, 1f));
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.35f);
            fillRect.anchorMax = new Vector2(0f, 0.65f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handle = CreateImageChild(root.transform, "Handle", Color.white);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.sizeDelta = new Vector2(24f, 24f);
            handleRect.anchoredPosition = Vector2.zero;

            SimpleSlider slider = root.GetComponent<SimpleSlider>();
            SerializedObject serializedSlider = new(slider);
            serializedSlider.FindProperty("backgroundRect").objectReferenceValue = backgroundRect;
            serializedSlider.FindProperty("fillRect").objectReferenceValue = fillRect;
            serializedSlider.FindProperty("handleRect").objectReferenceValue = handleRect;
            serializedSlider.FindProperty("backgroundImage").objectReferenceValue = background.GetComponent<Image>();
            serializedSlider.FindProperty("fillImage").objectReferenceValue = fill.GetComponent<Image>();
            serializedSlider.FindProperty("handleImage").objectReferenceValue = handle.GetComponent<Image>();
            serializedSlider.FindProperty("m_TargetGraphic").objectReferenceValue = handle.GetComponent<Image>();
            serializedSlider.ApplyModifiedPropertiesWithoutUndo();

            GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);
            Selection.activeGameObject = root;
        }

        [MenuItem(MenuPath, true)]
        public static bool ValidateCreate()
        {
            return true;
        }

        private static GameObject CreateImageChild(Transform parent, string name, Color color)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);

            Image image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return child;
        }
    }
}
