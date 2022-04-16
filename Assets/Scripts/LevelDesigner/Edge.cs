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

        /// <summary>
        /// For drawing
        /// </summary>
        public float Angle;

        /// <summary>
        /// For drawing
        /// </summary>
        public float Strength;

        public string Name
        {
            get
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
                        return $"{From.Name}--{To.Name}";
                }
            }
        }

        public string NodesName => $"{From.Name}-{To.Name}";

        public Edge(Vertex from, Vertex to, EdgeType type)
        {
            From = from;
            To = to;
            Type = type;
        }

        public override string ToString()
        {
            return $"{Name} #angle:{Angle:0.00} strength:{Strength:0.00}";
        }
    }
}
