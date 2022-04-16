using System;
using UnityEngine;
using LevelDesigner;
using UnityEditor;
using UnityEngine.UIElements;

namespace LevelDesignerEditor
{
    public class Connection : IDisposable
    {
        private const float LoopSize = GraphWindow.NodeRadius * 4;
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

        public VisualElement DOM { get; private set; }
        private Rect _rect;
        private Vector3[] _pathPoints;

        private Node _source, _target;
        public Edge Edge;

        private GraphWindow _parent;

        public Connection(Node source, Node target, Edge edge)
        {
            _source = source;
            _target = target;
            Edge = edge;

            DOM = new VisualElement();
            DOM.AddToClassList("connection");

            DOM.RegisterCallback<MouseDownEvent>(OnMouseDown);
            DOM.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            DOM.RegisterCallback<MouseUpEvent>(OnMouseUp);
            DOM.RegisterCallback<MouseOutEvent>(OnMouseOut);
        }

        public void Dispose()
        {
            DOM.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            DOM.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            DOM.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            DOM.UnregisterCallback<MouseOutEvent>(OnMouseOut);

            DOM.parent.Remove(DOM);
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            if (e.button != 0)
            {
                return;
            }

            var hit = false;
            if (_pathPoints != null)
            {
                var circle = new Circle(e.localMousePosition + _rect.position, 10f);
                for (var i = 0; i < _pathPoints.Length - 1 && !hit; i++)
                {
                    var capsule = new Capsule(_pathPoints[i], _pathPoints[i + 1], 10f);
                    if (Utils.CapsuleCircle(capsule, circle))
                    {
                        hit = true;
                    }
                }
            }

            if (hit)
            {
                _parent.SetEditingConnection(this);
                e.StopPropagation();
            }
            else
            {
                _parent.SetEditingConnection(null);
            }
        }

        private void OnMouseMove(MouseMoveEvent e)
        {
            // e.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            // e.StopPropagation();
        }

        private void OnMouseOut(MouseOutEvent e)
        {
            // e.StopPropagation();
        }

        public void SetParent(GraphWindow parent)
        {
            _parent = parent;
        }

        public void Draw()
        {
            if (_source == null || _target == null)
            {
                return;
            }

            var sourcePos = _source.WorldPosition;
            var targetPos = _target.WorldPosition;
            var p1 = sourcePos;
            var p2 = targetPos;
            var conePosCheck = GraphWindow.NodeRadius;
            var dotPosCheck = GraphWindow.NodeRadius + DotSize;

            if (_source == _target)
            {
                var ra = Edge.Angle * Mathf.Deg2Rad;
                var ral = (Edge.Angle - 45) * Mathf.Deg2Rad;
                var rar = (Edge.Angle + 45) * Mathf.Deg2Rad;
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

            _rect = new Rect(minX, minY, maxX - minX, maxY - minY);
            DOM.style.width = _rect.width;
            DOM.style.height = _rect.height;
            DOM.style.left = _rect.x;
            DOM.style.top = _rect.y;

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
            var tex = _parent.EditingConnection == this ? _pathTexHigh : _pathTex;
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
    }
}
