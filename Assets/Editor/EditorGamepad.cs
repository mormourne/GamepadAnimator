#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public class EditorGamepad
{
    static EditorGamepad()
        => EditorApplication.update += Update;

    static void Update()
    {
        var gamepad = Gamepad.current;

        if (gamepad == null) return;

        if (!EditorApplication.isPlaying && gamepad.rightTrigger.isPressed)
        {
            if (Selection.activeGameObject != null && Selection.activeGameObject.scene != null)
            {
                float speed = 0.1f;
                Vector2 leftStick = gamepad.leftStick.ReadValue();
                Vector2 rightStick = gamepad.rightStick.ReadValue();
                Quaternion rotation = Quaternion.Euler(-leftStick.x * speed, rightStick.y * speed, leftStick.y * speed);
                Selection.activeGameObject.transform.localRotation *= rotation;
                SceneView.lastActiveSceneView.pivot += new Vector3(leftStick.x, rightStick.y, leftStick.y) * speed;
                //Selection.activeGameObject.transform.position += new Vector3(leftStick.x, rightStick.y, leftStick.y) * speed;
            }
            Debug.Log(gamepad.leftStick.ReadValue()); 
            //EditorApplication.EnterPlaymode();
        }
    }
}

#endif
