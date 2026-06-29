using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ActionHudRoundedRectMaskGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float cornerRadius = 8f;
        [SerializeField, Range(1, 12)] private int cornerSegments = 5;

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                float next = Mathf.Max(0f, value);
                if (Mathf.Approximately(cornerRadius, next))
                {
                    return;
                }

                cornerRadius = next;
                SetVerticesDirty();
            }
        }

        public int CornerSegments
        {
            get => cornerSegments;
            set
            {
                int next = Mathf.Clamp(value, 1, 12);
                if (cornerSegments == next)
                {
                    return;
                }

                cornerSegments = next;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
            if (radius <= 0.001f)
            {
                PopulateRect(vh, rect);
                return;
            }

            List<Vector2> points = new List<Vector2>((cornerSegments + 1) * 4);
            AppendCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f);
            AppendCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f);
            AppendCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f);
            AppendCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f);

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = rect.center;
            vh.AddVert(vertex);

            for (int i = 0; i < points.Count; i++)
            {
                vertex.position = points[i];
                vh.AddVert(vertex);
            }

            for (int i = 1; i <= points.Count; i++)
            {
                int next = i == points.Count ? 1 : i + 1;
                vh.AddTriangle(0, i, next);
            }
        }

        private void AppendCorner(List<Vector2> points, Vector2 center, float radius, float fromDegrees, float toDegrees)
        {
            int steps = Mathf.Max(1, cornerSegments);
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(fromDegrees, toDegrees, i / (float)steps) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private void PopulateRect(VertexHelper vh, Rect rect)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector2(rect.xMin, rect.yMin);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMin, rect.yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMin);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Clamp(cornerSegments, 1, 12);
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
