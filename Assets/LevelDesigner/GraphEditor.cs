﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public TextAsset source;

        public GameObject nodePrefab;
        public GameObject edgePrefab;
        public NodePos[] nodePos;

        [Button]
        public void LoadFromSource()
        {
            if (source == null)
            {
                return;
            }

            var g = Graph.Parse(source.text);
            ResetWithGraph(g);
        }

        [Button]
        public void ExportSource()
        {
            var graph = ToVirtualGraph();
            var fp = $"Assets/{name} {DateTime.Now:yyyy-MM-dd-HH-mm-ss}.graph.txt";
            File.WriteAllText(fp, graph.ToString());
            AssetDatabase.Refresh();
        }

        private NodePos FindNodePos(string nodeName)
        {
            if (nodePos == null)
                nodePos = new NodePos[0];

            return nodePos.FirstOrDefault(po => po.name == nodeName);
        }

        private void Update()
        {
            foreach (Transform node in nodes)
            {
                var pos = node.position;
                var po = FindNodePos(node.name);
                if (po == null)
                {
                    Array.Resize(ref nodePos, nodePos.Length + 1);
                    nodePos[nodePos.Length - 1] = new NodePos()
                    {
                        name = node.name,
                        position = pos
                    };
                }
                else
                {
                    po.position = pos;
                }
            }
        }

        [Button]
        public NodeEditor AddNode()
        {
            var go = Instantiate(nodePrefab, nodes, true);
            go.name = $"Node{nodes.childCount}";
            return go.GetComponent<NodeEditor>();
        }

        [Button]
        public EdgeEditor AddEdge()
        {
            var go = Instantiate(edgePrefab, edges, true);
            go.name = $"Edge{edges.childCount}";
            return go.GetComponent<EdgeEditor>();
        }

        private Graph ToVirtualGraph()
        {
            var graph = new Graph();
            foreach (Transform nodeTrs in nodes)
            {
                var node = nodeTrs.GetComponent<NodeEditor>();
                graph.AddVertex(node.name, node.weight);
            }

            foreach (Transform edgeTrs in edges)
            {
                var edge = edgeTrs.GetComponent<EdgeEditor>();
                graph.AddEdge(edge.from.name, edge.to.name, edge.type);
            }

            return graph;
        }

        [Button]
        public void CalculateThirdOrderStabilityFactor()
        {
            var graph = ToVirtualGraph();
            Debug.Log(graph.CalculateNthOrderStabilityFactor(3));
        }

        [Button]
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
                dict.Add(vertex.Name, node);
                var po = FindNodePos(vertex.Name);
                if (po != null)
                {
                    node.transform.position = po.position;
                }
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
