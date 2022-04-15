using System;
using UnityEditor;
using UnityEngine;

namespace LevelDesigner
{
    [RequireComponent(typeof(GraphEditor))]
    public class GraphHandles : MonoBehaviour
    {
    }

    [CustomEditor(typeof(GraphHandles))]
    public class GraphHandlesEditor : Editor
    {
        // private Vector3?[] _handlePos;

        private void OnSceneGUI()
        {
            var ge = ((GraphHandles) target).GetComponent<GraphEditor>();
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = {textColor = new Color(1f, 0.7f, 0f)},
                alignment = TextAnchor.MiddleCenter,
            };

            var len = ge.nodes.childCount;
            // if (_handlePos == null)
            // {
            //     _handlePos = new Vector3?[len];
            // }
            //
            // if (_handlePos.Length != len)
            // {
            //     Array.Resize(ref _handlePos, len);
            // }

            for (var i = 0; i < len; i++)
            {
                var node = ge.nodes.GetChild(i);
                var pos = node.position;
                Handles.Label(pos, node.name, style);
                // if (_handlePos[i] == null)
                // {
                //     _handlePos[i] = pos;
                // }
                //
                // _handlePos[i] = Handles.PositionHandle(_handlePos[i].Value, Quaternion.identity);
            }
        }
    }
}
