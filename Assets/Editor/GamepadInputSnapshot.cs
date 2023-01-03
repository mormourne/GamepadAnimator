using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using GamepadButton = UnityEngine.InputSystem.LowLevel.GamepadButton;
public class GamepadInputSnapshot
{
    public enum GamepadButtonPressedState
    {
        Unpressed,
        PressedThisFrame,
        IsPressed,
        ReleasedThisFrame
    }

    public Dictionary<GamepadButton, bool> buttonPressedStates;
    public GamepadInputSnapshot(Gamepad gamepad)
    {
        buttonPressedStates = new Dictionary<GamepadButton, bool>();

        foreach (GamepadButton button in System.Enum.GetValues(typeof(GamepadButton)))
        {
            if (!buttonPressedStates.ContainsKey(button))
            {
                buttonPressedStates.Add(button, gamepad[button].isPressed);
            }
            
        }
        
    }

    public Dictionary<GamepadButton, GamepadButtonPressedState> CompareInput(GamepadInputSnapshot other)
    {
        Dictionary<GamepadButton, GamepadButtonPressedState> gamepadButtonsStateChange = new Dictionary<GamepadButton, GamepadButtonPressedState>();
        foreach (var key in buttonPressedStates.Keys)
        {
            bool current = buttonPressedStates[key];
            bool previous = other.buttonPressedStates[key];

            GamepadButtonPressedState state;
            if (current && previous) state = GamepadButtonPressedState.IsPressed;
            else if (current && !previous) state = GamepadButtonPressedState.PressedThisFrame;
            else if (!current && previous) state = GamepadButtonPressedState.ReleasedThisFrame;
            else state = GamepadButtonPressedState.Unpressed;

            gamepadButtonsStateChange.Add(key, state);
        }
        return gamepadButtonsStateChange;
    }


}
