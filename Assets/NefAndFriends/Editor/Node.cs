using System;
using NefAndFriends.LevelDesigner;
using UnityEngine;
using UnityEngine.UIElements;

namespace NefAndFriends.LevelDesignerEditor
{
    public class Node : IDisposable
    {
        public VisualElement DOM { get; }

        // dragging
        public LevelDesigner.Vertex Vertex { get; }

        private bool _isSelected;

        public Circle Circle
        {
            get
            {
                var center = DOM.worldBound.center;
                return new Circle(center, DOM.worldBound.width * 0.5f);
            }
        }

        public Node(LevelDesigner.Vertex vertex, VisualElement parent)
        {
            Vertex = vertex;

            // root
            DOM = new VisualElement();
            DOM.AddToClassList("node");

            // title
            var domTitle = new Label();
            domTitle.AddToClassList("title");
            domTitle.text = vertex.Name;
            DOM.Add(domTitle);

            // type
            SetVertexType(vertex.Type);

            // hierarchy
            parent.Add(DOM);

            // position
            var parentPos = DOM.parent.parent.worldBound.position;
            SetPosition(Utils.World2Canvas(vertex.Position) + parentPos);
        }

        public void OnGUI()
        {
            // ((Label) DOM.Children().First()).text = DOM.worldBound.ToString();
        }

        public void Dispose()
        {
            DOM.parent.Remove(DOM);
        }

        public void SetSelected(bool flag)
        {
            if (_isSelected != flag)
            {
                if (flag)
                {
                    DOM.AddToClassList("node-high");
                }
                else
                {
                    DOM.RemoveFromClassList("node-high");
                }
            }

            _isSelected = flag;
        }

        private void SetLocalPosition(Vector2 localPos)
        {
            DOM.style.left = localPos.x;
            DOM.style.top = localPos.y;
        }

        public void SetPosition(Vector2 worldPos)
        {
            var parentPos = DOM.parent.parent.worldBound.position;
            var relPos = worldPos - parentPos;
            SetLocalPosition(relPos);
            Vertex.Position = Utils.Canvas2World(relPos);
        }

        public void SetVertexType(VertexType vertexType)
        {
            Vertex.Type = vertexType;
            DOM.style.backgroundColor = vertexType switch
            {
                VertexType.Interest => new Color(0.153f, 0.682f, 0.376f),
                VertexType.Start => new Color(0.161f, 0.502f, 0.725f),
                VertexType.Save => new Color(0.827f, 0.329f, 0f),
                VertexType.Boss => new Color(0.753f, 0.224f, 0.169f),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
