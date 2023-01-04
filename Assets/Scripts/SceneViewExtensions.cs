using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class SceneViewExtensions
{
    // Looks at the pivot point from the specified *distance* and rotation.
    public static void LookAtBasedOnDistance(this SceneView sceneView, in Vector3 pivot,
        float distance, in Quaternion rotation, bool instant = true)
    {
        sceneView.LookAt(pivot, rotation,
            GetSizeFromDistance(sceneView, distance), false, instant);
    }
        

    // From SceneView.cs / GetPerspectiveCameraDistance().
    public static float GetSizeFromDistance(SceneView sceneView, float distance) =>
        distance * Mathf.Sin(sceneView.camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
}
