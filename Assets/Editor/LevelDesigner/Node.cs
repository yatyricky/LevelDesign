using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LevelDesigner;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;
using Vertex = LevelDesigner.Vertex;

namespace LevelDesignerEditor
{
    public class Node : IDisposable
    {
        public VisualElement DOM { get; private set; }

        public Vector2 Position { get; private set; }

        public Vector2 WorldPosition => Position + new Vector2(30f, 30f) + Utils.GetDOMLocalPosition(DOM.parent);

        private bool _dragging;

        private Vector2 _anchor;

        // private List<Node> _outgoings;
        // private int _incomingPortIndex;
        private string _name;
        // private Color _edgeColor;

        private Label _domTitle;

        public Vertex Vertex;
        private GraphWindow _parent;

        public Node() : this("Node", new Vector2(0, 0))
        {
        }

        public Node(string name, Vector2 position)
        {
            DOM = new VisualElement();
            DOM.AddToClassList("node");
            _domTitle = new Label();
            _domTitle.AddToClassList("title");
            DOM.Add(_domTitle);

            // _edgeColor = _palette[_instances++ % _palette.Length];
            // _edgeColor = Color.cyan;

            _name = name;
            Position = position;
            _dragging = false;
            // _outgoings = new List<Node>();
            Update();
            DOM.RegisterCallback<MouseDownEvent>(OnMouseDown);
            DOM.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            DOM.RegisterCallback<MouseUpEvent>(OnMouseUp);
            DOM.RegisterCallback<MouseOutEvent>(OnMouseOut);
        }

        public void SetParent(GraphWindow parent)
        {
            _parent = parent;
        }

        // public Node AddOutgoing(Node next)
        // {
        //     _outgoings.Add(next);
        //     return this;
        // }

        private void Update()
        {
            DOM.style.left = Position.x;
            DOM.style.top = Position.y;
        }

        // public void ResetIncomingPorts()
        // {
        //     _incomingPortIndex = 0;
        // }

        // private Vector2 GetIncomingPort()
        // {
        //     return new Vector2(Position.x, Position.y + 24 + _incomingPortIndex++ * 16);
        // }

        public void Draw()
        {
            _domTitle.text = _name; // + $"{Position}";

            switch (Vertex.Type)
            {
                case NodeType.Normal:
                    DOM.style.backgroundColor = new Color(0.239f, 0.294f, 0.329f);
                    break;
                case NodeType.Start:
                    DOM.style.backgroundColor = new Color(0.082f, 0.235f, 0.467f);
                    break;
                case NodeType.Save:
                    DOM.style.backgroundColor = new Color(0.467f, 0.294f, 0.082f);
                    break;
                case NodeType.Boss:
                    DOM.style.backgroundColor = new Color(0.467f, 0.082f, 0.173f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Update();
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            if (e.button != 0)
            {
                return;
            }
            
            _parent.SetEditingNode(this);

            if (_dragging)
            {
                e.StopImmediatePropagation();
                return;
            }

            var parent = DOM.parent;
            var siblings = parent.Children().ToList();
            var dict = new Dictionary<VisualElement, int>();
            for (var i = 0; i < parent.childCount; i++)
            {
                var item = siblings[i];
                var order = item == DOM ? siblings.Count : i;
                dict.Add(item, order);
            }

            parent.Sort((a, b) => dict[a] - dict[b]);

            var mousePos = e.mousePosition;
            _anchor = mousePos - Position;
            _dragging = true;
            e.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent e)
        {
            if (!_dragging)
                return;

            Position = e.mousePosition - _anchor;
            Vertex.Position = GraphWindow.Screen2World(Position);
            Update();

            e.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            if (!_dragging)
                return;

            _dragging = false;
            e.StopPropagation();
        }

        private void OnMouseOut(MouseOutEvent e)
        {
            if (!_dragging)
                return;

            Position = e.mousePosition - _anchor;
            Update();

            e.StopPropagation();
        }

        public void Dispose()
        {
            DOM.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            DOM.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            DOM.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            DOM.UnregisterCallback<MouseOutEvent>(OnMouseOut);

            DOM.parent.Remove(DOM);
        }
    }
}
