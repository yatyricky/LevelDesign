using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace LevelDesigner
{
    public class Graph
    {
        private static readonly Regex EdgeDefine = new Regex(@"(?<from>\w+)[ \t]*(?<conn>[->\*]{2})[ \t]*(?<to>\w+)[ \t]*(#[ \t]*((angle[ \t]*:[ \t]*(?<angle>[\d-\.]+))|[ \t]|(strength[ \t]*:[ \t]*(?<strength>[\d-\.]+)))*)?", RegexOptions.Compiled);
        private static readonly Regex VertexDefine = new Regex(@"#[ \t]*(?<name>\w+)[ \t]+((pos[ \t]*:[ \t]*\([ \t]*(?<posX>[\d\.-]+)[ \t]*,[ \t]*(?<posY>[\d\.-]+)[ \t]*\))|[ \t]|(type[ \t]*:[ \t]*(?<type>\w+)))*", RegexOptions.Compiled);

        public Vertex[] Vertices = new Vertex[0];

        public int VerticesLength { get; private set; }

        private Dictionary<string, int> _vertexIndexes = new();

        public Edge[] Edges = new Edge[0];
        private int _edgesLength;
        private Dictionary<string, int> _edgeIndexes = new();

        private HashSet<int>[] _vertexDegrees = new HashSet<int>[0];

        private bool _dirty;

        public bool Dirty
        {
            get => _dirty;
            set
            {
                if (value != _dirty)
                {
                }

                _dirty = value;
            }
        }

        public string TakeSnapShot()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Vertices: {string.Join(", ", from e in Vertices select e.Name)}");
            sb.AppendLine($"VerticesLength: {VerticesLength}");
            sb.AppendLine($"_vertexIndexes: {string.Join(", ", from kv in _vertexIndexes select $"[{kv.Key}]={kv.Value}")}");
            sb.AppendLine($"Edges: {string.Join(", ", from e in Edges select e.Name)}");
            sb.AppendLine($"_edgesLength: {_edgesLength}");
            sb.AppendLine($"_edgeIndexes: {string.Join(", ", from kv in _edgeIndexes select $"[{kv.Key}]={kv.Value}")}");
            sb.AppendLine($"_vertexDegrees: {string.Join("; ", from e in _vertexDegrees select string.Join(",", e))}");
            return sb.ToString();
        }

        public string Name { get; set; }

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

                    g.AddEdge(@from, to, edgeType);

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
                                vertex.Type = VertexType.Start;
                                break;
                            case "boss":
                                vertex.Type = VertexType.Boss;
                                break;
                            case "save":
                                vertex.Type = VertexType.Save;
                                break;
                            case "poi":
                                vertex.Type = VertexType.Normal;
                                break;
                            default:
                                Debug.LogError($"Unknown Vertex Type @ line {line}. Allowed: start|boss|save|poi");
                                vertex.Type = VertexType.Normal;
                                break;
                        }
                    }
                    else
                    {
                        vertex.Type = VertexType.Normal;
                    }

                    continue;
                }

                Debug.LogError($"Syntax error @ line {line}");
            }

            g.Dirty = false;
            return g;
        }

        #region vertex

        private Vertex FindVertex(string name)
        {
            var i = IndexOfVertex(name);
            return i == -1 ? null : Vertices[i];
        }

        public HashSet<string> GetVertexNames()
        {
            return new(_vertexIndexes.Keys);
        }

        public Vertex AddVertex()
        {
            var currentNames = GetVertexNames();

            var i = 0;
            string newName;
            do
            {
                newName = $"V{++i}";
            } while (currentNames.Contains(newName));

            return AddVertex(newName);
        }

        public Vertex AddVertex(string name, float weight = 1f)
        {
            var curr = FindVertex(name);
            if (curr != null)
            {
                return curr;
            }

            var vertex = new Vertex(name, weight, this);
            var i = VerticesLength++;
            _vertexIndexes.Add(name, i);
            if (Vertices.Length < VerticesLength)
            {
                Array.Resize(ref Vertices, VerticesLength);
            }

            Vertices[i] = vertex;

            if (_vertexDegrees.Length < VerticesLength)
            {
                Array.Resize(ref _vertexDegrees, VerticesLength);
            }

            _vertexDegrees[i] ??= new HashSet<int>();

            Dirty = true;
            return vertex;
        }

        private Vertex FindOrAddVertex(string name)
        {
            return FindVertex(name) ?? AddVertex(name);
        }

        public Vertex RemoveVertex(string name)
        {
            return RemoveVertex(IndexOfVertex(name));
        }

        public Vertex RemoveVertex(Vertex vertex)
        {
            if (vertex == null)
            {
                return null;
            }

            return RemoveVertex(IndexOfVertex(vertex.Name));
        }

        public Vertex RemoveVertex(int index)
        {
            var vertex = Vertices[index];
            var len = _edgesLength - 1;
            for (var i = len; i >= 0; i--)
            {
                var edge = Edges[i];
                if (edge.From == vertex || edge.To == vertex)
                {
                    RemoveEdge(i);
                }
            }

            var lastI = --VerticesLength;
            var last = Vertices[lastI];
            var lastDegrees = _vertexDegrees[lastI];
            Vertices[index] = last;
            _vertexDegrees[index] = lastDegrees;
            _vertexIndexes[last.Name] = index;
            for (int i = 0; i < VerticesLength; i++)
            {
                var set = _vertexDegrees[i];
                if (set.Contains(lastI))
                {
                    set.Remove(lastI);
                    var res = set.Add(index);
                    if (!res)
                    {
                        throw new Exception("??");
                    }
                }
            }

            Dirty = true;
            return vertex;
        }

        public int IndexOfVertex(string name)
        {
            return _vertexIndexes.TryGetValue(name, out var i) ? i : -1;
        }

        // public List<Edge> GetOutgoingEdges(string name)
        // {
        //     return (from edge in Edges where edge.From.Name == name select edge).ToList();
        // }
        //
        // public List<Edge> GetIncomingEdges(string name)
        // {
        //     return (from edge in Edges where edge.To.Name == name select edge).ToList();
        // }

        #endregion

        #region edge

        public int IndexOfEdge(string idName)
        {
            return _edgeIndexes.TryGetValue(idName, out var i) ? i : -1;
        }

        public Edge FindEdge(string idName)
        {
            var i = IndexOfEdge(idName);
            return i == -1 ? null : Edges[i];
        }

        public Edge AddEdge(string from, string to, EdgeType type = EdgeType.Undirected)
        {
            var edgeName = Edge.GetNameID(from, to, type);
            var find = FindEdge(edgeName);
            if (find != null)
            {
                return find;
            }

            var fromVertI = IndexOfVertex(from);
            var toVertI = IndexOfVertex(to);
            var nodeFrom = Vertices[fromVertI];
            var nodeTo = Vertices[toVertI];
            var edge = new Edge(nodeFrom, nodeTo, type, this);
            var i = _edgesLength++;
            _edgeIndexes[edgeName] = i;
            if (_edgesLength > Edges.Length)
            {
                Array.Resize(ref Edges, _edgesLength);
            }

            Edges[i] = edge;

            var fromI = IndexOfVertex(from);
            _vertexDegrees[fromI].Add(toVertI);

            var toI = IndexOfVertex(to);
            _vertexDegrees[toI].Add(fromVertI);

            Dirty = true;
            return edge;
        }

        public Edge RemoveEdge(int index)
        {
            var last = Edges[--_edgesLength];
            var edge = Edges[index];
            Edges[index] = last;
            _edgeIndexes[last.Name] = index;

            var from = edge.From;
            var to = edge.To;
            var fromI = IndexOfVertex(from.Name);
            var toI = IndexOfVertex(to.Name);
            _vertexDegrees[fromI].Remove(toI);
            _vertexDegrees[toI].Remove(fromI);

            Dirty = true;
            return edge;
        }

        public Edge RemoveEdge(Edge edge)
        {
            if (edge == null)
            {
                return null;
            }

            return RemoveEdge(IndexOfEdge(edge.Name));
        }

        #endregion

        public bool IsConnected
        {
            get
            {
                var visited = new bool[VerticesLength];

                for (var i = 0; i < VerticesLength; i++)
                    visited[i] = false; // Mark all nodes as unvisited.

                var compNum = 0; // For counting connected components.
                for (var v = 0; v < VerticesLength; v++)
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

                        foreach (var k in _vertexDegrees[w])
                        {
                            if (visited[k])
                                continue;

                            // We’ve found another node in this component.
                            visited[k] = true;
                            q.Enqueue(k);
                        }
                    }
                }

                return compNum == 1;
            }
        }

        public int Connectivity
        {
            get
            {
                for (var n = 1; n < VerticesLength; n++)
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

                return VerticesLength;
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
                    g.RemoveVertex(vertex);
                }

                if (!g.IsConnected)
                {
                    count++;
                }
            }
            // Parallel.ForEach(combs, comb =>
            // {
            // });

            return 1f - (float) count / combs.Count;
        }

        public float CalculateNthOrderStabilityFactor(int n)
        {
            var dt = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var top = 0f;
            var bottom = 0f;
            for (var i = 1; i <= n; i++)
            {
                var inv = 1f / MathUtils.Factorial(i);
                top += inv * CalculateNthOrderCheeger(i);
                bottom += inv;
            }

            Debug.Log($"==== PERF {DateTimeOffset.Now.ToUnixTimeMilliseconds() - dt} ms ====");
            return top / bottom;
        }

        public override string ToString()
        {
            return $"{string.Join("\n", from edge in Edges select edge.ToString())}\n\n{string.Join("\n", from vertex in Vertices select vertex.ToString())}";
        }

        public Graph Clone()
        {
            var g = new Graph();
            Array.Resize(ref g.Vertices, VerticesLength);
            Array.Copy(Vertices, g.Vertices, VerticesLength);
            g.VerticesLength = VerticesLength;
            g._vertexIndexes = new Dictionary<string, int>(_vertexIndexes);

            Array.Resize(ref g.Edges, _edgesLength);
            Array.Copy(Edges, g.Edges, _edgesLength);
            g._edgesLength = _edgesLength;
            g._edgeIndexes = new Dictionary<string, int>(_edgeIndexes);

            Array.Resize(ref g._vertexDegrees, VerticesLength);
            for (var i = 0; i < VerticesLength; i++)
            {
                g._vertexDegrees[i] = new HashSet<int>(_vertexDegrees[i]);
            }

            return g;
        }
    }
}
