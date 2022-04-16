using System;
using UnityEngine;

namespace LevelDesigner
{
    public enum NodeType
    {
        Normal,
        Start,
        Save,
        Boss
    }

    public class Vertex
    {
        public string Name;
        public float Weight;
        public NodeType Type;

        /// <summary>
        /// For drawing
        /// </summary>
        public Vector2 Position;

        public Vertex(string name, float weight)
        {
            Name = name;
            Weight = weight;
        }

        private string NodeTypeToString()
        {
            switch (Type)
            {
                case NodeType.Normal:
                    return "poi";
                case NodeType.Start:
                    return "start";
                case NodeType.Save:
                    return "save";
                case NodeType.Boss:
                    return "boss";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return $"#{Name} pos:({Position.x:0.00},{Position.y:0.00}) type:{NodeTypeToString()} weight:{Weight}";
        }
    }
}
