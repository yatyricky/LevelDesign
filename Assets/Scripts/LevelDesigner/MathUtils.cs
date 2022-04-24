using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelDesigner
{
    public class MathUtils
    {
        public static List<List<T>> GetCombinations<T>(IList<T> list, int n)
        {
            if (n <= 0)
            {
                return new List<List<T>>();
            }

            var len = list.Count;
            n = Math.Min(len, n);
            var ptrs = new List<int>();
            for (var i = 0; i < n; i++)
            {
                ptrs.Add(i);
            }

            var result = new List<List<T>>();
            while (true)
            {
                var one = new List<T>();
                for (var i = 0; i < n; i++)
                {
                    var p = ptrs[i];
                    one.Add(list[p]);
                }

                result.Add(one);
                // cant move
                if (ptrs[0] >= len - n)
                {
                    break;
                }

                // move ptr
                for (var i = n - 1; i >= 0; i--)
                {
                    if (ptrs[i] < len - (n - i))
                    {
                        ptrs[i] = ptrs[i] + 1;
                        for (var j = i + 1; j < n; j++)
                        {
                            ptrs[j] = ptrs[i] + j - i;
                        }

                        break;
                    }
                }
            }

            return result;
        }

        public static int Factorial(int number)
        {
            int i;
            var fact = number;
            for (i = number - 1; i > 1; i--)
            {
                fact *= i;
            }

            return fact;
        }

        public static float BezierQuadratic(float start, float end, float tangent, float time)
        {
            var t1 = 1f - time;
            return t1 * t1 * start + 2 * t1 * time * tangent + time * time * end;
        }

        public static Vector3 BezierQuadratic(Vector3 start, Vector3 end, Vector3 tangent, float t)
        {
            return new Vector3(BezierQuadratic(start.x, end.x, tangent.x, t), BezierQuadratic(start.y, end.y, tangent.y, t), BezierQuadratic(start.z, end.z, tangent.z, t));
        }

        public static float BezierCubic(float start, float end, float startTangent, float endTangent, float time)
        {
            var t1 = 1f - time;
            return t1 * t1 * t1 * start + 3 * t1 * t1 * time * startTangent + 3 * t1 * time * time * endTangent + time * time * time * end;
        }

        public static Vector3 BezierCubic(Vector3 start, Vector3 end, Vector3 startTangent, Vector3 endTangent, float time)
        {
            return new Vector3(BezierCubic(start.x, end.x, startTangent.x, endTangent.x, time), BezierCubic(start.y, end.y, startTangent.y, endTangent.y, time), BezierCubic(start.z, end.z, startTangent.z, endTangent.z, time));
        }
    }
}
