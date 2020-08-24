using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

public static class ucCoordToUE
{
    public static Vector3 F3(Vector3 v3)
    {
        return Quaternion.AngleAxis(90, Vector3.right) * v3;
    }

    public static Vector4 F4(Vector4 v4)
    {
        return Quaternion.AngleAxis(90, Vector3.right) * v4;
    }
}

public static class ucCoordToUnity
{
    public static Vector3 F3(Vector3 v3)
    {
        return Quaternion.AngleAxis(-90, Vector3.right) * v3;
    }

    public static Vector4 F4(Vector4 v4)
    {
        return Quaternion.AngleAxis(-90, Vector3.right) * v4;
    }
}
