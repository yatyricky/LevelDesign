using System.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace NefAndFriends.LevelDesignerEditor
{
    [CreateAssetMenu]
    public class Debugger : ScriptableObject
    {
        private static Debugger _asset;

        public static Debugger Asset
        {
            get
            {
                if (_asset == null)
                {
                    _asset = AssetDatabase.LoadAssetAtPath<Debugger>("Assets/NefAndFriends/Editor/window debugger.asset");
                }

                return _asset;
            }
        }

        public GraphWindow window;

        
        public int actionType;

        [OnInspectorGUI]
        private void ReadActionType()
        {
            if (window == null)
            {
                return;
            }

            var type = typeof(GraphWindow);
            var f = type.GetField("_currAct", BindingFlags.Instance | BindingFlags.NonPublic);
            actionType = (int)(GraphWindow.ActionType)f.GetValue(window);
        }
    }
}
