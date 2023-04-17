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
            { VertexType.Normal, "Assets/NefAndFriends/Editor/Textures/label_normal.png" },
            { VertexType.Start, "Assets/NefAndFriends/Editor/Textures/label_start.png" },
            { VertexType.Save, "Assets/NefAndFriends/Editor/Textures/label_save.png" },
            { VertexType.Boss, "Assets/NefAndFriends/Editor/Textures/label_boss.png" },
        };

        private static Dictionary<VertexType, Texture2D> _labelTextures;

        private static Texture2D GetLabelTexture(VertexType type)
        {
            _labelTextures ??= new Dictionary<VertexType, Texture2D>();

            if (!_labelTextures.TryGetValue(type, out var texture))
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(LabelTextureAssetNames[type]);
                _labelTextures.Add(type, texture);
            }

            return texture;
        }

        private static int? _hotControl;

        private static int HotControl
        {
            get
            {
                _hotControl ??= GUIUtility.GetControlID(0, FocusType.Passive);
                return _hotControl.Value;
            }
        }

        private SceneGraph This => target as SceneGraph;

        private ReorderableList _verticesList;

        private void OnEnable()
        {
            _verticesList = new ReorderableList(This.graph.vertices, typeof(Vertex))
            {
                draggable = false,
                multiSelect = true,
                drawHeaderCallback = DrawVertexHeader,
                drawElementCallback = DrawVertex,
                onAddCallback = DrawAddVertex,
                onRemoveCallback = DrawRemoveVertex
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

        private void DrawAddVertex(ReorderableList list)
        {
            This.graph.AddVertex();
        }

        private void DrawRemoveVertex(ReorderableList list)
        {
            for (var i = list.selectedIndices.Count - 1; i >= 0; i--)
            {
                This.graph.RemoveVertex(This.graph.vertices[list.selectedIndices[i]]);
            }
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
            var shift = Event.current.shift;
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    var camera = SceneView.currentDrawingSceneView.camera;
                    var position = camera.transform.position;
                    var distanceFromCam = new Vector3(position.x, position.y, 0);
                    var plane = new Plane(Vector3.forward, distanceFromCam);
                    var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    var worldPos = Vector3.zero;
                    if (plane.Raycast(ray, out var enter))
                    {
                        worldPos = ray.GetPoint(enter);
                    }

                    var nearest = (from e in This.graph.vertices select (dist: (e.Position - worldPos).sqrMagnitude, elem: e) into tuple orderby tuple.dist select tuple.elem).FirstOrDefault();
                    if (nearest != null)
                    {
                        This.CurrentVertex = nearest;
                    }

                    if (Event.current.button == 1 && shift)
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("New Node"), false, CreateVertex, worldPos);

                        if (This.CurrentVertex != null)
                        {
                            menu.AddItem(new GUIContent($"Delete {This.CurrentVertex.Name}"), false, DeleteVertex, This.CurrentVertex);
                        }

                        menu.ShowAsContext();
                        Event.current.Use();
                    }

                    GUIUtility.hotControl = HotControl;
                    break;
                case EventType.MouseUp:
                    GUIUtility.hotControl = 0;
                    break;
            }

            EditorGUI.BeginChangeCheck();
            foreach (var vertex in This.graph.vertices)
            {
                var style = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 16,
                    padding = new RectOffset(2, 2, 2, 2),
                    normal =
                    {
                        background = GetLabelTexture(vertex.Type),
                    }
                };

                Handles.Label(vertex.Position, new GUIContent(vertex.Name), style);
                if (shift)
                {
                    vertex.Position = Handles.PositionHandle(vertex.Position, Quaternion.identity);
                }
            }


            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(This);
            }
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
