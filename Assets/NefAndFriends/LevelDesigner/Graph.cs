using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;

namespace NefAndFriends.LevelDesigner
{
    [Serializable]
    public class Graph : ISerializationCallbackReceiver
    {
        private static readonly Regex EdgeDefine = new(@"(?<from>\w+)[ \t]*(?<conn>[->\*]{2})[ \t]*(?<to>\w+)[ \t]*(#[ \t]*((angle[ \t]*:[ \t]*(?<angle>[\d-\.]+))|[ \t]|(strength[ \t]*:[ \t]*(?<strength>[\d-\.]+)))*)?", RegexOptions.Compiled);
        private static readonly Regex VertexDefine = new(@"#[ \t]*(?<name>\w+)[ \t]+((pos[ \t]*:[ \t]*\([ \t]*(?<posX>[\d\.-]+)[ \t]*,[ \t]*(?<posY>[\d\.-]+)[ \t]*\))|[ \t]|(type[ \t]*:[ \t]*(?<type>\w+)))*", RegexOptions.Compiled);

        public UnorderedList<Vertex> vertices = new();
        public UnorderedList<Edge> edges = new();

        private UnorderedList<UnorderedList<Vertex>> _vertexVertex = new();
        private UnorderedList<UnorderedList<Edge>> _vertexEdge = new();

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
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Vertices: {string.Join(",", from e in vertices select $"[{e.Index}]{e.Name}")}");
            sb.AppendLine($"Edges: {string.Join(",", from e in edges select $"[{e.Index}]{e.@from.Name}-{e.to.Name}")}");
            sb.Append("_vertexVertex:");
            for (var i = 0; i < _vertexVertex.Count; i++)
            {
                sb.Append(i < _vertexVertex.Count ? $"[{i}]{string.Join(",", from v in _vertexVertex[i] select v.Index)} " : $"[{i}]NULL ");
            }

            sb.AppendLine();
            sb.Append("_vertexEdge:");
            for (var i = 0; i < _vertexEdge.Count; i++)
            {
                sb.Append(i < _vertexEdge.Count ? $"[{i}]{string.Join(",", from v in _vertexEdge[i] select v.Index)} " : $"[{i}]NULL ");
            }

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

                    g.FindOrAddEdge(from, to, edgeType);

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
                        vertex.Position = new Vector3(float.Parse(posX), float.Parse(posY));
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
            return vertices.FirstOrDefault(vertex => vertex.Name == name);
        }

        public HashSet<string> GetVertexNames()
        {
            var set = new HashSet<string>();
            foreach (var vertex in vertices)
            {
                set.Add(vertex.Name);
            }

            return set;
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
            var vertex = new Vertex(name, weight, this)
            {
                Index = vertices.Count
            };
            vertices.Add(vertex);
            _vertexVertex.Add(new UnorderedList<Vertex>());
            _vertexEdge.Add(new UnorderedList<Edge>());

            // TakeSnapShot($"AddVertex {name}");

            Dirty = true;
            return vertex;
        }

        private Vertex FindOrAddVertex(string name)
        {
            return FindVertex(name) ?? AddVertex(name);
        }

        // public Vertex RemoveVertex(string name)
        // {
        //     return RemoveVertex(IndexOfVertex(name));
        // }

        public Vertex RemoveVertex(Vertex vertex)
        {
            return RemoveVertex(vertex.Index);
        }

