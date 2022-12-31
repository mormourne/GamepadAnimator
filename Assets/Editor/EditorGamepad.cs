#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public class EditorGamepad
{
    static Gamepad gamepad;

    private static double lastFrameTime;
    private static double deltaTime;

    static GameObject selection;
    static GameObject selectionRoot;

    const float leftStickSqrMagnitudeDeadzone = 0.1f;
    const float rightStickSqrMagnitudeDeadzone = 0.1f;

    const double changeSelectionCooldown = 0.2f;
    static double changeSelectionNextAvailability = 0f;

    private static bool isRotatingSelection = false;
    private static Vector3 selectionAxisForward;
    private static Vector3 selectionAxisUp;
    private static Quaternion selectionStartRotation;

    private static bool isRotatingCamera = false;
    private static Vector3 cameraAxisUp;
    private static Vector3 cameraAxisRight;
    private static Quaternion cameraStartRotation;

    static EditorGamepad()
    {
        EditorApplication.update += Update;
        lastFrameTime = EditorApplication.timeSinceStartup;
        deltaTime = 0f;
    }
        

    

    static void Update()
    {
        if (EditorApplication.isPlaying) return;

        gamepad = Gamepad.current;
        if (gamepad == null) return;

        if (Selection.activeGameObject == null || Selection.activeGameObject.scene == null) return;

        deltaTime = EditorApplication.timeSinceStartup - lastFrameTime;
        lastFrameTime = EditorApplication.timeSinceStartup;

        if (selection != Selection.activeGameObject)
        {
            OnSelectionChanged();
        }

        if (CheckRotatingSelection())
        {
            RotateSelection();
        }
        else if (CheckRotatingCamera())
        {
            if (CheckArrowKeys())
            {
                return;
            }
            RotateCamera();
        }
        else if (gamepad.leftStickButton.isPressed)
        {
            ChangeSelectionFromHierarchy();
        }
        else if (gamepad.rightShoulder.isPressed || gamepad.leftShoulder.isPressed)
        {
            TryRewindAnimation(gamepad.rightShoulder.isPressed);
        }
    }


    private static bool TryRewindAnimation(bool forward)
    {
        if (!CheckAnimationWindowOpen(out AnimationWindow animationWindow))
        {
            return false;
        }
        animationWindow.time += (forward ? 1f : -1f) * (float) deltaTime;
        return true;
    }

    private static bool CheckAnimationWindowOpen(out AnimationWindow animationWindow)
    {
        if (!EditorWindow.HasOpenInstances<AnimationWindow>())
        {
            animationWindow = null;
            return false;
        }

        animationWindow = EditorWindow.GetWindow<AnimationWindow>();
        return true;
    }
  
    private static bool CheckRotatingSelection()
    {
        if (gamepad.rightTrigger.isPressed)
        {
            if (!isRotatingSelection)
            {
                isRotatingSelection = true;
                selectionAxisForward = selection.transform.InverseTransformDirection(SceneView.lastActiveSceneView.camera.transform.forward);
                selectionAxisUp = selection.transform.InverseTransformDirection(SceneView.lastActiveSceneView.camera.transform.up);
                selectionStartRotation = selection.transform.rotation;
            }
            else
            {
                Undo.RecordObject(selection.transform, "Rotate " + selection.name);
            }
            return true;
        }
        else
        {
            if (isRotatingSelection)
            {
                isRotatingSelection = false;
            }
            return false;
        }
    }



    private static bool CheckRotatingCamera()
    {
        if (gamepad.leftTrigger.isPressed)
        {
            if (!isRotatingCamera)
            {
                isRotatingCamera = true;
                CameraResetRotationAndAxes();
            }
            return true;
        }
        else
        {
            if (isRotatingCamera)
            {
                isRotatingCamera = false;
            }
            return false;
        }
    }

    private static void CameraResetRotationAndAxes()
    {
        cameraAxisUp = SceneView.lastActiveSceneView.camera.transform.InverseTransformDirection(Vector3.up);
        cameraAxisRight = Vector3.right;
        cameraStartRotation = SceneView.lastActiveSceneView.camera.transform.rotation;
    }


    private static bool CheckArrowKeys()
    {
        bool snapRotation = false;
        Quaternion rotation = Quaternion.identity;
        if (gamepad.dpad.up.isPressed)
        {
            snapRotation = true;
            rotation = Quaternion.Euler(90f, 180f, 0f);
        }
        else if (gamepad.dpad.down.isPressed)
        {
            snapRotation = true;
            rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else if (gamepad.dpad.left.isPressed)
        {
            snapRotation = true;
            rotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else if (gamepad.dpad.right.isPressed)
        {
            snapRotation = true;
            rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        if (snapRotation)
        {
            SceneView.lastActiveSceneView.rotation = rotation;
            CameraResetRotationAndAxes();
            return true;
        }

        return false;
    }



    private static void ChangeSelectionFromHierarchy() 
    {
        if (gamepad.buttonNorth.isPressed && CheckChangeSelectionCooldown())
        {
            Transform selectionParent = selection.transform.parent;
            if (selectionParent != null)
            {
                Selection.activeGameObject = selectionParent.gameObject;
            }
        }
        else if (gamepad.buttonSouth.isPressed && selection.transform.childCount > 0 && CheckChangeSelectionCooldown())
        {
            Transform selectionFirstChild = selection.transform.GetChild(0);
            Selection.activeGameObject = selectionFirstChild.gameObject;
        }
        else
        {
            bool west = gamepad.buttonWest.isPressed;
            bool east = gamepad.buttonEast.isPressed;
            if (!east && !west)
            {
                return;
            }
            Transform parent = selection.transform.parent;
            if (parent != null && parent.childCount > 1 && CheckChangeSelectionCooldown())
            {
                int childCount = parent.childCount;
                int currentIndex = selection.transform.GetSiblingIndex();
                int newIndex = Mod(west ? currentIndex - 1 : currentIndex + 1, childCount);
                Selection.activeGameObject = parent.GetChild(newIndex).gameObject;
            }
        }
    }

    private static bool CheckChangeSelectionCooldown()
    {
        if (EditorApplication.timeSinceStartup > changeSelectionNextAvailability)
        {
            changeSelectionNextAvailability = EditorApplication.timeSinceStartup + changeSelectionCooldown;
            return true;
        }
        return false;
    }

    private static void OnSelectionChanged()
    {
        selection = Selection.activeGameObject;
        selectionRoot = selection.transform.root.gameObject;
        SceneView.lastActiveSceneView.pivot = selection.transform.position;
    }

    

    private static void RotateSelection()
    {
        GetStickInputs(out Vector2 leftStick, out Vector2 rightStick);

        float leftDegrees = leftStick == Vector2.zero ? 0f : Mathf.Atan2(leftStick.y, leftStick.x) * Mathf.Rad2Deg - 90f;
        float rightDegrees = rightStick == Vector2.zero ? 0f : -Mathf.Atan2(rightStick.y, rightStick.x) * Mathf.Rad2Deg + 90f;

        selection.transform.rotation = selectionStartRotation 
            * Quaternion.AngleAxis(leftDegrees, selectionAxisForward) 
            * Quaternion.AngleAxis(rightDegrees, selectionAxisUp);
    }

    private static void RotateCamera()
    {
        GetStickInputs(out Vector2 leftStick, out Vector2 rightStick);

        float leftDegrees = leftStick == Vector2.zero ? 0f : -Mathf.Atan2(leftStick.y, leftStick.x) * Mathf.Rad2Deg - 90f;
        float rightDegrees = Mathf.Atan2(rightStick.y, rightStick.x) * Mathf.Rad2Deg;

        Quaternion rotation = cameraStartRotation * Quaternion.AngleAxis(leftDegrees, cameraAxisUp) * Quaternion.AngleAxis(rightDegrees, cameraAxisRight);
        rotation *= Quaternion.Euler(0f, 0f, -rotation.eulerAngles.z);
        
        SceneView scene = SceneView.lastActiveSceneView;
        scene.LookAt(scene.pivot, scene.cameraDistance, rotation);
    }

   
    private static void GetStickInputs(out Vector2 leftStick, out Vector2 rightStick, bool deadzoned = true) 
    {
        leftStick = gamepad.leftStick.ReadValue();
        rightStick = gamepad.rightStick.ReadValue();
        if (deadzoned)
        {
            leftStick = leftStick.sqrMagnitude < leftStickSqrMagnitudeDeadzone ? Vector2.zero : leftStick;
            rightStick = rightStick.sqrMagnitude < rightStickSqrMagnitudeDeadzone ? Vector2.zero : rightStick;
        }
        
    }

   

    static int Mod(int k, int n) 
    {
        int r = k % n;
        return r < 0 ? r + n : r;
    }
}

#endif
 