#if UNITY_EDITOR || DAISHOU_TEST_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// DebugScenarioTestRegistry に登録されたテストケースを一覧表示し、順番に実行する汎用テストパネルです。
/// ゲーム固有の処理は DebugCommand と IDebugScenarioTestModule 側へ寄せ、このクラスは表示と実行制御だけを担当します。
/// </summary>
public sealed class DebugScenarioTestPanel : MonoBehaviour
{
    private const KeyCode ToggleKey = KeyCode.F2;
    private const string PrimaryFontAssetPath = "Assets/Fonts/Morisawa/BO-A1GothicStdN-Regular.otf";
    private const string SecondaryFontAssetPath = "Assets/Fonts/Morisawa/BO-A1GothicStdN-Medium.otf";

    private static DebugScenarioTestPanel _instance;
    private static Sprite _fallbackSprite;

    private readonly DebugScenarioTestRunner _runner = new DebugScenarioTestRunner();
    private readonly Dictionary<string, DebugScenarioTestRunResult> _results = new Dictionary<string, DebugScenarioTestRunResult>(StringComparer.Ordinal);
    private readonly List<Image> _testRows = new List<Image>();

    private RectTransform _panelRoot;
    private RectTransform _categoryContent;
    private RectTransform _resultFilterContent;
    private RectTransform _testListContent;
    private RectTransform _stepListContent;
    private InputField _searchInput;
    private Text _titleText;
    private Text _summaryText;
    private Text _detailText;
    private Text _statusText;
    private Text _resultSummaryText;
    private Text _llmInstructionText;
    private Button _runSelectedButton;
    private Button _runSelectionButton;
    private Button _runCategoryButton;
    private Button _runAllButton;
    private Button _stopButton;
    private Button _copyInstructionButton;

    private IReadOnlyList<DebugScenarioTestCase> _allTests = Array.Empty<DebugScenarioTestCase>();
    private IReadOnlyList<DebugScenarioTestCase> _visibleTests = Array.Empty<DebugScenarioTestCase>();
    private readonly HashSet<string> _rangeSelectedTestIds = new HashSet<string>(StringComparer.Ordinal);
    private DebugScenarioTestCase _selectedTest;
    private string _selectedCategory = string.Empty;
    private DebugScenarioTestStatus? _selectedResultStatus;
    private bool _isRunning;
    private bool _stopRequested;
    private bool _inputBlockedBeforeVisible;
    private CancellationTokenSource _runCts;

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

        var root = new GameObject(nameof(DebugScenarioTestPanel));
        _instance = root.AddComponent<DebugScenarioTestPanel>();
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

