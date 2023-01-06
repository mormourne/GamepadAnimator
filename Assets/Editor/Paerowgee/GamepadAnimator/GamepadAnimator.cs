using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using GamepadButton = UnityEngine.InputSystem.LowLevel.GamepadButton;
using GamepadButtonPressedState = Paerowgee.GamepadAnimator.GamepadInputSnapshot.GamepadButtonPressedState;
using Utils = Paerowgee.GamepadAnimator.Utils;

namespace Paerowgee.GamepadAnimator
{
    [InitializeOnLoad]
    public class GamepadAnimator
    {
        private static bool enabled;
        public static bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled != value)
                {
                    EditorPrefs.SetBool(EDITOR_PREFS_ENABLED, value);
                }
                enabled = value;
            }
        }


        static Gamepad gamepad;
        static GamepadInputSnapshot lastFrameInputSnapshot;
        static Dictionary<GamepadButton, GamepadButtonPressedState> gamepadButtonStates;

        enum InputState
        {
            None,
            RootInitiated,
            SelectionOutsideOfInitiatedRoot,
            ModifySelection,
            ModifyCamera,
            ModifyRoot,
            ChangeSelectionWithinHierarchy,
            AnimationWindow,
            CameraTransition
        }

        private static InputState inputState;
        private static InputState inputStatePreCameraTransition;

        private static double lastFrameTime;
        private static float deltaTime;

        static GameObject selection;
        static GameObject selectionRoot;

        const float rootMovementSpeed = 1f;
        const float cameraZoomSpeed = 3f;
        const float leftStickSqrMagnitudeDeadzone = 0.1f;
        const float rightStickSqrMagnitudeDeadzone = 0.1f;
        const double changeSelectionCooldown = 0.2f;
        const float rewindingFastThreshold = 0.3f;

        private static double changeSelectionNextAvailability = 0f;

        private static Vector3 selectionAxisForward;
        private static Vector3 selectionAxisUp;
        private static Quaternion selectionStartRotation;

        private static Vector3 cameraAxisUp;
        private static Vector3 cameraAxisRight;
        private static Quaternion cameraStartRotation;
        private static float cameraSize;
        private static Quaternion cameraRotation;

        private static bool currentRewindForward = false;
        private static float rewindingPressTimer = 0f;

        private static Vector3 cameraTransitionStart;
        private static Transform cameraTransitionEnd;
        private static Vector3 cameraTransitionIntermidatePivot;
        private static float cameraTransitionDuration;
        private static float cameraTransitionTimer;

        private static RigTree rigTree;

        public const string EDITOR_PREFS_ENABLED = "EDITOR_PREFS_ENABLED";



        static GamepadAnimator()
        {
            EditorApplication.update += Update;
            lastFrameTime = EditorApplication.timeSinceStartup;
            deltaTime = 0f;
            enabled = EditorPrefs.GetBool(EDITOR_PREFS_ENABLED, true);
            inputState = InputState.None;
            gamepadButtonStates = null;
            lastFrameInputSnapshot = null;
        }



        static void Update()
        {
            if (!Enabled) return;
            if (EditorApplication.isPlaying) return;

            gamepad = Gamepad.current;
            if (gamepad == null) return;

            deltaTime = (float)(EditorApplication.timeSinceStartup - lastFrameTime);
            lastFrameTime = EditorApplication.timeSinceStartup;

            CalculateGamepadButtonStates();
            DetermineInputState();

            CheckForRigSnapshot();

            switch (inputState)
            {
                case InputState.None:
                    break;
                case InputState.RootInitiated:
                    break;
                case InputState.SelectionOutsideOfInitiatedRoot:
                    break;
                case InputState.ModifySelection:
                    SelectionRotate();
                    break;
                case InputState.ModifyCamera:
                    if (!SnapRotationToPredefined())
                    {
                        ZoomCamera();
                        RotateCamera();
                        ApplyCameraRotation();
                    }
                    break;
                case InputState.ModifyRoot:
                    MoveRoot();
                    break;
                case InputState.ChangeSelectionWithinHierarchy:
                    ChangeSelectionWithinHierarchy();
                    break;
                case InputState.AnimationWindow:
                    RewindAnimation();
                    break;
                case InputState.CameraTransition:
                    CameraTransition();
                    break;
                default:
                    break;
            }





        }

        static void DetermineInputState()
        {
            if (inputState == InputState.CameraTransition)
            {
                CameraTransition();
                return;
            }
            if (gamepadButtonStates == null)
            {
                return;
            }

            if (inputState != InputState.None)
            {
                if (selectionRoot == null)
                {
                    inputState = InputState.None;
                    return;
                }
                else if (Selection.activeGameObject == null || Selection.activeGameObject.scene == null || (Selection.activeGameObject.transform.root != selectionRoot.transform))
                {
                    inputState = InputState.SelectionOutsideOfInitiatedRoot;
                    return;
                }
                else if (selection != Selection.activeGameObject)
                {
                    OnSelectionChanged(true);
                    return;
                }
            }


            if (inputState == InputState.None)
            {
                if (Selection.activeGameObject != null && Selection.activeGameObject.scene != null && gamepadButtonStates[GamepadButton.Start] == GamepadButtonPressedState.PressedThisFrame)
                {
                    inputState = InputState.RootInitiated;
                    OnSelectionChanged();
                    return;
                }

            }
            else if (inputState == InputState.SelectionOutsideOfInitiatedRoot)
            {
                if (Selection.activeGameObject != null && Selection.activeGameObject.scene != null || (Selection.activeGameObject.transform.root == selectionRoot.transform))
                {
                    inputState = InputState.RootInitiated;
                    OnSelectionChanged();
                }
            }
            else if (inputState == InputState.RootInitiated)
            {
                if (gamepadButtonStates[GamepadButton.RightTrigger] == GamepadButtonPressedState.PressedThisFrame)
                {
                    inputState = InputState.ModifySelection;
                    InitSelectionRotate();
                }
                else if (gamepadButtonStates[GamepadButton.LeftTrigger] == GamepadButtonPressedState.PressedThisFrame)
                {
                    inputState = InputState.ModifyCamera;
                    CameraResetRotationAndAxes();
                }
                else if ((gamepadButtonStates[GamepadButton.LeftShoulder].Pressed()
                    || gamepadButtonStates[GamepadButton.RightShoulder].Pressed())
                    && EditorWindow.HasOpenInstances<AnimationWindow>())
                {
                    inputState = InputState.AnimationWindow;
                    InitRewind(gamepadButtonStates[GamepadButton.RightShoulder].Pressed());
                }
                else if (gamepadButtonStates[GamepadButton.LeftStick] == GamepadButtonPressedState.PressedThisFrame)
                {
                    inputState = InputState.ChangeSelectionWithinHierarchy;
                }
            }
            else if (inputState == InputState.ModifySelection)
            {
                if (gamepadButtonStates[GamepadButton.RightTrigger].Unpressed())
                {
                    inputState = InputState.RootInitiated;
                }
                else if (gamepadButtonStates[GamepadButton.LeftShoulder] == GamepadButtonPressedState.PressedThisFrame)
                {
                    inputState = InputState.ModifyRoot;
                }
            }
            else if (inputState == InputState.ModifyRoot) //TODO add camera transition
            {
                bool cameraTransition = false;
                if (gamepadButtonStates[GamepadButton.RightTrigger].Unpressed())
                {
                    inputState = InputState.RootInitiated;
                    cameraTransition = true;

                }
                else if (gamepadButtonStates[GamepadButton.LeftShoulder].Unpressed())
                {
                    inputState = InputState.ModifySelection;
                    cameraTransition = true;
                }

                if (cameraTransition)
                {
                    InitCameraTransition(SceneView.lastActiveSceneView.pivot, selection.transform);
                }
            }
            else if (inputState == InputState.ModifyCamera)
            {
                if (gamepadButtonStates[GamepadButton.LeftTrigger].Unpressed())
                {
                    inputState = InputState.RootInitiated;
                }
            }
            else if (inputState == InputState.AnimationWindow)
            {
                if (gamepadButtonStates[GamepadButton.LeftShoulder].Unpressed()
                    && gamepadButtonStates[GamepadButton.RightShoulder].Unpressed())
                {
                    inputState = InputState.RootInitiated;
                }
                else
                {
                    CheckRewindChangeDirection(gamepadButtonStates[GamepadButton.RightShoulder].Pressed());
                }
            }
            else if (inputState == InputState.ChangeSelectionWithinHierarchy)
            {
                if (gamepadButtonStates[GamepadButton.LeftStick].Unpressed())
                {
                    inputState = InputState.RootInitiated;
                }
            }
        }


        static void CalculateGamepadButtonStates()
        {
            GamepadInputSnapshot currentInputSnapshot = new GamepadInputSnapshot(gamepad);
            if (lastFrameInputSnapshot != null)
            {
                gamepadButtonStates = currentInputSnapshot.CompareInput(lastFrameInputSnapshot);
            }
            lastFrameInputSnapshot = currentInputSnapshot;
        }

        private static void CheckForRigSnapshot()
        {
            if (inputState == InputState.None || inputState == InputState.SelectionOutsideOfInitiatedRoot) return;
            if (gamepadButtonStates[GamepadButton.Select] == GamepadButtonPressedState.PressedThisFrame)
            {
                rigTree = new RigTree(selectionRoot.transform);
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        private static void InitSelectionRotate()
        {
            selectionAxisForward = selection.transform.InverseTransformDirection(SceneView.lastActiveSceneView.camera.transform.forward);
            selectionAxisUp = selection.transform.InverseTransformDirection(SceneView.lastActiveSceneView.camera.transform.up);
            selectionStartRotation = selection.transform.rotation;
        }

        private static void SelectionRotate()
        {
            if (inputState != InputState.ModifySelection) return;

            Undo.RecordObject(selection.transform, "Rotate " + selection.name);
            GetStickInputs(out Vector2 leftStick, out Vector2 rightStick);

            float leftDegrees = leftStick == Vector2.zero ? 0f : Mathf.Atan2(leftStick.y, leftStick.x) * Mathf.Rad2Deg - 90f;
            float rightDegrees = rightStick == Vector2.zero ? 0f : -Mathf.Atan2(rightStick.y, rightStick.x) * Mathf.Rad2Deg + 90f;

            selection.transform.rotation = selectionStartRotation
                * Quaternion.AngleAxis(leftDegrees, selectionAxisForward)
                * Quaternion.AngleAxis(rightDegrees, selectionAxisUp);
        }

        private static void MoveRoot()
        {
            Undo.RecordObject(selectionRoot.transform, "Move " + selectionRoot.name);

            GetStickInputs(out Vector2 leftStick, out Vector2 rightStick);
            GetCameraLocalXZAxes(out Vector3 xAxis, out Vector3 zAxis);

            selectionRoot.transform.position += (xAxis * leftStick.x + Vector3.up * rightStick.y + zAxis * leftStick.y) * rootMovementSpeed * deltaTime;

        }

        private static bool SnapRotationToPredefined()
        {
            bool snapRotation = false;
            Quaternion rotation = Quaternion.identity;
            if (gamepadButtonStates[GamepadButton.DpadUp].Pressed())
            {
                snapRotation = true;
                rotation = Quaternion.Euler(90f, 180f, 0f);
            }
            else if (gamepadButtonStates[GamepadButton.DpadDown].Pressed())
            {
                snapRotation = true;
                rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            else if (gamepadButtonStates[GamepadButton.DpadLeft].Pressed())
            {
                snapRotation = true;
                rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            else if (gamepadButtonStates[GamepadButton.DpadRight].Pressed())
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

        private static void ZoomCamera()
        {

            SceneView scene = SceneView.lastActiveSceneView;
            cameraSize = scene.size;
            bool rightShoulder = gamepadButtonStates[GamepadButton.RightShoulder].Pressed();
            bool rightTrigger = gamepadButtonStates[GamepadButton.RightTrigger].Pressed();


            if (!rightShoulder && !rightTrigger)
            {
                return;
            }

            cameraSize += cameraZoomSpeed * deltaTime * (rightShoulder ? 1f : -1f);

        }


        private static void RotateCamera()
        {
            GetStickInputs(out Vector2 leftStick, out Vector2 rightStick);

            float leftDegrees = leftStick == Vector2.zero ? 0f : -Mathf.Atan2(leftStick.y, leftStick.x) * Mathf.Rad2Deg - 90f;
            float rightDegrees = Mathf.Atan2(rightStick.y, rightStick.x) * Mathf.Rad2Deg;

            cameraRotation = cameraStartRotation * Quaternion.AngleAxis(leftDegrees, cameraAxisUp) * Quaternion.AngleAxis(rightDegrees, cameraAxisRight);
            cameraRotation *= Quaternion.Euler(0f, 0f, -cameraRotation.eulerAngles.z);
        }


        private static void ApplyCameraRotation()
        {
            SceneView scene = SceneView.lastActiveSceneView;
            scene.LookAtDirect(scene.pivot, cameraRotation, cameraSize);
        }

        private static void InitRewind(bool forward)
        {
            if (!CheckAnimationWindowOpen(out AnimationWindow animationWindow))
            {
                return;
            }

            RewindFrame(animationWindow, forward);
            currentRewindForward = forward;
            rewindingPressTimer = 0f;
        }

        private static void CheckRewindChangeDirection(bool forward)
        {
            if (!CheckAnimationWindowOpen(out AnimationWindow animationWindow))
            {
                return;
            }
            bool changeDirection = (currentRewindForward && !forward) || (!currentRewindForward && forward);
            if (changeDirection)
            {
                InitRewind(forward);
            }
        }

        private static void RewindAnimation()
        {
            if (!CheckAnimationWindowOpen(out AnimationWindow animationWindow))
            {
                return;
            }


            rewindingPressTimer += deltaTime;
            if (rewindingPressTimer >= rewindingFastThreshold)
            {
                RewindFrame(animationWindow, currentRewindForward);
            }



        }

        private static bool ChangeSelectionWithinHierarchy()
        {
            if (gamepadButtonStates[GamepadButton.North].Pressed())
            {
                Transform selectionParent = selection.transform.parent;
                if (selectionParent != null && CheckChangeSelectionCooldown())
                {
                    Selection.activeGameObject = selectionParent.gameObject;
                    return true;
                }
                return false;
            }
            else if (gamepadButtonStates[GamepadButton.South].Pressed() && selection.transform.childCount > 0 && CheckChangeSelectionCooldown())
            {
                Transform selectionFirstChild = selection.transform.GetChild(0);
                Selection.activeGameObject = selectionFirstChild.gameObject;
                return true;
            }
            else
            {
                bool west = gamepadButtonStates[GamepadButton.West].Pressed();
                bool east = gamepadButtonStates[GamepadButton.East].Pressed();
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



        private static void InitCameraTransition(Vector3 start, Transform end)
        {
            cameraTransitionStart = inputState == InputState.CameraTransition ? cameraTransitionIntermidatePivot : start;
            cameraTransitionEnd = end;
            cameraTransitionDuration = 0.5f;
            cameraTransitionTimer = 0f;
            inputStatePreCameraTransition = inputState;
            inputState = InputState.CameraTransition;
        }

        private static void CameraTransition()
        {
            cameraTransitionTimer += deltaTime;
            if (cameraTransitionTimer >= cameraTransitionDuration)
            {
                cameraTransitionTimer = cameraTransitionDuration;
                inputState = inputStatePreCameraTransition;
            }
            float lerp = cameraTransitionTimer / cameraTransitionDuration;
            float easedQuadLerp = lerp < 0.5f ? 2f * lerp * lerp : 1f - Mathf.Pow(-2f * lerp + 2f, 2f) / 2f;
            cameraTransitionIntermidatePivot = Vector3.Lerp(cameraTransitionStart, cameraTransitionEnd.position, easedQuadLerp);
            SceneView scene = SceneView.lastActiveSceneView;
            scene.LookAtBasedOnDistance(cameraTransitionIntermidatePivot, scene.cameraDistance, scene.rotation);
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







        private static void CameraResetRotationAndAxes()
        {
            cameraAxisUp = SceneView.lastActiveSceneView.camera.transform.InverseTransformDirection(Vector3.up);
            cameraAxisRight = Vector3.right;
            cameraStartRotation = SceneView.lastActiveSceneView.camera.transform.rotation;
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

        private static void OnSelectionChanged(bool withTransition = false)
        {
            SceneView scene = SceneView.lastActiveSceneView;
            GameObject oldSelection = selection;
            selection = Selection.activeGameObject;
            selectionRoot = selection.transform.root.gameObject;
            if (oldSelection == null || !withTransition)
            {
                scene.LookAtBasedOnDistance(selection.transform.position, scene.cameraDistance, scene.rotation);
            }
            else
            {
                InitCameraTransition(oldSelection.transform.position, selection.transform);
            }


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
            if (!Enabled) return;

            if (inputState == InputState.None || inputState == InputState.SelectionOutsideOfInitiatedRoot)
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

            DrawRigTree();
        }

        private static void DrawRigTree()
        {
            if (rigTree == null) return;
            Gizmos.color = Color.cyan;
            DrawRigNode(rigTree);
        }

        private static void DrawRigNode(RigTree _node)
        {
            Gizmos.DrawSphere(_node.position, 0.025f);
            foreach (var item in _node.children)
            {
                DrawRigNode(item);
                Gizmos.DrawLine(_node.position, item.position);
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

}
