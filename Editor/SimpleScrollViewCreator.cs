using EightAID.EIGHTAIDLib.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EightAID.EIGHTAIDLib.Editor
{
    public static class SimpleScrollViewCreator
    {
        private const string MenuPath = "GameObject/UI/EightAID Simple Scroll View";
        private const string DefaultSpritePath = "Assets/Scripts/EIGHTAIDLib/Sprites/SimpleSliderWhite.png";

        [MenuItem(MenuPath, false, 2036)]
        public static void Create(MenuCommand menuCommand)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultSpritePath);

            GameObject root = new("SimpleScrollView", typeof(RectTransform), typeof(Image), typeof(SimpleScrollView));
            Undo.RegisterCreatedObjectUndo(root, "Create EightAID Simple Scroll View");

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(360f, 260f);

            Image rootImage = root.GetComponent<Image>();
            rootImage.sprite = sprite;
            rootImage.color = new Color(0f, 0f, 0f, 0.18f);
            rootImage.raycastTarget = true;

            GameObject viewport = new("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(0f, 24f);
            viewportRect.offsetMax = new Vector2(-24f, 0f);

            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.sprite = sprite;
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewportImage.raycastTarget = true;

            GameObject content = new("Content", typeof(RectTransform), typeof(SimpleScrollViewLayout));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(320f, 236f);

            GameObject vertical = CreateSimpleSlider("VerticalSlider", root.transform, sprite);
            RectTransform verticalRect = vertical.GetComponent<RectTransform>();
            verticalRect.anchorMin = new Vector2(1f, 0.5f);
            verticalRect.anchorMax = new Vector2(1f, 0.5f);
            verticalRect.pivot = new Vector2(0.5f, 0.5f);
            verticalRect.sizeDelta = new Vector2(236f, 24f);
            verticalRect.anchoredPosition = new Vector2(-12f, 12f);
            verticalRect.localEulerAngles = new Vector3(0f, 0f, 90f);

            GameObject horizontal = CreateSimpleSlider("HorizontalSlider", root.transform, sprite);
            RectTransform horizontalRect = horizontal.GetComponent<RectTransform>();
            horizontalRect.anchorMin = new Vector2(0f, 0f);
            horizontalRect.anchorMax = new Vector2(1f, 0f);
            horizontalRect.pivot = new Vector2(0.5f, 0.5f);
            horizontalRect.offsetMin = new Vector2(0f, 0f);
            horizontalRect.offsetMax = new Vector2(-24f, 24f);

            SimpleScrollView scrollView = root.GetComponent<SimpleScrollView>();
            SerializedObject serialized = new(scrollView);
            serialized.FindProperty("viewportRect").objectReferenceValue = viewportRect;
            serialized.FindProperty("contentRect").objectReferenceValue = contentRect;
            serialized.FindProperty("verticalSlider").objectReferenceValue = vertical.GetComponent<SimpleSlider>();
            serialized.FindProperty("horizontalSlider").objectReferenceValue = horizontal.GetComponent<SimpleSlider>();
            serialized.FindProperty("enableVertical").boolValue = true;
            serialized.FindProperty("enableHorizontal").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);
            Selection.activeGameObject = root;
        }

        [MenuItem(MenuPath, true)]
        public static bool ValidateCreate()
        {
            return true;
        }

        private static GameObject CreateSimpleSlider(string name, Transform parent, Sprite sprite)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image), typeof(SimpleSlider));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(240f, 24f);

            Image raycastImage = root.GetComponent<Image>();
            raycastImage.sprite = sprite;
            raycastImage.color = new Color(1f, 1f, 1f, 0f);
            raycastImage.raycastTarget = true;

            GameObject background = CreateImageChild(root.transform, "Background", sprite, new Color(0.22f, 0.22f, 0.22f, 1f), false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.35f);
            backgroundRect.anchorMax = new Vector2(1f, 0.65f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fill = CreateImageChild(root.transform, "Fill", sprite, new Color(0.92f, 0.76f, 0.28f, 1f), false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.35f);
            fillRect.anchorMax = new Vector2(0f, 0.65f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handle = CreateImageChild(root.transform, "Handle", sprite, Color.white, true);
            handle.AddComponent<SimpleSliderHandleDragForwarder>().SetSlider(root.GetComponent<SimpleSlider>());
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.sizeDelta = new Vector2(20f, 20f);
            handleRect.anchoredPosition = Vector2.zero;

            SimpleSlider slider = root.GetComponent<SimpleSlider>();
            SerializedObject serialized = new(slider);
            serialized.FindProperty("backgroundRect").objectReferenceValue = backgroundRect;
            serialized.FindProperty("fillRect").objectReferenceValue = fillRect;
            serialized.FindProperty("handleRect").objectReferenceValue = handleRect;
            serialized.FindProperty("backgroundImage").objectReferenceValue = background.GetComponent<Image>();
            serialized.FindProperty("fillImage").objectReferenceValue = fill.GetComponent<Image>();
            serialized.FindProperty("handleImage").objectReferenceValue = handle.GetComponent<Image>();
            serialized.FindProperty("m_TargetGraphic").objectReferenceValue = handle.GetComponent<Image>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject CreateImageChild(Transform parent, string name, Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);

            Image image = child.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;

            return child;
        }
    }
}