    public static void HideNow()
    {
        if (_instance != null)
        {
            _instance.SetVisible(false);
        }
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
        }
        else if (_panelRoot != null && _panelRoot.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            SetVisible(false);
        }
        else if (_panelRoot != null && _panelRoot.gameObject.activeSelf)
        {
            InputSystemBase.SetInputBlockedForDebugUi(true);
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
        RectTransform header = CreateHeader(_panelRoot, font, sprite);
        RectTransform main = CreateMainArea(_panelRoot);
        RectTransform left = CreateSection(main, "TestList", 690f, 0.9f);
        RectTransform right = CreateSection(main, "TestDetail", 850f, 1.1f);

        Text title = DebugUiFactory.CreateLabel(header, font, 24, TextAnchor.MiddleLeft, Color.white);
        title.text = "テスト画面  F2";
        title.GetComponent<LayoutElement>().preferredWidth = 280f;

        _searchInput = DebugUiFactory.CreateInputField(header, font, sprite, "テスト名、カテゴリ、説明で検索");
        _searchInput.textComponent.fontSize = 18;
        _searchInput.onValueChanged.AddListener(_ => RefreshTestList());

        _runSelectedButton = DebugUiFactory.CreateButton(header, font, sprite, "選択1件", RunSelected, 110f, 38f);
        _runSelectionButton = DebugUiFactory.CreateButton(header, font, sprite, "選択範囲", RunSelection, 120f, 38f);
        _runCategoryButton = DebugUiFactory.CreateButton(header, font, sprite, "カテゴリ", RunCategory, 110f, 38f);
        _runAllButton = DebugUiFactory.CreateButton(header, font, sprite, "表示中全部", RunAll, 120f, 38f);
        _stopButton = DebugUiFactory.CreateButton(header, font, sprite, "停止", StopRunningTests, 84f, 38f);
        _stopButton.GetComponent<Image>().color = new Color(0.58f, 0.24f, 0.22f, 0.96f);
        _stopButton.interactable = false;
        Button clearButton = DebugUiFactory.CreateButton(header, font, sprite, "結果クリア", ClearResults, 120f, 38f);
        clearButton.GetComponent<Image>().color = new Color(0.24f, 0.25f, 0.28f, 0.96f);

        CreateSectionTitle(left, font, "カテゴリ");
        _categoryContent = CreateCategoryBar(left);
        CreateSectionTitle(left, font, "結果フィルタ");
        _resultFilterContent = CreateResultFilterBar(left);
        _resultSummaryText = DebugUiFactory.CreateLabel(left, font, 13, TextAnchor.MiddleLeft, new Color(0.70f, 0.78f, 0.84f));
        _resultSummaryText.GetComponent<LayoutElement>().preferredHeight = 24f;
        CreateSectionTitle(left, font, "テストケース");
        _testListContent = DebugUiFactory.CreateScrollContent(left, sprite, 650f, true);
        SetScrollBackground(_testListContent, new Color(0.065f, 0.075f, 0.082f, 0.98f));

        CreateSectionTitle(right, font, "テスト詳細");
        _titleText = DebugUiFactory.CreateLabel(right, font, 24, TextAnchor.MiddleLeft, Color.white);
        _titleText.text = "テストを選択";

        _summaryText = DebugUiFactory.CreateLabel(right, font, 15, TextAnchor.UpperLeft, new Color(0.78f, 0.86f, 0.94f));
        _summaryText.GetComponent<LayoutElement>().preferredHeight = 54f;

        _detailText = DebugUiFactory.CreateLabel(right, font, 14, TextAnchor.UpperLeft, new Color(0.70f, 0.76f, 0.84f));
        _detailText.GetComponent<LayoutElement>().preferredHeight = 146f;

        CreateSectionTitle(right, font, "ステップ");
        _stepListContent = DebugUiFactory.CreateScrollContent(right, sprite, 350f, true);
        SetScrollBackground(_stepListContent, new Color(0.060f, 0.068f, 0.075f, 0.98f));

        _statusText = DebugUiFactory.CreateLabel(right, font, 15, TextAnchor.UpperLeft, new Color(0.95f, 0.86f, 0.55f));
        _statusText.GetComponent<LayoutElement>().preferredHeight = 92f;

        CreateSectionTitle(right, font, "失敗分析 / LLM修正依頼");
        _llmInstructionText = DebugUiFactory.CreateLabel(right, font, 12, TextAnchor.UpperLeft, new Color(0.82f, 0.88f, 0.94f));
        _llmInstructionText.GetComponent<LayoutElement>().preferredHeight = 120f;
        _copyInstructionButton = DebugUiFactory.CreateButton(right, font, sprite, "修正依頼文をコピー", CopyFailureInstruction, 220f, 34f);

        LoadTests();
        RefreshTestList();
    }

    private void LoadTests()
    {
        // テスト定義はプロジェクト側のモジュール登録と Resources/DebugTests の asset から集約します。
        _allTests = DebugScenarioTestRegistry.GetTests();
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
            RuntimeDebugPanel.HideNow();
            _inputBlockedBeforeVisible = InputSystemBase.IsInputBlockedForDebugUi;
            InputSystemBase.SetDebugUiInputCaptured(true);
            InputSystemBase.SetInputBlockedForDebugUi(true);
        }

        _panelRoot.gameObject.SetActive(visible);
        if (visible)
        {
            LoadTests();
            RefreshTestList();
            _searchInput?.ActivateInputField();
            if (EventSystem.current != null && _searchInput != null)
            {
                EventSystem.current.SetSelectedGameObject(_searchInput.gameObject);
            }
        }
        else if (wasVisible)
        {
            InputSystemBase.SetDebugUiInputCaptured(false);
            InputSystemBase.SetInputBlockedForDebugUi(_inputBlockedBeforeVisible);
            ClearSelectedDebugUi();
        }
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

