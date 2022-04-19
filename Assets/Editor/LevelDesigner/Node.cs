using System;
using LevelDesigner;
using UnityEngine;
using UnityEngine.UIElements;

namespace LevelDesignerEditor
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
            switch (vertexType)
            {
                case VertexType.Normal:
                    DOM.style.backgroundColor = new Color(0.239f, 0.294f, 0.329f);
                    break;
                case VertexType.Start:
                    DOM.style.backgroundColor = new Color(0.082f, 0.235f, 0.467f);
                    break;
                case VertexType.Save:
                    DOM.style.backgroundColor = new Color(0.467f, 0.294f, 0.082f);
                    break;
                case VertexType.Boss:
                    DOM.style.backgroundColor = new Color(0.467f, 0.082f, 0.173f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
