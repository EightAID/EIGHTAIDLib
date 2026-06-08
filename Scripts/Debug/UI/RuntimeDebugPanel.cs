#if UNITY_EDITOR || EIGHTAID_TEST_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 実行中のゲームに重ねて表示する汎用デバッグパネルです。
/// コマンド本体は DebugCommandRegistry へ登録し、このパネルは検索、カテゴリ絞り込み、
/// 引数入力、実行ログ表示だけを担当します。ゲーム固有処理はここへ直接書かず、
/// プロジェクト側の IDebugCommandModule に分離してください。
/// </summary>
public sealed class RuntimeDebugPanel : MonoBehaviour
{
    private const KeyCode ToggleKey = KeyCode.F1;
    private const string PrimaryFontAssetPath = "Assets/Fonts/Morisawa/BO-A1GothicStdN-Regular.otf";
    private const string SecondaryFontAssetPath = "Assets/Fonts/Morisawa/BO-A1GothicStdN-Medium.otf";
    private const int MaxLogLines = 18;

    private static RuntimeDebugPanel _instance;
    private static Sprite _fallbackSprite;

    private RectTransform _panelRoot;
    private RectTransform _commandListContent;
    private RectTransform _categoryFilterContent;
    private RectTransform _argumentContent;
    private RectTransform _logContent;
    private RectTransform _choicePopupRoot;
    private InputField _searchInput;
    private Text _titleText;
    private Text _descriptionText;
    private Text _statusText;
    private Text _emptyArgumentText;
    private Button _executeButton;
    private DebugCommand _selectedCommand;
    private int _selectedIndex;
    private string _selectedCategory = string.Empty;
    private bool _hasPausedGame;
    private bool _inputBlockedBeforeVisible;
    private float _resumeTimeScale = 1f;
    private IReadOnlyList<DebugCommand> _visibleCommands = Array.Empty<DebugCommand>();

