using System.Collections.Generic;
using UnityEngine;

public static class Helpers
{
    private static Dictionary<int, double> FibMap = new Dictionary<int, double>();
    public static Vector3 ConvertPositionToWorldPosition(Vector2Int position, Vector2 worldSize) => new Vector3(position.x / worldSize.x, 0.5f, position.y / worldSize.y);

    public static double MagnitudeSquared(Vector2Int position1, Vector2Int position2) => System.Math.Pow(position1.x - position2.x, 2) + System.Math.Pow(position1.y - position2.y, 2);

    public static double GetFib(int n)
    {
        if (FibMap.ContainsKey(n))
        {
            return FibMap[n];
        }

        if (n > 1)
        {
            double n1 = 0;
            double n2 = 0;

            if (FibMap.ContainsKey(n - 2))
            {
                n2 = FibMap[n - 2];
            }
            else
            {
                n2 = GetFib(n - 2);
                FibMap.Add(n - 2, n2);
            }

            if (FibMap.ContainsKey(n - 1))
            {
                n1 = FibMap[n - 1];
            }
            else
            {
                n1 = GetFib(n - 1);
                FibMap.Add(n - 1, n1);
            }

            return n1 + n2;
        }

        if (n == 1)
        {
            return 1;
        }

        return 0;
    }
}
