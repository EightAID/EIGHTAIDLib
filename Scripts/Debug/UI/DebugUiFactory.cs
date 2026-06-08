#if UNITY_EDITOR || EIGHTAID_TEST_BUILD
using System;
using UnityEngine;
using UnityEngine.UI;

public static class DebugUiFactory
{
    public static Text CreateLabel(RectTransform parent, Font font, int fontSize, TextAnchor alignment, Color color)
    {
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        labelObject.transform.SetParent(parent, false);

        Text label = labelObject.GetComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        LayoutElement layout = labelObject.GetComponent<LayoutElement>();
        layout.preferredHeight = fontSize + 12f;
        return label;
    }

    public static Button CreateButton(RectTransform parent, Font font, Sprite sprite, string text, Action onClick, float width, float height = 34f, bool stretchWidth = false)
    {
        var buttonObject = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.2f, 0.27f, 0.34f, 0.96f);

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredHeight = height;
        if (stretchWidth)
        {
            layout.flexibleWidth = 1f;
        }
        else
        {
            layout.preferredWidth = width;
        }

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());

        Text label = CreateLabel(buttonObject.GetComponent<RectTransform>(), font, 14, TextAnchor.MiddleLeft, Color.white);
        label.text = text;
        Stretch(label.rectTransform, 10f, 0f, 10f, 0f);
        return button;
    }

    public static InputField CreateInputRow(RectTransform parent, Font font, Sprite sprite, string label, string placeholder)
    {
        var row = new GameObject(label + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8f;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandHeight = false;

        row.GetComponent<LayoutElement>().preferredHeight = 30f;

        Text labelText = CreateLabel(row.GetComponent<RectTransform>(), font, 14, TextAnchor.MiddleLeft, Color.white);
        labelText.text = label;
        labelText.GetComponent<LayoutElement>().preferredWidth = 110f;

        InputField input = CreateInputField(row.GetComponent<RectTransform>(), font, sprite, placeholder);
        LayoutElement inputLayout = input.GetComponent<LayoutElement>();
        inputLayout.preferredWidth = 420f;
        inputLayout.flexibleWidth = 1f;
        return input;
    }

    public static InputField CreateInputField(RectTransform parent, Font font, Sprite sprite, string placeholder)
    {
        var fieldObject = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
        fieldObject.transform.SetParent(parent, false);

        Image image = fieldObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.14f);

        LayoutElement layout = fieldObject.GetComponent<LayoutElement>();
        layout.preferredHeight = 30f;
        layout.flexibleWidth = 1f;

        Text text = CreateInnerText(fieldObject.transform, font, Color.white);
        Text placeholderText = CreateInnerText(fieldObject.transform, font, new Color(1f, 1f, 1f, 0.45f));
        placeholderText.text = placeholder;

        InputField input = fieldObject.GetComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.lineType = InputField.LineType.SingleLine;
        input.targetGraphic = image;
        return input;
    }

    public static Slider CreateSliderRow(
        RectTransform parent,
        Font font,
        Sprite sprite,
        string label,
        float minValue,
        float maxValue,
        float defaultValue,
        out InputField valueInput)
    {
        var row = new GameObject(label + "SliderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 32f;

        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8f;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandHeight = true;

        Text labelText = CreateLabel(row.GetComponent<RectTransform>(), font, 14, TextAnchor.MiddleLeft, Color.white);
        labelText.text = label;
        labelText.GetComponent<LayoutElement>().preferredWidth = 110f;

        var sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderObject.transform.SetParent(row.transform, false);
        LayoutElement sliderLayout = sliderObject.GetComponent<LayoutElement>();
        sliderLayout.preferredWidth = 300f;
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.preferredHeight = 30f;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderObject.transform, false);
        Stretch(background.GetComponent<RectTransform>(), 0f, 12f, 0f, 12f);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = sprite;
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = new Color(1f, 1f, 1f, 0.14f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>(), 0f, 12f, 0f, 12f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Stretch(fill.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.sprite = sprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = new Color(0.25f, 0.58f, 0.82f, 0.95f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        Stretch(handleArea.GetComponent<RectTransform>(), 8f, 0f, 8f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 26f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = sprite;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = new Color(0.82f, 0.92f, 1f, 1f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.targetGraphic = handleImage;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.wholeNumbers = false;
        slider.value = Mathf.Clamp(defaultValue, minValue, maxValue);

        valueInput = CreateInputField(row.GetComponent<RectTransform>(), font, sprite, "0");
        LayoutElement inputLayout = valueInput.GetComponent<LayoutElement>();
        inputLayout.preferredWidth = 86f;
        inputLayout.flexibleWidth = 0f;
        valueInput.contentType = InputField.ContentType.DecimalNumber;
        valueInput.text = slider.value.ToString("0.##");
        return slider;
    }

    public static Toggle CreateToggleRow(RectTransform parent, Font font, string label, bool defaultValue)
    {
        var row = new GameObject(label + "ToggleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 30f;

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        Text labelText = CreateLabel(row.GetComponent<RectTransform>(), font, 14, TextAnchor.MiddleLeft, Color.white);
        labelText.text = label;
        labelText.GetComponent<LayoutElement>().preferredWidth = 110f;

        var toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
        toggleObject.transform.SetParent(row.transform, false);
        toggleObject.GetComponent<LayoutElement>().preferredWidth = 30f;
        toggleObject.GetComponent<LayoutElement>().preferredHeight = 30f;

        var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(toggleObject.transform, false);
        Stretch(backgroundObject.GetComponent<RectTransform>(), 4f, 4f, 4f, 4f);
        backgroundObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);

        var checkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkObject.transform.SetParent(backgroundObject.transform, false);
        Stretch(checkObject.GetComponent<RectTransform>(), 6f, 6f, 6f, 6f);
        checkObject.GetComponent<Image>().color = new Color(0.55f, 0.95f, 0.7f, 1f);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = backgroundObject.GetComponent<Image>();
        toggle.graphic = checkObject.GetComponent<Image>();
        toggle.isOn = defaultValue;
        return toggle;
    }

    public static Dropdown CreateDropdownRow(RectTransform parent, Font font, Sprite sprite, string label)
    {
        var row = new GameObject(label + "DropdownRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 30f;

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        Text labelText = CreateLabel(row.GetComponent<RectTransform>(), font, 14, TextAnchor.MiddleLeft, Color.white);
        labelText.text = label;
        labelText.GetComponent<LayoutElement>().preferredWidth = 110f;

        return CreateDropdown(row.GetComponent<RectTransform>(), font, sprite);
    }

    public static Button CreateChoiceRow(RectTransform parent, Font font, Sprite sprite, string label, out Text valueText)
    {
        var row = new GameObject(label + "ChoiceRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 30f;

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        Text labelText = CreateLabel(row.GetComponent<RectTransform>(), font, 14, TextAnchor.MiddleLeft, Color.white);
        labelText.text = label;
        labelText.GetComponent<LayoutElement>().preferredWidth = 110f;

        var buttonObject = new GameObject("ChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(row.transform, false);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.14f);

        LayoutElement buttonLayout = buttonObject.GetComponent<LayoutElement>();
        buttonLayout.preferredWidth = 420f;
        buttonLayout.flexibleWidth = 1f;
        buttonLayout.preferredHeight = 30f;

        valueText = CreateInnerText(buttonObject.transform, font, Color.white);
        Stretch(valueText.rectTransform, 10f, 6f, 34f, 6f);

        Text arrow = CreateInnerText(buttonObject.transform, font, Color.white);
        arrow.text = "v";
        arrow.alignment = TextAnchor.MiddleCenter;
        arrow.rectTransform.anchorMin = new Vector2(1f, 0f);
        arrow.rectTransform.anchorMax = new Vector2(1f, 1f);
        arrow.rectTransform.pivot = new Vector2(1f, 0.5f);
        arrow.rectTransform.sizeDelta = new Vector2(24f, 0f);
        arrow.rectTransform.anchoredPosition = new Vector2(-4f, 0f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    public static RectTransform CreateScrollContent(RectTransform parent, Sprite sprite, float height, bool expandHeight = false)
    {
        var root = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect), typeof(LayoutElement));
        root.transform.SetParent(parent, false);

        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.07f);
        root.GetComponent<Mask>().showMaskGraphic = true;

        LayoutElement layout = root.GetComponent<LayoutElement>();
        if (height > 0f)
        {
            layout.preferredHeight = height;
            layout.minHeight = Mathf.Min(height, 180f);
        }
        else
        {
            layout.minHeight = 180f;
            layout.preferredHeight = 240f;
        }
        layout.flexibleHeight = expandHeight ? 1f : 0f;

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(root.transform, false);

        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(6f, 0f);
        content.offsetMax = new Vector2(-6f, 0f);

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 4f;
        contentLayout.padding = new RectOffset(5, 5, 5, 5);
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = root.GetComponent<ScrollRect>();
        scroll.viewport = root.GetComponent<RectTransform>();
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        return content;
    }

    public static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static Text CreateInnerText(Transform parent, Font font, Color color)
    {
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Stretch(textObject.GetComponent<RectTransform>(), 10f, 6f, 10f, 6f);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = 14;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        return text;
    }

    private static Dropdown CreateDropdown(RectTransform parent, Font font, Sprite sprite)
    {
        var root = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown), typeof(LayoutElement));
        root.transform.SetParent(parent, false);

        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.14f);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.preferredWidth = 390f;
        layout.preferredHeight = 34f;

        Dropdown dropdown = root.GetComponent<Dropdown>();
        dropdown.targetGraphic = image;

        Text caption = CreateInnerText(root.transform, font, Color.white);
        Stretch(caption.rectTransform, 10f, 6f, 28f, 6f);
        dropdown.captionText = caption;

        Text arrow = CreateInnerText(root.transform, font, Color.white);
        arrow.text = "v";
        arrow.alignment = TextAnchor.MiddleCenter;
        arrow.rectTransform.anchorMin = new Vector2(1f, 0f);
        arrow.rectTransform.anchorMax = new Vector2(1f, 1f);
        arrow.rectTransform.pivot = new Vector2(1f, 0.5f);
        arrow.rectTransform.sizeDelta = new Vector2(24f, 0f);
        arrow.rectTransform.anchoredPosition = new Vector2(-4f, 0f);

        var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        template.transform.SetParent(root.transform, false);
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, 2f);
        templateRect.sizeDelta = new Vector2(0f, 180f);
        template.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.17f, 0.98f);
        template.SetActive(false);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(template.transform, false);
        Stretch(viewport.GetComponent<RectTransform>(), 4f, 4f, 4f, 4f);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ScrollRect scroll = template.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;

        var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        item.transform.SetParent(content.transform, false);
        item.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 28f);

        var itemBackground = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
        itemBackground.transform.SetParent(item.transform, false);
        Stretch(itemBackground.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        itemBackground.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

        var itemCheckmark = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
        itemCheckmark.transform.SetParent(item.transform, false);
        Stretch(itemCheckmark.GetComponent<RectTransform>(), 8f, 8f, 356f, 8f);
        itemCheckmark.GetComponent<Image>().color = new Color(0.55f, 0.95f, 0.7f, 1f);

        Text itemLabel = CreateInnerText(item.transform, font, Color.white);
        Stretch(itemLabel.rectTransform, 32f, 2f, 8f, 2f);

        Toggle toggle = item.GetComponent<Toggle>();
        toggle.targetGraphic = itemBackground.GetComponent<Image>();
        toggle.graphic = itemCheckmark.GetComponent<Image>();
        toggle.isOn = true;

        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        return dropdown;
    }
}
#endif
