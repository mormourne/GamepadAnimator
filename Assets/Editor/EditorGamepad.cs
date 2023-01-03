#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public class EditorGamepad
{
    static Gamepad gamepad;

    private static double lastFrameTime;
    private static float deltaTime;

    static GameObject selection;
    static GameObject selectionRoot;

    const float rootMovementSpeed = 1f;

    const float cameraZoomSpeed = 3f;

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
    private static float cameraDistance;
    private static Quaternion cameraRotation;

    private static bool isPressingRewind = false;
    private static bool isPressingRewindForward = false;
    private static float rewindingPressTimer = 0f;
    const float rewindingFastThreshold = 0.3f;

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

        if (!CheckSelection()) return;


        deltaTime = (float)(EditorApplication.timeSinceStartup - lastFrameTime);
        lastFrameTime = EditorApplication.timeSinceStartup;

        
        //GetCameraLocalXZAxes(out Vector3 xAxis, out Vector3 zAxis);

        if (CheckRotatingCamera())
        {
            if (!SnapRotationToPredefined())
            {
                ZoomCamera();
                RotateCamera();
                ApplyCameraRotation();
            }
            return;
        }

        if (CheckMoveRoot())
        {
            return;
        }

        if (CheckRotatingSelection())
        {
            RotateSelection();
            return;
        }
        if (CheckChangeSelectionFromHierarchy())
        {
            return;
        }

        TryRewindAnimation();


    }

    private static bool CheckSelection()
    {
        if (Selection.activeGameObject == null || Selection.activeGameObject.scene == null) return false;
        if (selectionRoot == null)
        {
            if (gamepad.startButton.isPressed)
            {
                OnSelectionChanged();
                return true;
            }
            else
            {
                return false;
            }
        }
        else if (selection.transform.root == selectionRoot.transform)
        {
            if (selection != Selection.activeGameObject)
            {
                OnSelectionChanged();
            }
            return true;
        }

        return false;
    }



    private static bool TryRewindAnimation()
    {
        if (!CheckAnimationWindowOpen(out AnimationWindow animationWindow))
        {
            return false;
        }

        bool forward = gamepad.rightShoulder.isPressed;
        bool backward = gamepad.leftShoulder.isPressed;
        if (!forward && !backward)
        {
            isPressingRewind = false;
            rewindingPressTimer = 0f;
            return false;
        }

        if (isPressingRewind)
        {
            bool changeDirection = (isPressingRewindForward && backward) || (!isPressingRewindForward && forward);
            if (changeDirection)
            {
                RewindFrame(animationWindow, forward);
                isPressingRewindForward = forward;
                rewindingPressTimer = 0f;
            }
            else
            {
                rewindingPressTimer += deltaTime;
                if (rewindingPressTimer >= rewindingFastThreshold)
                {
                    RewindFrame(animationWindow, forward);
                }
            }
        }
        else
        {
            RewindFrame(animationWindow, forward);
            isPressingRewind = true;
            isPressingRewindForward = forward;
            rewindingPressTimer = 0f;
        }

        return true;
    }


    private static void RewindFrame(AnimationWindow animationWindow, bool forward)
    {
        animationWindow.frame += forward ? 1 : -1;
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

    private static bool ZoomCamera()
    {
        SceneView scene = SceneView.lastActiveSceneView;
        cameraDistance = scene.cameraDistance;
        bool rightShoulder = gamepad.rightShoulder.isPressed;
        bool rightTrigger = gamepad.rightTrigger.isPressed;


        if (!rightShoulder && !rightTrigger)
        {
            return false;
        }

        cameraDistance += cameraZoomSpeed * deltaTime * (rightShoulder ? 1f : -1f);


        return true;
    }


    private static bool SnapRotationToPredefined()
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



    private static bool CheckChangeSelectionFromHierarchy()
    {
        if (!gamepad.leftStickButton.isPressed)
        {
            return false;
        }

        if (gamepad.buttonNorth.isPressed && CheckChangeSelectionCooldown())
        {
            Transform selectionParent = selection.transform.parent;
            if (selectionParent != null)
            {
                Selection.activeGameObject = selectionParent.gameObject;
            }
            return true;
        }
        else if (gamepad.buttonSouth.isPressed && selection.transform.childCount > 0 && CheckChangeSelectionCooldown())
        {
            Transform selectionFirstChild = selection.transform.GetChild(0);
            Selection.activeGameObject = selectionFirstChild.gameObject;
            return true;
        }
        else
        {
            bool west = gamepad.buttonWest.isPressed;
            bool east = gamepad.buttonEast.isPressed;
            if (!east && !west)
            {
                return false;
            }
            Transform parent = selection.transform.parent;
            if (parent != null && parent.childCount > 1 && CheckChangeSelectionCooldown())
            {
                int childCount = parent.childCount;
                int currentIndex = selection.transform.GetSiblingIndex();
                int newIndex = Mod(west ? currentIndex - 1 : currentIndex + 1, childCount);
                Selection.activeGameObject = parent.GetChild(newIndex).gameObject;
            }

            return true;
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

    private static void ApplyCameraRotation()
    {
        SceneView scene = SceneView.lastActiveSceneView;
        scene.LookAt(scene.pivot, cameraDistance, cameraRotation);
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

        cameraRotation = cameraStartRotation * Quaternion.AngleAxis(leftDegrees, cameraAxisUp) * Quaternion.AngleAxis(rightDegrees, cameraAxisRight);
        cameraRotation *= Quaternion.Euler(0f, 0f, -cameraRotation.eulerAngles.z);
    }

    private static bool CheckMoveRoot()
    {
        if (!gamepad.rightTrigger.isPressed || !gamepad.leftShoulder.isPressed)
        {
            return false;
        }
        GetStickInputs(out Vector2 leftStick, out Vector2 rightStick);
        GetCameraLocalXZAxes(out Vector3 xAxis, out Vector3 zAxis);
        //Debug.Log("CheckMoveRoot " + xAxis + " " + zAxis);
        selectionRoot.transform.position += (xAxis * leftStick.x + Vector3.up * rightStick.y + zAxis * leftStick.y) * rootMovementSpeed * deltaTime;
        return true;
    }

    private static void GetCameraLocalXZAxes(out Vector3 xAxis, out Vector3 zAxis)
    {
        Transform sceneCameraTransform = SceneView.lastActiveSceneView.camera.transform;
        xAxis = Vector3.ProjectOnPlane(sceneCameraTransform.right, Vector3.up).normalized;
        zAxis = Vector3.ProjectOnPlane(sceneCameraTransform.forward + sceneCameraTransform.up, Vector3.up).normalized;
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

    [DrawGizmo(GizmoType.Selected)]
    private static void DrawHierarchyGizmos(Transform transform, GizmoType gizmoType)
    {
        if (selectionRoot == null || transform.root != selectionRoot.transform)
        {
            return;
        }

        if (transform.parent != null)
        {
            Transform parent = transform.parent;

            Gizmos.color = Color.blue;
            DrawGizmoInPseudoScreenSpace(transform.parent.position);

            int siblingIndex = transform.GetSiblingIndex();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform currentSibling = parent.GetChild(i);
                Gizmos.color = i == siblingIndex ? Color.green : Color.yellow;
                DrawGizmoInPseudoScreenSpace(currentSibling.position);
            }
        }

        if (transform.childCount > 0)
        {
            Gizmos.color = Color.red;
            DrawGizmoInPseudoScreenSpace(transform.GetChild(0).position);
        }
    }

    private static void DrawGizmoInPseudoScreenSpace(Vector3 gizmoWorldPosition)
    {
        Camera sceneCamera = SceneView.lastActiveSceneView.camera;
        Vector3 distanceFromCamera = (gizmoWorldPosition - sceneCamera.transform.position).normalized;
        Vector3 gizmoPosition = sceneCamera.transform.position + distanceFromCamera * sceneCamera.nearClipPlane * 2f;
        float gizmoSize = 0.0003f;
        Gizmos.DrawSphere(gizmoPosition, gizmoSize);
    }



    static int Mod(int k, int n)
    {
        int r = k % n;
        return r < 0 ? r + n : r;
    }
}

#endif
