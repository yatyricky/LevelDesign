using System.Collections.Generic;
using System.IO;
using System.Linq;
using LevelDesigner;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LevelDesignerEditor
{
    public class GraphWindow : EditorWindow
    {
        public enum ActionType
        {
            None,
            DraggingNode,
            Panning,
            Connecting,
        }

        private const string LevelsDir = "Assets/Levels";
        public const float NodeRadius = 30f;

        [MenuItem("Window/Level Designer #1")]
        public static void OpenGraphWindow()
        {
            var window = CreateInstance<GraphWindow>();
            window.titleContent = new GUIContent("Level Designer");
            window.minSize = new Vector2(600, 600);
            window.Show();

            Debugger.Asset.window = window;
        }

        private VisualElement _header, _body, _canvas, _inspector;
        private VisualElement _rootNodes, _rootConnections;
        private Button _new, _load, _save, _saveAs;
        private Dictionary<string, Node> _nodes;
        private Dictionary<string, Connection> _connections;

        // make connection
        private VisualElement _connStart, _connEnd;
        private Node _connStartNode, _connEndNode;
        private Vector2 _connStartStartPos;
        private Vector2 _connStartCurrPos;

        private VisualElement _nodeEditor;
        private Node _editingNode;
        private TextField _nodeEditorName;
        private ToolbarMenu _nodeEditorType;

        private VisualElement _connectionEditor;
        private Connection _editingConnection;
        private ToolbarMenu _connectionEditorType;

        private ActionType _currAct;
        private Vector2 _mouseStart;
        private Vector2 _mouseStartTarget;

        private string _filePath;
        private Graph _graphData;

        private void OnEnable()
        {
            // Init();

            // view
            var root = rootVisualElement;
            var xml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/LevelDesigner/GraphWindow.uxml");
            var dom = xml.CloneTree();
            var css = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/LevelDesigner/GraphWindow.uss");
            dom.styleSheets.Add(css);

            root.Add(dom);

            // base
            _header = root.Q<VisualElement>("header");
            _body = root.Q<VisualElement>("body");
            _canvas = root.Q<VisualElement>("canvas");
            _inspector = root.Q<VisualElement>("inspector");

            _rootNodes = root.Q<VisualElement>("nodes");
            _rootConnections = root.Q<VisualElement>("connections");
            _connStart = root.Q<VisualElement>("conn-start");
            _connEnd = root.Q<VisualElement>("conn-end");

            // buttons
            _new = root.Q<Button>("new-file");
            _new.clickable.clicked += NewSource;
            _load = root.Q<Button>("load");
            _load.clickable.clicked += LoadSource;
            _save = root.Q<Button>("save");
            _save.clickable.clicked += SaveSource;
            _saveAs = root.Q<Button>("save-as");
            _saveAs.clickable.clicked += SaveAsSource;

            // node editor
            _nodeEditor = root.Q<VisualElement>("node-editor");
            _nodeEditorName = root.Q<TextField>("node-name");
            _nodeEditorType = root.Q<ToolbarMenu>("node-type");
            _nodeEditor.style.display = DisplayStyle.None;

            _nodeEditorType.menu.AppendAction("POI", SetNodeEditorType, GetNodeEditorType, VertexType.Normal);
            _nodeEditorType.menu.AppendAction("Start", SetNodeEditorType, GetNodeEditorType, VertexType.Start);
            _nodeEditorType.menu.AppendAction("Save", SetNodeEditorType, GetNodeEditorType, VertexType.Save);
            _nodeEditorType.menu.AppendAction("Boss", SetNodeEditorType, GetNodeEditorType, VertexType.Boss);

            // connection editor
            _connectionEditor = root.Q<VisualElement>("connection-editor");
            _connectionEditor.style.display = DisplayStyle.None;
            _connectionEditorType = root.Q<ToolbarMenu>("connection-type");

            _connectionEditorType.menu.AppendAction("Undirected", SetConnectionEditorType, GetConnectionEditorType, EdgeType.Undirected);
            _connectionEditorType.menu.AppendAction("Directed", SetConnectionEditorType, GetConnectionEditorType, EdgeType.Directed);
            _connectionEditorType.menu.AppendAction("Shortcut", SetConnectionEditorType, GetConnectionEditorType, EdgeType.ShortCut);
            _connectionEditorType.menu.AppendAction("Mechanism", SetConnectionEditorType, GetConnectionEditorType, EdgeType.Mechanism);

            _inspector.style.display = DisplayStyle.None;

            _nodes = new Dictionary<string, Node>();
            _connections = new Dictionary<string, Connection>();

            _currAct = ActionType.None;

            _body.RegisterCallback<MouseDownEvent>(OnMouseDown);
            _body.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            _body.RegisterCallback<MouseUpEvent>(OnMouseUp);
            _body.RegisterCallback<MouseOutEvent>(OnMouseOut);
        }

        private DropdownMenuAction.Status GetConnectionEditorType(DropdownMenuAction arg)
        {
            if (_editingConnection == null)
            {
                return DropdownMenuAction.Status.Normal;
            }

            return _editingConnection.Edge.Type == (EdgeType) arg.userData ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
        }

        private void SetConnectionEditorType(DropdownMenuAction obj)
        {
            _editingConnection?.SetEdgeType((EdgeType) obj.userData);
        }

        private DropdownMenuAction.Status GetNodeEditorType(DropdownMenuAction arg)
        {
            if (_editingNode == null)
            {
                return DropdownMenuAction.Status.Normal;
            }

            return _editingNode.Vertex.Type == (VertexType) arg.userData ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
        }

        private void SetNodeEditorType(DropdownMenuAction obj)
        {
            _editingNode?.SetVertexType((VertexType) obj.userData);
        }

        private void OnGUI()
        {
            // style
            _body.style.height = position.height;
            _header.style.width = position.width;

            if (_graphData != null)
            {
                DrawGraph();
            }

            // controls
            var current = Event.current;
            if (current.keyCode == KeyCode.Escape)
            {
                Close();
            }
        }

        private void OnDisable()
        {
            _body.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            _body.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            _body.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            _body.UnregisterCallback<MouseOutEvent>(OnMouseOut);
        }

        private Node RaycastNodeBorder(Vector2 mousePos)
        {
            foreach (var kv in _nodes)
            {
                var rect = kv.Value.DOM.worldBound;
                var r0 = rect.width * 0.5f - 5f;
                var r1 = r0 + 10f;
                if (rect.Contains(mousePos))
                {
                    var ring = new Ring(rect.center, r0, r1);
                    if (Utils.RingContains(ring, mousePos))
                    {
                        return kv.Value;
                    }
                }
            }

            return null;
        }

        private Node RaycastNode(Vector2 mousePos)
        {
            foreach (var kv in _nodes)
            {
                if (kv.Value.DOM.worldBound.Contains(mousePos))
                {
                    return kv.Value;
                }
            }

            return null;
        }

        private Connection RayCastConnection()
        {
            foreach (var kv in _connections)
            {
                var oRect = kv.Value.DOM.worldBound;
                var rect = new Rect(oRect.position - new Vector2(5f, 5f), oRect.size + new Vector2(10f, 10f));
                if (rect.Contains(_mouseStart))
                {
                    if (kv.Value.PathCastPoint(_mouseStart))
                    {
                        return kv.Value;
                    }
                }
            }

            return null;
        }

        private void LeftMouseDown(MouseDownEvent e)
        {
            if (_currAct != ActionType.None)
            {
                return;
            }

            _mouseStart = e.mousePosition;
            if (_connStartNode != null)
            {
                _currAct = ActionType.Connecting;
                _connStartStartPos = _connStart.worldBound.center;
                _connStartCurrPos = _connStartStartPos + Vector2.one;
                return;
            }

            // node
            var node = RaycastNode(_mouseStart);
            SetEditingNode(node);
            if (node != null)
            {
                _currAct = ActionType.DraggingNode;
                Utils.BringDOMToFront(node.DOM);
                _mouseStartTarget = node.DOM.worldBound.position;
                return;
            }

            // conn
            var connection = RayCastConnection();
            SetEditingConnection(connection);
        }

        private void RightMouseDown(MouseDownEvent e)
        {
            if (_currAct != ActionType.None)
            {
                return;
            }

            _currAct = ActionType.Panning;
            _mouseStart = e.mousePosition;
            _mouseStartTarget = _canvas.worldBound.position;
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            switch (e.button)
            {
                case 0:
                    LeftMouseDown(e);
                    break;
                case 1:
                    RightMouseDown(e);
                    break;
            }
        }

        private void OnMouseMove(MouseMoveEvent e)
        {
            var mousePos = e.mousePosition;
            var delta = mousePos - _mouseStart;
            var pos = _mouseStartTarget + delta;
            switch (_currAct)
            {
                case ActionType.None:
                    var node = RaycastNodeBorder(mousePos);
                    SetStartConnectionNode(node, mousePos);
                    break;
                case ActionType.DraggingNode:
                    _editingNode.SetPosition(pos);
                    SetStartConnectionNode(null, Vector2.zero);
                    break;
                case ActionType.Panning:
                    var parentPos = _canvas.parent.worldBound.position;
                    var targetPos = pos - parentPos;
                    _canvas.style.left = targetPos.x;
                    _canvas.style.top = targetPos.y;
                    SetStartConnectionNode(null, Vector2.zero);
                    break;
                case ActionType.Connecting:
                    _connStartCurrPos = mousePos;
                    var targetNode = RaycastNode(mousePos);
                    SetEndConnectionNode(targetNode, mousePos);
                    break;
            }
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            switch (_currAct)
            {
                case ActionType.DraggingNode:
                case ActionType.Panning:
                    _currAct = ActionType.None;
                    break;
                case ActionType.Connecting:
                    _currAct = ActionType.None;
                    if (_connEndNode != null)
                    {
                        _graphData.AddEdge(_connStartNode.Vertex.Name, _connEndNode.Vertex.Name);
                    }
                    SetEndConnectionNode(null, Vector2.zero);
                    break;
            }
        }

        private void OnMouseOut(MouseOutEvent e)
        {
        }

        private void Update()
        {
            Repaint();
        }

        private void SetEditingNode(Node node)
        {
            _editingNode?.SetSelected(false);
            _editingNode = node;
            _editingNode?.SetSelected(true);
            if (node != null)
            {
                _nodeEditorName.value = node.Vertex.Name;
                SetEditingConnection(null);
            }
        }

        private void SetEditingConnection(Connection connection)
        {
            _editingConnection?.SetSelected(false);
            _editingConnection = connection;
            _editingConnection?.SetSelected(true);
            if (connection != null)
            {
                // _connectionEditorType
                // _nodeEditorName.value = node.Vertex.Name;
                SetEditingNode(null);
            }
        }

        private void SetStartConnectionNode(Node node, Vector2 mousePos)
        {
            _connStartNode = node;
            if (node != null)
            {
                _connStart.style.display = DisplayStyle.Flex;

                var canvasPos = _canvas.worldBound.position;

                var rect = node.DOM.worldBound;
                var dir = mousePos - rect.center;
                var v = dir.normalized * rect.width * 0.5f;
                var p = rect.center + v;

                var rp = p - _connStart.worldBound.size * 0.5f - canvasPos;
                _connStart.style.left = rp.x;
                _connStart.style.top = rp.y;
            }
            else
            {
                _connStart.style.display = DisplayStyle.None;
            }
        }

        private void SetEndConnectionNode(Node node, Vector2 mousePos)
        {
            _connEndNode = node;
            if (_connStartNode == null || _connEndNode == null)
            {
                _connEnd.style.display = DisplayStyle.None;
            }
            else
            {
                _connEnd.style.display = DisplayStyle.Flex;

                var canvasPos = _canvas.worldBound.position;
                var rp = mousePos - _connEnd.worldBound.size * 0.5f - canvasPos;
                _connEnd.style.left = rp.x;
                _connEnd.style.top = rp.y;
            }
        }

        private void DrawNodes()
        {
            // noes
            var currNodes = new HashSet<string>(_nodes.Keys);
            for (var i = 0; i < _graphData.Vertices.Count; i++)
            {
                var vertex = _graphData.Vertices[i];
                Node node;
                if (currNodes.Contains(vertex.Name))
                {
                    currNodes.Remove(vertex.Name);
                    node = _nodes[vertex.Name];
                }
                else
                {
                    node = AddNode(vertex);
                }

                node.OnGUI();
            }

            foreach (var nodeName in currNodes)
            {
                RemoveNode(nodeName);
            }
        }

        private void DrawConnections()
        {
            // connections
            var currConnections = new HashSet<string>(_connections.Keys);
            for (var i = 0; i < _graphData.Edges.Count; i++)
            {
                var edge = _graphData.Edges[i];
                var connectionName = edge.NodesName;
                Connection connection;
                if (currConnections.Contains(connectionName))
                {
                    currConnections.Remove(connectionName);
                    connection = _connections[connectionName];
                }
                else
                {
                    connection = AddConnection(edge);
                }

                connection.OnGUI();
            }

            foreach (var connectionName in currConnections)
            {
                RemoveConnection(connectionName);
            }
        }

        private void DrawNodeInspector()
        {
            // node editor
            if (_editingNode == null)
            {
                _nodeEditor.style.display = DisplayStyle.None;
            }
            else
            {
                _nodeEditor.style.display = DisplayStyle.Flex;
                // _editingNode.Vertex.Name = _nodeEditorName.value;
                _nodeEditorName.value = _editingNode.Vertex.Name;
                _nodeEditorType.text = _editingNode.Vertex.Type.ToString();
            }
        }

        private void DrawConnectionInspector()
        {
            // connection editor
            if (_editingConnection == null)
            {
                _connectionEditor.style.display = DisplayStyle.None;
            }
            else
            {
                _connectionEditor.style.display = DisplayStyle.Flex;
                _connectionEditorType.text = _editingConnection.Edge.Type.ToString();
            }
        }

        private void DrawMakeConnection()
        {
            // make connection
            if (_connStartNode == null || _currAct != ActionType.Connecting)
            {
                return;
            }

            var offset = new Vector2(0, 21f);
            Handles.BeginGUI();
            Handles.DrawLine(_connStartStartPos - offset, _connStartCurrPos - offset);
            Handles.EndGUI();
        }

        private void DrawGraph()
        {
            DrawNodes();
            DrawConnections();

            DrawNodeInspector();
            DrawConnectionInspector();
            if (_editingNode == null && _editingConnection == null)
            {
                _inspector.style.display = DisplayStyle.None;
            }
            else
            {
                _inspector.style.display = DisplayStyle.Flex;
            }

            DrawMakeConnection();
        }

        private Node AddNode(LevelDesigner.Vertex vertex)
        {
            var node = new Node(vertex, _rootNodes);
            _nodes.Add(vertex.Name, node);
            return node;
        }

        private Node RemoveNode(string nodeName)
        {
            if (_nodes.TryGetValue(nodeName, out var node))
            {
                _rootNodes.Remove(node.DOM);
                _nodes.Remove(nodeName);
                return node;
            }

            Debug.LogError($"Unable to remove node {nodeName}");
            return null;
        }

        private Connection AddConnection(Edge edge)
        {
            var fromNodeName = edge.From.Name;
            var toNodeName = edge.To.Name;
            var connectionName = edge.NodesName;
            var source = _nodes[fromNodeName];
            var target = _nodes[toNodeName];
            var connection = new Connection(source, target, edge, _rootConnections);
            _connections.Add(connectionName, connection);
            return connection;
        }

        private Node RemoveConnection(string connectionName)
        {
            if (_nodes.TryGetValue(connectionName, out var node))
            {
                _rootNodes.Remove(node.DOM);
                _nodes.Remove(connectionName);
                return node;
            }

            Debug.LogError($"Unable to remove node {connectionName}");
            return null;
        }

        private void GetFilePath(out string defaultName, out string dirPath)
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                defaultName = "graph.txt";
                dirPath = Path.GetFullPath(LevelsDir);
            }
            else
            {
                defaultName = Path.GetFileName(_filePath);
                dirPath = Path.GetDirectoryName(_filePath) ?? Path.GetFullPath(LevelsDir);
            }
        }

        private void NewSource()
        {
            _graphData = new Graph();
            _graphData.AddVertex("Start");
        }

        private void LoadSource()
        {
            GetFilePath(out _, out var dirPath);
            var fp = EditorUtility.OpenFilePanel("选择一个配置", dirPath, "txt");
            if (fp.Length == 0)
                return;

            Clear();

            _filePath = fp;
            _graphData = Graph.Parse(File.ReadAllText(fp));
        }

        private void SaveSource()
        {
            if (_graphData == null)
            {
                EditorUtility.DisplayDialog("错误", "请先加载/新建一个配置", "好的");
                return;
            }

            GetFilePath(out var defaultName, out var dirPath);
            File.WriteAllText(Path.Combine(dirPath, defaultName), _graphData.ToString());
        }

        private void SaveAsSource()
        {
            if (_graphData == null)
            {
                EditorUtility.DisplayDialog("错误", "请先加载/新建一个配置", "好的");
                return;
            }

            GetFilePath(out var defaultName, out var dirPath);
            var fp = EditorUtility.SaveFilePanel("选择一个配置", dirPath, defaultName, "txt");
            if (fp.Length == 0)
                return;

            File.WriteAllText(fp, _graphData.ToString());
        }

        private void Clear()
        {
            var connections = _connections.Values.ToList();
            for (var i = connections.Count - 1; i >= 0; i--)
            {
                var conn = connections[i];
                conn.Dispose();
            }

            var nodes = _nodes.Values.ToList();
            for (var i = nodes.Count - 1; i >= 0; i--)
            {
                var conn = nodes[i];
                conn.Dispose();
            }

            _connections.Clear();
            _nodes.Clear();
            _graphData = null;

            _canvas.style.left = 0;
            _canvas.style.top = 0;
        }
    }
}
