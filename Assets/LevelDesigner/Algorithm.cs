using System;
using System.Collections.Generic;

namespace LevelDesigner
{
    public class Algorithm
    {
        public static List<List<T>> GetCombinations<T>(List<T> list, int n)
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
    }
}
