using System;
using System.Collections.Generic;
using UnityEngine;

namespace EightAID.StoryGraph
{
    /// <summary>
    /// StoryGraph のノード種別に共通する識別子です。
    /// プロジェクト固有ノードは「プロジェクト名.機能名」の形式で追加します。
    /// </summary>
    public static class StoryNodeTypeIds
    {
        public const string Root = "core.root";
        public const string Message = "core.message";
        public const string Branch = "core.branch";
        public const string End = "core.end";
        public const string Comment = "core.comment";
        public const string Delay = "core.delay";
    }

    /// <summary>
    /// ノードから出る遷移の用途です。Handler は Success／Failure を使って結果を分岐できます。
    /// </summary>
    public enum StoryEdgeRole
    {
        Next,
        True,
        False,
        Default,
        Success,
        Failure
    }

    /// <summary>
    /// ノード間の接続情報です。Editor の経路座標も共通構造へ保存します。
    /// </summary>
    [Serializable]
    public sealed class StoryEdgeRecord
    {
        public StoryEdgeRecord(
            string targetNodeId,
            StoryEdgeRole role,
            bool hasManualRoute = false,
            float manualRouteX = 0f,
            IEnumerable<Vector2> routePoints = null)
        {
            _targetNodeId = targetNodeId;
            _role = role;
            _hasManualRoute = hasManualRoute;
            _manualRouteX = manualRouteX;
            if (routePoints != null)
            {
                _routePoints.AddRange(routePoints);
            }
        }

        [Tooltip("遷移先ノードのIDです。")]
        [SerializeField] private string _targetNodeId;

        [Tooltip("この接続を使う場面です。")]
        [SerializeField] private StoryEdgeRole _role;

        [SerializeField] private bool _hasManualRoute;
        [SerializeField] private float _manualRouteX;
        [SerializeField] private List<Vector2> _routePoints = new List<Vector2>();

        public string TargetNodeId => _targetNodeId;
        public StoryEdgeRole Role => _role;
        public bool HasManualRoute => _hasManualRoute;
        public float ManualRouteX => _manualRouteX;
        public IReadOnlyList<Vector2> RoutePoints => _routePoints;
    }

    /// <summary>
    /// グラフ上の1ノードです。payloadJson の形式は nodeTypeId ごとに定義します。
    /// </summary>
    [Serializable]
    public sealed class StoryNodeRecord
    {
        public StoryNodeRecord(string id, string nodeTypeId, IStoryNodePayload payload, Vector2 editorPosition)
        {
            _id = id;
            _nodeTypeId = nodeTypeId;
            _payload = payload;
            _editorPosition = editorPosition;
        }

        [Tooltip("グラフ内で一意なノードIDです。")]
        [SerializeField] private string _id;

        [Tooltip("ノードの種類です。例: core.message、sample.set-flag")]
        [SerializeField] private string _nodeTypeId;

        [Tooltip("ノード固有の保存値です。プロジェクト側で定義した型を保持します。")]
        [SerializeReference] private IStoryNodePayload _payload;

        [Tooltip("Editor 上の表示位置です。実行時の挙動には影響しません。")]
        [SerializeField] private Vector2 _editorPosition;

        [Tooltip("このノードから出る接続です。")]
        [SerializeField] private List<StoryEdgeRecord> _edges = new List<StoryEdgeRecord>();

        public string Id => _id;
        public string NodeTypeId => _nodeTypeId;
        public IStoryNodePayload Payload => _payload;
        public Vector2 EditorPosition => _editorPosition;
        public IReadOnlyList<StoryEdgeRecord> Edges => _edges;

        public void SetEditorPosition(Vector2 position)
        {
            _editorPosition = position;
        }

        public void SetPayload(IStoryNodePayload payload)
        {
            _payload = payload;
        }

        public void ReplaceEdges(IEnumerable<StoryEdgeRecord> edges)
        {
            _edges.Clear();
            if (edges != null)
            {
                _edges.AddRange(edges);
            }
        }
    }
}