    private void RefreshTestList()
    {
        if (_testListContent == null)
        {
            return;
        }

        foreach (Transform child in _testListContent)
        {
            Destroy(child.gameObject);
        }

        _testRows.Clear();
        Font font = LoadFont();
        Sprite sprite = GetFallbackSprite();
        string search = _searchInput != null ? _searchInput.text : string.Empty;
        RefreshCategories(font, sprite);
        RefreshResultFilters(font, sprite);
        RefreshResultSummary();

        IEnumerable<DebugScenarioTestCase> tests = _allTests;
        if (!string.IsNullOrWhiteSpace(_selectedCategory))
        {
            tests = tests.Where(test => string.Equals(test.Category, _selectedCategory, StringComparison.Ordinal));
        }

        if (_selectedResultStatus.HasValue)
        {
            tests = tests.Where(test => ResolveStatus(test) == _selectedResultStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            tests = tests.Where(test => Matches(test, search));
        }

        _visibleTests = tests.ToArray();
        foreach (DebugScenarioTestCase test in _visibleTests)
        {
            DebugScenarioTestCase captured = test;
            Image row = CreateTestRow(_testListContent, font, sprite, test, () => HandleTestRowClick(captured));
            _testRows.Add(row);
        }

        if (_selectedTest == null || !_visibleTests.Contains(_selectedTest))
        {
            SelectTest(_visibleTests.Count > 0 ? _visibleTests[0] : null);
        }
        else
        {
            SelectTest(_selectedTest);
        }

        UpdateRowSelection();
    }

    private void RefreshCategories(Font font, Sprite sprite)
    {
        if (_categoryContent == null)
        {
            return;
        }

        foreach (Transform child in _categoryContent)
        {
            Destroy(child.gameObject);
        }

        string[] categories = _allTests
            .Where(test => test != null && !string.IsNullOrWhiteSpace(test.Category))
            .Select(test => test.Category)
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
    }

    private void CreateCategoryButton(string label, string value, Font font, Sprite sprite)
    {
        bool selected = string.Equals(_selectedCategory, value, StringComparison.Ordinal);
        Color categoryColor = GetCategoryColor(value);
        Button button = DebugUiFactory.CreateButton(_categoryContent, font, sprite, ShortenCategoryLabel(label), () =>
        {
            _selectedCategory = value;
            RefreshTestList();
        }, 92f, 34f);
        button.GetComponent<Image>().color = selected
            ? new Color(categoryColor.r, categoryColor.g, categoryColor.b, 0.96f)
            : new Color(categoryColor.r, categoryColor.g, categoryColor.b, 0.34f);

        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.fontSize = 11;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.horizontalOverflow = HorizontalWrapMode.Wrap;
            buttonText.verticalOverflow = VerticalWrapMode.Truncate;
            DebugUiFactory.Stretch(buttonText.rectTransform, 4f, 0f, 4f, 0f);
        }
    }

    private void RefreshResultFilters(Font font, Sprite sprite)
    {
        if (_resultFilterContent == null)
        {
            return;
        }

        foreach (Transform child in _resultFilterContent)
        {
            Destroy(child.gameObject);
        }

        CreateResultFilterButton("すべて", null, font, sprite);
        CreateResultFilterButton("未実行", DebugScenarioTestStatus.NotRun, font, sprite);
        CreateResultFilterButton("成功", DebugScenarioTestStatus.Passed, font, sprite);
        CreateResultFilterButton("失敗", DebugScenarioTestStatus.Failed, font, sprite);
        CreateResultFilterButton("エラー", DebugScenarioTestStatus.Error, font, sprite);
        CreateResultFilterButton("実行中", DebugScenarioTestStatus.Running, font, sprite);
        CreateResultFilterButton("停止", DebugScenarioTestStatus.Canceled, font, sprite);
    }

