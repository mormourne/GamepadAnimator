#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public class EditorGamepad
{
    static Transform selection;
    static GameObject editorGamepadForward;

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

        var gamepad = Gamepad.current;

        if (gamepad == null) return;

        if (!EditorApplication.isPlaying && gamepad.rightTrigger.isPressed)
        {
            if (Selection.activeGameObject != null && Selection.activeGameObject.scene != null)
            {
                float speed = 0.1f;
                Vector2 leftStick = gamepad.leftStick.ReadValue();
                Vector2 rightStick = gamepad.rightStick.ReadValue();
                Quaternion rotation = Quaternion.Euler(leftStick.y * speed, rightStick.y * speed, -leftStick.x * speed);
                //Selection.activeGameObject.transform.localRotation *= rotation;
                //Selection.activeGameObject.transform.rotation *= rotation;
                Selection.activeGameObject.transform.Rotate(new Vector3(-leftStick.y, rightStick.y, leftStick.x), speed, Space.Self);
                //SceneView.lastActiveSceneView.pivot += new Vector3(leftStick.x, rightStick.y, leftStick.y) * speed;
                editorGamepadForward.transform.position = Selection.activeGameObject.transform.position + Selection.activeGameObject.transform.forward * 0.1f;
                editorGamepadForward.transform.forward = Selection.activeGameObject.transform.forward;
                editorGamepadForward.transform.up = Selection.activeGameObject.transform.up;
            }
            //Debug.Log(gamepad.leftStick.ReadValue()); 

        }
    }
}

#endif
