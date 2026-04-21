using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Analytics
{
    public sealed class ExhibitionLogWriter
    {
        private const string LogFolderName = "ExhibitionLogs";
        private const string SummaryCsvFileName = "session_summary.csv";
        private const string EventCsvFileName = "session_events.csv";
        private static readonly Encoding CsvEncoding = new UTF8Encoding(true);
        private static readonly Encoding JsonEncoding = new UTF8Encoding(false);

        private readonly string _logDirectory;
        private readonly string _sessionPrefix;

        public ExhibitionLogWriter(string sessionId, DateTime startedAtLocal)
        {
            _logDirectory = Path.Combine(Application.persistentDataPath, LogFolderName);
            string safeSessionId = string.IsNullOrWhiteSpace(sessionId) ? "unknown" : sessionId;
            _sessionPrefix = $"{startedAtLocal:yyyyMMdd_HHmmss}_{safeSessionId}";
        }

        public string LogDirectory => _logDirectory;
        public string EventsJsonlPath => Path.Combine(_logDirectory, _sessionPrefix + ".events.jsonl");
        public string SummaryJsonPath => Path.Combine(_logDirectory, _sessionPrefix + ".summary.json");
        public string SummaryCsvPath => Path.Combine(_logDirectory, SummaryCsvFileName);
        public string EventCsvPath => Path.Combine(_logDirectory, EventCsvFileName);

        public void WriteEventJsonl(ExhibitionLogEvent logEvent)
        {
            if (logEvent == null)
            {
                return;
            }

            SafeWrite(() =>
            {
                EnsureDirectory();
                File.AppendAllText(EventsJsonlPath, logEvent.ToJson() + Environment.NewLine, JsonEncoding);
            }, "event jsonl");
        }

        public void WriteSummaryJson(ExhibitionLogSessionSummary summary)
        {
            if (summary == null)
            {
                return;
            }

            SafeWrite(() =>
            {
                EnsureDirectory();
                File.WriteAllText(SummaryJsonPath, summary.ToJson(), JsonEncoding);
            }, "summary json");
        }

        public void AppendEventCsv(ExhibitionLogEvent logEvent)
        {
            if (logEvent == null)
            {
                return;
            }

            SafeWrite(() =>
            {
                EnsureDirectory();
                AppendCsvLine(EventCsvPath, EventCsvHeader, BuildEventCsvRow(logEvent));
            }, "event csv");
        }

        public void AppendSummaryCsv(ExhibitionLogSessionSummary summary)
        {
            if (summary == null)
            {
                return;
            }

            SafeWrite(() =>
            {
                EnsureDirectory();
                AppendCsvLine(SummaryCsvPath, SummaryCsvHeader, BuildSummaryCsvRow(summary));
            }, "summary csv");
        }

        public void TrimOldSessionFiles(int maxSessionFiles)
        {
            if (maxSessionFiles <= 0)
            {
                return;
            }

            SafeWrite(() =>
            {
                if (!Directory.Exists(_logDirectory))
                {
                    return;
                }

                var files = new List<FileInfo>();
                var directory = new DirectoryInfo(_logDirectory);
                files.AddRange(directory.GetFiles("*.events.jsonl"));
                files.AddRange(directory.GetFiles("*.summary.json"));
                files.Sort((left, right) => right.CreationTimeUtc.CompareTo(left.CreationTimeUtc));

                for (int i = maxSessionFiles; i < files.Count; i++)
                {
                    files[i].Delete();
                }
            }, "trim old session files");
        }

        private void EnsureDirectory()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private static void AppendCsvLine(string path, string[] header, string[] row)
        {
            bool needsHeader = !File.Exists(path) || new FileInfo(path).Length == 0;
            using var writer = new StreamWriter(path, append: true, CsvEncoding);
            if (needsHeader)
            {
                writer.WriteLine(ToCsvLine(header));
            }

            writer.WriteLine(ToCsvLine(row));
        }

        private static string ToCsvLine(IReadOnlyList<string> cells)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(EscapeCsv(cells[i]));
            }

            return builder.ToString();
        }

        private static string EscapeCsv(string value)
        {
            value ??= string.Empty;
            bool needsQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!needsQuote)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Number(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static void SafeWrite(Action action, string label)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ExhibitionLog] Failed to write {label}: {ex.Message}");
            }
        }

        private static readonly string[] SummaryCsvHeader =
        {
            "sessionId",
            "machineId",
            "buildVersion",
            "exhibitionDate",
            "saveSlotId",
            "startedAtLocal",
            "startedAtUtc",
            "endedAtLocal",
            "endedAtUtc",
            "elapsedSeconds",
            "startReason",
            "endReason",
            "isCleared",
            "returnedToTitle",
            "gameOverCount",
            "retryCurrentStageCount",
            "retryFromMapStartCount",
            "battleCount",
            "stageClearCount",
            "menuOpenCount",
            "lastScene",
            "lastStageId",
            "lastEventTitle"
        };

        private static readonly string[] EventCsvHeader =
        {
            "sessionId",
            "machineId",
            "buildVersion",
            "exhibitionDate",
            "eventTimeLocal",
            "eventTimeUtc",
            "elapsedSeconds",
            "eventName",
            "sceneName",
            "sceneBuildIndex",
            "stageId",
            "eventTitle",
            "enemyId",
            "enemyName",
            "turnCount",
            "value",
            "result",
            "reason",
            "payload"
        };

        private static string[] BuildSummaryCsvRow(ExhibitionLogSessionSummary summary)
        {
            return new[]
            {
                summary.sessionId,
                summary.machineId,
                summary.buildVersion,
                summary.exhibitionDate,
                summary.saveSlotId,
                summary.startedAtLocal,
                summary.startedAtUtc,
                summary.endedAtLocal,
                summary.endedAtUtc,
                Number(summary.elapsedSeconds),
                summary.startReason,
                summary.endReason,
                Number(summary.isCleared),
                Number(summary.returnedToTitle),
                Number(summary.gameOverCount),
                Number(summary.retryCurrentStageCount),
                Number(summary.retryFromMapStartCount),
                Number(summary.battleCount),
                Number(summary.stageClearCount),
                Number(summary.menuOpenCount),
                summary.lastScene,
                summary.lastStageId,
                summary.lastEventTitle
            };
        }

        private static string[] BuildEventCsvRow(ExhibitionLogEvent logEvent)
        {
            return new[]
            {
                logEvent.sessionId,
                logEvent.machineId,
                logEvent.buildVersion,
                logEvent.exhibitionDate,
                logEvent.eventTimeLocal,
                logEvent.eventTimeUtc,
                Number(logEvent.elapsedSeconds),
                logEvent.eventName,
                logEvent.sceneName,
                Number(logEvent.sceneBuildIndex),
                logEvent.stageId,
                logEvent.eventTitle,
                logEvent.enemyId,
                logEvent.enemyName,
                Number(logEvent.turnCount),
                Number(logEvent.value),
                logEvent.result,
                logEvent.reason,
                logEvent.payload
            };
        }
    }
}
