using System;
using TMPro;
using UnityEngine;

namespace LevelDesigner
{
    [ExecuteAlways]
    public class NodeEditor : MonoBehaviour
    {
        public float weight;
        public VertexType nodeType;

        public Material matNormal;
        public Material matStart;
        public Material matSave;
        public Material matBoss;

        private TextMeshPro _text;
        private MeshRenderer _renderer;

        private void OnEnable()
        {
            _text = transform.GetChild(0).GetComponent<TextMeshPro>();
            _renderer = GetComponent<MeshRenderer>();
        }

        private void Update()
        {
            transform.localScale = new Vector3(weight, weight, weight);
            _text.text = name;
            switch (nodeType)
            {
                case VertexType.Normal:
                    _renderer.material = matNormal;
                    break;
                case VertexType.Start:
                    _renderer.material = matStart;
                    break;
                case VertexType.Save:
                    _renderer.material = matSave;
                    break;
                case VertexType.Boss:
                    _renderer.material = matBoss;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
