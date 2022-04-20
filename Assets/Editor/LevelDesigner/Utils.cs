using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace LevelDesignerEditor
{
    public struct Ring
    {
        public Vector2 P;
        public float R0;
        public float R1;

        public Ring(Vector2 p, float r0, float r1)
        {
            P = p;
            R0 = r0;
            R1 = r1;
        }

        public override string ToString()
        {
            return $"Circle[{P};{R0};{R1}]";
        }
    }

    public struct Capsule
    {
        public Vector2 P1;
        public Vector2 P2;
        public float R;

        public Capsule(Vector2 p1, Vector2 p2, float r)
        {
            P1 = p1;
            P2 = p2;
            R = r;
        }

        public override string ToString()
        {
            return $"Capsule[{P1};{P2};{R}]";
        }
    }

    public struct Circle
    {
        public Vector2 P;
        public float R;

        public Circle(Vector2 p, float r)
        {
            P = p;
            R = r;
        }

        public override string ToString()
        {
            return $"Circle[{P};{R}]";
        }
    }

    public class Utils
    {
        public static Vector2 GetDOMLocalPosition(VisualElement ve)
        {
            var style = ve.style;
            return new Vector2(style.left.value.value, style.top.value.value);
        }

        public static Vector2 GetRelativePosition(VisualElement ve, string targetName)
        {
            var curr = ve;
            var pos = new Vector2();
            while (curr != null && curr.name != targetName)
            {
                pos += GetDOMLocalPosition(curr);
                curr = curr.parent;
            }

            return pos;
        }

        public static Vector2 GetCanvasPosition(VisualElement ve)
        {
            return GetRelativePosition(ve, "canvas");
        }

        public static bool CapsuleCircle(Capsule capsule, Circle circle)
        {
            var rs = (capsule.R + circle.R) * (capsule.R + circle.R);
            var vw = capsule.P2 - capsule.P1;
            var vws2 = vw.sqrMagnitude;
            var t = Mathf.Clamp01(Vector2.Dot(circle.P - capsule.P1, vw) / vws2);
            var proj = vw * t + capsule.P1;
            var res = (proj - circle.P).sqrMagnitude <= rs;
            // Debug.Log($"CapsuleCircle {capsule} {circle} yields {res}");
            return res;
        }

        public static bool CapsuleContains(Capsule capsule, Vector2 point)
        {
            var rs = capsule.R * capsule.R;
            var vw = capsule.P2 - capsule.P1;
            var vws2 = vw.sqrMagnitude;
            var t = Mathf.Clamp01(Vector2.Dot(point - capsule.P1, vw) / vws2);
            var proj = vw * t + capsule.P1;
            var res = (proj - point).sqrMagnitude <= rs;
            // Debug.Log($"CapsuleCircle {capsule} {circle} yields {res}");
            return res;
        }

        public static bool RingContains(Ring ring, Vector2 point)
        {
            var dir = ring.P - point;
            var dist = dir.magnitude;
            return ring.R0 <= dist && dist <= ring.R1;
        }

        // public static bool RectCircle()

        public static Vector2 World2Canvas(Vector2 vec)
        {
            return new Vector2(vec.x, -vec.y) * 50f;
        }

        public static Vector2 Canvas2World(Vector2 vec)
        {
            return new Vector2(vec.x, -vec.y) * 0.02f;
        }

        public static void BringDOMToFront(VisualElement DOM)
        {
            var parent = DOM.parent;
            var siblings = parent.Children().ToList();
            var dict = new Dictionary<VisualElement, int>();
            for (var i = 0; i < parent.childCount; i++)
            {
                var item = siblings[i];
                var order = item == DOM ? siblings.Count : i;
                dict.Add(item, order);
            }

            parent.Sort((a, b) => dict[a] - dict[b]);
        }

        public static void ResizeArray<T>(ref T[] arr, int len)
        {
            if (arr.Length < len)
            {
                Array.Resize(ref arr, len);
            }
        }
    }
}
