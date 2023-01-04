using UnityEditor;
using UnityEngine;

namespace Paerowgee.GamepadAnimator
{
    public class GamepadAnimatorWindow : EditorWindow
    {
        [MenuItem("Window/Paerowgee/Gamepad Animator")]
        public static void ShowWindow()
        {
            GetWindow<GamepadAnimatorWindow>(false, "Gamepad Animator", true);
        }

        void OnGUI()
        {
            GamepadAnimator.Enabled = EditorGUILayout.Toggle("Enabled", GamepadAnimator.Enabled);
        }
    }
}