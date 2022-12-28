using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;


public class EditorGamepadGO : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
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
                Quaternion rotation = Quaternion.Euler(leftStick.y * speed, rightStick.y * speed, -leftStick.x * speed);
                //Selection.activeGameObject.transform.localRotation *= rotation;
                //Selection.activeGameObject.transform.rotation *= rotation;
                Selection.activeGameObject.transform.Rotate(new Vector3(-leftStick.y, rightStick.y, leftStick.x), speed, Space.Self);
                //SceneView.lastActiveSceneView.pivot += new Vector3(leftStick.x, rightStick.y, leftStick.y) * speed;
                Gizmos.DrawCube(Vector3.zero, Vector3.one);

            }
            //Debug.Log(gamepad.leftStick.ReadValue()); 

        }
    }
}
