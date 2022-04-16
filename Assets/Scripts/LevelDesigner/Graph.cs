using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LevelDesigner
{
    public class Graph
    {
        private static readonly Regex EdgeDefine = new Regex(@"(?<from>\w+)[ \t]*(?<conn>[->\*]{2})[ \t]*(?<to>\w+)[ \t]*(#[ \t]*((angle[ \t]*:[ \t]*(?<angle>[\d-\.]+))|[ \t]|(strength[ \t]*:[ \t]*(?<strength>[\d-\.]+)))*)?", RegexOptions.Compiled);
        private static readonly Regex VertexDefine = new Regex(@"#[ \t]*(?<name>\w+)[ \t]+((pos[ \t]*:[ \t]*\([ \t]*(?<posX>[\d\.-]+)[ \t]*,[ \t]*(?<posY>[\d\.-]+)[ \t]*\))|[ \t]|(type[ \t]*:[ \t]*(?<type>\w+)))*", RegexOptions.Compiled);

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
            var g = new Graph();

            foreach (var rawLine in source.Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                var e = EdgeDefine.Match(line);
                if (e.Success)
                {
                    var from = e.Groups["from"].Value;
                    var conn = e.Groups["conn"].Value;
                    var to = e.Groups["to"].Value;
                    g.FindOrAddVertex(from);
                    g.FindOrAddVertex(to);
                    EdgeType edgeType;
                    switch (conn)
                    {
                        case "--":
                            edgeType = EdgeType.Undirected;
                            break;
                        case "->":
                            edgeType = EdgeType.Directed;
                            break;
                        case ">>":
                            edgeType = EdgeType.ShortCut;
                            break;
                        case "*>":
                            edgeType = EdgeType.Mechanism;
                            break;
                        default:
                            edgeType = EdgeType.Undirected;
                            Debug.LogError($"Unknown connector {conn}");
                            break;
                    }

                    var edge = g.AddEdge(from, to, edgeType);

                    var angle = e.Groups["angle"].Value;
                    if (!string.IsNullOrEmpty(angle))
                    {
                        edge.Angle = float.Parse(angle);
                    }

                    var strength = e.Groups["strength"].Value;
                    if (!string.IsNullOrEmpty(strength))
                    {
                        edge.Strength = float.Parse(strength);
                    }

                    continue;
                }

                var v = VertexDefine.Match(line);
                if (v.Success)
                {
                    var name = v.Groups["name"].Value;
                    var vertex = g.FindOrAddVertex(name);
                    var posX = v.Groups["posX"].Value;
                    var posY = v.Groups["posY"].Value;

                    if (!string.IsNullOrEmpty(posX))
                    {
                        vertex.Position = new Vector2(float.Parse(posX), float.Parse(posY));
                    }

                    var vType = v.Groups["type"].Value;
                    if (!string.IsNullOrEmpty(vType))
                    {
                        switch (vType)
                        {
                            case "start":
                                vertex.Type = NodeType.Start;
                                break;
                            case "boss":
                                vertex.Type = NodeType.Boss;
                                break;
                            case "save":
                                vertex.Type = NodeType.Save;
                                break;
                            case "poi":
                                vertex.Type = NodeType.Normal;
                                break;
                            default:
                                Debug.LogError($"Unknown Vertex Type @ line {line}. Allowed: start|boss|save|poi");
                                vertex.Type = NodeType.Normal;
                                break;
                        }
                    }
                    else
                    {
                        vertex.Type = NodeType.Normal;
                    }

                    continue;
                }

                Debug.LogError($"Syntax error @ line {line}");
            }

            return g;
        }

        private Vertex FindVertex(string name)
        {
            return Vertices.FirstOrDefault(node => node.Name == name);
        }

        public Vertex AddVertex(string name, float weight = 1f)
        {
            var vertex = new Vertex(name, weight);
            Vertices.Add(vertex);
            return vertex;
        }

        private Vertex FindOrAddVertex(string name)
        {
            return FindVertex(name) ?? AddVertex(name);
        }

        public Edge AddEdge(string from, string to, EdgeType type = EdgeType.Undirected)
        {
            var nodeFrom = FindVertex(from);
            var nodeTo = FindVertex(to);
            var edge = new Edge(nodeFrom, nodeTo, type);
            Edges.Add(edge);
            return edge;
        }

        public void RemoveVertex(string name)
        {
            var node = FindVertex(name);
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
                    var combs = MathUtils.GetCombinations(Vertices, n);
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
            var combs = MathUtils.GetCombinations(Vertices, n);
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
                var inv = 1f / MathUtils.Factorial(i);
                top += inv * CalculateNthOrderCheeger(i);
                bottom += inv;
            }

            return top / bottom;
        }

        public override string ToString()
        {
            return $"{string.Join("\n", from edge in Edges select edge.ToString())}\n\n{string.Join("\n", from vertex in Vertices select vertex.ToString())}";
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
