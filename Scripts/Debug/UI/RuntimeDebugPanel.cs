#if UNITY_EDITOR || EIGHTAID_TEST_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using EightAID.EIGHTAIDLib.Input;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
    private const Key ToggleKey = Key.F1;
    private const string PrimaryFontAssetPath = "Assets/Fonts/Morisawa/BO-A1GothicStdN-Regular.otf";
    private const string SecondaryFontAssetPath = "Assets/Fonts/Morisawa/BO-A1GothicStdN-Medium.otf";
    private const int MaxLogLines = 18;

    private static RuntimeDebugPanel _instance;
    private static Sprite _fallbackSprite;
    private Font _font;
    private Canvas _canvas;
    private ScrollRect _commandScroll;
    private Button _subcategoryButton;
    private Text _commandCountText;
    private Text _emptyCommandText;
    private string[] _categories = Array.Empty<string>();
    private readonly Dictionary<string, Button> _categoryButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
    private readonly List<RuntimeDebugCommandRow> _rowViews = new List<RuntimeDebugCommandRow>();
    private const float CommandRowHeight = 56f;
    private const float CommandRowStride = 60f;
    private float _lastViewportHeight;
    private bool _refreshPending;
    private bool _rebuildArguments;
    private InputAction _cancelAction;
    private bool _cancelPending;
    private GameObject _previousSelected;

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
    private readonly Dictionary<string, List<GameObject>> _argumentRows = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);
    private readonly List<Image> _commandRows = new List<Image>();
    private readonly List<string> _logLines = new List<string>();

    public static Canvas DebugCanvas => _instance != null ? _instance._canvas : null;

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

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        ReleaseCancelAction();
    }

    private void ReleaseCancelAction()
    {
        if (_cancelAction == null) return;
        _cancelAction.performed -= HandleCancelAction;
        _cancelAction = null;
    }

    private void HandleCancelAction(InputAction.CallbackContext context)
    {
        _cancelPending = true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (DebugAvailability.IsEnabled && _panelRoot != null) EnsureEventSystem();
        _rebuildArguments = true;
    }

    private void Update()
    {
        if (!DebugAvailability.IsEnabled)
        {
            SetVisible(false);
            return;
        }

        if (DeviceInput.WasKeyPressed(ToggleKey))
        {
            if (_panelRoot == null)
            {
                BuildUi();
            }

            SetVisible(!_hasPausedGame);
            return;
        }

        if (_panelRoot == null || !_panelRoot.gameObject.activeSelf)
        {
            return;
        }

        InputSystemBase.SetInputBlockedForDebugUi(true);

        if (_cancelPending || DeviceInput.WasKeyPressed(Key.Escape))
        {
            _cancelPending = false;
            if (_choicePopupRoot != null && _choicePopupRoot.gameObject.activeSelf)
            {
                HideChoicePopup();
                return;
            }

            SetVisible(false);
            return;
        }

        if (_refreshPending)
        {
            _refreshPending = false;
            RefreshCommandList();
        }

        if (_choicePopupRoot.gameObject.activeSelf)
        {
            return;
        }

        if (IsTextInputFocused())
        {
            return;
        }

        GameObject currentSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        bool noSelection = currentSelection == null;
        if (noSelection && DeviceInput.WasKeyPressed(Key.UpArrow))
        {
            MoveSelection(-1);
        }
        else if (noSelection && DeviceInput.WasKeyPressed(Key.DownArrow))
        {
            MoveSelection(1);
        }
        else if ((noSelection || currentSelection.GetComponent<RuntimeDebugCommandRow>() != null) &&
                 (DeviceInput.WasKeyPressed(Key.Enter) || DeviceInput.WasKeyPressed(Key.NumpadEnter)))
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
        _canvas = canvas;

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
        _searchInput.onValueChanged.AddListener(_ => _refreshPending = true);
        DebugUiFactory.CreateButton(header, font, sprite, "検索クリア", () => _searchInput.text = string.Empty, 112f, 36f);

        CreateSectionTitle(left, font, "コマンド一覧");
        _categoryFilterContent = CreateCategoryFilterBar(left);
        _subcategoryButton = DebugUiFactory.CreateButton(left, font, sprite, "すべての分類  ▾", ShowSubcategoryChoices, 260f, 32f);
        _commandCountText = DebugUiFactory.CreateLabel(left, font, 13, TextAnchor.MiddleLeft, new Color(.65f, .76f, .85f));
        _commandCountText.GetComponent<LayoutElement>().preferredHeight = 20f;
        _commandListContent = DebugUiFactory.CreateScrollContent(left, sprite, 360f, true, false);
        _commandScroll = _commandListContent.GetComponentInParent<ScrollRect>();
        _commandScroll.onValueChanged.AddListener(_ => RefreshVirtualCommandRows());
        _emptyCommandText = DebugUiFactory.CreateLabel(_commandScroll.viewport, font, 16, TextAnchor.MiddleCenter, Color.white);
        _emptyCommandText.text = "該当するコマンドがありません。検索や分類を変更してください。";
        DebugUiFactory.Stretch(_emptyCommandText.rectTransform, 12f, 12f, 12f, 12f);
        _emptyCommandText.raycastTarget = false;
        _emptyCommandText.gameObject.SetActive(false);

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
        _panelRoot.gameObject.SetActive(false);
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

        var eventSystemObject = new GameObject("RuntimeDebugEventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        InputSystemTextInput.Attach(eventSystemObject.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>());
        // シーン側の EventSystem と競合しないよう、代替入力もシーンと一緒に破棄する。
        // 次のシーンに入力がなければ HandleSceneLoaded が再作成する。
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
        var bar = new GameObject("CategoryFilterBar", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        bar.transform.SetParent(parent, false);
        bar.GetComponent<LayoutElement>().preferredHeight = 34f;
        var layout = bar.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 5f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
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
            _previousSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
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
            EnsureEventSystem();
            _cancelPending = false;
            ReleaseCancelAction();
            var module = EventSystem.current.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
            _cancelAction = module != null ? module.cancel?.action : null;
            if (_cancelAction != null) _cancelAction.performed += HandleCancelAction;
            _rebuildArguments = true;
            _refreshPending = false;
            RefreshCommandList();
            FocusSearchInput();
        }
        else if (wasVisible && _hasPausedGame)
        {
            Time.timeScale = _resumeTimeScale;
            InputSystemBase.SetDebugUiInputCaptured(false);
            InputSystemBase.SetInputBlockedForDebugUi(_inputBlockedBeforeVisible);
            ReleaseCancelAction();
            ClearSelectedDebugUi();
            if (_previousSelected != null && _previousSelected.activeInHierarchy && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_previousSelected);
            }
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

        Font font = LoadFont();
        Sprite sprite = GetFallbackSprite();
        string search = _searchInput.text;
        var context = new DebugCommandContext(new DebugArgumentValues());
        IReadOnlyList<DebugCommand> allCommands = DebugCommandRegistry.GetVisibleCommands(context, string.Empty);
        RefreshCategoryFilters(allCommands, font, sprite);
        _visibleCommands = allCommands.Where(command => MatchesCategory(command.Category, _selectedCategory) &&
            DebugCommandRegistry.MatchesSearch(command, search)).ToArray();

        int previousIndex = _selectedCommand == null ? -1 : Array.FindIndex((DebugCommand[])_visibleCommands, command => ReferenceEquals(command, _selectedCommand));
        _selectedIndex = previousIndex >= 0 ? previousIndex : 0;
        _commandCountText.text = $"{_visibleCommands.Count} 件 / 全 {allCommands.Count} 件  ·  ↑↓ 選択  /  Enter 実行  /  Esc 閉じる";
        _emptyCommandText.gameObject.SetActive(_visibleCommands.Count == 0);
        _commandListContent.sizeDelta = new Vector2(_commandListContent.sizeDelta.x, _visibleCommands.Count * CommandRowStride + 10f);
        _commandScroll.StopMovement();
        EnsureSelectedCommandInView();
        RefreshVirtualCommandRows();

        if (_visibleCommands.Count > 0)
        {
            SelectCommand(_visibleCommands[_selectedIndex]);
        }
        else
        {
            SelectNoCommand();
        }
    }

    private static string CategoryGroup(string category)
    {
        int separator = category.IndexOf(" / ", StringComparison.Ordinal);
        return separator < 0 ? category : category.Substring(0, separator);
    }

    private static bool MatchesCategory(string category, string selected)
    {
        return string.IsNullOrEmpty(selected) || string.Equals(category, selected, StringComparison.Ordinal) ||
            category.StartsWith(selected + " / ", StringComparison.Ordinal);
    }

    private void RefreshCategoryFilters(IReadOnlyList<DebugCommand> commands, Font font, Sprite sprite)
    {
        _categories = commands.Select(command => command.Category).Distinct(StringComparer.Ordinal).ToArray();
        if (!_categories.Any(category => MatchesCategory(category, _selectedCategory)))
        {
            _selectedCategory = string.Empty;
        }

        string[] groups = new[] { string.Empty }.Concat(_categories.Select(CategoryGroup).Distinct(StringComparer.Ordinal)).ToArray();
        foreach (var pair in _categoryButtons)
        {
            pair.Value.gameObject.SetActive(groups.Contains(pair.Key));
        }
        foreach (string group in groups)
        {
            if (!_categoryButtons.TryGetValue(group, out Button button))
            {
                string value = group;
                button = DebugUiFactory.CreateButton(_categoryFilterContent, font, sprite,
                    string.IsNullOrEmpty(group) ? "すべて" : group,
                    () => { _selectedCategory = value; RefreshCommandList(); }, 80f, 34f, true);
                Text label = button.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                _categoryButtons.Add(group, button);
            }
            bool selected = group == CategoryGroup(_selectedCategory);
            button.GetComponent<Image>().color = selected ? new Color(.16f, .40f, .56f) : new Color(.12f, .17f, .22f);
        }
        _subcategoryButton.GetComponentInChildren<Text>().text = string.IsNullOrEmpty(_selectedCategory)
            ? "細分類: すべて  ▾" : $"細分類: {_selectedCategory}  ▾";
    }

    private void ShowSubcategoryChoices()
    {
        string group = CategoryGroup(_selectedCategory);
        string[] values = new[] { group }.Concat(_categories.Where(category => MatchesCategory(category, group) && category != group)).ToArray();
        string[] labels = values.Select((value, index) => index == 0 ? "この分類のすべて" : value).ToArray();
        ShowStableSearchableChoicePopup(_subcategoryButton.GetComponent<RectTransform>(), labels,
            Mathf.Max(0, Array.IndexOf(values, _selectedCategory)), string.Empty, 0,
            index => { _selectedCategory = values[index]; RefreshCommandList(); });
    }

    private void LateUpdate()
    {
        if (_panelRoot == null || !_panelRoot.gameObject.activeSelf)
        {
            return;
        }
        float height = _commandScroll.viewport.rect.height;
        if (!Mathf.Approximately(height, _lastViewportHeight))
        {
            _lastViewportHeight = height;
            EnsureSelectedCommandInView();
            RefreshVirtualCommandRows();
        }
    }

    private void RefreshVirtualCommandRows()
    {
        float height = Mathf.Max(0f, _commandScroll.viewport.rect.height);
        int first = Mathf.Clamp(Mathf.FloorToInt((_commandListContent.anchoredPosition.y - 5f) / CommandRowStride),
            0, Mathf.Max(0, _visibleCommands.Count - 1));
        int count = Mathf.Min(_visibleCommands.Count - first, Mathf.CeilToInt(height / CommandRowStride) + 2);
        while (_rowViews.Count < count)
        {
            Image image = CreateCommandRow(_commandListContent, LoadFont(), GetFallbackSprite());
            _commandRows.Add(image);
            _rowViews.Add(image.GetComponent<RuntimeDebugCommandRow>());
        }
        for (int i = 0; i < _rowViews.Count; i++)
        {
            RuntimeDebugCommandRow row = _rowViews[i];
            row.gameObject.SetActive(i < count);
            if (i >= count) continue;
            int index = first + i;
            row.Bind(_visibleCommands[index], index);
            var rect = (RectTransform)row.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -5f - index * CommandRowStride);
            rect.sizeDelta = new Vector2(-10f, CommandRowHeight);
        }
        UpdateCommandRowSelection();
    }

    private void SelectCommandAt(int index)
    {
        if (index < 0 || index >= _visibleCommands.Count) return;
        _selectedIndex = index;
        SelectCommand(_visibleCommands[index]);
        UpdateCommandRowSelection();
    }

    private void EnsureSelectedCommandInView()
    {
        float height = Mathf.Max(0f, _commandScroll.viewport.rect.height);
        float top = _commandListContent.anchoredPosition.y;
        float rowTop = 5f + _selectedIndex * CommandRowStride;
        if (rowTop < top) top = rowTop;
        else if (rowTop + CommandRowHeight > top + height) top = rowTop + CommandRowHeight - height;
        top = Mathf.Clamp(top, 0f, Mathf.Max(0f, _commandListContent.sizeDelta.y - height));
        _commandListContent.anchoredPosition = new Vector2(0f, top);
    }

    private void SelectCommand(DebugCommand command)
    {
        bool rebuild = _rebuildArguments || !ReferenceEquals(_selectedCommand, command);
        _rebuildArguments = false;
        _selectedCommand = command;
        if (!rebuild)
        {
            return;
        }
        _valueReaders.Clear();
        _argumentRows.Clear();
        HideChoicePopup();

        foreach (Transform child in _argumentContent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        var context = new DebugCommandContext(new DebugArgumentValues());
        string unavailableReason = command.GetUnavailableReason(context);
        _titleText.text = $"{command.Category} / {command.Label}";
        _descriptionText.text = command.Id + "\n" + command.Description;
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
            int firstChild = _argumentContent.childCount;
            CreateArgumentField(argument, font, sprite);
            var rows = new List<GameObject>();
            for (int i = firstChild; i < _argumentContent.childCount; i++)
            {
                rows.Add(_argumentContent.GetChild(i).gameObject);
            }
            _argumentRows[argument.Key] = rows;
        }
        RefreshArgumentVisibility();
    }

    private void RefreshArgumentVisibility()
    {
        if (_selectedCommand == null) return;
        foreach (DebugArgumentDefinition argument in _selectedCommand.Arguments)
        {
            bool visible = string.IsNullOrEmpty(argument.VisibleWhenKey) ||
                (_valueReaders.TryGetValue(argument.VisibleWhenKey, out Func<object> reader) &&
                 string.Equals(reader()?.ToString(), argument.VisibleWhenValue, StringComparison.Ordinal));
            if (!_argumentRows.TryGetValue(argument.Key, out List<GameObject> rows)) continue;
            foreach (GameObject row in rows) row.SetActive(visible);
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
                CreateChoiceField(argument.Label, options, defaultIndex, font, sprite, out Func<int> enumIndexReader, RefreshArgumentVisibility);
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

                CreateChoiceField(argument.Label, optionLabels, optionDefaultIndex, font, sprite, out Func<int> optionIndexReader, RefreshArgumentVisibility);
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

    private void CreateChoiceField(string label, IReadOnlyList<string> options, int defaultIndex, Font font, Sprite sprite, out Func<int> indexReader, Action onChanged = null)
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
                onChanged?.Invoke();
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
        while (!string.IsNullOrEmpty(EventSystem.current?.currentInputModule?.input.compositionString))
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

    private Image CreateCommandRow(RectTransform parent, Font font, Sprite sprite)
    {
        var row = new GameObject("CommandRow", typeof(RectTransform), typeof(Image), typeof(RuntimeDebugCommandRow));
        row.transform.SetParent(parent, false);
        Image image = row.GetComponent<Image>();
        image.sprite = sprite;
        var button = row.GetComponent<RuntimeDebugCommandRow>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => SelectCommandAt(button.CommandIndex));
        Text category = DebugUiFactory.CreateLabel((RectTransform)row.transform, font, 12, TextAnchor.MiddleCenter, new Color(.65f, .8f, .9f));
        DebugUiFactory.Stretch(category.rectTransform, 6f, 4f, 0f, 4f);
        category.rectTransform.anchorMax = new Vector2(0f, 1f);
        category.rectTransform.offsetMax = new Vector2(126f, -4f);
        Text title = DebugUiFactory.CreateLabel((RectTransform)row.transform, font, 18, TextAnchor.UpperLeft, Color.white);
        DebugUiFactory.Stretch(title.rectTransform, 140f, 5f, 10f, 27f);
        Text description = DebugUiFactory.CreateLabel((RectTransform)row.transform, font, 12, TextAnchor.LowerLeft, new Color(.64f, .70f, .77f));
        DebugUiFactory.Stretch(description.rectTransform, 140f, 31f, 10f, 5f);
        foreach (Text text in new[] { category, title, description })
        {
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }
        button.Initialize(category, title, description, SelectCommandAt, MoveSelection);
        return image;
    }

    private void SelectNoCommand()
    {
        _selectedCommand = null;
        _valueReaders.Clear();
        foreach (Transform child in _argumentContent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        _titleText.text = "該当するコマンドがありません";
        _descriptionText.text = "検索や分類を変更してください。";
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
        EnsureSelectedCommandInView();
        RefreshVirtualCommandRows();
        RuntimeDebugCommandRow selected = _rowViews.FirstOrDefault(row => row.gameObject.activeSelf && row.CommandIndex == _selectedIndex);
        if (selected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(selected.gameObject);
        }
    }

    private void UpdateCommandRowSelection()
    {
        for (int i = 0; i < _commandRows.Count; i++)
        {
            _commandRows[i].color = _rowViews[i].CommandIndex == _selectedIndex
                ? new Color(0.12f, 0.35f, 0.50f, 0.92f)
                : new Color(1f, 1f, 1f, 0.04f);
        }
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

    private Font LoadFont()
    {
        if (_font != null) return _font;
        _font = ResolveFont();
        return _font;
    }

    private static Font ResolveFont()
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
