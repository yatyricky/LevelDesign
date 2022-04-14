using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LevelDesigner
{
    public class Graph
    {
        public List<Vertex> Vertices = new List<Vertex>();
        public List<Edge> Edges = new List<Edge>();

        /// <summary>
        /// A--B
        /// A->B
        /// C>>B
        /// D*>A
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Graph Parse(string source)
        {
            var verts = new HashSet<string>();
            var g = new Graph();
            var i = 0;
            var buffer = new StringBuilder();
            var s = 0; // 0:expect id 1:expect edge
            EdgeType? edge = null;
            string prev = null;

            while (i < source.Length + 1)
            {
                var c = i < source.Length ? source[i] : '\0';
                var isWhiteSpace = c == ' ' || c == '\n' || c == '\r' || c == '\t';
                var isEdge = c == '-' || c == '>' || c == '*';
                var isEof = c == '\0';
                switch (s)
                {
                    case 0:
                        if (isWhiteSpace || isEdge || isEof)
                        {
                            if (buffer.Length > 0)
                            {
                                var id = buffer.ToString();
                                buffer.Clear();
                                if (isEdge)
                                {
                                    buffer.Append(c);
                                }

                                if (!verts.Contains(id))
                                {
                                    verts.Add(id);
                                    g.AddVertex(id);
                                }

                                if (edge == null)
                                {
                                    prev = g.GetVertex(id).Name;
                                    s = 1;
                                }
                                else
                                {
                                    g.AddEdge(prev, id, edge.Value);
                                    prev = null;
                                    edge = null;
                                }
                            }
                        }
                        else
                        {
                            buffer.Append(c);
                        }

                        break;
                    case 1:
                        if (isEdge)
                        {
                            buffer.Append(c);
                        }
                        else
                        {
                            var id = buffer.ToString();
                            buffer.Clear();
                            if (!isWhiteSpace && !isEof)
                            {
                                buffer.Append(c);
                            }

                            switch (id)
                            {
                                case "--":
                                    edge = EdgeType.Undirected;
                                    break;
                                case "->":
                                    edge = EdgeType.Directed;
                                    break;
                                case ">>":
                                    edge = EdgeType.ShortCut;
                                    break;
                                case "*>":
                                    edge = EdgeType.Mechanism;
                                    break;
                                default:
                                    edge = EdgeType.Undirected;
                                    Debug.LogError($"Unknown connector {id}");
                                    break;
                            }

                            s = 0;
                        }

                        break;
                }

                i++;
            }

            return g;
        }

        private Vertex GetVertex(string name)
        {
            return Vertices.First(node => node.Name == name);
        }

        public Vertex AddVertex(string name, float weight = 1f)
        {
            var vertex = new Vertex(name, weight);
            Vertices.Add(vertex);
            return vertex;
        }

        public void AddEdge(string from, string to, EdgeType type = EdgeType.Undirected)
        {
            var nodeFrom = GetVertex(from);
            var nodeTo = GetVertex(to);
            var edge = new Edge(nodeFrom, nodeTo, type);
            Edges.Add(edge);
        }

        public void RemoveVertex(string name)
        {
            var node = GetVertex(name);
            Vertices.Remove(node);
            var len = Edges.Count - 1;
            for (var i = len; i >= 0; i--)
            {
                var edge = Edges[i];
                if (edge.From == node || edge.To == node)
                {
                    Edges.RemoveAt(i);
                }
            }
        }

        public List<Edge> GetOutgoingEdges(string name)
        {
            return (from edge in Edges where edge.From.Name == name select edge).ToList();
        }

        public List<Edge> GetIncomingEdges(string name)
        {
            return (from edge in Edges where edge.To.Name == name select edge).ToList();
        }

        // public List<Edge> GetConnectedEdges(string name)
        // {
        //     return (from edge in Edges where edge.From.Name == name || edge.To.Name == name select edge).ToList();
        // }

        public int IndexOfVertex(string name)
        {
            for (var i = 0; i < Vertices.Count; i++)
            {
                if (Vertices[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool IsConnected
        {
            get
            {
                var visited = new bool[Vertices.Count];

                for (var i = 0; i < Vertices.Count; i++)
                    visited[i] = false; // Mark all nodes as unvisited.

                var compNum = 0; // For counting connected components.
                for (var v = 0; v < Vertices.Count; v++)
                {
                    // If v is not yet visited, it’s the start of a newly
                    // discovered connected component containing v.
                    if (visited[v])
                        continue;

                    // Process the component that contains v.
                    compNum++;
                    var q = new Queue<int>(); // For implementing a breadth-first traversal.
                    q.Enqueue(v); // Start the traversal from vertex v.
                    visited[v] = true;
                    while (q.Any())
                    {
                        var w = q.Dequeue(); // w is a node in this component.

                        foreach (var edge in GetOutgoingEdges(Vertices[w].Name))
                        {
                            var k = IndexOfVertex(edge.To.Name);
                            if (visited[k])
                                continue;

                            // We’ve found another node in this component.
                            visited[k] = true;
                            q.Enqueue(k);
                        }

                        foreach (var edge in GetIncomingEdges(Vertices[w].Name))
                        {
                            var k = IndexOfVertex(edge.From.Name);
                            if (visited[k])
                                continue;

                            // We’ve found another node in this component.
                            visited[k] = true;
                            q.Enqueue(k);
                        }
                    }

                    // cout << endl << endl;
                }

                return compNum == 1;
            }
        }

        public int Connectivity
        {
            get
            {
                for (var n = 1; n < Vertices.Count; n++)
                {
                    var combs = Algorithm.GetCombinations(Vertices, n);
                    foreach (var comb in combs)
                    {
                        var g = Clone();
                        foreach (var vertex in comb)
                        {
                            g.RemoveVertex(vertex.Name);
                        }

                        if (!g.IsConnected)
                        {
                            return n;
                        }
                    }
                }

                return Vertices.Count;
            }
        }

        public float CalculateNthOrderCheeger(int n)
        {
            var combs = Algorithm.GetCombinations(Vertices, n);
            var count = 0;
            foreach (var comb in combs)
            {
                var g = Clone();
                foreach (var vertex in comb)
                {
                    g.RemoveVertex(vertex.Name);
                }

                if (!g.IsConnected)
                {
                    count++;
                }
            }

            return 1f - (float) count / combs.Count;
        }

        public float CalculateNthOrderStabilityFactor(int n)
        {
            var top = 0f;
            var bottom = 0f;
            for (var i = 1; i <= n; i++)
            {
                var inv = 1f / Algorithm.Factorial(i);
                top += inv * CalculateNthOrderCheeger(i);
                bottom += inv;
            }

            return top / bottom;
        }

        public override string ToString()
        {
            // var sb = new StringBuilder();
            // sb.Append("V:{");
            // foreach (var node in Vertices)
            // {
            //     sb.Append(node);
            //     sb.Append(' ');
            // }
            //
            // sb.Append("}\n");
            // sb.Append("E:{");
            // foreach (var edge in Edges)
            // {
            //     sb.Append(edge);
            //     sb.Append(' ');
            // }
            //
            // sb.Append("}");
            // return sb.ToString();
            return string.Join("\n", from edge in Edges select edge.ToString());
        }

        public Graph Clone()
        {
            var g = new Graph();
            g.Vertices.AddRange(Vertices);
            g.Edges.AddRange(Edges);
            return g;
        }
    }
}
