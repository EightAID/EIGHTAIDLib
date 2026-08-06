using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CurvedTextGraphic : MaskableGraphic
{
    [Header("Text")]
    [SerializeField, TextArea] private string text = "CURVED TEXT";
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float fontSize = 92f;
    [SerializeField] private FontStyles fontStyle = FontStyles.Bold;
    [SerializeField] private bool fitToTexture = true;
    [SerializeField, Min(0f)] private float horizontalPadding = 48f;
    [SerializeField, Min(0f)] private float verticalPadding = 24f;

    [Header("Warp")]
    [SerializeField] private AnimationCurve curve = new(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f));
    [SerializeField] private float curveHeight = 80f;
    [SerializeField, Range(8, 128)] private int horizontalSegments = 64;
    [SerializeField, Range(1, 16)] private int verticalSegments = 4;

    [Header("Texture")]
    [SerializeField, Range(256, 2048)] private int textureWidth = 1024;
    [SerializeField, Range(64, 1024)] private int textureHeight = 256;
    [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;

    private const int RenderLayer = 30;

    private GameObject renderRoot;
    private Camera renderCamera;
    private Canvas renderCanvas;
    private TextMeshProUGUI renderText;
    private RenderTexture renderTexture;
    private bool renderDirty = true;

    public override Texture mainTexture => renderTexture != null ? renderTexture : Texture2D.whiteTexture;

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureRenderObjects();
        MarkDirty();
    }

    protected override void OnDisable()
    {
        ReleaseRenderObjects();
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        ReleaseRenderObjects();
        base.OnDestroy();
    }

    protected void OnValidate()
    {
        horizontalSegments = Mathf.Clamp(horizontalSegments, 8, 128);
        verticalSegments = Mathf.Clamp(verticalSegments, 1, 16);
        textureWidth = Mathf.Clamp(textureWidth, 256, 2048);
        textureHeight = Mathf.Clamp(textureHeight, 64, 1024);
        fontSize = Mathf.Max(1f, fontSize);
        horizontalPadding = Mathf.Max(0f, horizontalPadding);
        verticalPadding = Mathf.Max(0f, verticalPadding);
        MarkDirty();
    }

    private void LateUpdate()
    {
        RenderSourceIfDirty();
    }

    private void MarkDirty()
    {
        renderDirty = true;
        SetVerticesDirty();
        SetMaterialDirty();
    }

    private void EnsureRenderObjects()
    {
        if (renderRoot == null)
        {
            renderRoot = new GameObject($"{name} Curved Text Renderer");
            renderRoot.hideFlags = HideFlags.HideAndDontSave;
            renderRoot.layer = RenderLayer;

            GameObject cameraObject = new("Camera", typeof(Camera), typeof(UniversalAdditionalCameraData));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            cameraObject.layer = RenderLayer;
            cameraObject.transform.SetParent(renderRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            renderCamera = cameraObject.GetComponent<Camera>();
            renderCamera.orthographic = true;
            renderCamera.orthographicSize = 1.5f;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.cullingMask = 1 << RenderLayer;
            renderCamera.allowHDR = false;
            renderCamera.allowMSAA = true;
            renderCamera.enabled = false;

            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.hideFlags = HideFlags.HideAndDontSave;
            canvasObject.layer = RenderLayer;
            canvasObject.transform.SetParent(renderRoot.transform, false);
            renderCanvas = canvasObject.GetComponent<Canvas>();
            renderCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            renderCanvas.worldCamera = renderCamera;
            renderCanvas.planeDistance = 1f;

            GameObject textObject = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.hideFlags = HideFlags.HideAndDontSave;
            textObject.layer = RenderLayer;
            textObject.transform.SetParent(canvasObject.transform, false);
            renderText = textObject.GetComponent<TextMeshProUGUI>();
            renderText.alignment = TextAlignmentOptions.Center;
            renderText.enableWordWrapping = false;
            renderText.overflowMode = TextOverflowModes.Overflow;
            renderText.rectTransform.anchorMin = Vector2.zero;
            renderText.rectTransform.anchorMax = Vector2.one;
            renderCanvas.enabled = false;
        }

        EnsureRenderTexture();
    }

    private void EnsureRenderTexture()
    {
        if (renderTexture != null && renderTexture.width == textureWidth && renderTexture.height == textureHeight)
        {
            renderTexture.filterMode = filterMode;
            return;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            DestroyGeneratedObject(renderTexture);
        }

        renderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32)
        {
            name = $"{name} Curved Text",
            filterMode = filterMode,
            hideFlags = HideFlags.HideAndDontSave
        };
        renderTexture.Create();
        renderCamera.targetTexture = renderTexture;
        renderCamera.aspect = (float)textureWidth / textureHeight;
    }

    private void UpdateRenderSource()
    {
        renderText.text = text;
        renderText.fontSize = fontSize;
        renderText.enableAutoSizing = fitToTexture;
        renderText.fontSizeMin = 1f;
        renderText.fontSizeMax = fontSize;
        renderText.fontStyle = fontStyle;
        renderText.color = color;
        renderText.rectTransform.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        renderText.rectTransform.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        TMP_FontAsset activeFont = font != null ? font : TMP_Settings.defaultFontAsset;
        if (activeFont != null)
        {
            renderText.font = activeFont;
        }

        renderText.ForceMeshUpdate();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        RenderSourceIfDirty();
        vertexHelper.Clear();
        Rect rect = GetPixelAdjustedRect();
        int columns = horizontalSegments + 1;

        for (int yIndex = 0; yIndex <= verticalSegments; yIndex++)
        {
            float normalizedY = (float)yIndex / verticalSegments;
            float baseY = Mathf.Lerp(rect.yMin, rect.yMax, normalizedY);
            for (int xIndex = 0; xIndex <= horizontalSegments; xIndex++)
            {
                float normalizedX = (float)xIndex / horizontalSegments;
                float x = Mathf.Lerp(rect.xMin, rect.xMax, normalizedX);
                float y = baseY + curve.Evaluate(normalizedX) * curveHeight;
                vertexHelper.AddVert(new Vector3(x, y), Color.white, new Vector2(normalizedX, normalizedY));
            }
        }

        for (int yIndex = 0; yIndex < verticalSegments; yIndex++)
        {
            for (int xIndex = 0; xIndex < horizontalSegments; xIndex++)
            {
                int bottomLeft = yIndex * columns + xIndex;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + columns;
                int topRight = topLeft + 1;
                vertexHelper.AddTriangle(bottomLeft, topLeft, topRight);
                vertexHelper.AddTriangle(bottomLeft, topRight, bottomRight);
            }
        }
    }

    private void RenderSourceIfDirty()
    {
        if (!renderDirty)
        {
            return;
        }

        renderDirty = false;
        EnsureRenderObjects();
        UpdateRenderSource();
        renderCanvas.enabled = true;
        renderCamera.enabled = true;
        Canvas.ForceUpdateCanvases();
        renderCamera.Render();
        renderCamera.enabled = false;
        renderCanvas.enabled = false;
    }

    private void ReleaseRenderObjects()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            DestroyGeneratedObject(renderTexture);
            renderTexture = null;
        }

        if (renderRoot != null)
        {
            DestroyGeneratedObject(renderRoot);
            renderRoot = null;
            renderCamera = null;
            renderCanvas = null;
            renderText = null;
        }
    }

    private static void DestroyGeneratedObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