    private readonly Dictionary<string, Func<object>> _valueReaders = new Dictionary<string, Func<object>>(StringComparer.Ordinal);
    private readonly List<Image> _commandRows = new List<Image>();
    private readonly List<string> _logLines = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        EnsureExistsNow();
    }

    public static void EnsureExistsNow()
    {
        if (_instance != null || !DebugAvailability.IsEnabled)
        {
            return;
        }

        var root = new GameObject(nameof(RuntimeDebugPanel));
        _instance = root.AddComponent<RuntimeDebugPanel>();
    }

    public static void SetResumeTimeScale(float timeScale)
    {
        float clamped = Mathf.Clamp(timeScale, 0f, 10f);
        if (_instance != null && _instance._hasPausedGame)
        {
            _instance._resumeTimeScale = clamped;
            return;
        }

        Time.timeScale = clamped;
    }

    public static void HideNow()
    {
        if (_instance != null)
        {
            _instance.SetVisible(false);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        BuildUi();
        SetVisible(false);
    }

    private void Update()
    {
        if (!DebugAvailability.IsEnabled)
        {
            SetVisible(false);
            return;
        }

        if (Input.GetKeyDown(ToggleKey))
        {
            if (_panelRoot == null)
            {
                BuildUi();
            }

            SetVisible(_panelRoot == null || !_panelRoot.gameObject.activeSelf);
            return;
        }

        if (_panelRoot == null || !_panelRoot.gameObject.activeSelf)
        {
            return;
        }

        InputSystemBase.SetInputBlockedForDebugUi(true);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_choicePopupRoot != null && _choicePopupRoot.gameObject.activeSelf)
            {
                HideChoicePopup();
                return;
            }

            SetVisible(false);
            return;
        }

        if (IsTextInputFocused())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ExecuteSelectedCommand();
        }
    }

    private void BuildUi()
    {
        if (_panelRoot != null)
        {
            return;
        }

        Font font = LoadFont();
        Sprite sprite = GetFallbackSprite();
        EnsureEventSystem();
        Canvas canvas = CreateCanvas();

        _panelRoot = CreatePanelRoot(canvas.transform, sprite);
        _choicePopupRoot = CreateChoicePopupRoot(_panelRoot, sprite);
        RectTransform header = CreateHeader(_panelRoot, font, sprite);
        RectTransform main = CreateMainArea(_panelRoot);
        RectTransform left = CreatePanelSection(main, "CommandPalette", 840f, 1.15f);
        RectTransform right = CreatePanelSection(main, "CommandInspector", 620f, 0.95f);
        RectTransform log = CreateLogPanel(_panelRoot, font, sprite);

        _searchInput = DebugUiFactory.CreateInputField(header, font, sprite, "> コマンド名、カテゴリ、IDで検索");
        _searchInput.textComponent.fontSize = 22;
        ((Text)_searchInput.placeholder).fontSize = 18;
        _searchInput.onValueChanged.AddListener(_ => RefreshCommandList());

        CreateSectionTitle(left, font, "コマンド一覧");
        _categoryFilterContent = CreateCategoryFilterBar(left);
        _commandListContent = DebugUiFactory.CreateScrollContent(left, sprite, 470f, true);

        CreateSectionTitle(right, font, "詳細と引数");
        _titleText = DebugUiFactory.CreateLabel(right, font, 24, TextAnchor.MiddleLeft, Color.white);
        _titleText.text = "コマンドを選択";

        _descriptionText = DebugUiFactory.CreateLabel(right, font, 15, TextAnchor.UpperLeft, new Color(0.78f, 0.84f, 0.92f));
        _descriptionText.GetComponent<LayoutElement>().preferredHeight = 86f;
        _descriptionText.text = string.Empty;

        _argumentContent = DebugUiFactory.CreateScrollContent(right, sprite, 190f);

        _executeButton = DebugUiFactory.CreateButton(right, font, sprite, "実行  Enter", ExecuteSelectedCommand, 260f, 42f);
        _executeButton.interactable = false;

        _statusText = DebugUiFactory.CreateLabel(right, font, 14, TextAnchor.UpperLeft, new Color(0.95f, 0.86f, 0.55f));
        _statusText.GetComponent<LayoutElement>().preferredHeight = 48f;
        _statusText.text = string.Empty;

        CreateSectionTitle(log, font, "実行ログ");
        _logContent = DebugUiFactory.CreateScrollContent(log, sprite, 104f);
        AddLog("デバッグコマンドパネルを起動しました。");
        RefreshCommandList();
    }

    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("RuntimeDebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static RectTransform CreateChoicePopupRoot(RectTransform parent, Sprite sprite)
    {
        var popup = new GameObject("ChoicePopup", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        popup.transform.SetParent(parent, false);

        RectTransform rect = popup.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(760f, 520f);

        Image image = popup.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.055f, 0.065f, 0.078f, 0.99f);

        VerticalLayoutGroup layout = popup.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        popup.SetActive(false);
        return rect;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("RuntimeDebugEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    private static RectTransform CreatePanelRoot(Transform parent, Sprite sprite)
    {
        var root = new GameObject("RuntimeDebugPanelRoot", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1660f, 900f);

        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.035f, 0.04f, 0.048f, 0.96f);
        return rect;
    }

    private static RectTransform CreateHeader(RectTransform parent, Font font, Sprite sprite)
    {
        var header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        header.transform.SetParent(parent, false);

        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(22f, -86f);
        rect.offsetMax = new Vector2(-22f, -22f);

        HorizontalLayoutGroup layout = header.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;

        var promptObject = new GameObject("Prompt", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        promptObject.transform.SetParent(header.transform, false);
        promptObject.GetComponent<Image>().sprite = sprite;
        promptObject.GetComponent<Image>().color = new Color(0.18f, 0.42f, 0.55f, 0.95f);
        promptObject.GetComponent<LayoutElement>().preferredWidth = 62f;

        Text prompt = DebugUiFactory.CreateLabel(promptObject.GetComponent<RectTransform>(), font, 26, TextAnchor.MiddleCenter, Color.white);
        prompt.text = ">";
        DebugUiFactory.Stretch(prompt.rectTransform, 0f, 0f, 0f, 0f);
        return rect;
    }

    private static RectTransform CreateMainArea(RectTransform parent)
    {
        var main = new GameObject("MainArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        main.transform.SetParent(parent, false);

        RectTransform rect = main.GetComponent<RectTransform>();
        DebugUiFactory.Stretch(rect, 22f, 102f, 22f, 202f);

        HorizontalLayoutGroup layout = main.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        return rect;
    }

    private static RectTransform CreatePanelSection(RectTransform parent, string name, float preferredWidth, float flexibleWidth)
    {
        var section = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        section.transform.SetParent(parent, false);

        Image image = section.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.035f);

        LayoutElement layoutElement = section.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.flexibleWidth = flexibleWidth;
        layoutElement.flexibleHeight = 1f;

        VerticalLayoutGroup layout = section.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        return section.GetComponent<RectTransform>();
    }

    private static RectTransform CreateLogPanel(RectTransform parent, Font font, Sprite sprite)
    {
        var log = new GameObject("LogPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        log.transform.SetParent(parent, false);

        RectTransform rect = log.GetComponent<RectTransform>();
        DebugUiFactory.Stretch(rect, 22f, 708f, 22f, 22f);

        Image image = log.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.025f, 0.03f, 0.036f, 0.98f);

        VerticalLayoutGroup layout = log.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        return rect;
    }

    private static void CreateSectionTitle(RectTransform parent, Font font, string title)
    {
        Text label = DebugUiFactory.CreateLabel(parent, font, 14, TextAnchor.MiddleLeft, new Color(0.55f, 0.72f, 0.84f));
        label.text = title;
        label.GetComponent<LayoutElement>().preferredHeight = 22f;
    }

    private static RectTransform CreateCategoryFilterBar(RectTransform parent)
    {
        var bar = new GameObject("CategoryFilterBar", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        bar.transform.SetParent(parent, false);

        LayoutElement layoutElement = bar.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 68f;
        layoutElement.flexibleWidth = 1f;

        GridLayoutGroup layout = bar.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(104f, 30f);
        layout.spacing = new Vector2(6f, 6f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 7;
        return bar.GetComponent<RectTransform>();
    }

    private void SetVisible(bool visible)
    {
        if (_panelRoot == null)
        {
            return;
        }

        bool wasVisible = _panelRoot.gameObject.activeSelf;
        if (visible && !wasVisible)
        {
            DebugScenarioTestPanel.HideNow();
            _resumeTimeScale = Time.timeScale;
            _inputBlockedBeforeVisible = InputSystemBase.IsInputBlockedForDebugUi;
            InputSystemBase.SetDebugUiInputCaptured(true);
            InputSystemBase.SetInputBlockedForDebugUi(true);
            _hasPausedGame = true;
            Time.timeScale = 0f;
        }

        _panelRoot.gameObject.SetActive(visible);
        HideChoicePopup();
        if (visible)
        {
            RefreshCommandList();
            FocusSearchInput();
            if (_selectedCommand != null)
            {
                SelectCommand(_selectedCommand);
            }
        }
        else if (wasVisible && _hasPausedGame)
        {
            Time.timeScale = _resumeTimeScale;
            InputSystemBase.SetDebugUiInputCaptured(false);
            InputSystemBase.SetInputBlockedForDebugUi(_inputBlockedBeforeVisible);
            ClearSelectedDebugUi();
            _hasPausedGame = false;
        }
    }

    private bool IsTextInputFocused()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null)
        {
            return false;
        }

        InputField inputField = selected.GetComponent<InputField>() ?? selected.GetComponentInParent<InputField>();
        return inputField != null && inputField.isFocused;
    }

    private void ClearSelectedDebugUi()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
        {
            return;
        }

        Transform selected = EventSystem.current.currentSelectedGameObject.transform;
        if (_panelRoot != null && selected.IsChildOf(_panelRoot))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void RefreshCommandList()
    {
        if (_commandListContent == null)
        {
            return;
        }

        foreach (Transform child in _commandListContent)
        {
            Destroy(child.gameObject);
        }

        _commandRows.Clear();
        Font font = LoadFont();
        Sprite sprite = GetFallbackSprite();
        string search = _searchInput != null ? _searchInput.text : string.Empty;
        var context = new DebugCommandContext(new DebugArgumentValues());
        IReadOnlyList<DebugCommand> allVisibleCommands = DebugCommandRegistry.GetVisibleCommands(context, string.Empty);
        RefreshCategoryFilters(allVisibleCommands, font, sprite);

        IEnumerable<DebugCommand> filteredCommands = DebugCommandRegistry.GetVisibleCommands(context, search);
        if (!string.IsNullOrWhiteSpace(_selectedCategory))
        {
            filteredCommands = filteredCommands.Where(command => string.Equals(command.Category, _selectedCategory, StringComparison.Ordinal));
        }

        _visibleCommands = filteredCommands.ToArray();

        if (_selectedIndex >= _visibleCommands.Count)
        {
            _selectedIndex = Mathf.Max(0, _visibleCommands.Count - 1);
        }

        for (int i = 0; i < _visibleCommands.Count; i++)
        {
            DebugCommand command = _visibleCommands[i];
            DebugCommand captured = command;
            int capturedIndex = i;
            Image rowImage = CreateCommandRow(_commandListContent, font, sprite, command, () =>
            {
                _selectedIndex = capturedIndex;
                SelectCommand(captured);
                UpdateCommandRowSelection();
            });
            _commandRows.Add(rowImage);
        }

        if (_visibleCommands.Count > 0)
        {
            SelectCommand(_visibleCommands[_selectedIndex]);
        }
        else
        {
            SelectNoCommand();
        }

        UpdateCommandRowSelection();
    }

    private void RefreshCategoryFilters(IReadOnlyList<DebugCommand> commands, Font font, Sprite sprite)
    {
        if (_categoryFilterContent == null)
        {
            return;
        }

        foreach (Transform child in _categoryFilterContent)
        {
            Destroy(child.gameObject);
        }

        string[] categories = commands
            .Select(command => command.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(_selectedCategory) && !categories.Contains(_selectedCategory))
        {
            _selectedCategory = string.Empty;
        }

        CreateCategoryButton("すべて", string.Empty, font, sprite);
        foreach (string category in categories)
        {
            CreateCategoryButton(category, category, font, sprite);
        }

        Button clearSearch = DebugUiFactory.CreateButton(
            _categoryFilterContent,
            font,
            sprite,
            "検索クリア",
            () =>
            {
                if (_searchInput != null)
                {
                    _searchInput.text = string.Empty;
                }

                _selectedIndex = 0;
                RefreshCommandList();
            },
            118f,
            28f);
        clearSearch.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.25f, 0.96f);
    }

    private void CreateCategoryButton(string label, string categoryValue, Font font, Sprite sprite)
    {
        bool selected = string.Equals(_selectedCategory, categoryValue, StringComparison.Ordinal);
        Color categoryColor = GetCategoryColor(categoryValue);
        Button button = DebugUiFactory.CreateButton(
            _categoryFilterContent,
            font,
            sprite,
            label,
            () =>
            {
                _selectedCategory = categoryValue;
                _selectedIndex = 0;
                RefreshCommandList();
            },
            108f,
            28f);
        button.GetComponent<Image>().color = selected
            ? new Color(categoryColor.r, categoryColor.g, categoryColor.b, 0.96f)
            : new Color(categoryColor.r, categoryColor.g, categoryColor.b, 0.32f);
    }

    private void SelectCommand(DebugCommand command)
    {
        _selectedCommand = command;
        _valueReaders.Clear();
        HideChoicePopup();

        foreach (Transform child in _argumentContent)
        {
            Destroy(child.gameObject);
        }

        var context = new DebugCommandContext(new DebugArgumentValues());
        string unavailableReason = command.GetUnavailableReason(context);
        _titleText.text = $"{command.Category} / {command.Label}";
        _descriptionText.text = string.IsNullOrWhiteSpace(command.Description) ? command.Id : command.Description;
        _executeButton.interactable = string.IsNullOrWhiteSpace(unavailableReason);
        _statusText.text = string.IsNullOrWhiteSpace(unavailableReason) ? string.Empty : unavailableReason;

        Font font = LoadFont();
        Sprite sprite = GetFallbackSprite();

        if (command.Arguments.Count == 0)
        {
            _emptyArgumentText = DebugUiFactory.CreateLabel(_argumentContent, font, 15, TextAnchor.MiddleLeft, new Color(0.68f, 0.74f, 0.82f));
            _emptyArgumentText.text = "引数はありません。Enterで実行できます。";
            return;
        }

        foreach (DebugArgumentDefinition argument in command.Arguments)
        {
            CreateArgumentField(argument, font, sprite);
        }
    }

    private void CreateArgumentField(DebugArgumentDefinition argument, Font font, Sprite sprite)
    {
        switch (argument.Kind)
        {
            case DebugArgumentKind.Bool:
                Toggle toggle = DebugUiFactory.CreateToggleRow(_argumentContent, font, argument.Label, Convert.ToBoolean(argument.DefaultValue));
                _valueReaders[argument.Key] = () => toggle.isOn;
                break;
            case DebugArgumentKind.Int:
                InputField intInput = DebugUiFactory.CreateInputRow(_argumentContent, font, sprite, argument.Label, argument.DefaultValue?.ToString() ?? "0");
                intInput.contentType = InputField.ContentType.IntegerNumber;
                intInput.text = argument.DefaultValue?.ToString() ?? "0";
                _valueReaders[argument.Key] = () => ParseInt(argument, intInput.text);
                break;
            case DebugArgumentKind.Float:
                if (argument.MinValue is float min && argument.MaxValue is float max && max > min)
                {
                    float defaultValue = argument.DefaultValue is float value ? value : 0f;
                    Slider slider = DebugUiFactory.CreateSliderRow(_argumentContent, font, sprite, argument.Label, min, max, defaultValue, out InputField sliderInput);
                    bool syncing = false;
                    slider.onValueChanged.AddListener(value =>
                    {
                        if (syncing)
                        {
                            return;
                        }

                        syncing = true;
                        sliderInput.text = value.ToString("0.##");
                        syncing = false;
                    });
                    sliderInput.onEndEdit.AddListener(raw =>
                    {
                        if (syncing)
                        {
                            return;
                        }

                        syncing = true;
                        slider.value = ParseFloat(argument, raw);
                        sliderInput.text = slider.value.ToString("0.##");
                        syncing = false;
                    });
                    _valueReaders[argument.Key] = () => slider.value;
                }
                else
                {
                    InputField floatInput = DebugUiFactory.CreateInputRow(_argumentContent, font, sprite, argument.Label, argument.DefaultValue?.ToString() ?? "0");
                    floatInput.contentType = InputField.ContentType.DecimalNumber;
                    floatInput.text = argument.DefaultValue?.ToString() ?? "0";
                    _valueReaders[argument.Key] = () => ParseFloat(argument, floatInput.text);
                }
                break;
            case DebugArgumentKind.String:
                InputField stringInput = DebugUiFactory.CreateInputRow(_argumentContent, font, sprite, argument.Label, argument.DefaultValue as string ?? string.Empty);
                stringInput.text = argument.DefaultValue as string ?? string.Empty;
                _valueReaders[argument.Key] = () => stringInput.text;
                break;
            case DebugArgumentKind.Enum:
                Array values = Enum.GetValues(argument.ValueType);
                List<string> options = values.Cast<object>().Select(value => value.ToString()).ToList();
                int defaultIndex = Mathf.Max(0, options.IndexOf(argument.DefaultValue.ToString()));
                CreateChoiceField(argument.Label, options, defaultIndex, font, sprite, out Func<int> enumIndexReader);
                _valueReaders[argument.Key] = () => values.GetValue(enumIndexReader.Invoke());
                break;
            case DebugArgumentKind.Option:
                IReadOnlyList<DebugOption> debugOptions = DebugOptionProviderRegistry.GetOptions(argument.OptionProviderId, new DebugCommandContext(new DebugArgumentValues()));
                List<string> optionLabels = debugOptions.Select(option => option.DisplayText).ToList();
                if (optionLabels.Count == 0)
                {
                    optionLabels.Add("候補がありません");
                }

                int optionDefaultIndex = 0;
                string defaultOptionId = argument.DefaultValue as string;
                if (!string.IsNullOrWhiteSpace(defaultOptionId))
                {
                    for (int i = 0; i < debugOptions.Count; i++)
                    {
                        if (debugOptions[i].Id == defaultOptionId)
                        {
                            optionDefaultIndex = i;
                            break;
                        }
                    }
                }

                CreateChoiceField(argument.Label, optionLabels, optionDefaultIndex, font, sprite, out Func<int> optionIndexReader);
                _valueReaders[argument.Key] = () =>
                {
                    int index = optionIndexReader.Invoke();
                    return index >= 0 && index < debugOptions.Count
                        ? debugOptions[index].Id
                        : string.Empty;
                };
                break;
        }
    }

    private void CreateChoiceField(string label, IReadOnlyList<string> options, int defaultIndex, Font font, Sprite sprite, out Func<int> indexReader)
    {
        int selectedIndex = Mathf.Clamp(defaultIndex, 0, Mathf.Max(0, options.Count - 1));
        Button button = DebugUiFactory.CreateChoiceRow(_argumentContent, font, sprite, label, out Text valueText);

        void UpdateLabel()
        {
            valueText.text = options.Count > 0 && selectedIndex >= 0 && selectedIndex < options.Count
                ? options[selectedIndex]
                : string.Empty;
        }

        UpdateLabel();
        button.onClick.AddListener(() =>
        {
            ShowStableSearchableChoicePopup(button.GetComponent<RectTransform>(), options, selectedIndex, string.Empty, 0, nextIndex =>
            {
                selectedIndex = nextIndex;
                UpdateLabel();
            });
        });

        indexReader = () => selectedIndex;
    }

    private void ShowChoicePopup(RectTransform source, IReadOnlyList<string> options, int selectedIndex, Action<int> onSelected)
    {
        int pageStart = Mathf.Clamp(selectedIndex - 5, 0, Mathf.Max(0, options.Count - 10));
        ShowChoicePopupPage(source, options, selectedIndex, onSelected, pageStart);
    }

    private void ShowChoicePopupPage(RectTransform source, IReadOnlyList<string> options, int selectedIndex, Action<int> onSelected, int pageStart)
    {
        if (_choicePopupRoot == null)
        {
            return;
        }

        foreach (Transform child in _choicePopupRoot)
        {
            Destroy(child.gameObject);
        }

        Font font = LoadFont();
        Sprite sprite = GetFallbackSprite();
        int itemSlots = options.Count > 12 ? 10 : 12;
        int pageEnd = Mathf.Min(options.Count, pageStart + itemSlots);
        int visibleRows = pageEnd - pageStart;

        if (pageStart > 0)
        {
            Button previous = DebugUiFactory.CreateButton(
                _choicePopupRoot,
                font,
                sprite,
                "<< 前の候補",
                () => ShowChoicePopupPage(source, options, selectedIndex, onSelected, Mathf.Max(0, pageStart - itemSlots)),
                390f,
                30f,
                true);
            previous.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.28f, 0.96f);
            visibleRows++;
        }

        for (int i = pageStart; i < pageEnd; i++)
        {
            int capturedIndex = i;
            string prefix = capturedIndex == selectedIndex ? "> " : "  ";
            Button item = DebugUiFactory.CreateButton(
                _choicePopupRoot,
                font,
                sprite,
                prefix + options[capturedIndex],
                () =>
                {
                    onSelected?.Invoke(capturedIndex);
                    HideChoicePopup();
                },
                390f,
                30f,
                true);
            item.GetComponent<Image>().color = capturedIndex == selectedIndex
                ? new Color(0.12f, 0.35f, 0.50f, 0.96f)
                : new Color(1f, 1f, 1f, 0.10f);
        }

        if (pageEnd < options.Count)
        {
            Button next = DebugUiFactory.CreateButton(
                _choicePopupRoot,
                font,
                sprite,
                "次の候補 >>",
                () => ShowChoicePopupPage(source, options, selectedIndex, onSelected, pageEnd),
                390f,
                30f,
                true);
            next.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.28f, 0.96f);
            visibleRows++;
        }

        PositionChoicePopup(source, visibleRows);
        _choicePopupRoot.SetAsLastSibling();
        _choicePopupRoot.gameObject.SetActive(true);
    }

    private void PositionChoicePopup(RectTransform source, int visibleItemCount)
    {
        if (_panelRoot == null || _choicePopupRoot == null || source == null)
        {
            return;
        }

        float width = Mathf.Min(780f, _panelRoot.rect.width - 96f);
        float height = Mathf.Min(560f, Mathf.Clamp(visibleItemCount, 6, 12) * 38f + 84f);
        _choicePopupRoot.sizeDelta = new Vector2(width, height);
        _choicePopupRoot.anchoredPosition = Vector2.zero;
    }

    private void ShowStableSearchableChoicePopup(
        RectTransform source,
        IReadOnlyList<string> options,
        int selectedIndex,
        string search,
        int pageStart,
        Action<int> onSelected)
    {
        if (_choicePopupRoot == null)
        {
            return;
        }

        foreach (Transform child in _choicePopupRoot)
        {
            Destroy(child.gameObject);
        }

        Font font = LoadFont();
        Sprite sprite = GetFallbackSprite();
        int itemSlots = 8;
        int currentPageStart = Mathf.Max(0, pageStart);

        InputField searchInput = DebugUiFactory.CreateInputField(_choicePopupRoot, font, sprite, "候補を検索");
        searchInput.SetTextWithoutNotify(search ?? string.Empty);
        searchInput.GetComponent<LayoutElement>().preferredHeight = 34f;

        Button closeButton = DebugUiFactory.CreateButton(
            _choicePopupRoot,
            font,
            sprite,
            "閉じる（選択を変更しない）",
            HideChoicePopup,
            720f,
            30f,
            true);
        closeButton.GetComponent<Image>().color = new Color(0.16f, 0.20f, 0.24f, 0.96f);

        var resultsObject = new GameObject("ChoiceResults", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        resultsObject.transform.SetParent(_choicePopupRoot, false);
        RectTransform resultsRoot = resultsObject.GetComponent<RectTransform>();
        LayoutElement resultsLayout = resultsObject.GetComponent<LayoutElement>();
        resultsLayout.flexibleWidth = 1f;
        resultsLayout.preferredHeight = 420f;

        VerticalLayoutGroup resultsGroup = resultsObject.GetComponent<VerticalLayoutGroup>();
        resultsGroup.spacing = 4f;
        resultsGroup.childControlWidth = true;
        resultsGroup.childControlHeight = true;
        resultsGroup.childForceExpandWidth = true;
        resultsGroup.childForceExpandHeight = false;

        void RenderResults()
        {
            foreach (Transform child in resultsRoot)
            {
                Destroy(child.gameObject);
            }

            List<int> matches = BuildChoiceMatches(options, searchInput.text);
            int maxPageStart = Mathf.Max(0, matches.Count - itemSlots);
            currentPageStart = Mathf.Clamp(currentPageStart, 0, maxPageStart);
            int rows = 3;

            if (matches.Count == 0)
            {
                Text empty = DebugUiFactory.CreateLabel(resultsRoot, font, 14, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.9f));
                empty.text = "一致する候補がありません";
                empty.GetComponent<LayoutElement>().preferredHeight = 30f;
                PositionChoicePopup(source, rows);
                return;
            }

            int pageEnd = Mathf.Min(matches.Count, currentPageStart + itemSlots);
            Text pageLabel = DebugUiFactory.CreateLabel(resultsRoot, font, 12, TextAnchor.MiddleLeft, new Color(0.58f, 0.70f, 0.82f));
            pageLabel.text = $"{currentPageStart + 1}-{pageEnd} / {matches.Count}";
            pageLabel.GetComponent<LayoutElement>().preferredHeight = 22f;
            rows++;

            for (int i = currentPageStart; i < pageEnd; i++)
            {
                int optionIndex = matches[i];
                string prefix = optionIndex == selectedIndex ? "> " : "  ";
                Button item = DebugUiFactory.CreateButton(
                    resultsRoot,
                    font,
                    sprite,
                    prefix + options[optionIndex],
                    () =>
                    {
                        onSelected?.Invoke(optionIndex);
                        HideChoicePopup();
                    },
                    720f,
                    30f,
                    true);
                item.GetComponent<Image>().color = optionIndex == selectedIndex
                    ? new Color(0.12f, 0.35f, 0.50f, 0.96f)
                    : new Color(1f, 1f, 1f, 0.10f);
                rows++;
            }

            if (matches.Count > itemSlots)
            {
                var navRow = new GameObject("ChoiceNavigation", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                navRow.transform.SetParent(resultsRoot, false);
                navRow.GetComponent<LayoutElement>().preferredHeight = 30f;
                HorizontalLayoutGroup navLayout = navRow.GetComponent<HorizontalLayoutGroup>();
                navLayout.spacing = 6f;
                navLayout.childControlWidth = true;
                navLayout.childForceExpandWidth = true;
                navLayout.childControlHeight = true;

                Button previous = DebugUiFactory.CreateButton(
                    navRow.GetComponent<RectTransform>(),
                    font,
                    sprite,
                    "前へ",
                    () =>
                    {
                        currentPageStart = Mathf.Max(0, currentPageStart - itemSlots);
                        RenderResults();
                    },
                    120f,
                    30f,
                    true);
                previous.interactable = currentPageStart > 0;

                Button next = DebugUiFactory.CreateButton(
                    navRow.GetComponent<RectTransform>(),
                    font,
                    sprite,
                    "次へ",
                    () =>
                    {
                        currentPageStart = Mathf.Min(maxPageStart, currentPageStart + itemSlots);
                        RenderResults();
                    },
                    120f,
                    30f,
                    true);
                next.interactable = pageEnd < matches.Count;
                rows++;
            }

            PositionChoicePopup(source, rows);
        }

        bool refreshQueued = false;
        searchInput.onValueChanged.AddListener(_ =>
        {
            currentPageStart = 0;
            if (refreshQueued)
            {
                return;
            }

            refreshQueued = true;
            StartCoroutine(RefreshChoiceResultsNextFrame(() =>
            {
                refreshQueued = false;
                if (_choicePopupRoot != null && _choicePopupRoot.gameObject.activeSelf && searchInput != null)
                {
                    RenderResults();
                }
            }));
        });

        RenderResults();
        _choicePopupRoot.SetAsLastSibling();
        _choicePopupRoot.gameObject.SetActive(true);
        searchInput.ActivateInputField();
        EventSystem.current?.SetSelectedGameObject(searchInput.gameObject);
    }

    private static IEnumerator RefreshChoiceResultsNextFrame(Action refresh)
    {
        yield return null;
        while (!string.IsNullOrEmpty(Input.compositionString))
        {
            yield return null;
        }

        refresh?.Invoke();
    }

    private void ShowSearchableChoicePopup(
        RectTransform source,
        IReadOnlyList<string> options,
        int selectedIndex,
        string search,
        int pageStart,
        Action<int> onSelected)
    {
        if (_choicePopupRoot == null)
        {
            return;
        }

        foreach (Transform child in _choicePopupRoot)
        {
            Destroy(child.gameObject);
        }

        Font font = LoadFont();
        Sprite sprite = GetFallbackSprite();
        string normalizedSearch = search ?? string.Empty;
        List<int> matches = BuildChoiceMatches(options, normalizedSearch);
        int itemSlots = 8;
        int maxPageStart = Mathf.Max(0, matches.Count - itemSlots);
        int clampedPageStart = Mathf.Clamp(pageStart, 0, maxPageStart);

        InputField searchInput = DebugUiFactory.CreateInputField(_choicePopupRoot, font, sprite, "候補を検索");
        searchInput.text = normalizedSearch;
        searchInput.GetComponent<LayoutElement>().preferredHeight = 34f;
        searchInput.onValueChanged.AddListener(nextSearch =>
            ShowSearchableChoicePopup(source, options, selectedIndex, nextSearch, 0, onSelected));

        int rows = 1;
        if (matches.Count == 0)
        {
            Text empty = DebugUiFactory.CreateLabel(_choicePopupRoot, font, 14, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.9f));
            empty.text = "一致する候補がありません";
            empty.GetComponent<LayoutElement>().preferredHeight = 30f;
            rows++;
        }
        else
        {
            int pageEnd = Mathf.Min(matches.Count, clampedPageStart + itemSlots);
            Text pageLabel = DebugUiFactory.CreateLabel(_choicePopupRoot, font, 12, TextAnchor.MiddleLeft, new Color(0.58f, 0.70f, 0.82f));
            pageLabel.text = $"{clampedPageStart + 1}-{pageEnd} / {matches.Count}";
            pageLabel.GetComponent<LayoutElement>().preferredHeight = 22f;
            rows++;

            for (int i = clampedPageStart; i < pageEnd; i++)
            {
                int optionIndex = matches[i];
                string prefix = optionIndex == selectedIndex ? "> " : "  ";
                Button item = DebugUiFactory.CreateButton(
                    _choicePopupRoot,
                    font,
                    sprite,
                    prefix + options[optionIndex],
                    () =>
                    {
                        onSelected?.Invoke(optionIndex);
                        HideChoicePopup();
                    },
                    720f,
                    30f,
                    true);
                item.GetComponent<Image>().color = optionIndex == selectedIndex
                    ? new Color(0.12f, 0.35f, 0.50f, 0.96f)
                    : new Color(1f, 1f, 1f, 0.10f);
                rows++;
            }

            if (matches.Count > itemSlots)
            {
                var navRow = new GameObject("ChoiceNavigation", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                navRow.transform.SetParent(_choicePopupRoot, false);
                navRow.GetComponent<LayoutElement>().preferredHeight = 30f;
                HorizontalLayoutGroup navLayout = navRow.GetComponent<HorizontalLayoutGroup>();
                navLayout.spacing = 6f;
                navLayout.childControlWidth = true;
                navLayout.childForceExpandWidth = true;
                navLayout.childControlHeight = true;

                Button previous = DebugUiFactory.CreateButton(
                    navRow.GetComponent<RectTransform>(),
                    font,
                    sprite,
                    "前へ",
                    () => ShowSearchableChoicePopup(source, options, selectedIndex, normalizedSearch, Mathf.Max(0, clampedPageStart - itemSlots), onSelected),
                    120f,
                    30f,
                    true);
                previous.interactable = clampedPageStart > 0;

                Button next = DebugUiFactory.CreateButton(
                    navRow.GetComponent<RectTransform>(),
                    font,
                    sprite,
                    "次へ",
                    () => ShowSearchableChoicePopup(source, options, selectedIndex, normalizedSearch, Mathf.Min(maxPageStart, clampedPageStart + itemSlots), onSelected),
                    120f,
                    30f,
                    true);
                next.interactable = pageEnd < matches.Count;
                rows++;
            }
        }

        PositionChoicePopup(source, rows);
        _choicePopupRoot.SetAsLastSibling();
        _choicePopupRoot.gameObject.SetActive(true);
        searchInput.ActivateInputField();
        EventSystem.current?.SetSelectedGameObject(searchInput.gameObject);
    }

    private static List<int> BuildChoiceMatches(IReadOnlyList<string> options, string search)
    {
        var matches = new List<int>();
        string normalizedSearch = string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim();
        for (int i = 0; i < options.Count; i++)
        {
            string option = options[i] ?? string.Empty;
            if (normalizedSearch.Length == 0 || option.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                matches.Add(i);
            }
        }

        return matches;
    }

    private void HideChoicePopup()
    {
        if (_choicePopupRoot == null)
        {
            return;
        }

        _choicePopupRoot.gameObject.SetActive(false);
    }

    private static int ParseInt(DebugArgumentDefinition argument, string raw)
    {
        int.TryParse(raw, out int value);
        if (argument.MinValue is int min)
        {
            value = Mathf.Max(min, value);
        }
        if (argument.MaxValue is int max)
        {
            value = Mathf.Min(max, value);
        }
        return value;
    }

    private static float ParseFloat(DebugArgumentDefinition argument, string raw)
    {
        float.TryParse(raw, out float value);
        if (argument.MinValue is float min)
        {
            value = Mathf.Max(min, value);
        }
        if (argument.MaxValue is float max)
        {
            value = Mathf.Min(max, value);
        }
        return value;
    }

    private void ExecuteSelectedCommand()
    {
        ExecuteSelectedCommandAsync().Forget();
    }

    private async UniTaskVoid ExecuteSelectedCommandAsync()
    {
        if (_selectedCommand == null)
        {
            return;
        }

        HideChoicePopup();
        var args = new DebugArgumentValues();
        foreach (KeyValuePair<string, Func<object>> pair in _valueReaders)
        {
            args.Set(pair.Key, pair.Value.Invoke());
        }

        _statusText.color = new Color(0.95f, 0.86f, 0.55f);
        _statusText.text = "実行中...";
        AddLog($"> {_selectedCommand.Category}/{_selectedCommand.Label}");
        DebugCommandResult result = await _selectedCommand.ExecuteAsync(new DebugCommandContext(args));
        _statusText.color = result.IsSuccess ? new Color(0.6f, 1f, 0.72f) : new Color(1f, 0.55f, 0.5f);
        _statusText.text = string.IsNullOrWhiteSpace(result.Message)
            ? result.IsSuccess ? "Success" : "Failed"
            : result.Message;
        AddLog($"{(result.IsSuccess ? "OK" : "ERR")}  {_statusText.text}");
        if (result.IsSuccess && _selectedCommand.ClosePanelAfterExecute)
        {
            SetVisible(false);
        }
    }

    private static Image CreateCommandRow(RectTransform parent, Font font, Sprite sprite, DebugCommand command, Action onClick)
    {
        var row = new GameObject(command.Id + "Row", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        Image image = row.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.04f);

        LayoutElement layout = row.GetComponent<LayoutElement>();
        layout.preferredHeight = 48f;

        Button button = row.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());

        var stripeObject = new GameObject("CategoryStripe", typeof(RectTransform), typeof(Image));
        stripeObject.transform.SetParent(row.transform, false);
        RectTransform stripeRect = stripeObject.GetComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0f, 0f);
        stripeRect.anchorMax = new Vector2(0f, 1f);
        stripeRect.pivot = new Vector2(0f, 0.5f);
        stripeRect.sizeDelta = new Vector2(4f, 0f);
        stripeRect.anchoredPosition = Vector2.zero;
        stripeObject.GetComponent<Image>().color = GetCategoryColor(command.Category);

        Text category = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 12, TextAnchor.MiddleCenter, new Color(0.78f, 0.9f, 1f));
        category.text = command.Category.ToUpperInvariant();
        category.horizontalOverflow = HorizontalWrapMode.Overflow;
        category.verticalOverflow = VerticalWrapMode.Truncate;
        category.GetComponent<LayoutElement>().ignoreLayout = true;
        category.rectTransform.anchorMin = new Vector2(0f, 0f);
        category.rectTransform.anchorMax = new Vector2(0f, 1f);
        category.rectTransform.pivot = new Vector2(0f, 0.5f);
        category.rectTransform.anchoredPosition = new Vector2(14f, 0f);
        category.rectTransform.sizeDelta = new Vector2(92f, 0f);

        Text title = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 18, TextAnchor.UpperLeft, Color.white);
        title.text = command.Label;
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        title.verticalOverflow = VerticalWrapMode.Truncate;
        title.GetComponent<LayoutElement>().ignoreLayout = true;
        DebugUiFactory.Stretch(title.rectTransform, 118f, 6f, 230f, 21f);

        Text description = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 12, TextAnchor.LowerLeft, new Color(0.62f, 0.68f, 0.74f));
        description.text = string.IsNullOrWhiteSpace(command.Description)
            ? "説明は未設定です"
            : Truncate(command.Description, 120);
        description.horizontalOverflow = HorizontalWrapMode.Overflow;
        description.verticalOverflow = VerticalWrapMode.Truncate;
        description.GetComponent<LayoutElement>().ignoreLayout = true;
        DebugUiFactory.Stretch(description.rectTransform, 118f, 27f, 18f, 5f);

        Text id = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 11, TextAnchor.UpperRight, new Color(0.54f, 0.60f, 0.66f));
        id.text = command.Id;
        id.horizontalOverflow = HorizontalWrapMode.Overflow;
        id.verticalOverflow = VerticalWrapMode.Truncate;
        id.GetComponent<LayoutElement>().ignoreLayout = true;
        DebugUiFactory.Stretch(id.rectTransform, 520f, 7f, 14f, 25f);
        return image;
    }

    private void SelectNoCommand()
    {
        _selectedCommand = null;
        _valueReaders.Clear();
        foreach (Transform child in _argumentContent)
        {
            Destroy(child.gameObject);
        }

        _titleText.text = "No matching command";
        _descriptionText.text = "Change the command line filter.";
        _executeButton.interactable = false;
        _statusText.text = string.Empty;
    }

    private void MoveSelection(int delta)
    {
        if (_visibleCommands.Count == 0)
        {
            return;
        }

        _selectedIndex = (_selectedIndex + delta + _visibleCommands.Count) % _visibleCommands.Count;
        SelectCommand(_visibleCommands[_selectedIndex]);
        UpdateCommandRowSelection();
    }

    private void UpdateCommandRowSelection()
    {
        for (int i = 0; i < _commandRows.Count; i++)
        {
            _commandRows[i].color = i == _selectedIndex
                ? new Color(0.12f, 0.35f, 0.50f, 0.92f)
                : new Color(1f, 1f, 1f, 0.04f);
        }
    }

    private static Color GetCategoryColor(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return new Color(0.45f, 0.62f, 0.74f);
        }

        if (category.Contains("シーン") || category.Contains("Scene"))
        {
            return new Color(0.28f, 0.58f, 0.95f);
        }

        if (category.Contains("カード") || category.Contains("Card"))
        {
            return new Color(0.28f, 0.78f, 0.58f);
        }

        if (category.Contains("会話") || category.Contains("Dialogue"))
        {
            return new Color(0.68f, 0.48f, 0.95f);
        }

        if (category.Contains("マップ") || category.Contains("Stage"))
        {
            return new Color(0.88f, 0.68f, 0.28f);
        }

        if (category.Contains("システム") || category.Contains("System"))
        {
            return new Color(0.95f, 0.42f, 0.44f);
        }

        if (category.Contains("テスト") || category.Contains("Test"))
        {
            return new Color(0.40f, 0.86f, 0.62f);
        }

        if (category.Contains("セーブ") || category.Contains("Save"))
        {
            return new Color(0.50f, 0.95f, 0.70f);
        }

        if (category.Contains("プレイヤー") || category.Contains("Player"))
        {
            return new Color(1f, 0.72f, 0.35f);
        }

        if (category.Contains("表示") || category.Contains("検証") || category.Contains("Utility"))
        {
            return new Color(0.45f, 0.78f, 0.95f);
        }

        switch (category)
        {
            case "Scene":
            case "シーン":
                return new Color(0.45f, 0.70f, 1f);
            case "Save":
            case "セーブ":
                return new Color(0.50f, 0.95f, 0.70f);
            case "Player":
            case "プレイヤー":
                return new Color(1f, 0.72f, 0.35f);
            case "Utility":
            case "ユーティリティ":
                return new Color(0.78f, 0.62f, 1f);
            default:
                return new Color(0.55f, 0.72f, 0.84f);
        }
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength - 3) + "...";
    }

    private void AddLog(string message)
    {
        if (_logContent == null)
        {
            return;
        }

        _logLines.Add(message);
        while (_logLines.Count > MaxLogLines)
        {
            _logLines.RemoveAt(0);
        }

        foreach (Transform child in _logContent)
        {
            Destroy(child.gameObject);
        }

        Font font = LoadFont();
        foreach (string line in _logLines)
        {
            Text text = DebugUiFactory.CreateLabel(_logContent, font, 13, TextAnchor.MiddleLeft, GetLogColor(line));
            text.text = line;
            text.GetComponent<LayoutElement>().preferredHeight = 22f;
        }
    }

    private static Color GetLogColor(string line)
    {
        if (line.StartsWith("ERR", StringComparison.Ordinal))
        {
            return new Color(1f, 0.48f, 0.46f);
        }

        if (line.StartsWith("OK", StringComparison.Ordinal))
        {
            return new Color(0.58f, 1f, 0.72f);
        }

        return new Color(0.78f, 0.84f, 0.9f);
    }

    private void FocusSearchInput()
    {
        if (_searchInput == null)
        {
            return;
        }

        _searchInput.ActivateInputField();
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_searchInput.gameObject);
        }
    }

    private static Sprite GetFallbackSprite()
    {
        if (_fallbackSprite != null)
        {
            return _fallbackSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        return _fallbackSprite;
    }

    private static Font LoadFont()
    {
#if UNITY_EDITOR
        Font projectFont = AssetDatabase.LoadAssetAtPath<Font>(PrimaryFontAssetPath);
        if (projectFont != null)
        {
            return projectFont;
        }

        projectFont = AssetDatabase.LoadAssetAtPath<Font>(SecondaryFontAssetPath);
        if (projectFont != null)
        {
            return projectFont;
        }
#endif
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

}
#endif
