using System;
using UnityEngine;

namespace LevelDesigner
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
        public string Name;
        public float Weight;
        public VertexType Type;

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
    }
}
