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

        private EdgeType _type;

        public EdgeType Type
        {
            get => _type;
            set
            {
                _type = value;
                _graph.Dirty = true;
            }
        }

        private readonly Graph _graph;

        private string Name
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

        public string OrderedNodesName
        {
            get
            {
                var fromName = From.Name;
                var toName = To.Name;
                string sameKey;
                if (string.CompareOrdinal(fromName, toName) <= 0)
                {
                    sameKey = $"{fromName}-{toName}";
                }
                else
                {
                    sameKey = $"{toName}-{fromName}";
                }

                return sameKey;
            }
        }

        public Edge(Vertex from, Vertex to, EdgeType type, Graph graph)
        {
            _graph = graph;
            From = from;
            To = to;
            Type = type;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
