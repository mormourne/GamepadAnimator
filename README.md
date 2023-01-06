# GamepadAnimator
WIP. Current version 0.2.

This tool allows you to create animations using gamepad in Unity Editor. 

Currently works only in humanoid-friendly way, i.e. root can be moved and rotated and all of its children can only be rotated.

Both camera and selection rotation are working in *absolute* mode: for example in case of Y-axis camera rotation, 
you can draw an imaginary circle on the ground around the selection, and the new camera position will correspond to the position of the left stick
on it's foundation.

# Installation
- Copy **Assets/Editor/Paerowgee** folder to the **Assets/Editor** folder of your project.
- Open **Window/Paerowgee/Gamepad Animator** in Unity Editor and make sure **Enable** toggle is on.
- Add you character to the scene, select it and press **Start** on your gamepad.

# Controls
- **Start**: Select the root object to modify. Whatever GameObject is selected, it's **transform.root** will be selected as the root object, 
so make sure the character is not a child of any other GameObject.
- **L2 + ...**: Camera modification:
  - **...Left stick**: Rotate camera around **Y** axis;
  - **...Right stick**: Rotate camera around **X** axis;
  - **...R2/R1**: Zoom in/out;
  - **...Dpad keys**: Snap to pre-defiened rotations.
- **R2 + ...**: Selection modification:
  - **...Left stick**: Rotate selection around camera's **Z** axis;
  - **...Right stick**: Rotate selection around camera's **Y** axis;
  - **...L1 + Left Stick + Right Stick**: Move root;
- **Left stick button + ...**: Jump in GameObject's hierarchy. Parent is highlighted with blue, first child - red, siblings - yellow, current selection - green:
  - **...Button North (Y)**: Select parent;
  - **...Button South (A)**: Select first child;
  - **...Buttons West and East (X and B)**: Iterate forward/backward through siblings;
- **L1/R1**: In case Unity's Animation Window is open, rewind current animation backward/forward.
- **Select**: Create "rig snapshot" (schematic representation of root's transform hierarchy).