        public Vertex RemoveVertex(int index)
        {
            // TakeSnapShot($"Will RemoveVertex {index}");
            var edgesToRemove = _vertexEdge[index];
            for (var i = edgesToRemove.Count - 1; i >= 0; i--)
            {
                RemoveEdge(edgesToRemove[i]);
            }

            var vertex = vertices[index];
            vertices.RemoveAt(index);
            if (index < vertices.Count)
            {
                vertices[index].Index = index;
            }

            _vertexVertex.RemoveAt(index);
            _vertexEdge.RemoveAt(index);

            // TakeSnapShot($"Did RemoveVertex {index}");

            Dirty = true;
            return vertex;
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

        // public Edge FindEdge(string idName)
        // {
        //     var i = IndexOfEdge(idName);
        //     return i == -1 ? null : Edges[i];
        // }

        public Edge AddEdge(Vertex from, Vertex to, EdgeType type = EdgeType.Undirected)
        {
            var edge = new Edge(from, to, type, this)
            {
                Index = edges.Count
            };
            edges.Add(edge);
            _vertexVertex[from.Index].Add(to);
            _vertexEdge[from.Index].Add(edge);
            if (from != to)
            {
                _vertexVertex[to.Index].Add(from);
                _vertexEdge[to.Index].Add(edge);
            }

            // TakeSnapShot($"AddEdge {from.Name} {to.Name}");

            Dirty = true;
            return edge;
        }

        public Edge FindEdge(string from, string to)
        {
            return edges.FirstOrDefault(e => e.from.Name == from && e.to.Name == to);
        }

        public Edge FindOrAddEdge(string from, string to, EdgeType type = EdgeType.Undirected)
        {
            return FindEdge(from, to) ?? AddEdge(FindVertex(from), FindVertex(to), type);
        }

        public Edge RemoveEdge(int index)
        {
            var edge = edges[index];
            edges.RemoveAt(index);
            if (index < edges.Count)
            {
                edges[index].Index = index;
            }

            _vertexVertex[edge.from.Index].Remove(edge.to);
            _vertexVertex[edge.to.Index].Remove(edge.from);

            _vertexEdge[edge.from.Index].Remove(edge);
            _vertexEdge[edge.to.Index].Remove(edge);
            // TakeSnapShot($"RemoveEdge {index}");

            Dirty = true;
            return edge;
        }

        public Edge RemoveEdge(Edge edge)
        {
            return RemoveEdge(edge.Index);
        }

        #endregion

        public bool IsConnected
        {
            get
            {
                var visited = new bool[vertices.Count];

                // Mark all nodes as unvisited.
                for (var i = 0; i < vertices.Count; i++)
                {
                    visited[i] = false;
                }

                var compNum = 0; // For counting connected components.
                for (var v = 0; v < vertices.Count; v++)
                {
                    // If v is not yet visited, it’s the start of a newly
                    // discovered connected component containing v.
                    if (visited[v])
                    {
                        continue;
                    }

                    // Process the component that contains v.
                    compNum++;
                    var q = new Queue<int>(); // For implementing a breadth-first traversal.
                    q.Enqueue(v); // Start the traversal from vertex v.
                    visited[v] = true;
                    while (q.Count > 0)
                    {
                        var w = q.Dequeue(); // w is a node in this component.

                        for (var i = 0; i < _vertexVertex[w].Count; i++)
                        {
                            var k = _vertexVertex[w][i];
                            if (visited[k.Index])
                            {
                                continue;
                            }

                            // We’ve found another node in this component.
                            visited[k.Index] = true;
                            q.Enqueue(k.Index);
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
                for (var n = 1; n < vertices.Count; n++)
                {
                    var combs = MathUtils.GetCombinations(vertices, n);
                    for (var i = 0; i < combs.Count; i++)
                    {
                        var comb = combs[i];
                        var g = ShallowClone();
                        for (var j = 0; j < comb.Count; j++)
                        {
                            g.RemoveVertex(comb[j].Index);
                        }

                        if (!g.IsConnected)
                        {
                            return n;
                        }
                    }
                }

                return vertices.Count;
            }
        }

        public float CalculateNthOrderCheeger(int n)
        {
            var combs = MathUtils.GetCombinations(vertices, n);
            var count = 0;
            for (var i = 0; i < combs.Count; i++)
            {
                var comb = combs[i];
                var g = ShallowClone();
                for (var j = 0; j < comb.Count; j++)
                {
                    g.RemoveVertex(comb[j]);
                }

                if (!g.IsConnected)
                {
                    count++;
                }
            }

            return 1f - (float)count / combs.Count;
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

            for (var i = 0; i < vertices.Count; i++)
            {
                vertices[i].Index = i;
            }

            for (var i = 0; i < edges.Count; i++)
            {
                edges[i].Index = i;
            }

            Debug.Log($"==== PERF {DateTimeOffset.Now.ToUnixTimeMilliseconds() - dt} ms ====");
            return top / bottom;
        }

        public override string ToString()
        {
            return $"{string.Join("\n", from edge in edges select edge.ToString())}\n\n{string.Join("\n", from vertex in vertices select vertex.ToString())}";
        }

        private Graph ShallowClone()
        {
            var g = new Graph
            {
                vertices = vertices.Clone()
            };
            for (var i = 0; i < vertices.Count; i++)
            {
                g.vertices[i].Index = i;
            }

            g.edges = edges.Clone();
            for (var i = 0; i < edges.Count; i++)
            {
                g.edges[i].Index = i;
            }

            g._vertexVertex = new UnorderedList<UnorderedList<Vertex>>(_vertexVertex.Count);
            for (int i = 0, n = _vertexVertex.Count; i < n; i++)
            {
                g._vertexVertex.Add(_vertexVertex[i].Clone());
            }

            g._vertexEdge = new UnorderedList<UnorderedList<Edge>>(_vertexEdge.Count);
            for (int i = 0, n = _vertexEdge.Count; i < n; i++)
            {
                g._vertexEdge.Add(_vertexEdge[i].Clone());
            }

            return g;
        }

        private void SetChildRefs()
        {
            if (vertices != null)
            {
                foreach (var vertex in vertices)
                {
                    vertex.SetParent(this);
                }
            }

            if (edges != null)
            {
                foreach (var edge in edges)
                {
                    edge.SetParent(this);
                }
            }
        }

        [UsedImplicitly]
        private Vertex ListAddNewVertex()
        {
            return AddVertex();
        }

        [UsedImplicitly]
        private Edge ListAddNewEdge()
        {
            return AddEdge(null, null);
        }

        private void InitNonSerializedData()
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                vertices[i].Index = i;
            }

            for (var i = 0; i < edges.Count; i++)
            {
                edges[i].Index = i;
            }

            _vertexVertex = new UnorderedList<UnorderedList<Vertex>>();
            for (var i = 0; i < vertices.Count; i++)
            {
                _vertexVertex.Add(new UnorderedList<Vertex>());
            }

            _vertexEdge = new UnorderedList<UnorderedList<Edge>>();
            for (var i = 0; i < vertices.Count; i++)
            {
                _vertexEdge.Add(new UnorderedList<Edge>());
            }

            foreach (var edge in edges)
            {
                var from = edge.from;
                var to = edge.to;
                _vertexVertex[from.Index].Add(to);
                _vertexEdge[from.Index].Add(edge);
                if (from != to)
                {
                    _vertexVertex[to.Index].Add(from);
                    _vertexEdge[to.Index].Add(edge);
                }
            }
        }

        public void OnBeforeSerialize()
        {
            InitNonSerializedData();
        }

        public void OnAfterDeserialize()
        {
            InitNonSerializedData();
        }
    }
}
