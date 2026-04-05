using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EightAID.EIGHTAIDLib.UI
{
    /// <summary>
    /// Helper component that visualizes UI hit areas and child colliders in the SceneView.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UIHitAreaVisualizer : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private bool drawOnlyWhenSelected = true;
        [SerializeField] private bool includeInactive = true;
        [SerializeField] private Color visualizationColor = new Color(1f, 0f, 0f, 0.9f);

        [Header("Targets")]
        [SerializeField] private bool drawRaycastTargets = true;
        [SerializeField] private bool drawChildColliders = true;
        [SerializeField] private bool includeRootRectTransform = false;

        [Header("Style")]
        [SerializeField] [Range(0f, 1f)] private float fillAlpha = 0.08f;
        [SerializeField] private float lineThickness = 2f;

        public void Configure(
            Color color,
            bool onlyWhenSelected,
            bool inactiveObjects,
            bool raycastTargets,
            bool childColliders,
            bool rootRectTransform,
            float alpha,
            float thickness)
        {
            visualizationColor = color;
            drawOnlyWhenSelected = onlyWhenSelected;
            includeInactive = inactiveObjects;
            drawRaycastTargets = raycastTargets;
            drawChildColliders = childColliders;
            includeRootRectTransform = rootRectTransform;
            fillAlpha = Mathf.Clamp01(alpha);
            lineThickness = Mathf.Max(0.1f, thickness);
        }

#if UNITY_EDITOR
        private static readonly Vector3[] RectWorldCorners = new Vector3[4];
        private static readonly Vector3[] BoxColliderCorners = new Vector3[8];

        private void OnDrawGizmos()
        {
            if (!ShouldDraw())
            {
                return;
            }

            DrawVisualization();
        }

        private bool ShouldDraw()
        {
            if (!enabled)
            {
                return false;
            }

            if (!drawOnlyWhenSelected)
            {
                return true;
            }

            var active = Selection.activeGameObject;
            return active != null && (active == gameObject || active.transform.IsChildOf(transform));
        }

        private void DrawVisualization()
        {
            Color outlineColor = visualizationColor;
            Color fillColor = visualizationColor;
            fillColor.a = Mathf.Clamp01(fillAlpha);

            using (new Handles.DrawingScope(outlineColor))
            {
                if (drawRaycastTargets)
                {
                    DrawRaycastTargets(fillColor, outlineColor);
                }

                if (drawChildColliders)
                {
                    DrawColliders(fillColor, outlineColor);
                }
            }
        }

        private void DrawRaycastTargets(Color fillColor, Color outlineColor)
        {
            var rectTransforms = GetComponentsInChildren<RectTransform>(includeInactive);
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                var rectTransform = rectTransforms[i];
                if (rectTransform == null)
                {
                    continue;
                }

                if (!includeRootRectTransform && rectTransform == transform)
                {
                    continue;
                }

                if (!IsRaycastTarget(rectTransform))
                {
                    continue;
                }

                rectTransform.GetWorldCorners(RectWorldCorners);
                Handles.DrawSolidRectangleWithOutline(RectWorldCorners, fillColor, outlineColor);
                DrawRectOutline(RectWorldCorners);
            }
        }

        private bool IsRaycastTarget(RectTransform rectTransform)
        {
            var graphic = rectTransform.GetComponent<Graphic>();
            return graphic != null && graphic.raycastTarget && graphic.enabled;
        }

        private void DrawColliders(Color fillColor, Color outlineColor)
        {
            var colliders3D = GetComponentsInChildren<Collider>(includeInactive);
            for (int i = 0; i < colliders3D.Length; i++)
            {
                DrawCollider(colliders3D[i], fillColor, outlineColor);
            }

            var colliders2D = GetComponentsInChildren<Collider2D>(includeInactive);
            for (int i = 0; i < colliders2D.Length; i++)
            {
                DrawCollider2D(colliders2D[i], fillColor, outlineColor);
            }
        }

        private void DrawCollider(Collider collider, Color fillColor, Color outlineColor)
        {
            if (collider == null || !collider.enabled)
            {
                return;
            }

            switch (collider)
            {
                case BoxCollider boxCollider:
                    DrawBoxCollider(boxCollider, fillColor, outlineColor);
                    break;
                case SphereCollider sphereCollider:
                    DrawSphereCollider(sphereCollider, fillColor, outlineColor);
                    break;
                case CapsuleCollider capsuleCollider:
                    DrawCapsuleCollider(capsuleCollider, fillColor, outlineColor);
                    break;
                default:
                    DrawBounds(collider.bounds, fillColor, outlineColor);
                    break;
            }
        }

        private void DrawCollider2D(Collider2D collider, Color fillColor, Color outlineColor)
        {
            if (collider == null || !collider.enabled)
            {
                return;
            }

            switch (collider)
            {
                case BoxCollider2D boxCollider:
                    DrawBoxCollider2D(boxCollider, fillColor, outlineColor);
                    break;
                case CircleCollider2D circleCollider:
                    DrawCircleCollider2D(circleCollider, outlineColor);
                    break;
                case CapsuleCollider2D capsuleCollider:
                    DrawCapsuleCollider2D(capsuleCollider, fillColor, outlineColor);
                    break;
                case PolygonCollider2D polygonCollider:
                    DrawPolygonCollider2D(polygonCollider, fillColor, outlineColor);
                    break;
                default:
                    DrawBounds(collider.bounds, fillColor, outlineColor);
                    break;
            }
        }

        private void DrawRectOutline(Vector3[] corners)
        {
            Handles.DrawAAPolyLine(lineThickness, corners[0], corners[1], corners[2], corners[3], corners[0]);
        }

        private void DrawBoxCollider(BoxCollider collider, Color fillColor, Color outlineColor)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(collider.transform.position, collider.transform.rotation, collider.transform.lossyScale);
            Vector3 center = collider.center;
            Vector3 size = collider.size * 0.5f;

            BoxColliderCorners[0] = matrix.MultiplyPoint3x4(center + new Vector3(-size.x, -size.y, -size.z));
            BoxColliderCorners[1] = matrix.MultiplyPoint3x4(center + new Vector3(-size.x, -size.y, size.z));
            BoxColliderCorners[2] = matrix.MultiplyPoint3x4(center + new Vector3(-size.x, size.y, size.z));
            BoxColliderCorners[3] = matrix.MultiplyPoint3x4(center + new Vector3(-size.x, size.y, -size.z));
            BoxColliderCorners[4] = matrix.MultiplyPoint3x4(center + new Vector3(size.x, -size.y, -size.z));
            BoxColliderCorners[5] = matrix.MultiplyPoint3x4(center + new Vector3(size.x, -size.y, size.z));
            BoxColliderCorners[6] = matrix.MultiplyPoint3x4(center + new Vector3(size.x, size.y, size.z));
            BoxColliderCorners[7] = matrix.MultiplyPoint3x4(center + new Vector3(size.x, size.y, -size.z));

            Handles.color = fillColor;
            Handles.DrawAAConvexPolygon(BoxColliderCorners[0], BoxColliderCorners[1], BoxColliderCorners[2], BoxColliderCorners[3]);
            Handles.DrawAAConvexPolygon(BoxColliderCorners[4], BoxColliderCorners[5], BoxColliderCorners[6], BoxColliderCorners[7]);
            Handles.DrawAAConvexPolygon(BoxColliderCorners[0], BoxColliderCorners[1], BoxColliderCorners[5], BoxColliderCorners[4]);
            Handles.DrawAAConvexPolygon(BoxColliderCorners[2], BoxColliderCorners[3], BoxColliderCorners[7], BoxColliderCorners[6]);
            Handles.DrawAAConvexPolygon(BoxColliderCorners[1], BoxColliderCorners[2], BoxColliderCorners[6], BoxColliderCorners[5]);
            Handles.DrawAAConvexPolygon(BoxColliderCorners[0], BoxColliderCorners[3], BoxColliderCorners[7], BoxColliderCorners[4]);
            Handles.color = outlineColor;

            DrawLine(BoxColliderCorners[0], BoxColliderCorners[1]);
            DrawLine(BoxColliderCorners[1], BoxColliderCorners[2]);
            DrawLine(BoxColliderCorners[2], BoxColliderCorners[3]);
            DrawLine(BoxColliderCorners[3], BoxColliderCorners[0]);
            DrawLine(BoxColliderCorners[4], BoxColliderCorners[5]);
            DrawLine(BoxColliderCorners[5], BoxColliderCorners[6]);
            DrawLine(BoxColliderCorners[6], BoxColliderCorners[7]);
            DrawLine(BoxColliderCorners[7], BoxColliderCorners[4]);
            DrawLine(BoxColliderCorners[0], BoxColliderCorners[4]);
            DrawLine(BoxColliderCorners[1], BoxColliderCorners[5]);
            DrawLine(BoxColliderCorners[2], BoxColliderCorners[6]);
            DrawLine(BoxColliderCorners[3], BoxColliderCorners[7]);
        }

        private void DrawSphereCollider(SphereCollider collider, Color fillColor, Color outlineColor)
        {
            float radius = collider.radius * GetMaxAbsScale(collider.transform.lossyScale);
            Vector3 center = collider.transform.TransformPoint(collider.center);

            Handles.color = fillColor;
            Handles.DrawSolidDisc(center, collider.transform.up, radius);
            Handles.color = outlineColor;
            Handles.DrawWireDisc(center, collider.transform.up, radius, lineThickness);
            Handles.DrawWireDisc(center, collider.transform.right, radius, lineThickness);
            Handles.DrawWireDisc(center, collider.transform.forward, radius, lineThickness);
        }

        private void DrawCapsuleCollider(CapsuleCollider collider, Color fillColor, Color outlineColor)
        {
            DrawBounds(collider.bounds, fillColor, outlineColor);
        }

        private void DrawBoxCollider2D(BoxCollider2D collider, Color fillColor, Color outlineColor)
        {
            using (new Handles.DrawingScope(collider.transform.localToWorldMatrix))
            {
                Vector2 halfSize = collider.size * 0.5f;
                Vector3[] points =
                {
                    collider.offset + new Vector2(-halfSize.x, -halfSize.y),
                    collider.offset + new Vector2(-halfSize.x, halfSize.y),
                    collider.offset + new Vector2(halfSize.x, halfSize.y),
                    collider.offset + new Vector2(halfSize.x, -halfSize.y)
                };

                Handles.DrawSolidRectangleWithOutline(points, fillColor, outlineColor);
                DrawPolyline(points, true);
            }
        }

        private void DrawCircleCollider2D(CircleCollider2D collider, Color outlineColor)
        {
            float radius = collider.radius * GetMaxAbsScale(collider.transform.lossyScale);
            Vector3 center = collider.transform.TransformPoint(collider.offset);
            Handles.DrawWireDisc(center, collider.transform.forward, radius, lineThickness);
        }

        private void DrawCapsuleCollider2D(CapsuleCollider2D collider, Color fillColor, Color outlineColor)
        {
            DrawBounds(collider.bounds, fillColor, outlineColor);
        }

        private void DrawPolygonCollider2D(PolygonCollider2D collider, Color fillColor, Color outlineColor)
        {
            using (new Handles.DrawingScope(collider.transform.localToWorldMatrix))
            {
                for (int pathIndex = 0; pathIndex < collider.pathCount; pathIndex++)
                {
                    Vector2[] path = collider.GetPath(pathIndex);
                    if (path == null || path.Length < 2)
                    {
                        continue;
                    }

                    Vector3[] points = new Vector3[path.Length];
                    for (int i = 0; i < path.Length; i++)
                    {
                        points[i] = path[i];
                    }

                    if (points.Length >= 3)
                    {
                        Handles.color = fillColor;
                        Handles.DrawAAConvexPolygon(points);
                        Handles.color = outlineColor;
                    }

                    DrawPolyline(points, true);
                }
            }
        }

        private void DrawBounds(Bounds bounds, Color fillColor, Color outlineColor)
        {
            Handles.color = fillColor;
            Handles.DrawSolidRectangleWithOutline(
                new[]
                {
                    new Vector3(bounds.min.x, bounds.min.y, bounds.center.z),
                    new Vector3(bounds.min.x, bounds.max.y, bounds.center.z),
                    new Vector3(bounds.max.x, bounds.max.y, bounds.center.z),
                    new Vector3(bounds.max.x, bounds.min.y, bounds.center.z)
                },
                fillColor,
                outlineColor);

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(max.x, max.y, min.z)
            };

            Handles.color = outlineColor;
            DrawLine(corners[0], corners[1]);
            DrawLine(corners[1], corners[2]);
            DrawLine(corners[2], corners[3]);
            DrawLine(corners[3], corners[0]);
            DrawLine(corners[4], corners[5]);
            DrawLine(corners[5], corners[6]);
            DrawLine(corners[6], corners[7]);
            DrawLine(corners[7], corners[4]);
            DrawLine(corners[0], corners[4]);
            DrawLine(corners[1], corners[5]);
            DrawLine(corners[2], corners[6]);
            DrawLine(corners[3], corners[7]);
        }

        private void DrawLine(Vector3 from, Vector3 to)
        {
            Handles.DrawAAPolyLine(lineThickness, from, to);
        }

        private void DrawPolyline(Vector3[] points, bool closed)
        {
            if (points == null || points.Length < 2)
            {
                return;
            }

            if (closed)
            {
                var closedPoints = new Vector3[points.Length + 1];
                for (int i = 0; i < points.Length; i++)
                {
                    closedPoints[i] = points[i];
                }

                closedPoints[points.Length] = points[0];
                Handles.DrawAAPolyLine(lineThickness, closedPoints);
                return;
            }

            Handles.DrawAAPolyLine(lineThickness, points);
        }

        private float GetMaxAbsScale(Vector3 scale)
        {
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }
#endif
    }
}
