using System;
using EightAID.EIGHTAIDLib.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EightAID.EIGHTAIDLib.Analytics
{
    public sealed class ExhibitionLogManager : PersistentSingletonMonoBehaviour<ExhibitionLogManager>
    {
        private const string SettingsResourcePath = "ExhibitionLogSettings";
        private const string MachineIdPrefsKey = "EightAID.ExhibitionLog.MachineId";

        private ExhibitionLogSettings _settings;
        private ExhibitionLogWriter _writer;
        private ExhibitionLogSessionSummary _summary;
        private string _pendingPlayMode;
        private DateTime _startedAtUtc;
        private DateTime _startedAtLocal;
        private float _startedRealtime;
        private float _battleStartedRealtime;
        private bool _hasActiveSession;
        private bool _hasEnded;

        public static ExhibitionLogManager GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var obj = new GameObject(nameof(ExhibitionLogManager));
            return obj.AddComponent<ExhibitionLogManager>();
        }

        public static void StartSessionSafe(string startReason, string saveSlotId)
        {
            GetOrCreate().StartSession(startReason, saveSlotId);
        }

        public static void EndSessionSafe(string endReason)
        {
            GetOrCreate().EndSession(endReason);
        }

        public static void RecordEventSafe(string eventName, string stageId = null, string eventTitle = null, string reason = null, string result = null, int value = 0, string payload = null)
        {
            GetOrCreate().RecordEvent(eventName, stageId, eventTitle, reason, result, value, payload);
        }

        public static void RecordSceneLoadedSafe(string sceneName, int buildIndex)
        {
            GetOrCreate().RecordSceneLoaded(sceneName, buildIndex);
        }

        public static void RecordSceneTransitionSafe(string fromScene, string toScene, int toBuildIndex)
        {
            GetOrCreate().RecordEvent("scene_transition", payload: $"from={fromScene};to={toScene};toBuildIndex={toBuildIndex}");
        }

        public static void RecordGameOverSafe(string reason, string stageId, string eventTitle)
        {
            GetOrCreate().RecordGameOver(reason, stageId, eventTitle);
        }

        public static void RecordRetryCurrentStageSafe()
        {
            GetOrCreate().RecordRetryCurrentStage();
        }

        public static void RecordRetryFromMapStartSafe()
        {
            GetOrCreate().RecordRetryFromMapStart();
        }

        public static void RecordMenuOpenSafe()
        {
            GetOrCreate().RecordMenuOpen();
        }

        public static void RecordBattleStartSafe(string stageId, string eventTitle, string enemyId, string enemyName)
        {
            GetOrCreate().RecordBattleStart(stageId, eventTitle, enemyId, enemyName);
        }

        public static void RecordBattleEndSafe(string result, string stageId, string eventTitle, string enemyId, string enemyName, int turnCount, int rewardGold = 0)
        {
            GetOrCreate().RecordBattleEnd(result, stageId, eventTitle, enemyId, enemyName, turnCount, rewardGold);
        }

        public static void RecordStageNodeSelectedSafe(string stageId, string nodeType, string displayName)
        {
            GetOrCreate().RecordEvent("stage_node_selected", stageId, displayName, payload: $"nodeType={nodeType}");
        }

        public static void RecordStageNodeClearedSafe(string stageId, string nodeType, string displayName)
        {
            GetOrCreate().RecordStageNodeCleared(stageId, nodeType, displayName);
        }

        public static void SetPlayModeSafe(string playMode)
        {
            GetOrCreate().SetPlayMode(playMode);
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            LoadSettings();
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting += OnApplicationQuitting;
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.quitting -= OnApplicationQuitting;
        }

        public void StartSession(string startReason, string saveSlotId)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (_hasActiveSession && !_hasEnded)
            {
                EndSession("replaced_by_new_session");
            }

            _startedAtUtc = DateTime.UtcNow;
            _startedAtLocal = DateTime.Now;
            _startedRealtime = Time.realtimeSinceStartup;
            _battleStartedRealtime = 0f;
            _hasActiveSession = true;
            _hasEnded = false;

            _summary = new ExhibitionLogSessionSummary
            {
                sessionId = Guid.NewGuid().ToString("N"),
                machineId = ResolveMachineId(),
                buildVersion = Application.version,
                exhibitionDate = _startedAtLocal.ToString("yyyy-MM-dd"),
                playMode = string.IsNullOrWhiteSpace(_pendingPlayMode) ? string.Empty : _pendingPlayMode,
                saveSlotId = saveSlotId ?? string.Empty,
                startedAtUtc = ToIso(_startedAtUtc),
                startedAtLocal = ToIso(_startedAtLocal),
                startReason = string.IsNullOrWhiteSpace(startReason) ? "unknown" : startReason,
                endReason = string.Empty,
                lastScene = SceneManager.GetActiveScene().name,
                lastStageId = string.Empty,
                lastEventTitle = string.Empty
            };

            _writer = new ExhibitionLogWriter(_summary.sessionId, _startedAtLocal);
            _writer.TrimOldSessionFiles(_settings != null ? _settings.maxSessionFiles : 200);
            RecordEvent("session_start", payload: $"startReason={_summary.startReason};saveSlotId={_summary.saveSlotId}");
            WriteSummaryJson();
        }

        public void EndSession(string endReason)
        {
            if (!IsEnabled || !_hasActiveSession || _summary == null || _hasEnded)
            {
                return;
            }

            DateTime endedAtUtc = DateTime.UtcNow;
            DateTime endedAtLocal = DateTime.Now;
            _summary.endedAtUtc = ToIso(endedAtUtc);
            _summary.endedAtLocal = ToIso(endedAtLocal);
            _summary.elapsedSeconds = ElapsedSeconds;
            _summary.endReason = string.IsNullOrWhiteSpace(endReason) ? "unknown" : endReason;
            _summary.isCleared = _summary.endReason == "clear" ? 1 : _summary.isCleared;
            _summary.returnedToTitle = IsTitleReturnReason(_summary.endReason) ? 1 : _summary.returnedToTitle;

            RecordEvent("session_end", reason: _summary.endReason);
            _hasEnded = true;
            WriteSummaryJson();
            if (ShouldWriteSummaryCsv)
            {
                _writer?.AppendSummaryCsv(_summary);
            }

            if (_summary.isCleared == 1)
            {
                _writer?.AppendClearTimeCsv(_summary);
            }
        }

        public void SetPlayMode(string playMode)
        {
            string resolved = string.IsNullOrWhiteSpace(playMode) ? "unknown" : playMode.Trim();
            _pendingPlayMode = resolved;

            if (_summary == null || _hasEnded)
            {
                return;
            }

            _summary.playMode = resolved;
            RecordEvent("play_mode_selected", payload: $"playMode={resolved}");
            WriteSummaryJson();
        }

        public void RecordEvent(string eventName, string stageId = null, string eventTitle = null, string reason = null, string result = null, int value = 0, string payload = null)
        {
            if (!IsEnabled || !_hasActiveSession || _summary == null || _writer == null || _hasEnded)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!string.IsNullOrWhiteSpace(stageId))
            {
                _summary.lastStageId = stageId;
            }

            if (!string.IsNullOrWhiteSpace(eventTitle))
            {
                _summary.lastEventTitle = eventTitle;
            }

            _summary.lastScene = scene.name;
            _summary.elapsedSeconds = ElapsedSeconds;

            var nowUtc = DateTime.UtcNow;
            var nowLocal = DateTime.Now;
            var logEvent = new ExhibitionLogEvent
            {
                sessionId = _summary.sessionId,
                machineId = _summary.machineId,
                buildVersion = _summary.buildVersion,
                exhibitionDate = _summary.exhibitionDate,
                eventTimeUtc = ToIso(nowUtc),
                eventTimeLocal = ToIso(nowLocal),
                elapsedSeconds = _summary.elapsedSeconds,
                eventName = string.IsNullOrWhiteSpace(eventName) ? "unknown" : eventName,
                sceneName = scene.name,
                sceneBuildIndex = scene.buildIndex,
                stageId = stageId ?? _summary.lastStageId,
                eventTitle = eventTitle ?? _summary.lastEventTitle,
                reason = reason ?? string.Empty,
                result = result ?? string.Empty,
                value = value,
                payload = payload ?? string.Empty
            };

            if (ShouldWriteJsonl)
            {
                _writer.WriteEventJsonl(logEvent);
            }

            if (ShouldWriteEventCsv)
            {
                _writer.AppendEventCsv(logEvent);
            }
        }

        public void RecordSceneLoaded(string sceneName, int buildIndex)
        {
            if (_summary != null)
            {
                _summary.lastScene = sceneName ?? string.Empty;
            }

            RecordEvent("scene_loaded", payload: $"scene={sceneName};buildIndex={buildIndex}");
            WriteSummaryJson();
        }

        public void RecordGameOver(string reason, string stageId, string eventTitle)
        {
            if (_summary != null)
            {
                _summary.gameOverCount++;
            }

            RecordEvent("game_over", stageId, eventTitle, reason);
            WriteSummaryJson();
        }

        public void RecordRetryCurrentStage()
        {
            if (_summary != null)
            {
                _summary.retryCurrentStageCount++;
            }

            RecordEvent("retry_current_stage");
            WriteSummaryJson();
        }

        public void RecordRetryFromMapStart()
        {
            if (_summary != null)
            {
                _summary.retryFromMapStartCount++;
            }

            RecordEvent("retry_from_map_start");
            WriteSummaryJson();
        }

        public void RecordMenuOpen()
        {
            if (_summary != null)
            {
                _summary.menuOpenCount++;
            }

            RecordEvent("menu_open");
            WriteSummaryJson();
        }

        public void RecordBattleStart(string stageId, string eventTitle, string enemyId, string enemyName)
        {
            _battleStartedRealtime = Time.realtimeSinceStartup;
            if (_summary != null)
            {
                _summary.battleCount++;
            }

            RecordBattleEvent("battle_start", stageId, eventTitle, enemyId, enemyName, string.Empty, 0, 0, 0f);
            WriteSummaryJson();
        }

        public void RecordBattleEnd(string result, string stageId, string eventTitle, string enemyId, string enemyName, int turnCount, int rewardGold)
        {
            float battleElapsed = _battleStartedRealtime > 0f ? Time.realtimeSinceStartup - _battleStartedRealtime : 0f;
            RecordBattleEvent("battle_end", stageId, eventTitle, enemyId, enemyName, result, turnCount, rewardGold, battleElapsed);
            WriteSummaryJson();
        }

        public void RecordStageNodeCleared(string stageId, string nodeType, string displayName)
        {
            if (_summary != null)
            {
                _summary.stageClearCount++;
            }

            RecordEvent("stage_node_cleared", stageId, displayName, payload: $"nodeType={nodeType}");
            WriteSummaryJson();
        }

        private void RecordBattleEvent(string eventName, string stageId, string eventTitle, string enemyId, string enemyName, string result, int turnCount, int value, float battleElapsedSeconds)
        {
            if (!IsEnabled || !_hasActiveSession || _summary == null || _writer == null || _hasEnded)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(stageId))
            {
                _summary.lastStageId = stageId;
            }

            if (!string.IsNullOrWhiteSpace(eventTitle))
            {
                _summary.lastEventTitle = eventTitle;
            }

            Scene scene = SceneManager.GetActiveScene();
            _summary.lastScene = scene.name;
            _summary.elapsedSeconds = ElapsedSeconds;

            var nowUtc = DateTime.UtcNow;
            var nowLocal = DateTime.Now;
            var logEvent = new ExhibitionLogEvent
            {
                sessionId = _summary.sessionId,
                machineId = _summary.machineId,
                buildVersion = _summary.buildVersion,
                exhibitionDate = _summary.exhibitionDate,
                eventTimeUtc = ToIso(nowUtc),
                eventTimeLocal = ToIso(nowLocal),
                elapsedSeconds = _summary.elapsedSeconds,
                eventName = eventName,
                sceneName = scene.name,
                sceneBuildIndex = scene.buildIndex,
                stageId = stageId ?? string.Empty,
                eventTitle = eventTitle ?? string.Empty,
                enemyId = enemyId ?? string.Empty,
                enemyName = enemyName ?? string.Empty,
                turnCount = turnCount,
                value = value,
                result = result ?? string.Empty,
                payload = battleElapsedSeconds > 0f ? $"battleElapsedSeconds={battleElapsedSeconds:0.###}" : string.Empty
            };

            if (ShouldWriteJsonl)
            {
                _writer.WriteEventJsonl(logEvent);
            }

            if (ShouldWriteEventCsv)
            {
                _writer.AppendEventCsv(logEvent);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RecordSceneLoaded(scene.name, scene.buildIndex);
        }

        private void OnApplicationQuitting()
        {
            EndSession("app_quit");
        }

        private void LoadSettings()
        {
            _settings = Resources.Load<ExhibitionLogSettings>(SettingsResourcePath);
        }

        private bool IsEnabled => _settings == null || _settings.enabled;
        private bool ShouldWriteJsonl => _settings == null || _settings.writeJsonl;
        private bool ShouldWriteEventCsv => _settings == null || (_settings.writeCsv && _settings.writeEventCsv);
        private bool ShouldWriteSummaryCsv => _settings == null || (_settings.writeCsv && _settings.writeSessionSummaryCsv);
        private float ElapsedSeconds => Mathf.Max(0f, Time.realtimeSinceStartup - _startedRealtime);

        private void WriteSummaryJson()
        {
            if (ShouldWriteJsonl)
            {
                _writer?.WriteSummaryJson(_summary);
            }
        }

        private static string ResolveMachineId()
        {
            string existing = PlayerPrefs.GetString(MachineIdPrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            string created = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(MachineIdPrefsKey, created);
            PlayerPrefs.Save();
            return created;
        }

        private static string ToIso(DateTime value)
        {
            return value.ToString("O");
        }

        private static bool IsTitleReturnReason(string reason)
        {
            return reason == "return_to_title" ||
                   reason == "return_to_title_from_menu" ||
                   reason == "game_over_to_title" ||
                   reason == "idle_timeout" ||
                   reason == "clear_screen_to_title";
        }
    }
}
