using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace LevelDesigner
{
    [ExecuteAlways]
    public class GraphEditor : MonoBehaviour
    {
        [Serializable]
        public class NodePos
        {
            public string name;
            public Vector3 position;
        }

        public Transform nodes;
        public Transform edges;
        public GameObject nodePrefab;
        public GameObject edgePrefab;

        [BoxGroup("主要功能操作区域")]
        [LabelText("配置")]
        public TextAsset source;

        [BoxGroup("主要功能操作区域")]
        [Button("加载配置")]
        public void LoadFromSource()
        {
            if (source == null)
            {
                return;
            }

            var g = Graph.Parse(source.text);
            ResetWithGraph(g);
        }

        [BoxGroup("主要功能操作区域")]
        [Button("导出配置(会生成新文件)")]
        public void ExportSource()
        {
            var graph = ToVirtualGraph();
            var fp = $"Assets/{name} {DateTime.Now:yyyy-MM-dd-HH-mm-ss}.graph.txt";
            File.WriteAllText(fp, graph.ToString());
            AssetDatabase.Refresh();
        }

        private Graph ToVirtualGraph()
        {
            var graph = new Graph();
            foreach (Transform nodeTrs in nodes)
            {
                var node = nodeTrs.GetComponent<NodeEditor>();
                var v = graph.AddVertex(node.name, node.weight);
                v.Type = node.nodeType;
                var pos = nodeTrs.position;
                v.Position = new Vector2(pos.x, pos.z);
            }

            foreach (Transform edgeTrs in edges)
            {
                var edge = edgeTrs.GetComponent<EdgeEditor>();
                var e = graph.AddEdge(edge.from.name, edge.to.name, edge.type);
            }

            return graph;
        }

        [BoxGroup("主要功能操作区域")]
        [Button("计算γ3")]
        public void CalculateThirdOrderStabilityFactor()
        {
            var graph = ToVirtualGraph();
            Debug.Log(graph.CalculateNthOrderStabilityFactor(3));
        }

        [BoxGroup("主要功能操作区域")]
        [Button("新增点")]
        public NodeEditor AddNode()
        {
            var go = Instantiate(nodePrefab, nodes, true);
            go.name = $"Node{nodes.childCount}";
            return go.GetComponent<NodeEditor>();
        }

        [BoxGroup("主要功能操作区域")]
        [Button("新增边")]
        public EdgeEditor AddEdge()
        {
            var go = Instantiate(edgePrefab, edges, true);
            go.name = $"Edge{edges.childCount}";
            return go.GetComponent<EdgeEditor>();
        }

        [BoxGroup("主要功能操作区域")]
        [Button("所有点的y轴归零")]
        public void FlattenNodes()
        {
            foreach (Transform node in nodes)
            {
                var pos = node.position;
                node.position = new Vector3(pos.x, 0f, pos.z);
            }
        }

        private void ResetWithGraph(Graph graph)
        {
            for (var i = edges.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(edges.GetChild(i).gameObject);
            }

            for (var i = nodes.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(nodes.GetChild(i).gameObject);
            }

            var dict = new Dictionary<string, NodeEditor>();
            foreach (var vertex in graph.Vertices)
            {
                var node = AddNode();
                node.name = vertex.Name;
                node.nodeType = vertex.Type;
                dict.Add(vertex.Name, node);
                node.transform.position = new Vector3(vertex.Position.x, 0f, vertex.Position.y);
            }

            foreach (var edge in graph.Edges)
            {
                var line = AddEdge();
                line.from = dict[edge.From.Name];
                line.to = dict[edge.To.Name];
                line.type = edge.Type;
            }
        }

        [MenuItem("Calculate/Parser")]
        public static void Parser()
        {
            var g = Graph.Parse(File.ReadAllText("Assets/graph.txt"));
            Debug.Log(g);
        }
    }
}
