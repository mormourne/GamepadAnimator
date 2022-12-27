#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public class EditorGamepad
{
    static Gamepad gamepad;

    static GameObject editorGamepadForward;
    static GameObject editorGamepadUp;
    static GameObject editorGamepadRoot;

    static GameObject selection;
    static GameObject selectionRoot;

    const float speed = 0.005f;
    const float rotationSpeed = 0.5f;
    const float leftStickDeadzone = 0.25f;
    const float rightStickDeadzone = 0.25f;

    const double changeSelectionCooldown = 0.2f;
    static double changeSelectionNextAvailability = 0f;

    static EditorGamepad()
        => EditorApplication.update += Update;

    

    static void Update()
    {
        if (!InitGizmos())
        {
            return;
        }

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

                if (gamepad.rightTrigger.isPressed)
                {
                    RotateSelection();
                }
                else if (gamepad.rightShoulder.isPressed)
                {
                    ChangeSelectionFromHierarchy();
                }
                
                
            }
            

        }
    }

    private static void ChangeSelectionFromHierarchy() //TODO add timer for cd
    {
        Debug.Log(Time.deltaTime);
        if (gamepad.buttonNorth.isPressed && CheckChangeSelectionCooldown())
        {
            Transform selectionParent = selection.transform.parent;
            if (selectionParent != null)
            {
                Selection.activeGameObject = selectionParent.gameObject;
            }
        }
        else if (gamepad.buttonEast.isPressed && selection.transform.childCount > 0 && CheckChangeSelectionCooldown())
        {
            Transform selectionFirstChild = selection.transform.GetChild(0);
            Selection.activeGameObject = selectionFirstChild.gameObject;
        }
        else
        {
            bool west = gamepad.buttonWest.isPressed;
            bool south = gamepad.buttonSouth.isPressed;
            if (!south && !west)
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
        editorGamepadRoot.transform.position = selection.transform.position;
        editorGamepadForward.transform.position = selection.transform.position + selection.transform.forward * 0.25f;
        editorGamepadUp.transform.position = selection.transform.position + selection.transform.up * 0.25f;
    }

    private static void RotateSelection()
    {
        Vector2 leftStick = gamepad.leftStick.ReadValue();
        Vector2 rightStick = gamepad.rightStick.ReadValue();
        leftStick = new Vector2(Mathf.Abs(leftStick.x) > leftStickDeadzone ? leftStick.x : 0f, Mathf.Abs(leftStick.y) > leftStickDeadzone ? leftStick.y : 0f);
        rightStick = new Vector2(Mathf.Abs(rightStick.x) > rightStickDeadzone ? rightStick.x : 0f, Mathf.Abs(rightStick.y) > rightStickDeadzone ? rightStick.y : 0f);


        Transform editorCamera = SceneView.lastActiveSceneView.camera.transform;
        editorGamepadForward.transform.position += (Vector3.ProjectOnPlane(editorCamera.TransformDirection(Vector3.right), Vector3.up) * leftStick.x
            + Vector3.ProjectOnPlane(editorCamera.TransformDirection(Vector3.forward), Vector3.up) * leftStick.y
            + Vector3.up * rightStick.y) * speed;
        editorGamepadRoot.transform.position = Selection.activeGameObject.transform.position;
        editorGamepadUp.transform.position = editorGamepadRoot.transform.position + Selection.activeGameObject.transform.up * 0.25f;
        editorGamepadUp.transform.RotateAround(Selection.activeGameObject.transform.position, Selection.activeGameObject.transform.forward, rightStick.x * rotationSpeed);
        Selection.activeGameObject.transform.LookAt(editorGamepadForward.transform, (editorGamepadUp.transform.position - Selection.activeGameObject.transform.position));
    }

    private static bool InitGizmos() //TODO instantiate instead of looking in scene
    {
        if (editorGamepadForward == null)
        {
            editorGamepadForward = GameObject.Find("EditorGamepadForward");
            if (editorGamepadForward == null)
            {
                return false;
            }
        }

        if (editorGamepadUp == null)
        {
            editorGamepadUp = GameObject.Find("EditorGamepadUp");
            if (editorGamepadUp == null)
            {
                return false;
            }
        }

        if (editorGamepadRoot == null)
        {
            editorGamepadRoot = GameObject.Find("EditorGamepadRoot");
            if (editorGamepadRoot == null)
            {
                return false;
            }
        }
        return true;
    }

    static int Mod(int k, int n) 
    {
        int r = k % n;
        return r < 0 ? r + n : r;
    }
}

#endif
 