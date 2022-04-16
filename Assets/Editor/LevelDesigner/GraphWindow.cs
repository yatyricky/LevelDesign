using System;
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
        private const string LevelsDir = "Assets/Levels";
        public const float NodeRadius = 30f;

        [MenuItem("Window/Level Designer #1")]
        public static void OpenGraphWindow()
        {
            var window = CreateInstance<GraphWindow>();
            window.titleContent = new GUIContent("Level Designer");
            window.Show();
        }

        private VisualElement _body;
        private VisualElement _rootNodes, _rootConnections;
        private Button _new, _load, _save, _saveAs;
        private Dictionary<string, Node> _nodes;
        private Dictionary<string, Connection> _connections;

        // make connection
        private VisualElement _connStart;
        private Vector2 _connStartStartPos;
        private bool _connStartDragging;
        private Vector2 _connStartCurrPos;

        private VisualElement _nodeEditor;
        private Node _editingNode;
        private TextField _nodeEditorName;
        private ToolbarMenu _nodeEditorType;

        private VisualElement _connectionEditor;
        public Connection EditingConnection { get; private set; }
        private ToolbarMenu _connectionEditorType;

        private bool _dragging;
        private Vector2 _dragStart;
        private Vector2 _dragStartDom;

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

            _body = root.Q<VisualElement>("body");
            _rootNodes = root.Q<VisualElement>("nodes");
            _rootConnections = root.Q<VisualElement>("connections");
            _connStart = root.Q<VisualElement>("conn-start");
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

            _nodeEditorType.menu.AppendAction("POI", SetNodeEditorType, GetNodeEditorType, NodeType.Normal);
            _nodeEditorType.menu.AppendAction("Start", SetNodeEditorType, GetNodeEditorType, NodeType.Start);
            _nodeEditorType.menu.AppendAction("Save", SetNodeEditorType, GetNodeEditorType, NodeType.Save);
            _nodeEditorType.menu.AppendAction("Boss", SetNodeEditorType, GetNodeEditorType, NodeType.Boss);

            // connection editor
            _connectionEditor = root.Q<VisualElement>("connection-editor");
            _connectionEditor.style.display = DisplayStyle.None;
            _connectionEditorType = root.Q<ToolbarMenu>("connection-type");

            _connectionEditorType.menu.AppendAction("Undirected", SetConnectionEditorType, GetConnectionEditorType, EdgeType.Undirected);
            _connectionEditorType.menu.AppendAction("Directed", SetConnectionEditorType, GetConnectionEditorType, EdgeType.Directed);
            _connectionEditorType.menu.AppendAction("Shortcut", SetConnectionEditorType, GetConnectionEditorType, EdgeType.ShortCut);
            _connectionEditorType.menu.AppendAction("Mechanism", SetConnectionEditorType, GetConnectionEditorType, EdgeType.Mechanism);

            _nodes = new Dictionary<string, Node>();
            _connections = new Dictionary<string, Connection>();

            _body.RegisterCallback<MouseDownEvent>(OnBodyMouseDown);
            _body.RegisterCallback<MouseMoveEvent>(OnBodyMouseMove);
            _body.RegisterCallback<MouseUpEvent>(OnBodyMouseUp);
            _body.RegisterCallback<MouseOutEvent>(OnBodyMouseOut);

            // make connection
            _connStart.RegisterCallback<MouseDownEvent>(OnConnStartMouseDown);
            _connStart.RegisterCallback<MouseMoveEvent>(OnConnStartMouseMove);
            _connStart.RegisterCallback<MouseUpEvent>(OnConnStartMouseUp);
            _connStart.RegisterCallback<MouseOutEvent>(OnConnStartMouseOut);
        }

        private DropdownMenuAction.Status GetConnectionEditorType(DropdownMenuAction arg)
        {
            if (EditingConnection == null)
            {
                return DropdownMenuAction.Status.Normal;
            }

            return EditingConnection.Edge.Type == (EdgeType) arg.userData ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
        }

        private void SetConnectionEditorType(DropdownMenuAction obj)
        {
            if (EditingConnection != null)
            {
                EditingConnection.Edge.Type = (EdgeType) obj.userData;
            }
        }

        private DropdownMenuAction.Status GetNodeEditorType(DropdownMenuAction arg)
        {
            if (_editingNode == null)
            {
                return DropdownMenuAction.Status.Normal;
            }

            return _editingNode.Vertex.Type == (NodeType) arg.userData ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
        }

        private void SetNodeEditorType(DropdownMenuAction obj)
        {
            if (_editingNode != null)
            {
                _editingNode.Vertex.Type = (NodeType) obj.userData;
            }
        }

        private void OnGUI()
        {
            // style
            _body.style.height = position.height;

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
            _body.UnregisterCallback<MouseDownEvent>(OnBodyMouseDown);
            _body.UnregisterCallback<MouseMoveEvent>(OnBodyMouseMove);
            _body.UnregisterCallback<MouseUpEvent>(OnBodyMouseUp);
            _body.UnregisterCallback<MouseOutEvent>(OnBodyMouseOut);

            _connStart.UnregisterCallback<MouseDownEvent>(OnConnStartMouseDown);
            _connStart.UnregisterCallback<MouseMoveEvent>(OnConnStartMouseMove);
            _connStart.UnregisterCallback<MouseUpEvent>(OnConnStartMouseUp);
            _connStart.UnregisterCallback<MouseOutEvent>(OnConnStartMouseOut);
        }

        private void OnConnStartMouseDown(MouseDownEvent e)
        {
            if (e.button != 0)
            {
                return;
            }

            e.StopPropagation();
            _connStartStartPos = e.mousePosition;
            _connStartCurrPos = _connStartStartPos + Vector2.one;
            _connStartDragging = true;
        }

        private void OnConnStartMouseMove(MouseMoveEvent e)
        {
            // if (!_connStartDragging)
            // {
            //     return;
            // }
            //
            // e.StopPropagation();
            //
            // _connStartCurrPos = e.mousePosition;
        }

        private void OnConnStartMouseUp(MouseUpEvent e)
        {
            // if (!_connStartDragging)
            // {
            //     return;
            // }
            //
            // e.StopPropagation();
            // _connStartDragging = false;
        }

        private void OnConnStartMouseOut(MouseOutEvent e)
        {
            // if (!_connStartDragging)
            // {
            //     return;
            // }
            //
            // e.StopPropagation();
            // _connStartCurrPos = e.mousePosition;
        }

        private Node GetMouseHitNode(Vector2 mousePos)
        {
            Node find = null;
            foreach (var kv in _nodes)
            {
                var p = kv.Value.WorldPosition;
                var dir = mousePos - p;
                var dist = dir.magnitude;
                if (Mathf.Abs(dist - NodeRadius) <= 10f)
                {
                    find = kv.Value;
                    break;
                }
            }

            return find;
        }

        private void OnBodyMouseDown(MouseDownEvent e)
        {
            if (e.button == 0)
            {
                SetEditingNode(null);
                SetEditingConnection(null);
            }

            if (e.button != 1)
            {
                return;
            }

            if (_dragging)
            {
                e.StopImmediatePropagation();
                return;
            }

            e.StopPropagation();

            _dragging = true;
            _dragStart = e.mousePosition;
            _dragStartDom = Utils.GetDOMLocalPosition(_rootNodes);
        }

        private void OnBodyMouseMove(MouseMoveEvent e)
        {
            if (_dragging)
            {
                e.StopPropagation();

                var delta = e.mousePosition - _dragStart;

                var newPos = _dragStartDom + delta;
                _rootNodes.style.top = newPos.y;
                _rootNodes.style.left = newPos.x;
            }
            else
            {
                Node find = GetMouseHitNode(e.localMousePosition);

                if (_connStartDragging)
                {
                    e.StopPropagation();
                    _connStartCurrPos = e.mousePosition;
                    // Debug.Log($"set _connStartCurrPos to {_connStartCurrPos}");
                }
                else
                {
                    if (find == null)
                    {
                        _connStart.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        _connStart.style.display = DisplayStyle.Flex;
                        var dir = (e.localMousePosition - find.WorldPosition).normalized;
                        dir *= NodeRadius;
                        var pos = find.WorldPosition + dir;
                        _connStart.style.left = pos.x - 5f;
                        _connStart.style.top = pos.y - 5f;
                    }
                }
            }
        }

        private void OnBodyMouseUp(MouseUpEvent e)
        {
            if (_dragging)
            {
                e.StopPropagation();
                _dragging = false;
            }
            else if (_connStartDragging)
            {
                e.StopPropagation();
                _connStartDragging = false;
            }
        }

        private void OnBodyMouseOut(MouseOutEvent e)
        {
            if (_dragging)
            {
                e.StopPropagation();
            }
            else if (_connStartDragging)
            {
                e.StopPropagation();
            }
        }

        public void SetEditingNode(Node node)
        {
            _editingNode = node;
            if (node != null)
            {
                _nodeEditorName.value = node.Vertex.Name;
            }
        }

        public void SetEditingConnection(Connection connection)
        {
            EditingConnection = connection;
        }

        public static Vector2 World2Screen(Vector2 vec)
        {
            return new Vector2(vec.x, -vec.y) * 50f;
        }

        public static Vector2 Screen2World(Vector2 vec)
        {
            return new Vector2(vec.x, -vec.y) * 0.02f;
        }

        private void DrawGraph()
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
                    node = AddNode(vertex.Name, World2Screen(vertex.Position));
                    node.Vertex = vertex;
                    node.SetParent(this);
                }

                node.Draw();
            }

            foreach (var nodeName in currNodes)
            {
                RemoveNode(nodeName);
            }

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
                    connection = AddConnection(edge.From.Name, edge.To.Name, connectionName, edge);
                    connection.SetParent(this);
                }

                connection.Draw();
            }

            foreach (var connectionName in currConnections)
            {
                RemoveConnection(connectionName);
            }

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

            // connection editor
            if (EditingConnection == null)
            {
                _connectionEditor.style.display = DisplayStyle.None;
            }
            else
            {
                _connectionEditor.style.display = DisplayStyle.Flex;
                _connectionEditorType.text = EditingConnection.Edge.Type.ToString();
            }

            // make connection
            if (_connStartDragging)
            {
                var offset = new Vector2(0, -20f);
                Handles.BeginGUI();
                Handles.DrawLine(_connStartStartPos + offset, _connStartCurrPos + offset);
                Handles.EndGUI();
            }
        }

        private Node AddNode(string nodeName, Vector2 nodePos)
        {
            var node = new Node(nodeName, nodePos);
            _rootNodes.Add(node.DOM);
            _nodes.Add(nodeName, node);
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

        private Connection AddConnection(string fromNodeName, string toNodeName, string connectionName, Edge edge)
        {
            var source = _nodes[fromNodeName];
            var target = _nodes[toNodeName];
            var connection = new Connection(source, target, edge);
            _rootConnections.Add(connection.DOM);
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
        }
    }
}
