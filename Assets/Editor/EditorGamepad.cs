#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public class EditorGamepad
{
    static Transform selection;
    static GameObject editorGamepadForward;
    static GameObject editorGamepadUp;
    static GameObject editorGamepadRoot;

    static EditorGamepad()
        => EditorApplication.update += Update;

    

    static void Update()
    {
        if (editorGamepadForward == null)
        {
            editorGamepadForward = GameObject.Find("EditorGamepadForward");
            if (editorGamepadForward == null)
            {
                return;
            }
        }

        if (editorGamepadUp == null)
        {
            editorGamepadUp = GameObject.Find("EditorGamepadUp");
            if (editorGamepadUp == null)
            {
                return;
            }
        }

        if (editorGamepadRoot == null)
        {
            editorGamepadRoot = GameObject.Find("EditorGamepadRoot");
            if (editorGamepadRoot == null)
            {
                return;
            }
        }

        var gamepad = Gamepad.current;

        if (gamepad == null) return;

        if (!EditorApplication.isPlaying && gamepad.rightTrigger.isPressed)
        {
            if (Selection.activeGameObject != null && Selection.activeGameObject.scene != null)
            {
                float speed = 0.005f;
                float rotationSpeed = 0.5f;
                float leftStickDeadzone = 0.25f;
                float rightStickDeadzone = 0.25f;

                Vector2 leftStick = gamepad.leftStick.ReadValue();
                Vector2 rightStick = gamepad.rightStick.ReadValue();
                leftStick = new Vector2(Mathf.Abs(leftStick.x) > leftStickDeadzone ? leftStick.x : 0f, Mathf.Abs(leftStick.y) > leftStickDeadzone ? leftStick.y : 0f);
                rightStick = new Vector2(Mathf.Abs(rightStick.x) > rightStickDeadzone ? rightStick.x : 0f, Mathf.Abs(rightStick.y) > rightStickDeadzone ? rightStick.y : 0f);

                //Quaternion rotation = Quaternion.Euler(leftStick.y * speed, rightStick.y * speed, -leftStick.x * speed);
                //Selection.activeGameObject.transform.localRotation *= rotation;
                //Selection.activeGameObject.transform.rotation *= rotation;
                //Selection.activeGameObject.transform.Rotate(new Vector3(-leftStick.y, rightStick.y, leftStick.x), speed, Space.Self);
                //SceneView.lastActiveSceneView.pivot += new Vector3(leftStick.x, rightStick.y, leftStick.y) * speed;
                /*editorGamepadForward.transform.position = Selection.activeGameObject.transform.position + Selection.activeGameObject.transform.forward * 0.1f;
                editorGamepadForward.transform.forward = Selection.activeGameObject.transform.forward;
                editorGamepadForward.transform.up = Selection.activeGameObject.transform.up;*/
                Transform editorCamera = SceneView.lastActiveSceneView.camera.transform;
                editorGamepadForward.transform.position += (Vector3.ProjectOnPlane(editorCamera.TransformDirection(Vector3.right), Vector3.up)  * leftStick.x 
                    + Vector3.ProjectOnPlane(editorCamera.TransformDirection(Vector3.forward), Vector3.up) * leftStick.y
                    + Vector3.up * rightStick.y) * speed;
                editorGamepadRoot.transform.position = Selection.activeGameObject.transform.position;
                editorGamepadUp.transform.position = editorGamepadRoot.transform.position + Selection.activeGameObject.transform.up * 0.25f;
                editorGamepadUp.transform.RotateAround(Selection.activeGameObject.transform.position, Selection.activeGameObject.transform.forward, rightStick.x * rotationSpeed);
                Selection.activeGameObject.transform.LookAt(editorGamepadForward.transform, (editorGamepadUp.transform.position - Selection.activeGameObject.transform.position));
                //Selection.activeGameObject.transform.forward = (editorGamepadForward.transform.position - Selection.activeGameObject.transform.position).normalized;
                /*editorGamepadUp.transform.position = Selection.activeGameObject.transform.position + Selection.activeGameObject.transform.up * 0.25f;
                editorGamepadUp.transform.RotateAround(Selection.activeGameObject.transform.position, Selection.activeGameObject.transform.forward, rightStick.x * rotationSpeed);
                Selection.activeGameObject.transform.up = (editorGamepadUp.transform.position - Selection.activeGameObject.transform.position).normalized;*/
                //editorGamepadForward.transform.position += new Vector3(leftStick.x, rightStick.y, leftStick.y) * speed;
                //Selection.activeGameObject.transform.RotateAround(Selection.activeGameObject.transform.position, Selection.activeGameObject.transform.forward, rightStick.x * rotationSpeed);


                //editorGamepadForward.transform.position = Selection.activeGameObject.transform.position + Selection.activeGameObject.transform.forward * 0.25f;
            }
            Debug.Log(gamepad.leftStick.ReadValue() + " " + gamepad.rightStick.ReadValue()); 

        }
    }
}

#endif
