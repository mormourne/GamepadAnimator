#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public class EditorGamepad
{
    static Gamepad gamepad;

   

    static GameObject selection;
    static GameObject selectionRoot;

    const float selectionSpeed = 0.005f;
    const float selectionRotationSpeed = 0.5f;
    const float cameraSpeed = 0.05f;
    const float cameraRotationSpeed = 0.5f;
    const float leftStickDeadzone = 0.25f;
    const float rightStickDeadzone = 0.25f;

    const double changeSelectionCooldown = 0.2f;
    static double changeSelectionNextAvailability = 0f;

    static EditorGamepad()
        => EditorApplication.update += Update;

    

    static void Update()
    {
        gamepad = Gamepad.current;
        if (gamepad == null) return;

        if (!EditorApplication.isPlaying)
        {
            if (Selection.activeGameObject != null && Selection.activeGameObject.scene != null)
            {
                if (selection != Selection.activeGameObject)
                {
                    OnSelectionChanged();
                }

                //if (gamepad.rightTrigger.isPressed)
                if (CheckRightTrigger())
                {
                    RotateSelection();
                }
                else if (CheckLeftTrigger())
                {
                    RotateCamera();
                }
                else if (gamepad.rightShoulder.isPressed)
                {
                    ChangeSelectionFromHierarchy();
                }


            }


        }
    }

    private static bool isRightTriggerPressed = false;
    private static Vector3 rotationAxisForward;
    private static Vector3 rotationAxisRight;
    private static Quaternion selectionStartRotation;
    private static bool CheckRightTrigger()
    {
        if (gamepad.rightTrigger.isPressed)
        {
            if (!isRightTriggerPressed)
            {
                isRightTriggerPressed = true;
                //rotationAxis = SceneView.lastActiveSceneView.camera.transform.forward;
                rotationAxisForward = selection.transform.InverseTransformDirection(SceneView.lastActiveSceneView.camera.transform.forward);
                rotationAxisRight = selection.transform.InverseTransformDirection(SceneView.lastActiveSceneView.camera.transform.right);
                Debug.Log(rotationAxisForward);
                selectionStartRotation = selection.transform.rotation;
            }
            return true;
        }
        else
        {
            if (isRightTriggerPressed)
            {
                isRightTriggerPressed = false;
            }
            return false;
        }
    }


    private static bool isLeftTriggerPressed = false;
    private static Vector3 cameraAxisUp;
    private static Vector3 cameraAxisRight;
    private static Quaternion cameraStartRotation;

    private static bool CheckLeftTrigger()
    {
        if (gamepad.leftTrigger.isPressed)
        {
            if (!isLeftTriggerPressed)
            {
                isLeftTriggerPressed = true;
                cameraAxisUp = Vector3.up;
                cameraAxisRight = SceneView.lastActiveSceneView.camera.transform.right;
                cameraStartRotation = SceneView.lastActiveSceneView.camera.transform.rotation;
            }
            return true;
        }
        else
        {
            if (isLeftTriggerPressed)
            {
                isLeftTriggerPressed = false;
            }
            return false;
        }
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
        GetDeadzonedStickInputs(out Vector2 leftStick, out Vector2 rightStick);

        float leftDegrees = Mathf.Atan2(leftStick.y, leftStick.x) * Mathf.Rad2Deg;
        float rightDegrees = Mathf.Atan2(rightStick.y, rightStick.x) * Mathf.Rad2Deg;
        //Debug.Log(leftDegrees + " " + rightDegrees);
        selection.transform.rotation = selectionStartRotation * Quaternion.AngleAxis(leftDegrees, rotationAxisForward) * Quaternion.AngleAxis(rightDegrees, rotationAxisRight);
    }

    private static void RotateCamera()
    {
        GetDeadzonedStickInputs(out Vector2 leftStick, out Vector2 rightStick);

        float leftDegrees = Mathf.Atan2(leftStick.y, leftStick.x) * Mathf.Rad2Deg;
        float rightDegrees = Mathf.Atan2(rightStick.y, rightStick.x) * Mathf.Rad2Deg;
        //Debug.Log(leftDegrees + " " + rightDegrees);
        Quaternion rotation = cameraStartRotation * Quaternion.AngleAxis(leftDegrees, cameraAxisUp) * Quaternion.AngleAxis(rightDegrees, cameraAxisRight);
        SceneView scene = SceneView.lastActiveSceneView;
        scene.LookAt(scene.pivot, scene.cameraDistance, rotation);
    }

    /* private static void RotateCamera()
     {
         GetDeadzonedStickInputs(out Vector2 leftStick, out Vector2 rightStick);


         SceneView scene = SceneView.lastActiveSceneView;
         Transform editorCamera = SceneView.lastActiveSceneView.camera.transform;

         float distance = Mathf.Clamp(scene.cameraDistance - rightStick.y * cameraSpeed, 0f, 1000f);
         //Quaternion rotation = scene.rotation * Quaternion.Euler(rightStick.y * cameraRotationSpeed, -leftStick.x * cameraRotationSpeed, -rightStick.x * cameraRotationSpeed);
         //Quaternion rotation = scene.rotation * Quaternion.Euler(rightStick.y * cameraRotationSpeed, -leftStick.x * cameraRotationSpeed, 0f);
         //Quaternion rotation = Quaternion.Euler(-leftStick.y * cameraRotationSpeed, -leftStick.x * cameraRotationSpeed, 0f) * scene.rotation;
         //rotation *= Quaternion.Euler(0f, 0f, -rotation.eulerAngles.z);
         Quaternion rotation = scene.rotation
             * Quaternion.Euler(leftStick.y * cameraRotationSpeed, -leftStick.x * cameraRotationSpeed, 0f);
         rotation *= Quaternion.Euler(0f, 0f, -rotation.eulerAngles.z);

         //Debug.Log(scene.cameraDistance + " " + distance);

         scene.LookAt(scene.pivot, distance, rotation);
         //scene.rotation = rotation;
         //scene.size = SceneViewExtensions.GetSizeFromDistance(scene, distance);

         //scene.pivot += (editorCamera.forward * leftStick.y + editorCamera.right * leftStick.x) * cameraSpeed;
         //scene.rotation *= Quaternion.Euler(0f, rightStick.x * cameraRotationSpeed, 0f);


         //editorCamera.position += (editorCamera.forward * leftStick.y + editorCamera.right * leftStick.x) * cameraSpeed;
         //scene.rotation *= Quaternion.Euler((editorCamera.right * rightStick.y + editorCamera.up * rightStick.x) * cameraRotationSpeed);
         //editorCamera.rotation *= Quaternion.Euler((editorCamera.right * rightStick.y + editorCamera.up * rightStick.x) * cameraRotationSpeed);

     }*/

    private static void GetDeadzonedStickInputs(out Vector2 leftStick, out Vector2 rightStick)
    {
        leftStick = gamepad.leftStick.ReadValue();
        rightStick = gamepad.rightStick.ReadValue();
        leftStick = new Vector2(Mathf.Abs(leftStick.x) > leftStickDeadzone ? leftStick.x : 0f, Mathf.Abs(leftStick.y) > leftStickDeadzone ? leftStick.y : 0f);
        rightStick = new Vector2(Mathf.Abs(rightStick.x) > rightStickDeadzone ? rightStick.x : 0f, Mathf.Abs(rightStick.y) > rightStickDeadzone ? rightStick.y : 0f);
    }

   

    static int Mod(int k, int n) 
    {
        int r = k % n;
        return r < 0 ? r + n : r;
    }
}

#endif
 