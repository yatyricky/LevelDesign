using UnityEngine;
using UnityEngine.UIElements;

namespace LevelDesignerEditor
{
    public struct Capsule
    {
        public Vector2 P1;
        public Vector2 P2;
        public float R;

        public Capsule(Vector2 p1,Vector2 p2, float r)
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
    }
}
