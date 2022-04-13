using System;

namespace LevelDesigner
{
    public enum EdgeType
    {
        Undirected,
        Directed,
        ShortCut,
        Mechanism,
    }

    public class Edge
    {
        public Vertex From;
        public Vertex To;
        public EdgeType Type;

        public Edge(Vertex from, Vertex to, EdgeType type)
        {
            From = from;
            To = to;
            Type = type;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case EdgeType.Undirected:
                    return $"{From.Name}--{To.Name}";
                case EdgeType.Directed:
                    return $"{From.Name}->{To.Name}";
                case EdgeType.ShortCut:
                    return $"{From.Name}>>{To.Name}";
                case EdgeType.Mechanism:
                    return $"{From.Name}*>{To.Name}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
