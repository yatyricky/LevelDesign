using System;
using System.Collections.Generic;
using System.IO;
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

        private void OnEnable()
        {
            _verticesList = new ReorderableList(This.graph.vertices, typeof(Vertex))
            {
                draggable = false,
                multiSelect = true,
                displayAdd = false,
                displayRemove = false,
                drawHeaderCallback = DrawVertexHeader,
                drawElementCallback = DrawVertex,
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
            vertex.Name = EditorGUI.TextField(new Rect(rect.x + VertexBulletinWidth, rect.y, rect.width - VertexTypeWidth - VertexBulletinWidth, rect.height), vertex.Name);
            vertex.Type = (VertexType)EditorGUI.EnumPopup(new Rect(rect.x + rect.width - VertexTypeWidth, rect.y, VertexTypeWidth, rect.height), vertex.Type);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.TextArea(This.graph.TakeSnapShot());

            EditorGUI.BeginChangeCheck();
            _verticesList.DoLayoutList();

            if (This.CurrentVertex != null)
            {
                EditorGUILayout.LabelField("Current Vertex");
                var rect = EditorGUILayout.GetControlRect(false);
                DrawVertex(rect, This.CurrentVertex.Index, false, false);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(This);
            }

            if (GUILayout.Button("Read"))
            {
                This.graph = Graph.Parse(File.ReadAllText("Assets/Levels/simple.txt"));
            }
        }

        private void OnSceneGUI()
        {
            // draw and edit vertices
            EditorGUI.BeginChangeCheck();
            var buttonClicked = false;
            foreach (var vertex in This.graph.vertices)
            {
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
                    This.CurrentVertex = vertex;
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
                    Handles.BeginGUI();
                    GUI.DrawTexture(new Rect(guiPos.x - iBorder, guiPos.y - iBorder, iWidth + iBorder * 2, iLineHeight * 2 + iBorder * 2), GetEditorTexture("4x4_191919"));
                    GUI.DrawTexture(new Rect(guiPos.x, guiPos.y, iWidth, iLineHeight * 2), GetEditorTexture("4x4_383838"));
                    var row = new Vector2(guiPos.x, guiPos.y);
                    GUI.Label(new Rect(row.x, row.y, iLabelWidth, iLineHeight), "Name");
                    vertex.Name = GUI.TextField(new Rect(row.x + iLabelWidth, row.y, iWidth - iLabelWidth, iLineHeight), vertex.Name);
                    row.y += iLineHeight;
                    GUI.Label(new Rect(row.x, row.y, iLabelWidth, iLineHeight), "Type");
                    var types = Enum.GetValues(typeof(VertexType)).Cast<VertexType>().ToArray();
                    vertex.Type = types[GUI.Toolbar(new Rect(row.x + iLabelWidth, row.y, iWidth - iLabelWidth, iLineHeight), Array.IndexOf(types, vertex.Type), types.Select(e => e.ToString()).ToArray())];
                    Handles.EndGUI();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(This);
            }

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
                    switch (edge.Type)
                    {
                        case EdgeType.Directed:
                            v3.Normalize();
                            v3 *= Mathf.Min(mag * 0.1f, 0.5f);
                            var va = Quaternion.AngleAxis(15, Vector3.back) * v3;
                            var vb = Quaternion.AngleAxis(15, Vector3.forward) * v3;
                            Handles.DrawLine(v2, v2 + va, 2);
                            Handles.DrawLine(v2, v2 + vb, 2);
                            break;
                        case EdgeType.ShortCut:
                            Handles.DrawWireArc(v2 + v3 * 0.4f, Vector3.back, Quaternion.AngleAxis(90, Vector3.back) * v3, 180, 0.1f);
                            break;
                        case EdgeType.Mechanism:
                            break;
                    }
                }
            }

            // mouse event
            if (Event.current.shift && Event.current.type == EventType.MouseDown || buttonClicked)
            {
                if (Event.current.button == 1)
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

                    var menu = new GenericMenu();
                    if (This.CurrentVertex != null && buttonClicked)
                    {
                        menu.AddItem(new GUIContent($"Delete {This.CurrentVertex.Name}"), false, DeleteVertex, This.CurrentVertex);
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("New Node"), false, CreateVertex, worldPos);
                    }

                    menu.ShowAsContext();
                    Event.current.Use();
                }
            }
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
            EditorUtility.SetDirty(This);
        }

        private void DeleteVertex(object vertex)
        {
            This.graph.RemoveVertex((Vertex)vertex);
            EditorUtility.SetDirty(This);
        }
    }
}
