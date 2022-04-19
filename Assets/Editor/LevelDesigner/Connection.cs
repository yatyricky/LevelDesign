using System;
using UnityEngine;
using LevelDesigner;
using UnityEditor;
using UnityEngine.UIElements;

namespace LevelDesignerEditor
{
    public class Connection : IDisposable
    {
        private const float LoopSize = GraphWindow.NodeRadius * 3;
        private const float ConeSize = 15f;
        private const float DotSize = 7f;
        private const float ArrowAngle = 45f;

        private static Texture2D _pathTexInternal;

        private static Texture2D _pathTex
        {
            get
            {
                if (_pathTexInternal == null)
                    _pathTexInternal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Textures/line.png");
                return _pathTexInternal;
            }
        }

        private static Texture2D _pathTexHighInternal;

        private static Texture2D _pathTexHigh
        {
            get
            {
                if (_pathTexHighInternal == null)
                    _pathTexHighInternal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Textures/line_up.png");
                return _pathTexHighInternal;
            }
        }

        public VisualElement DOM { get; }

        private Node _source, _target;
        public Edge Edge { get; }

        private Vector3[] _pathPoints;
        private bool _isSelected;
        private float _angle;

        public Connection(Node source, Node target, Edge edge, VisualElement parent)
        {
            _source = source;
            _target = target;
            Edge = edge;

            DOM = new VisualElement();
            DOM.AddToClassList("connection");

            parent.Add(DOM);
        }

        public void Dispose()
        {
            DOM.parent.Remove(DOM);
        }

        public void OnGUI()
        {
            if (_source == null || _target == null)
            {
                return;
            }

            var rect1 = _source.DOM.worldBound;
            var rect2 = _target.DOM.worldBound;
            var sourcePos = rect1.center - new Vector2(0f, 21f);
            var targetPos = rect2.center - new Vector2(0f, 21f);
            var p1 = sourcePos;
            var p2 = targetPos;
            var conePosCheck = GraphWindow.NodeRadius;
            var dotPosCheck = GraphWindow.NodeRadius + DotSize;

            if (_source == _target)
            {
                var ra = _angle;
                var ral = _angle - 45 * Mathf.Deg2Rad;
                var rar = _angle + 45 * Mathf.Deg2Rad;
                var r = new Vector2(GraphWindow.NodeRadius * Mathf.Cos(ra), GraphWindow.NodeRadius * Mathf.Sin(ra));
                sourcePos = p1 + r;
                targetPos = sourcePos;

                var p1V = new Vector2(LoopSize * Mathf.Cos(ral), LoopSize * Mathf.Sin(ral));
                p1 = sourcePos + p1V;

                var p2V = new Vector2(LoopSize * Mathf.Cos(rar), LoopSize * Mathf.Sin(rar));
                p2 = sourcePos + p2V;

                conePosCheck = 1f;
                dotPosCheck = 1f;
            }
            else
            {
                var v = p2 - p1;
                var dir = v.normalized;
                var cos = Mathf.Cos(_angle);
                var sin = Mathf.Sin(_angle);
                var newDir = new Vector2(cos * dir.x - sin * dir.y, sin * dir.x + cos * dir.y);
                var pLen = v.magnitude * 0.5f / cos;
                var p1V = newDir * pLen;
                p1 = sourcePos + p1V;
                p2 = p1;
            }

            Handles.BeginGUI();
            _pathPoints = Handles.MakeBezierPoints(sourcePos, targetPos, p1, p2, 40);

            float maxX = _pathPoints[0].x, maxY = _pathPoints[0].y;
            var minX = maxX;
            var minY = maxY;
            foreach (var point in _pathPoints)
            {
                minX = Mathf.Min(minX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxX = Mathf.Max(maxX, point.x);
                maxY = Mathf.Max(maxY, point.y);
            }

            var rect = new Rect(minX, minY, maxX - minX, maxY - minY);
            var canvasPos = DOM.parent.parent.worldBound.position;
            DOM.style.width = rect.width;
            DOM.style.height = rect.height;
            DOM.style.left = rect.x - canvasPos.x;
            DOM.style.top = rect.y - canvasPos.y + 21f;

            var coneIndex = _pathPoints.Length - 2;
            var dotIndex = 0;
            if (Edge.Type == EdgeType.ShortCut)
            {
                coneIndex = _pathPoints.Length / 2;
            }
            else
            {
                for (var i = _pathPoints.Length - 2; i >= 0; i--)
                {
                    var p = _pathPoints[i];
                    if (Vector3.Distance(p, targetPos) <= conePosCheck)
                        continue;

                    coneIndex = i;
                    break;
                }

                for (var i = 0; i < _pathPoints.Length - 1; i++)
                {
                    var p = _pathPoints[i];
                    if (Vector3.Distance(p, sourcePos) <= dotPosCheck)
                        continue;

                    dotIndex = i;
                    break;
                }
            }

            var conePos = _pathPoints[coneIndex];
            var coneRot = Quaternion.AngleAxis(-ArrowAngle * 0.5f, Vector3.forward) * (conePos - _pathPoints[coneIndex + 1]);
            var dotPos = _pathPoints[dotIndex];
            var tex = _isSelected ? _pathTexHigh : _pathTex;
            Handles.DrawAAPolyLine(tex, _pathPoints);
            switch (Edge.Type)
            {
                case EdgeType.Undirected:
                    break;
                case EdgeType.Directed:
                    Handles.DrawSolidArc(conePos, Vector3.forward, coneRot, ArrowAngle, ConeSize);
                    break;
                case EdgeType.ShortCut:
                    Handles.DrawSolidArc(conePos, Vector3.forward, coneRot, ArrowAngle, ConeSize);
                    break;
                case EdgeType.Mechanism:
                    Handles.DrawSolidArc(conePos, Vector3.forward, coneRot, ArrowAngle, ConeSize);
                    Handles.DrawSolidArc(dotPos, Vector3.forward, coneRot, 360f, DotSize);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Handles.EndGUI();
        }

        public bool PathCastPoint(Vector2 p)
        {
            var hit = false;
            var fix = new Vector3(0f, 21f, 0f);
            if (_pathPoints != null)
            {
                for (var i = 0; i < _pathPoints.Length - 1 && !hit; i++)
                {
                    var capsule = new Capsule(_pathPoints[i] + fix, _pathPoints[i + 1] + fix, 8f);
                    if (Utils.CapsuleContains(capsule, p))
                    {
                        hit = true;
                    }
                }
            }

            return hit;
        }

        public void SetSelected(bool flag)
        {
            _isSelected = flag;
        }

        public void SetEdgeType(EdgeType edgeType)
        {
            Edge.Type = edgeType;
        }

        public void SetCurve(int count)
        {
            _angle = count <= 0 ? 0 : 20 * Mathf.Deg2Rad;
        }

        public void SetAngle(Vector2 vector2)
        {
            _angle = -Mathf.Atan2(vector2.y, vector2.x);
        }
    }
}
