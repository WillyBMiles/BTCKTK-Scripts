using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static Vector3 Perpendicular(this Vector3 input, bool alt = false)
    {
        if (alt)
            return new Vector3(input.y, -input.x, 0f);
        else
            return new Vector3(-input.y, input.x, 0f);
    }
}
