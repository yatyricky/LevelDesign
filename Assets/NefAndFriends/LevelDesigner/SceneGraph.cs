using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NefAndFriends.LevelDesigner
{
    public class SceneGraph : MonoBehaviour
    {
        public Graph graph;

        [NonSerialized]
        internal Vertex CurrentVertex;
    }

    [CustomEditor(typeof(SceneGraph))]
    public class SceneGraphEditor : Editor
    {
        private const float VertexBulletinWidth = 20f;
        private const float VertexTypeWidth = 120f;

        private static readonly Dictionary<VertexType, string> LabelTextureAssetNames = new()
        {
            { VertexType.POI, "label_normal" },
            { VertexType.Start, "label_start" },
            { VertexType.Save, "label_save" },
            { VertexType.Boss, "label_boss" },
        };

        private static Dictionary<string, Texture2D> _editorTextures;

        private static Texture2D GetEditorTexture(string fileName)
        {
            _editorTextures ??= new Dictionary<string, Texture2D>();
            if (!_editorTextures.TryGetValue(fileName, out var texture))
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/NefAndFriends/Editor/Textures/{fileName}.png");
                _editorTextures.Add(fileName, texture);
            }

            return texture;
        }

        private static Texture2D GetLabelTexture(VertexType type)
        {
            return GetEditorTexture(LabelTextureAssetNames[type]);
        }

        private SceneGraph This => target as SceneGraph;

        private ReorderableList _verticesList;
        private ReorderableList _edgesList;

        private Vertex _makingConnection;

        private void OnEnable()
        {
            _verticesList = new ReorderableList(This.graph.vertices, typeof(Vertex))
            {
                draggable = false,
                displayAdd = false,
                displayRemove = false,
                drawHeaderCallback = DrawVertexHeader,
                drawElementCallback = DrawVertex,
                onSelectCallback = SelectVertexInInspector,
            };

            _edgesList = new ReorderableList(This.graph.edges, typeof(Edge))
            {
                draggable = false,
                displayAdd = false,
                displayRemove = false,
                drawHeaderCallback = DrawEdgeHeader,
                drawElementCallback = DrawEdge,
            };
        }

        private void DrawVertexHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Vertices");
        }

        private void DrawVertex(Rect rect, int index, bool isActive, bool isFocused)
        {
            var graph = This.graph;
            var vertex = graph.vertices[index];
            vertex.SetParent(graph);
            if (Event.current.type == EventType.Repaint)
            {
                ((GUIStyle)"Radio").Draw(new Rect(rect.x, rect.y, VertexBulletinWidth, VertexBulletinWidth), GUIContent.none, 0, This.CurrentVertex == vertex, false);
            }

            vertex.Name = EditorGUI.TextField(new Rect(rect.x + VertexBulletinWidth, rect.y, rect.width - VertexTypeWidth - VertexBulletinWidth, rect.height), vertex.Name);
            vertex.Type = (VertexType)EditorGUI.EnumPopup(new Rect(rect.x + rect.width - VertexTypeWidth, rect.y, VertexTypeWidth, rect.height), vertex.Type);
        }

        private void SelectVertexInInspector(ReorderableList list)
        {
            if (list.selectedIndices.Count > 0)
            {
                This.CurrentVertex = This.graph.vertices[list.selectedIndices.First()];
                SceneView.RepaintAll();
            }
        }

        private void DrawEdgeHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Edges");
        }

        private void DrawEdge(Rect rect, int index, bool isActive, bool isFocused)
        {
            var graph = This.graph;
            var edge = graph.edges[index];
            edge.SetParent(graph);
            var options = (from e in graph.vertices select e.Name).ToArray();
            var vertexWidth = (rect.width - VertexTypeWidth) / 2f;
            edge.from = graph.vertices[EditorGUI.Popup(new Rect(rect.x, rect.y, vertexWidth, rect.height), edge.from.Index, options)];
            edge.to = graph.vertices[EditorGUI.Popup(new Rect(rect.x + vertexWidth, rect.y, vertexWidth, rect.height), edge.to.Index, options)];
            edge.Type = (EdgeType)EditorGUI.EnumPopup(new Rect(rect.x + vertexWidth * 2, rect.y, rect.width - vertexWidth * 2, rect.height), edge.Type);
        }

        private float? _sf3;

        public override void OnInspectorGUI()
        {
            _sf3 ??= This.graph.CalculateNthOrderStabilityFactor(3);
            EditorGUILayout.LabelField($"Stability Factor: {_sf3.Value}");

            EditorGUI.BeginChangeCheck();
            _verticesList.DoLayoutList();
            _edgesList.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(This);
            }
        }

        private void OnSceneGUI()
        {
            var shouldRepaintAll = false;

            // draw edges
            foreach (var edge in This.graph.edges)
            {
                if (edge.from != edge.to)
                {
                    var v1 = edge.from.Position;
                    var v2 = edge.to.Position;
                    Handles.DrawLine(v1, v2, 2);
                    var v3 = v1 - v2;
                    var mag = v3.magnitude;
                    var capSize = 0.1f * mag;
                    switch (edge.Type)
                    {
                        case EdgeType.Mechanism:
                            Handles.DrawSolidDisc(v1 - v3.normalized * (capSize * 0.3f), Vector3.back, capSize * 0.3f);
                            goto case EdgeType.Directed;
                        case EdgeType.Directed:
                            v3.Normalize();
                            v3 *= capSize;
                            // v3 *= Mathf.Min(mag * 0.1f, 0.5f);
                            var va = Quaternion.AngleAxis(15, Vector3.back) * v3;
                            var vb = Quaternion.AngleAxis(15, Vector3.forward) * v3;
                            Handles.DrawLine(v2, v2 + va, 2);
                            Handles.DrawLine(v2, v2 + vb, 2);
                            break;
                        case EdgeType.ShortCut:
                            Handles.DrawWireArc(v2 + v3 * 0.6f, Vector3.back, Quaternion.AngleAxis(90, Vector3.back) * v3, 180, capSize / 2f, 2f);
                            break;
                    }
                }
            }

            if (_makingConnection != null)
            {
                Handles.DrawLine(_makingConnection.Position, GetMouseWorldPos(), 2);
                shouldRepaintAll = true;
            }

            // draw and edit vertices
            EditorGUI.BeginChangeCheck();
            var buttonClicked = false;
            foreach (var vertex in This.graph.vertices)
            {
                vertex.SetParent(This.graph);
                var style = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 16,
                    padding = new RectOffset(2, 2, 2, 2),
                    normal =
                    {
                        background = GetLabelTexture(vertex.Type),
                    },
                    fixedHeight = 30,
                };

                var pos = vertex.Position;
                if (HandlesButton(pos, new GUIContent(vertex.Name), style))
                {
                    if (Event.current.button == 0)
                    {
                        // make connection to
                        if (_makingConnection != null)
                        {
                            This.graph.AddEdge(_makingConnection, vertex);
                            _sf3 = null;
                            _makingConnection = null;
                        }
                        else
                        {
                            This.CurrentVertex = This.CurrentVertex == vertex ? null : vertex;
                        }
                    }
                    else
                    {
                        This.CurrentVertex = vertex;
                    }

                    buttonClicked = true;
                    Repaint();
                }

                if (This.CurrentVertex == vertex)
                {
                    vertex.Position = Handles.PositionHandle(pos, Quaternion.identity);
                    var guiPos = HandleUtility.WorldToGUIPoint(pos);
                    guiPos.y += 30;
                    const int iWidth = 210;
                    const int iBorder = 2;
                    const int iLineHeight = 20;
                    const int iLabelWidth = 60;
                    const int iTextWidth = 100;
                    const int iCloseButton = 20;
                    const int iMakeConnectionButton = 30;
                    const int iLines = 2;

                    var outgoingEdges = This.graph.GetOutgoingEdges(vertex);
                    var incomingEdges = This.graph.GetIncomingEdges(vertex);
                    var totalLines = iLines + (outgoingEdges.Count > 0 ? outgoingEdges.Count + 1 : 0) + (incomingEdges.Count > 0 ? incomingEdges.Count + 1 : 0);

                    Handles.BeginGUI();
                    GUI.DrawTexture(new Rect(guiPos.x - iBorder, guiPos.y - iBorder, iWidth + iBorder * 2, iLineHeight * totalLines + iBorder * 2), GetEditorTexture("4x4_191919"));
                    GUI.DrawTexture(new Rect(guiPos.x, guiPos.y, iWidth, iLineHeight * totalLines), GetEditorTexture("4x4_383838"));

                    var row = new Vector2(guiPos.x, guiPos.y);

                    if (GUI.Button(new Rect(row.x - iMakeConnectionButton - iBorder, row.y, iMakeConnectionButton, iMakeConnectionButton), GetEditorTexture(_makingConnection == vertex ? "add_connection_active" : "add_connection"), new GUIStyle(GUI.skin.label)))
                    {
                        _makingConnection = _makingConnection == vertex ? null : vertex;
                    }

                    GUI.Label(new Rect(row.x, row.y, iLabelWidth, iLineHeight), "Name");
                    vertex.Name = GUI.TextField(new Rect(row.x + iLabelWidth, row.y, iWidth - iLabelWidth, iLineHeight), vertex.Name);

                    row.y += iLineHeight;
                    GUI.Label(new Rect(row.x, row.y, iLabelWidth, iLineHeight), "Type");
                    var types = Enum.GetValues(typeof(VertexType)).Cast<VertexType>().ToArray();
                    vertex.Type = types[GUI.Toolbar(new Rect(row.x + iLabelWidth, row.y, iWidth - iLabelWidth, iLineHeight), Array.IndexOf(types, vertex.Type), types.Select(e => e.ToString()).ToArray())];

                    var edgeTypes = Enum.GetValues(typeof(EdgeType)).Cast<EdgeType>().ToArray();
                    var edgeOptions = edgeTypes.Select(e => e.ToString()[0].ToString()).ToArray();

                    void DrawEdgeInspector(Edge edge, Vertex other)
                    {
                        edge.SetParent(This.graph);
                        row.y += iLineHeight;
                        GUI.Label(new Rect(row.x, row.y, iTextWidth, iLineHeight), other.Name + (other == vertex ? "↺" : ""));
                        edge.Type = edgeTypes[GUI.Toolbar(new Rect(row.x + iTextWidth, row.y, iWidth - iTextWidth - iCloseButton, iLineHeight), Array.IndexOf(edgeTypes, edge.Type), edgeOptions)];
                        if (GUI.Button(new Rect(row.x + iWidth - iCloseButton, row.y, iCloseButton, iLineHeight), GetEditorTexture("close"), new GUIStyle(GUI.skin.label)))
                        {
                            This.graph.RemoveEdge(edge);
                            _sf3 = null;
                        }
                    }

                    if (outgoingEdges.Count > 0)
                    {
                        row.y += iLineHeight;
                        GUI.Label(new Rect(row.x, row.y, iWidth, iLineHeight), "Outgoing Edges");
                        foreach (var edge in outgoingEdges)
                        {
                            DrawEdgeInspector(edge, edge.to);
                        }
                    }

                    if (incomingEdges.Count > 0)
                    {
                        row.y += iLineHeight;
                        GUI.Label(new Rect(row.x, row.y, iWidth, iLineHeight), "Incoming Edges");
                        foreach (var edge in incomingEdges)
                        {
                            DrawEdgeInspector(edge, edge.from);
                        }
                    }

                    Handles.EndGUI();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(This);
            }

            // mouse event
            if (Event.current.shift && Event.current.type == EventType.MouseDown || buttonClicked)
            {
                if (Event.current.button == 1)
                {
                    var menu = new GenericMenu();
                    if (This.CurrentVertex != null && buttonClicked)
                    {
                        menu.AddItem(new GUIContent($"Delete {This.CurrentVertex.Name}"), false, DeleteVertex, This.CurrentVertex);
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("New Node"), false, CreateVertex, GetMouseWorldPos());
                    }

                    menu.ShowAsContext();
                    Event.current.Use();
                }
            }

            if (shouldRepaintAll)
            {
                SceneView.RepaintAll();
            }
        }

        private static Vector3 GetMouseWorldPos()
        {
            var camera = SceneView.currentDrawingSceneView.camera;
            var position = camera.transform.position;
            var plane = new Plane(Vector3.forward, new Vector3(position.x, position.y, 0));
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var worldPos = Vector3.zero;
            if (plane.Raycast(ray, out var enter))
            {
                worldPos = ray.GetPoint(enter);
            }

            return worldPos;
        }

        private static bool HandlesButton(Vector3 position, GUIContent content, GUIStyle style)
        {
            if (HandleUtility.WorldToGUIPointWithDepth(position).z < 0.0)
            {
                return false;
            }

            Handles.BeginGUI();
            var rect = HandleUtility.WorldPointToSizedRect(position, content, style);
            var click = GUI.Button(new Rect(rect.x, rect.y + 16, rect.width, rect.height), content, style);
            Handles.EndGUI();
            return click;
        }

        private void CreateVertex(object worldPos)
        {
            var vertex = This.graph.AddVertex();
            vertex.Position = (Vector3)worldPos;
            _sf3 = null;
            EditorUtility.SetDirty(This);
        }

        private void DeleteVertex(object vertex)
        {
            This.graph.RemoveVertex((Vertex)vertex);
            _sf3 = null;
            EditorUtility.SetDirty(This);
        }
    }
}
