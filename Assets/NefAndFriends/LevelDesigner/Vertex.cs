using System;
using UnityEngine;

namespace NefAndFriends.LevelDesigner
{
    public class Vertex
    {
        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                var currentNames = _graph.GetVertexNames();
                currentNames.Remove(_name);
                if (currentNames.Contains(value))
                {
                    return;
                }

                _name = value;
            }
        }

        public float Weight;

        private VertexType _type;

        public VertexType Type
        {
            get => _type;
            set
            {
                _type = value;
                _graph.Dirty = true;
            }
        }

        private Vector3 _position;

        /// <summary>
        /// For drawing
        /// </summary>
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                _graph.Dirty = true;
            }
        }

        internal int Index;

        private Graph _graph;

        public Vertex(string name, float weight, Graph graph)
        {
            _graph = graph;
            Name = name;
            Weight = weight;
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
            return $"#{Name} pos:({Position.x:0.00},{Position.y:0.00}) type:{NodeTypeToString()} weight:{Weight}";
        }

        public Vertex Clone()
        {
            var v = new Vertex(Name, Weight, _graph)
            {
                _position = _position,
                _type = _type
            };
            return v;
        }
    }
}
