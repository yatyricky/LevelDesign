using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace LevelDesigner
{
    [Serializable]
    public class GraphEditor : MonoBehaviour
    {
        public Transform nodes;
        public Transform edges;
        public GameObject nodePrefab;
        public GameObject edgePrefab;

        [Button]
        public void AddNode()
        {
            var go = Instantiate(nodePrefab, nodes, true);
            go.name = $"Node{nodes.childCount}";
        }

        [Button]
        public void AddEdge()
        {
            var go = Instantiate(edgePrefab, edges, true);
            go.name = $"Edge{edges.childCount}";
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

        [MenuItem("Calculate/Zulaman")]
        public static void ZulAman()
        {
            var g = new Graph();
            g.AddVertex("Wreck");
            g.AddVertex("Gate");
            g.AddVertex("Ritual");
            g.AddVertex("Tinker");
            g.AddVertex("Library");
            g.AddVertex("GateWallW");
            g.AddVertex("GateWallE");
            g.AddVertex("Stream");
            g.AddVertex("LShrineW");
            g.AddVertex("Witch");
            g.AddVertex("Swamp");
            g.AddVertex("LShrineE");
            g.AddVertex("HShrine");
            g.AddVertex("Altar2");
            g.AddVertex("WaterEle");
            g.AddVertex("Sentinel");
            g.AddVertex("FoundryW");
            g.AddVertex("FoundryE");
            g.AddVertex("Warlock");
            g.AddVertex("Misty");
            g.AddVertex("Dracolich");
            g.AddVertex("CatacombE");
            g.AddVertex("CatacombW");
            g.AddVertex("Beetle");
            g.AddVertex("UTunnel");
            g.AddVertex("WallN");
            g.AddVertex("Hydra");
            g.AddVertex("Catapult");
            g.AddVertex("Guard");
            g.AddVertex("Altar4");
            g.AddVertex("LShrineN");
            g.AddVertex("Ghost");
            g.AddVertex("Hex");
            g.AddVertex("Ancient");
            
            g.AddEdge("Wreck", "Gate");
            g.AddEdge("Gate", "Ritual");
            g.AddEdge("Gate", "Tinker");
            g.AddEdge("Tinker", "Library");
            g.AddEdge("Library", "GateWallW");
            g.AddEdge("Library", "Stream");
            g.AddEdge("Stream", "Witch");
            g.AddEdge("Witch", "Swamp");
            g.AddEdge("Swamp", "LShrineE");
            g.AddEdge("Swamp", "HShrine");
            g.AddEdge("HShrine", "Gate");
            g.AddEdge("HShrine", "Altar2");
            g.AddEdge("Altar2", "WaterEle");
            g.AddEdge("Altar2", "Sentinel");
            g.AddEdge("Sentinel", "FoundryW");
            g.AddEdge("Sentinel", "FoundryE");
            g.AddEdge("Sentinel", "GateWallE");
            g.AddEdge("FoundryE", "FoundryW");
            g.AddEdge("FoundryW", "Warlock");
            g.AddEdge("FoundryW", "Misty");
            g.AddEdge("Misty", "Dracolich");
            g.AddEdge("Misty", "CatacombE");
            g.AddEdge("Misty", "CatacombW");
            g.AddEdge("CatacombW", "CatacombE");
            g.AddEdge("CatacombW", "Beetle");
            g.AddEdge("Beetle", "UTunnel");
            g.AddEdge("UTunnel", "WallN");
            g.AddEdge("WallN", "Hydra");
            g.AddEdge("WallN", "Catapult");
            g.AddEdge("Catapult", "Hydra");
            g.AddEdge("Hydra", "Guard");
            g.AddEdge("Guard", "Altar4");
            g.AddEdge("Altar4", "LShrineN");
            g.AddEdge("Altar4", "Ghost");
            g.AddEdge("LShrineN", "HShrine");
            g.AddEdge("LShrineN", "Hex");
            g.AddEdge("Ghost", "Hex");
            g.AddEdge("Hex", "Ancient");
            
            Debug.Log(g.CalculateNthOrderStabilityFactor(3));
        }
    }
}
