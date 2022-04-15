using System;
using UnityEngine;

namespace LevelDesigner
{
    [ExecuteAlways]
    public class EdgeEditor : MonoBehaviour
    {
        private const float ConeSize = 0.4f;
        private const float DotSize = 0.2f;
        private const int Points = 20;

        public NodeEditor from;
        public NodeEditor to;
        public EdgeType type;
        public float strength;
        public float angle;

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

            var pStart = from.transform.position;
            var pEnd = to.transform.position;
            var ws = from.weight * 0.5f;
            var we = to.weight * 0.5f;
            var qAngle = Quaternion.Euler(0f, angle, 0f);
            var start = pStart;
            var end = pEnd;
            Vector3 p1, p2;

            if (from == to)
            {
                strength = Mathf.Max(strength, 2f);

                var dir = qAngle * Vector3.forward;
                var r = dir * ws;
                start += r;
                end = start;

                var p1VNorm = Quaternion.AngleAxis(45, Vector3.up) * dir;
                var p1V = p1VNorm * (ws * strength);
                p1 = start + p1V;

                var p2VNorm = Quaternion.AngleAxis(-45, Vector3.up) * dir;
                var p2V = p2VNorm * (ws * strength);
                p2 = start + p2V;
            }
            else
            {
                strength = 2f;

                var v = end - start;
                var d = v.magnitude;

                var dir = v.normalized;
                var pLen = Math.Max(d - ws - we, 0f) * 0.5f;

                var p1VNorm = qAngle * dir;
                var p1V = p1VNorm * ((pLen + ws) / Mathf.Cos(angle * Mathf.Deg2Rad));
                p1 = start + p1V;
                p2 = p1;
            }

            _lr.positionCount = Points;
            var positions = new Vector3[Points];
            for (var i = 0; i < Points; i++)
            {
                positions[i] = MathUtils.BezierCubic(start, end, p1, p2, (float) i / (Points - 1));
            }

            var coneIndex = Points - 2;
            var dotIndex = 0;
            if (type == EdgeType.ShortCut)
            {
                coneIndex = Points / 2 - 3;
            }
            else
            {
                for (var i = Points - 2; i >= 0; i--)
                {
                    var p = positions[i];
                    if (Vector3.Distance(p, pEnd) <= we + ConeSize)
                        continue;

                    coneIndex = i;
                    break;
                }

                for (var i = 0; i < Points - 1; i++)
                {
                    var p = positions[i];
                    if (Vector3.Distance(p, pStart) <= ws + DotSize)
                        continue;

                    dotIndex = i;
                    break;
                }
            }

            var conePos = positions[coneIndex];
            var coneRot = Quaternion.LookRotation(positions[coneIndex + 1] - conePos, Vector3.up);
            var dotPos = positions[dotIndex];

            _lr.SetPositions(positions);

            switch (type)
            {
                case EdgeType.Undirected:
                    _cone.gameObject.SetActive(false);
                    _dot.gameObject.SetActive(false);
                    break;
                case EdgeType.Directed:
                    _cone.position = conePos;
                    _cone.rotation = coneRot;
                    _cone.gameObject.SetActive(true);
                    _dot.gameObject.SetActive(false);
                    break;
                case EdgeType.ShortCut:
                    _cone.position = conePos;
                    _cone.rotation = coneRot;
                    _cone.gameObject.SetActive(true);
                    _dot.gameObject.SetActive(false);
                    break;
                case EdgeType.Mechanism:
                    _cone.position = conePos;
                    _cone.rotation = coneRot;
                    _dot.position = dotPos;
                    _cone.gameObject.SetActive(true);
                    _dot.gameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _lr.startWidth = 0.1f;
            _lr.endWidth = 0.1f;

            name = $"{from.name}-{to.name}";
        }
    }
}
