using System;
using System.Collections.Generic;
using UnityEngine;

namespace EightAID.StoryGraph
{
    [Serializable]
    public sealed class EmptyStoryNodePayload : IStoryNodePayload
    {
        public int SchemaVersion => 1;
    }

    [Serializable]
    public sealed class MessagePayload : IStoryNodePayload
    {
        [SerializeField] private int _schemaVersion = 1;
        [Tooltip("表示する話者名です。空の場合は名前欄を非表示にできます。")]
        [SerializeField] private string _speakerName;
        [Tooltip("順番に表示する本文です。")]
        [SerializeField] private List<string> _lines = new List<string>();

        public int SchemaVersion => _schemaVersion;
        public string SpeakerName => _speakerName;
        public IReadOnlyList<string> Lines => _lines;
    }

    [Serializable]
    public sealed class BranchPayload : IStoryNodePayload
    {
        [SerializeField] private int _schemaVersion = 1;
        [Tooltip("プロジェクト側の条件評価器へ渡すキーです。")]
        [SerializeField] private string _conditionKey;
        [Tooltip("条件が一致すべき値です。")]
        [SerializeField] private string _expectedValue;

        public int SchemaVersion => _schemaVersion;
        public string ConditionKey => _conditionKey;
        public string ExpectedValue => _expectedValue;
    }

    [Serializable]
    public sealed class DelayPayload : IStoryNodePayload
    {
        [SerializeField] private int _schemaVersion = 1;
        [Min(0f), Tooltip("次のノードへ進むまでの秒数です。")]
        [SerializeField] private float _seconds;

        public int SchemaVersion => _schemaVersion;
        public float Seconds => _seconds;
    }

    [Serializable]
    public sealed class CommentPayload : IStoryNodePayload
    {
        [SerializeField] private int _schemaVersion = 1;
        [TextArea(2, 8), Tooltip("制作者向けのメモです。実行時には処理されません。")]
        [SerializeField] private string _text;

        public int SchemaVersion => _schemaVersion;
        public string Text => _text;
    }
}