    private void CreateResultFilterButton(string label, DebugScenarioTestStatus? status, Font font, Sprite sprite)
    {
        bool selected = _selectedResultStatus == status;
        Color color = status.HasValue ? GetStatusColor(status.Value) : new Color(0.45f, 0.62f, 0.74f);
        Button button = DebugUiFactory.CreateButton(_resultFilterContent, font, sprite, label, () =>
        {
            _selectedResultStatus = status;
            RefreshTestList();
        }, 92f, 30f);
        button.GetComponent<Image>().color = selected
            ? new Color(color.r, color.g, color.b, 0.92f)
            : new Color(color.r, color.g, color.b, 0.28f);

        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.fontSize = 12;
            buttonText.alignment = TextAnchor.MiddleCenter;
            DebugUiFactory.Stretch(buttonText.rectTransform, 4f, 0f, 4f, 0f);
        }
    }

    private void SelectTest(DebugScenarioTestCase test)
    {
        _selectedTest = test;
        UpdateRowSelection();
        RefreshDetail();
    }

    private void HandleTestRowClick(DebugScenarioTestCase test)
    {
        if (test == null)
        {
            SelectTest(null);
            return;
        }

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool additive = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                        Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

        if (shift && _selectedTest != null)
        {
            SelectRange(_selectedTest, test);
        }
        else if (additive)
        {
            ToggleRangeSelection(test);
        }
        else
        {
            _rangeSelectedTestIds.Clear();
            AddRangeSelection(test);
        }

        SelectTest(test);
    }

    private void SelectRange(DebugScenarioTestCase from, DebugScenarioTestCase to)
    {
        int fromIndex = IndexOfVisibleTest(from);
        int toIndex = IndexOfVisibleTest(to);
        if (fromIndex < 0 || toIndex < 0)
        {
            AddRangeSelection(to);
            return;
        }

        _rangeSelectedTestIds.Clear();
        int min = Mathf.Min(fromIndex, toIndex);
        int max = Mathf.Max(fromIndex, toIndex);
        for (int i = min; i <= max; i++)
        {
            AddRangeSelection(_visibleTests[i]);
        }
    }

    private void ToggleRangeSelection(DebugScenarioTestCase test)
    {
        if (test == null || string.IsNullOrWhiteSpace(test.Id))
        {
            return;
        }

        if (!_rangeSelectedTestIds.Remove(test.Id))
        {
            _rangeSelectedTestIds.Add(test.Id);
        }
    }

    private void AddRangeSelection(DebugScenarioTestCase test)
    {
        if (test != null && !string.IsNullOrWhiteSpace(test.Id))
        {
            _rangeSelectedTestIds.Add(test.Id);
        }
    }

    private int IndexOfVisibleTest(DebugScenarioTestCase test)
    {
        if (test == null)
        {
            return -1;
        }

        for (int i = 0; i < _visibleTests.Count; i++)
        {
            if (_visibleTests[i] == test)
            {
                return i;
            }
        }

        return -1;
    }

    private void RefreshDetail()
    {
        foreach (Transform child in _stepListContent)
        {
            Destroy(child.gameObject);
        }

        if (_selectedTest == null)
        {
            _titleText.text = "テストがありません";
            _summaryText.text = "条件に一致するテストケースがありません。";
            _detailText.text = string.Empty;
            _statusText.text = string.Empty;
            _llmInstructionText.text = string.Empty;
            _runSelectedButton.interactable = false;
            _copyInstructionButton.interactable = false;
            return;
        }

        _runSelectedButton.interactable = !_isRunning;
        _titleText.text = _selectedTest.DisplayName;
        _summaryText.text = _selectedTest.Summary;
        _detailText.text =
            $"カテゴリ: {_selectedTest.Category}\n" +
            $"目的: {_selectedTest.Purpose}\n" +
            $"前提: {_selectedTest.Preconditions}\n" +
            $"期待結果: {_selectedTest.ExpectedResult}";

        DebugScenarioTestRunResult runResult = GetResult(_selectedTest);
        _statusText.color = GetStatusColor(runResult != null ? runResult.Status : DebugScenarioTestStatus.NotRun);
        _statusText.text = runResult == null
            ? "未実行"
            : BuildStatusDetail(_selectedTest, runResult);

        _llmInstructionText.text = BuildFailureInstructionText(_selectedTest, runResult);
        _copyInstructionButton.interactable = !_isRunning && runResult != null &&
                                             (runResult.Status == DebugScenarioTestStatus.Failed ||
                                              runResult.Status == DebugScenarioTestStatus.Error);

        Font font = LoadFont();
        Sprite sprite = GetFallbackSprite();
        for (int i = 0; i < _selectedTest.Steps.Count; i++)
        {
            DebugScenarioTestStep step = _selectedTest.Steps[i];
            DebugScenarioTestStepResult stepResult = runResult != null && i < runResult.StepResults.Count
                ? runResult.StepResults[i]
                : null;
            CreateStepRow(_stepListContent, font, sprite, i + 1, step, stepResult);
        }
    }

    private void RunSelected()
    {
        RunSelectedAsync().Forget();
    }

    private async UniTaskVoid RunSelectedAsync()
    {
        if (_selectedTest == null || _isRunning)
        {
            return;
        }

        await RunOneAsync(_selectedTest);
    }

    private void RunAll()
    {
        RunAllAsync().Forget();
    }

    private void RunSelection()
    {
        RunSelectionAsync().Forget();
    }

    private void RunCategory()
    {
        RunCategoryAsync().Forget();
    }

    private async UniTaskVoid RunSelectionAsync()
    {
        if (_isRunning)
        {
            return;
        }

        IReadOnlyList<DebugScenarioTestCase> tests = GetRangeSelectedTests();
        if (tests.Count == 0 && _selectedTest != null)
        {
            tests = new[] { _selectedTest };
        }

        await RunManyAsync(tests);
    }

    private async UniTaskVoid RunCategoryAsync()
    {
        if (_isRunning)
        {
            return;
        }

        string category = !string.IsNullOrWhiteSpace(_selectedCategory)
            ? _selectedCategory
            : _selectedTest != null ? _selectedTest.Category : string.Empty;
        if (string.IsNullOrWhiteSpace(category))
        {
            await RunManyAsync(Array.Empty<DebugScenarioTestCase>());
            return;
        }

        DebugScenarioTestCase[] tests = _allTests
            .Where(test => test != null && string.Equals(test.Category, category, StringComparison.Ordinal))
            .ToArray();
        await RunManyAsync(tests);
    }

    private async UniTaskVoid RunAllAsync()
    {
        if (_isRunning)
        {
            return;
        }

        IReadOnlyList<DebugScenarioTestCase> tests = _visibleTests.Count > 0 ? _visibleTests : _allTests;
        await RunManyAsync(tests);
    }

    private async UniTask RunManyAsync(IReadOnlyList<DebugScenarioTestCase> tests)
    {
        if (tests == null || tests.Count == 0 || _isRunning)
        {
            return;
        }

        _isRunning = true;
        _stopRequested = false;
        _runCts?.Dispose();
        _runCts = new CancellationTokenSource();
        SetButtonsInteractable(false);
        foreach (DebugScenarioTestCase test in tests)
        {
            if (_stopRequested || (_runCts != null && _runCts.IsCancellationRequested))
            {
                break;
            }

            if (test == null)
            {
                continue;
            }

            SelectTest(test);
            await RunOneAsync(test, keepRunningFlag: true);
            await UniTask.Yield();
        }

        _isRunning = false;
        _stopRequested = false;
        _runCts?.Dispose();
        _runCts = null;
        Time.timeScale = 1f;
        RuntimeDebugPanel.SetResumeTimeScale(1f);
        SetButtonsInteractable(true);
        RefreshTestList();
    }

    private IReadOnlyList<DebugScenarioTestCase> GetRangeSelectedTests()
    {
        if (_rangeSelectedTestIds.Count == 0)
        {
            return Array.Empty<DebugScenarioTestCase>();
        }

        return _visibleTests
            .Where(test => test != null && _rangeSelectedTestIds.Contains(test.Id))
            .ToArray();
    }

    private async UniTask RunOneAsync(DebugScenarioTestCase test, bool keepRunningFlag = false)
    {
        if (!keepRunningFlag)
        {
            _isRunning = true;
            _stopRequested = false;
            _runCts?.Dispose();
            _runCts = new CancellationTokenSource();
            SetButtonsInteractable(false);
        }

        _results[test.Id] = new DebugScenarioTestRunResult
        {
            TestId = test.Id,
            TestName = test.DisplayName,
            Status = DebugScenarioTestStatus.Running,
            Message = "実行中...",
        };
        RefreshTestList();
        SelectTest(test);

        DebugScenarioTestRunResult result = await _runner.RunAsync(test, _runCts != null ? _runCts.Token : CancellationToken.None);
        _results[test.Id] = result;
        Debug.Log($"[DebugScenarioTest] {StatusMark(result.Status)} {test.DisplayName}: {result.Message}");

        if (!keepRunningFlag)
        {
            _isRunning = false;
            _stopRequested = false;
            _runCts?.Dispose();
            _runCts = null;
            Time.timeScale = 1f;
            RuntimeDebugPanel.SetResumeTimeScale(1f);
            SetButtonsInteractable(true);
        }

        RefreshTestList();
        SelectTest(test);
    }

    private void StopRunningTests()
    {
        if (!_isRunning)
        {
            return;
        }

        _stopRequested = true;
        _runCts?.Cancel();
        Time.timeScale = 1f;
        RuntimeDebugPanel.SetResumeTimeScale(1f);
        if (_statusText != null)
        {
            _statusText.text = "停止要求を受け付けました。現在のステップ完了後に停止します。";
        }
        SetButtonsInteractable(false);
    }

    private void ClearResults()
    {
        if (_isRunning)
        {
            return;
        }

        _results.Clear();
        RefreshTestList();
    }

    private void CopyFailureInstruction()
    {
        if (_llmInstructionText == null || string.IsNullOrWhiteSpace(_llmInstructionText.text))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = _llmInstructionText.text;
        if (_statusText != null)
        {
            _statusText.text += "\n修正依頼文をクリップボードにコピーしました。";
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_runSelectedButton != null)
        {
            _runSelectedButton.interactable = interactable && _selectedTest != null;
        }

        if (_runSelectionButton != null)
        {
            _runSelectionButton.interactable = interactable && (_rangeSelectedTestIds.Count > 0 || _selectedTest != null);
        }

        if (_runCategoryButton != null)
        {
            _runCategoryButton.interactable = interactable &&
                                             (!string.IsNullOrWhiteSpace(_selectedCategory) ||
                                              (_selectedTest != null && !string.IsNullOrWhiteSpace(_selectedTest.Category)));
        }

        if (_runAllButton != null)
        {
            _runAllButton.interactable = interactable;
        }

        if (_stopButton != null)
        {
            _stopButton.interactable = _isRunning && !_stopRequested;
        }

        if (_copyInstructionButton != null)
        {
            DebugScenarioTestRunResult result = GetResult(_selectedTest);
            _copyInstructionButton.interactable = interactable && result != null &&
                                                 (result.Status == DebugScenarioTestStatus.Failed ||
                                                  result.Status == DebugScenarioTestStatus.Error);
        }
    }

    private DebugScenarioTestRunResult GetResult(DebugScenarioTestCase test)
    {
        return test != null && !string.IsNullOrWhiteSpace(test.Id) && _results.TryGetValue(test.Id, out DebugScenarioTestRunResult result)
            ? result
            : null;
    }

    private static bool Matches(DebugScenarioTestCase test, string search)
    {
        return Contains(test.Id, search) ||
               Contains(test.DisplayName, search) ||
               Contains(test.Category, search) ||
               Contains(test.Summary, search) ||
               Contains(test.Purpose, search);
    }

    private static bool Contains(string source, string value)
    {
        return !string.IsNullOrEmpty(source) &&
               source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private DebugScenarioTestStatus ResolveStatus(DebugScenarioTestCase test)
    {
        DebugScenarioTestRunResult result = GetResult(test);
        return result != null ? result.Status : DebugScenarioTestStatus.NotRun;
    }

    private void RefreshResultSummary()
    {
        if (_resultSummaryText == null)
        {
            return;
        }

        int total = _allTests.Count;
        int passed = CountStatus(DebugScenarioTestStatus.Passed);
        int failed = CountStatus(DebugScenarioTestStatus.Failed);
        int error = CountStatus(DebugScenarioTestStatus.Error);
        int running = CountStatus(DebugScenarioTestStatus.Running);
        int canceled = CountStatus(DebugScenarioTestStatus.Canceled);
        int notRun = total - _results.Values.Count(result => result.Status != DebugScenarioTestStatus.NotRun);
        _resultSummaryText.text = $"結果: 成功 {passed} / 失敗 {failed} / エラー {error} / 停止 {canceled} / 実行中 {running} / 未実行 {Math.Max(0, notRun)}";
    }

    private int CountStatus(DebugScenarioTestStatus status)
    {
        return _allTests.Count(test => ResolveStatus(test) == status);
    }

    private Image CreateTestRow(RectTransform parent, Font font, Sprite sprite, DebugScenarioTestCase test, Action onClick)
    {
        var row = new GameObject(test.Id + "Row", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 62f;

        Image image = row.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.12f, 0.14f, 0.15f, 0.98f);

        Button button = row.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());

        DebugScenarioTestRunResult result = GetResult(test);
        DebugScenarioTestStatus status = result != null ? result.Status : DebugScenarioTestStatus.NotRun;

        var stripeObject = new GameObject("CategoryStripe", typeof(RectTransform), typeof(Image));
        stripeObject.transform.SetParent(row.transform, false);
        RectTransform stripeRect = stripeObject.GetComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0f, 0f);
        stripeRect.anchorMax = new Vector2(0f, 1f);
        stripeRect.pivot = new Vector2(0f, 0.5f);
        stripeRect.sizeDelta = new Vector2(5f, 0f);
        stripeRect.anchoredPosition = Vector2.zero;
        stripeObject.GetComponent<Image>().color = GetCategoryColor(test.Category);

        Text mark = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 22, TextAnchor.MiddleCenter, GetStatusColor(status));
        mark.text = StatusMark(status);
        mark.GetComponent<LayoutElement>().ignoreLayout = true;
        mark.rectTransform.anchorMin = new Vector2(0f, 0f);
        mark.rectTransform.anchorMax = new Vector2(0f, 1f);
        mark.rectTransform.sizeDelta = new Vector2(48f, 0f);
        mark.rectTransform.anchoredPosition = new Vector2(0f, 0f);

        Text title = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 16, TextAnchor.UpperLeft, Color.white);
        title.text = test.DisplayName;
        title.verticalOverflow = VerticalWrapMode.Truncate;
        title.GetComponent<LayoutElement>().ignoreLayout = true;
        DebugUiFactory.Stretch(title.rectTransform, 54f, 8f, 12f, 32f);

        Text sub = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 12, TextAnchor.LowerLeft, new Color(0.66f, 0.73f, 0.80f));
        sub.text = $"{test.Category}    {Truncate(test.Summary, 70)}";
        sub.verticalOverflow = VerticalWrapMode.Truncate;
        sub.GetComponent<LayoutElement>().ignoreLayout = true;
        DebugUiFactory.Stretch(sub.rectTransform, 54f, 34f, 12f, 6f);
        return image;
    }

    private static string BuildStatusDetail(DebugScenarioTestCase test, DebugScenarioTestRunResult result)
    {
        var builder = new StringBuilder();
        builder.Append($"{StatusMark(result.Status)} {result.Message} ({result.DurationSeconds:0.00}s)");
        if (result.Status == DebugScenarioTestStatus.Failed || result.Status == DebugScenarioTestStatus.Error)
        {
            builder.AppendLine();
            builder.Append($"分類: {FailureKindLabel(result.FailureKind)}");
            if (result.FailedStepIndex >= 0 && result.FailedStepIndex < test.Steps.Count)
            {
                DebugScenarioTestStep step = test.Steps[result.FailedStepIndex];
                builder.AppendLine();
                builder.Append($"失敗ステップ: {result.FailedStepIndex + 1}. {step.DisplayName} / {step.CommandId}");
            }

            builder.AppendLine();
            builder.Append("確認観点: 実装不具合だけでなく、テストケースの前提・期待値・必要シーンが正しいかも確認してください。");
        }

        return builder.ToString();
    }

    private static string BuildFailureInstructionText(DebugScenarioTestCase test, DebugScenarioTestRunResult result)
    {
        if (test == null)
        {
            return string.Empty;
        }

        if (result == null)
        {
            return "未実行です。失敗時はここに修正依頼文が表示されます。";
        }

        if (result.Status == DebugScenarioTestStatus.Passed)
        {
            return "このテストは成功しました。";
        }

        if (result.Status != DebugScenarioTestStatus.Failed && result.Status != DebugScenarioTestStatus.Error)
        {
            return result.Message;
        }

        if (!string.IsNullOrWhiteSpace(result.FailureInstruction))
        {
            return result.FailureInstruction;
        }

        return
            "以下のデバッグシナリオテストが失敗しています。実装不具合とテストケース誤りの両方を確認してください。\n" +
            $"TestId: {test.Id}\n" +
            $"TestName: {test.DisplayName}\n" +
            $"Category: {test.Category}\n" +
            $"Message: {result.Message}";
    }

    private static void CreateStepRow(RectTransform parent, Font font, Sprite sprite, int index, DebugScenarioTestStep step, DebugScenarioTestStepResult result)
    {
        var row = new GameObject("StepRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = result == null ? 58f : 78f;

        Image image = row.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.13f, 0.145f, 0.15f, 0.98f);

        DebugScenarioTestStatus status = result != null ? result.Status : DebugScenarioTestStatus.NotRun;
        Text mark = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 20, TextAnchor.MiddleCenter, GetStatusColor(status));
        mark.text = StatusMark(status);
        mark.GetComponent<LayoutElement>().ignoreLayout = true;
        mark.rectTransform.anchorMin = new Vector2(0f, 0f);
        mark.rectTransform.anchorMax = new Vector2(0f, 1f);
        mark.rectTransform.sizeDelta = new Vector2(44f, 0f);

        Text title = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 15, TextAnchor.UpperLeft, Color.white);
        title.text = $"{index}. {step.DisplayName}";
        title.verticalOverflow = VerticalWrapMode.Truncate;
        title.GetComponent<LayoutElement>().ignoreLayout = true;
        DebugUiFactory.Stretch(title.rectTransform, 50f, 8f, 12f, result == null ? 30f : 50f);

        Text desc = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 12, TextAnchor.LowerLeft, new Color(0.68f, 0.74f, 0.82f));
        desc.text = result == null
            ? $"{step.CommandId}  {step.ExpectedSummary}"
            : $"{step.CommandId}  {result.Message}";
        desc.verticalOverflow = VerticalWrapMode.Truncate;
        desc.GetComponent<LayoutElement>().ignoreLayout = true;
        DebugUiFactory.Stretch(desc.rectTransform, 50f, result == null ? 30f : 30f, 12f, result == null ? 6f : 26f);

        if (result != null && !string.IsNullOrWhiteSpace(result.Expected))
        {
            Text expected = DebugUiFactory.CreateLabel(row.GetComponent<RectTransform>(), font, 11, TextAnchor.LowerLeft, new Color(0.58f, 0.86f, 0.68f));
            expected.text = result.Expected;
            expected.verticalOverflow = VerticalWrapMode.Truncate;
            expected.GetComponent<LayoutElement>().ignoreLayout = true;
            DebugUiFactory.Stretch(expected.rectTransform, 50f, 54f, 12f, 6f);
        }
    }

    private void UpdateRowSelection()
    {
        for (int i = 0; i < _testRows.Count; i++)
        {
            bool selected = i < _visibleTests.Count && _visibleTests[i] == _selectedTest;
            bool rangeSelected = i < _visibleTests.Count &&
                                 _visibleTests[i] != null &&
                                 _rangeSelectedTestIds.Contains(_visibleTests[i].Id);
            _testRows[i].color = selected
                ? new Color(0.08f, 0.32f, 0.22f, 0.98f)
                : rangeSelected
                    ? new Color(0.13f, 0.24f, 0.34f, 0.98f)
                    : new Color(0.12f, 0.14f, 0.15f, 0.98f);
        }
    }

    private static string StatusMark(DebugScenarioTestStatus status)
    {
        switch (status)
        {
            case DebugScenarioTestStatus.Running:
                return "...";
            case DebugScenarioTestStatus.Passed:
                return "✓";
            case DebugScenarioTestStatus.Failed:
                return "✕";
            case DebugScenarioTestStatus.Error:
                return "!";
            case DebugScenarioTestStatus.Skipped:
                return "SKIP";
            case DebugScenarioTestStatus.Canceled:
                return "STOP";
            default:
                return "-";
        }
    }

    private static Color GetStatusColor(DebugScenarioTestStatus status)
    {
        switch (status)
        {
            case DebugScenarioTestStatus.Running:
                return new Color(0.55f, 0.75f, 1f);
            case DebugScenarioTestStatus.Passed:
                return new Color(0.48f, 1f, 0.62f);
            case DebugScenarioTestStatus.Failed:
            case DebugScenarioTestStatus.Error:
                return new Color(1f, 0.36f, 0.34f);
            case DebugScenarioTestStatus.Skipped:
                return new Color(0.62f, 0.66f, 0.70f);
            case DebugScenarioTestStatus.Canceled:
                return new Color(1f, 0.72f, 0.36f);
            default:
                return new Color(0.68f, 0.72f, 0.76f);
        }
    }

    private static string FailureKindLabel(DebugScenarioTestFailureKind kind)
    {
        switch (kind)
        {
            case DebugScenarioTestFailureKind.Assertion:
                return "期待値不一致";
            case DebugScenarioTestFailureKind.Infrastructure:
                return "テスト基盤/コマンド不足";
            case DebugScenarioTestFailureKind.Precondition:
                return "前提条件不足";
            case DebugScenarioTestFailureKind.TestCaseDefinition:
                return "テストケース定義の問題";
            case DebugScenarioTestFailureKind.Exception:
                return "例外";
            default:
                return "なし";
        }
    }

    private static Color GetCategoryColor(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return new Color(0.45f, 0.62f, 0.74f);
        }

        if (category.Contains("バトル"))
        {
            return new Color(0.95f, 0.45f, 0.42f);
        }

        if (category.Contains("プレイヤー"))
        {
            return new Color(0.96f, 0.72f, 0.36f);
        }

        if (category.Contains("カード"))
        {
            return new Color(0.38f, 0.82f, 0.58f);
        }

        if (category.Contains("シーン"))
        {
            return new Color(0.42f, 0.66f, 0.96f);
        }

        if (category.Contains("マップ"))
        {
            return new Color(0.82f, 0.68f, 0.34f);
        }

        if (category.Contains("会話"))
        {
            return new Color(0.70f, 0.52f, 0.96f);
        }

        Color fallback = new Color(0.50f, 0.76f, 0.88f);
        return DebugScenarioTestRegistry.ResolveCategoryColor(category, fallback);
    }

    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("DebugScenarioTestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32010;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static RectTransform CreatePanelRoot(Transform parent, Sprite sprite)
    {
        var root = new GameObject("DebugScenarioTestPanelRoot", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1620f, 900f);

        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.030f, 0.034f, 0.038f, 0.992f);
        return rect;
    }

    private static RectTransform CreateHeader(RectTransform parent, Font font, Sprite sprite)
    {
        var header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        header.transform.SetParent(parent, false);
        RectTransform rect = header.GetComponent<RectTransform>();
        DebugUiFactory.Stretch(rect, 22f, 22f, 22f, 820f);

        HorizontalLayoutGroup layout = header.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        return rect;
    }

    private static RectTransform CreateMainArea(RectTransform parent)
    {
        var main = new GameObject("Main", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        main.transform.SetParent(parent, false);
        RectTransform rect = main.GetComponent<RectTransform>();
        DebugUiFactory.Stretch(rect, 22f, 92f, 22f, 22f);

        HorizontalLayoutGroup layout = main.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        return rect;
    }

    private static RectTransform CreateSection(RectTransform parent, string name, float width, float flexibleWidth)
    {
        var section = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        section.transform.SetParent(parent, false);

        Image image = section.GetComponent<Image>();
        image.color = new Color(0.055f, 0.062f, 0.068f, 0.985f);

        LayoutElement element = section.GetComponent<LayoutElement>();
        element.preferredWidth = width;
        element.flexibleWidth = flexibleWidth;
        element.flexibleHeight = 1f;

        VerticalLayoutGroup layout = section.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        return section.GetComponent<RectTransform>();
    }

    private static RectTransform CreateCategoryBar(RectTransform parent)
    {
        var bar = new GameObject("CategoryBar", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        bar.transform.SetParent(parent, false);
        bar.GetComponent<LayoutElement>().preferredHeight = 70f;

        GridLayoutGroup layout = bar.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(92f, 30f);
        layout.spacing = new Vector2(6f, 6f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 7;
        return bar.GetComponent<RectTransform>();
    }

    private static RectTransform CreateResultFilterBar(RectTransform parent)
    {
        var bar = new GameObject("ResultFilterBar", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        bar.transform.SetParent(parent, false);
        bar.GetComponent<LayoutElement>().preferredHeight = 66f;

        GridLayoutGroup layout = bar.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(92f, 30f);
        layout.spacing = new Vector2(6f, 6f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 7;
        return bar.GetComponent<RectTransform>();
    }

    private static void CreateSectionTitle(RectTransform parent, Font font, string text)
    {
        Text label = DebugUiFactory.CreateLabel(parent, font, 14, TextAnchor.MiddleLeft, new Color(0.58f, 0.78f, 0.68f));
        label.text = text;
        label.GetComponent<LayoutElement>().preferredHeight = 22f;
    }

    private static void SetScrollBackground(RectTransform content, Color color)
    {
        if (content == null || content.parent == null)
        {
            return;
        }

        Image image = content.parent.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("DebugScenarioTestEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
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

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength - 3) + "...";
    }

    private static string ShortenCategoryLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label) || label.Length <= 8)
        {
            return label;
        }

        return label.Substring(0, 7) + "…";
    }
}
#endif
