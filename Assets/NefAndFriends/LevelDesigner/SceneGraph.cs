using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace NefAndFriends.LevelDesigner
{
    public class SceneGraph : MonoBehaviour
    {
        public Graph graph;
    }

    [CustomEditor(typeof(SceneGraph))]
    public class SceneGraphEditor : Editor
    {
        private SceneGraph This => target as SceneGraph;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Read"))
            {
                This.graph = Graph.Parse(File.ReadAllText("Assets/Levels/simple.txt"));
            }
        }

        private void OnSceneGUI()
        {
            if (Event.current.button == 1)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("New Node"), false, CreateNode, 1);
                    menu.ShowAsContext();

                    Event.current.Use();
                }
            }
        }

        private void CreateNode(object userData)
        {
            Debug.Log($"{userData}");
        }
    }
}
