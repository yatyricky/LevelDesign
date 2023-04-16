using System;
using UnityEngine;

namespace NefAndFriends.LevelDesigner
{
    [Serializable]
    public class Vertex
    {
        [SerializeField]
        private string name;

        public string Name
        {
            get => name;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                var currentNames = _graph.GetVertexNames();
                currentNames.Remove(name);
                if (currentNames.Contains(value))
                {
                    return;
                }

                name = value;
            }
        }

        public float weight;

        [SerializeField]
        private VertexType type;

        public VertexType Type
        {
            get => type;
            set
            {
                type = value;
                _graph.Dirty = true;
            }
        }

        [SerializeField]
        private Vector3 position;

        /// <summary>
        /// For drawing
        /// </summary>
        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                _graph.Dirty = true;
            }
        }

        internal int Index;

        [NonSerialized]
        private Graph _graph;

        public Vertex(string name, float weight, Graph graph)
        {
            _graph = graph;
            Name = name;
            this.weight = weight;
        }

        private string NodeTypeToString()
        {
            return Type switch
            {
                VertexType.Normal => "poi",
                VertexType.Start => "start",
                VertexType.Save => "save",
                VertexType.Boss => "boss",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override string ToString()
        {
            return $"#{Name} pos:({Position.x:0.00},{Position.y:0.00}) type:{NodeTypeToString()} weight:{weight}";
        }

        public Vertex Clone()
        {
            var v = new Vertex(Name, weight, _graph)
            {
                position = position,
                type = type
            };
            return v;
        }

        public void SetParent(Graph graph)
        {
            _graph = graph;
        }
    }
}
