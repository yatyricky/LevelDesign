using System;
using UnityEngine;

namespace NefAndFriends.LevelDesigner
{
    public enum VertexType
    {
        Normal,
        Start,
        Save,
        Boss
    }

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

        private Vector2 _position;

        /// <summary>
        /// For drawing
        /// </summary>
        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                _graph.Dirty = true;
            }
        }

        internal int Index;

        private readonly Graph _graph;

        public Vertex(string name, float weight, Graph graph)
        {
            _graph = graph;
            Name = name;
            Weight = weight;
        }

        private string NodeTypeToString()
        {
            switch (Type)
            {
                case VertexType.Normal:
                    return "poi";
                case VertexType.Start:
                    return "start";
                case VertexType.Save:
                    return "save";
                case VertexType.Boss:
                    return "boss";
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
