using System;
// using System.Collections;
using UnityEngine;

namespace NefAndFriends.LevelDesigner
{
    [Serializable]
    public class Edge
    {
        [SerializeReference]
        public Vertex from;

        [SerializeReference]
        public Vertex to;

        // private IEnumerable SelectVertex()
        // {
        //     var list = new ValueDropdownList<Vertex>();
        //     foreach (var entry in _graph.vertices)
        //     {
        //         list.Add(entry.Name, entry);
        //     }
        //
        //     return list;
        // }

        [SerializeField]
        private EdgeType type;

        public EdgeType Type
        {
            get => type;
            set
            {
                type = value;
                _graph.Dirty = true;
            }
        }

        [NonSerialized]
        private Graph _graph;

        public static string GetNameID(string from, string to, EdgeType type)
        {
            return type switch
            {
                EdgeType.Undirected => $"{from}--{to}",
                EdgeType.Directed => $"{from}->{to}",
                EdgeType.ShortCut => $"{from}>>{to}",
                EdgeType.Mechanism => $"{from}*>{to}",
                _ => $"{from}--{to}"
            };
        }

        public string Name => GetNameID(from.Name, to.Name, Type);

        public string NodesName => $"{from.Name}-{to.Name}";

        public string OrderedNodesName
        {
            get
            {
                var fromName = from.Name;
                var toName = to.Name;
                return string.CompareOrdinal(fromName, toName) <= 0 ? $"{fromName}-{toName}" : $"{toName}-{fromName}";
            }
        }

        internal int Index;

        public Edge(Vertex from, Vertex to, EdgeType type, Graph graph)
        {
            _graph = graph;
            this.from = from;
            this.to = to;
            Type = type;
        }

        public override string ToString()
        {
            return Name;
        }

        public Edge Clone()
        {
            var v = new Edge(from, to, Type, _graph);
            return v;
        }

        public void SetParent(Graph graph)
        {
            _graph = graph;
        }
    }
}
