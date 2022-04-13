using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelDesigner
{
    [ExecuteAlways]
    public class EdgeEditor : MonoBehaviour
    {
        private const float ConeSize = 0.5f;

        public NodeEditor from;
        public NodeEditor to;
        public EdgeType type;
        public float bias;

        // private readonly Gradient _white = InitGradient(Color.white);
        // private readonly Gradient _green = InitGradient(Color.green);
        // private readonly Gradient _yellow = InitGradient(Color.yellow);
        // private readonly Gradient _red = InitGradient(Color.red);

        private LineRenderer _lr;
        private Transform _cone;
        private Transform _dot;

        private void OnEnable()
        {
            _lr = GetComponent<LineRenderer>();
            _cone = transform.GetChild(0);
            _dot = transform.GetChild(1);
        }

        private void Update()
        {
            if (from == null || to == null)
            {
                _lr.startWidth = 0f;
                _lr.endWidth = 0f;
                return;
            }

            var positions = new List<Vector3>();
            var pStart = from.transform.position;
            var pEnd = to.transform.position;
            var dir = (pEnd - pStart).normalized;
            var start = pStart + dir * (from.weight * 0.5f);
            var end = pEnd - dir * (to.weight * 0.5f);
            var rotated = Quaternion.Euler(0, 90, 0) * dir;
            rotated *= bias;
            var mid = start + (end - start) * 0.5f + rotated;
            positions.Add(start);
            positions.Add(mid);
            positions.Add(end);
            var coneDir1 = (mid - start).normalized;
            var coneDir2 = (end - mid).normalized;
            var coneStart = start + coneDir1 * ConeSize;
            var coneEnd = end - coneDir2 * ConeSize;
            var targetRot = Quaternion.LookRotation(coneDir2, Vector3.up);
            switch (type)
            {
                case EdgeType.Undirected:
                    _cone.gameObject.SetActive(false);
                    _dot.gameObject.SetActive(false);
                    break;
                case EdgeType.Directed:
                    _cone.position = coneEnd;
                    _cone.rotation = targetRot;
                    _cone.gameObject.SetActive(true);
                    _dot.gameObject.SetActive(false);
                    break;
                case EdgeType.ShortCut:
                    _cone.position = mid;
                    _cone.rotation = targetRot;
                    _cone.gameObject.SetActive(true);
                    _dot.gameObject.SetActive(false);
                    break;
                case EdgeType.Mechanism:
                    _cone.position = coneEnd;
                    _cone.rotation = targetRot;
                    _dot.position = coneStart;
                    _cone.gameObject.SetActive(true);
                    _dot.gameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _lr.startWidth = 0.1f;
            _lr.endWidth = 0.1f;
            _lr.positionCount = positions.Count;
            _lr.SetPositions(positions.ToArray());

            name = $"{from.name}-{to.name}";
        }

        // private static Gradient InitGradient(Color color)
        // {
        //     var gradient = new Gradient();
        //     var colorKey = new GradientColorKey[2];
        //     colorKey[0].color = Color.white;
        //     colorKey[0].time = 0f;
        //     colorKey[1].color = color;
        //     colorKey[1].time = 1f;
        //     var alphaKey = new GradientAlphaKey[2];
        //     alphaKey[0].alpha = 1f;
        //     alphaKey[0].time = 0f;
        //     alphaKey[1].alpha = 1f;
        //     alphaKey[1].time = 1f;
        //     gradient.SetKeys(colorKey, alphaKey);
        //     return gradient;
        // }
    }
}
